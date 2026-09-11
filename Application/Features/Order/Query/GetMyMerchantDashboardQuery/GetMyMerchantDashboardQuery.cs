using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetMyMerchantDashboardQuery
{
    public record GetMyMerchantDashboardQuery : IRequest<Result<MerchantDashboardSummaryDto>>;

    public class GetMyMerchantDashboardQueryHandler
        : IRequestHandler<GetMyMerchantDashboardQuery, Result<MerchantDashboardSummaryDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;

        public GetMyMerchantDashboardQueryHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
        }

        public async Task<Result<MerchantDashboardSummaryDto>> Handle(
            GetMyMerchantDashboardQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<MerchantDashboardSummaryDto>("Merchant profile not found for current user");

            var pendingInvitations = await _context.MerchantOrders
                .AsNoTracking()
                .CountAsync(
                    mo => mo.MerchantId == merchant.MerchantId
                        && mo.ResponseStatus == MerchantOrderResponseStatus.Pending
                        && mo.Order.OrderState == OrderState.MerchantPending,
                    cancellationToken);

            var awaitingHandover = await _context.DeliveryMenOrders
                .AsNoTracking()
                .Where(d => !d.DeliveryReceivedFromMerchant)
                .Join(
                    _context.MerchantOrderPaymentDetails.AsNoTracking()
                        .Where(p => p.MerchantId == merchant.MerchantId),
                    d => new { d.OrderId, d.VehicleId },
                    p => new { p.OrderId, p.VehicleId },
                    (d, _) => d)
                .Join(
                    _context.Orders.AsNoTracking()
                        .Where(o => o.OrderState == OrderState.DeliveryAssigned || o.OrderState == OrderState.OnWay),
                    d => d.OrderId,
                    o => o.OrderId,
                    (d, _) => d)
                .CountAsync(cancellationToken);

            var terminalStates = new[] { OrderState.Completed, OrderState.Cancelled };
            var activeOrders = await _context.MerchantOrders
                .AsNoTracking()
                .CountAsync(
                    mo => mo.MerchantId == merchant.MerchantId
                        && !terminalStates.Contains(mo.Order.OrderState),
                    cancellationToken);

            var myVehiclesCount = await _context.Vehicles
                .AsNoTracking()
                .CountAsync(v => v.MerchantId == merchant.MerchantId, cancellationToken);

            var balance = await _journal.GetGlobalPartyBalanceAsync(
                LedgerPartyType.Merchant, merchant.MerchantId, cancellationToken);

            return Result.Success(new MerchantDashboardSummaryDto
            {
                MerchantId = merchant.MerchantId,
                FullName = merchant.FullName,
                CashOnReceive = merchant.CashOnReceive,
                PendingInvitationsCount = pendingInvitations,
                AwaitingHandoverCount = awaitingHandover,
                ActiveOrdersCount = activeOrders,
                MyVehiclesCount = myVehiclesCount,
                Balance = balance,
                AmountOwedToCompany = balance < 0 ? -balance : 0,
                AmountOwedByCompany = balance > 0 ? balance : 0
            });
        }
    }
}
