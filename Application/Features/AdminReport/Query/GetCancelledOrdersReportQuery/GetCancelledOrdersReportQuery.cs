using Application.Features.AdminReport.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AdminReport.Query.GetCancelledOrdersReportQuery
{
    public record GetCancelledOrdersReportQuery : IRequest<Result<CancelledOrdersReportDto>>
    {
        public List<int>? CityIds { get; set; }
        public List<int>? CustomerIds { get; set; }
        public List<int>? PaymentMethodIds { get; set; }
        public List<int>? CancellationFeeStates { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderCode { get; set; }
        public bool? CancellationFeePaid { get; set; }
        public bool? MoneyRefunded { get; set; }
    }

    public class GetCancelledOrdersReportQueryHandler
        : IRequestHandler<GetCancelledOrdersReportQuery, Result<CancelledOrdersReportDto>>
    {
        private readonly DatabaseContext _context;

        public GetCancelledOrdersReportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<CancelledOrdersReportDto>> Handle(
            GetCancelledOrdersReportQuery request,
            CancellationToken cancellationToken)
        {
            var feeWallets = await _context.CustomerWallets
                .AsNoTracking()
                .Where(cw => cw.Type == WalletType.OrderCancellationFees && cw.OrderId.HasValue)
                .ToListAsync(cancellationToken);

            var refunds = await _context.RefundablePaypalAmounts
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var moneyRefundedOrderIds = await _context.Orders
                .AsNoTracking()
                .Where(o => o.MoneyRefunded)
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);

            var cancelledStateOrderIds = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderState == OrderState.Cancelled)
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);

            var candidateOrderIds = feeWallets.Select(w => w.OrderId!.Value)
                .Union(refunds.Select(r => r.OrderId))
                .Union(moneyRefundedOrderIds)
                .Union(cancelledStateOrderIds)
                .Distinct()
                .ToList();

            if (candidateOrderIds.Count == 0)
            {
                return Result.Success(new CancelledOrdersReportDto());
            }

            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.City)
                .Where(o => candidateOrderIds.Contains(o.OrderId));

            if (request.CityIds is { Count: > 0 })
                ordersQuery = ordersQuery.Where(o => request.CityIds.Contains(o.CityId));

            if (request.CustomerIds is { Count: > 0 })
                ordersQuery = ordersQuery.Where(o => request.CustomerIds.Contains(o.CustomerId));

            if (request.PaymentMethodIds is { Count: > 0 })
                ordersQuery = ordersQuery.Where(o => request.PaymentMethodIds.Contains(o.PaymentMethodId));

            if (request.FromDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1);
                ordersQuery = ordersQuery.Where(o => o.CreatedDate < to);
            }

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var code = request.OrderCode.Trim();
                ordersQuery = ordersQuery.Where(o => o.OrderCode.Contains(code));
            }

            if (request.MoneyRefunded.HasValue)
                ordersQuery = ordersQuery.Where(o => o.MoneyRefunded == request.MoneyRefunded.Value);

            var orders = await ordersQuery
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync(cancellationToken);

            var feesByOrder = feeWallets
                .GroupBy(w => w.OrderId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var refundByOrder = refunds
                .GroupBy(r => r.OrderId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First());

            var items = new List<CancelledOrdersReportRowDto>();

            foreach (var order in orders)
            {
                var marker = $"Order #{order.OrderCode}";
                feesByOrder.TryGetValue(order.OrderId, out var wallets);
                var cancelFeeWallet = wallets?
                    .FirstOrDefault(w => w.Description.Contains(marker));

                // Prior-debt UnderPayment linked to this order is NOT this order's cancel fee
                var isCancelled = order.OrderState == OrderState.Cancelled
                    || order.MoneyRefunded
                    || refundByOrder.ContainsKey(order.OrderId)
                    || cancelFeeWallet != null;

                if (!isCancelled)
                    continue;

                var feeAmount = cancelFeeWallet?.Withdraw ?? 0;
                var feeState = cancelFeeWallet?.State;
                var feePaid = feeState == CustomerWalletState.Paid;

                if (request.CancellationFeePaid.HasValue && feePaid != request.CancellationFeePaid.Value)
                    continue;

                if (request.CancellationFeeStates is { Count: > 0 })
                {
                    if (!feeState.HasValue || !request.CancellationFeeStates.Contains((int)feeState.Value))
                        continue;
                }

                refundByOrder.TryGetValue(order.OrderId, out var refund);

                items.Add(new CancelledOrdersReportRowDto
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.FullName,
                    CustomerMobile = order.Customer.MobileNumber,
                    CityId = order.CityId,
                    CityName = order.City.Name,
                    OrderTotal = order.OrderTotal,
                    PreviousDebt = order.PreviousDebt,
                    PaymentMethod = (PaymentMethod)order.PaymentMethodId,
                    OrderState = order.OrderState,
                    CancellationFees = feeAmount,
                    CancellationFeeState = feeState,
                    CancellationFeePaid = feePaid,
                    RefundablePaypalAmount = refund?.RefundableAmount ?? 0,
                    PaypalRefundState = refund?.State,
                    MoneyRefunded = order.MoneyRefunded,
                    CreatedDate = order.CreatedDate,
                    CancelledDate = cancelFeeWallet?.CreatedDate
                        ?? refund?.CreatedDate
                        ?? (order.MoneyRefunded ? order.LastModifiedDate : null)
                });
            }

            var report = new CancelledOrdersReportDto
            {
                Items = items,
                Totals = new CancelledOrdersReportTotalsDto
                {
                    OrdersCount = items.Count,
                    TotalOrderAmount = items.Sum(i => i.OrderTotal),
                    TotalCancellationFees = items.Sum(i => i.CancellationFees),
                    TotalPaidCancellationFees = items.Where(i => i.CancellationFeePaid).Sum(i => i.CancellationFees),
                    TotalUnpaidCancellationFees = items.Where(i => !i.CancellationFeePaid).Sum(i => i.CancellationFees),
                    TotalRefundablePaypal = items.Sum(i => i.RefundablePaypalAmount)
                }
            };

            return Result.Success(report);
        }
    }
}
