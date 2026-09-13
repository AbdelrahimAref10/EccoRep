using Application.Features.Order.Command.OrderVehicleLifecycleCommands;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.MarkMerchantHandoverToDeliveryCommand
{
    public record MarkMerchantHandoverToDeliveryCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public List<int> VehicleIds { get; set; } = new();
        /// <summary>Optional shared image for all vehicles in this handover batch.</summary>
        public string? ImageUrl { get; set; }
    }

    public class MarkMerchantHandoverToDeliveryCommandHandler
        : IRequestHandler<MarkMerchantHandoverToDeliveryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly Infrastructure.Services.IImageService _imageService;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkMerchantHandoverToDeliveryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            Infrastructure.Services.IImageService imageService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(
            MarkMerchantHandoverToDeliveryCommand request,
            CancellationToken cancellationToken)
        {
            if (request.VehicleIds == null || request.VehicleIds.Count == 0)
                return Result.Failure<bool>("At least one vehicle is required");

            var vehicleIds = request.VehicleIds.Distinct().ToList();
            var isAdmin = _userSession.Roles.Contains(AppRoleNames.SuperAdmin);

            Domain.Models.Merchant? sessionMerchant = null;
            if (!isAdmin)
            {
                sessionMerchant = await _context.Merchants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId, cancellationToken);

                if (sessionMerchant == null)
                    return Result.Failure<bool>("Merchant profile not found for current user");
            }

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            var paymentDetails = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .Where(p => p.OrderId == request.OrderId && vehicleIds.Contains(p.VehicleId))
                .ToListAsync(cancellationToken);

            if (paymentDetails.Count != vehicleIds.Count)
                return Result.Failure<bool>("Merchant payment details missing for one or more vehicles");

            if (!isAdmin && paymentDetails.Any(p => p.MerchantId != sessionMerchant!.MerchantId))
                return Result.Failure<bool>("You can only hand over vehicles belonging to your merchant account");

            var imagePath = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : (_imageService.IsBase64String(request.ImageUrl)
                    ? _imageService.SaveBase64Image(request.ImageUrl, "order-vehicles")
                    : request.ImageUrl);

            var actor = _userSession.UserName ?? "System";

            foreach (var vehicleId in vehicleIds)
            {
                var snapshotResult = await VehicleSettlementSnapshotLoader.LoadAsync(
                    _context, request.OrderId, vehicleId, cancellationToken);
                if (snapshotResult.IsFailure)
                    return Result.Failure<bool>(snapshotResult.Error);

                try
                {
                    order.MarkVehicleReceivedFromOwner(snapshotResult.Value, imagePath, actor);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    return Result.Failure<bool>(ex.Message);
                }

                var assignment = await _context.DeliveryMenOrders
                    .AsTracking()
                    .FirstOrDefaultAsync(d => d.OrderId == request.OrderId && d.VehicleId == vehicleId, cancellationToken);
                assignment?.MarkReceivedFromMerchant(actor);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                order.OrderId,
                "Merchant handover",
                $"Vehicles were handed over to delivery on order #{order.OrderCode}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
