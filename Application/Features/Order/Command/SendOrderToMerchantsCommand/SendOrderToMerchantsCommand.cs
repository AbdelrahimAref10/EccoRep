using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.SendOrderToMerchantsCommand
{
    public record SendOrderToMerchantsCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public List<int> MerchantIds { get; set; } = new();
    }

    public class SendOrderToMerchantsCommandHandler : IRequestHandler<SendOrderToMerchantsCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IMerchantNotificationHubService _merchantNotifications;

        public SendOrderToMerchantsCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IMerchantNotificationHubService merchantNotifications)
        {
            _context = context;
            _userSession = userSession;
            _merchantNotifications = merchantNotifications;
        }

        public async Task<Result<bool>> Handle(SendOrderToMerchantsCommand request, CancellationToken cancellationToken)
        {
            if (request.MerchantIds == null || request.MerchantIds.Count == 0)
                return Result.Failure<bool>("At least one merchant must be selected");

            var merchantIds = request.MerchantIds.Distinct().ToList();

            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.Pending && order.OrderState != OrderState.MerchantPending)
                return Result.Failure<bool>($"Cannot send to merchants in {order.OrderState} state");

            var activeMerchants = await _context.Merchants
                .AsNoTracking()
                .Where(m => merchantIds.Contains(m.MerchantId) && m.IsActive && !m.IsDeleted)
                .Select(m => m.MerchantId)
                .ToListAsync(cancellationToken);

            if (activeMerchants.Count != merchantIds.Count)
                return Result.Failure<bool>("One or more merchants were not found or are inactive");

            var existingMerchantIds = await _context.MerchantOrders
                .AsNoTracking()
                .Where(mo => mo.OrderId == request.OrderId && merchantIds.Contains(mo.MerchantId))
                .Select(mo => mo.MerchantId)
                .ToListAsync(cancellationToken);

            var createdBy = _userSession.UserName ?? "System";
            var newMerchantIds = merchantIds.Except(existingMerchantIds).ToList();

            foreach (var merchantId in newMerchantIds)
            {
                await _context.MerchantOrders.AddAsync(
                    MerchantOrder.Create(request.OrderId, merchantId, createdBy),
                    cancellationToken);
            }

            order.MarkMerchantPending(createdBy);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify newly invited merchants immediately (sound + list refresh on merchant portal).
            var notifyIds = newMerchantIds.Count > 0 ? newMerchantIds : merchantIds;
            await _merchantNotifications.SendToMerchantsAsync(
                notifyIds,
                "New order invitation",
                $"Order #{order.OrderCode} is waiting for your confirmation.",
                NotificationType.OrderMerchantPending,
                order.OrderId,
                order.OrderCode);

            return Result.Success(true);
        }
    }
}
