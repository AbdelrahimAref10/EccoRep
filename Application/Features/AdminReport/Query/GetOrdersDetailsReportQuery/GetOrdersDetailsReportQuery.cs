using Application.Features.AdminReport.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AdminReport.Query.GetOrdersDetailsReportQuery
{
    public record GetOrdersDetailsReportQuery : IRequest<Result<OrdersDetailsReportDto>>
    {
        public List<int>? CityIds { get; set; }
        public List<int>? OrderStates { get; set; }
        public List<int>? CustomerIds { get; set; }
        public List<int>? PaymentMethodIds { get; set; }
        public List<int>? PaymentStates { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? ReservationFrom { get; set; }
        public DateTime? ReservationTo { get; set; }
        public string? OrderCode { get; set; }
        public bool? IsCancelled { get; set; }
    }

    public class GetOrdersDetailsReportQueryHandler
        : IRequestHandler<GetOrdersDetailsReportQuery, Result<OrdersDetailsReportDto>>
    {
        private readonly DatabaseContext _context;

        public GetOrdersDetailsReportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<OrdersDetailsReportDto>> Handle(
            GetOrdersDetailsReportQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.City)
                .Include(o => o.SubCategory)
                .Include(o => o.OrderPayments)
                .AsQueryable();

            if (request.CityIds is { Count: > 0 })
                query = query.Where(o => request.CityIds.Contains(o.CityId));

            if (request.OrderStates is { Count: > 0 })
                query = query.Where(o => request.OrderStates.Contains((int)o.OrderState));

            if (request.CustomerIds is { Count: > 0 })
                query = query.Where(o => request.CustomerIds.Contains(o.CustomerId));

            if (request.PaymentMethodIds is { Count: > 0 })
                query = query.Where(o => request.PaymentMethodIds.Contains(o.PaymentMethodId));

            if (request.FromDate.HasValue)
                query = query.Where(o => o.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedDate < to);
            }

            if (request.ReservationFrom.HasValue)
                query = query.Where(o => o.ReservationDateFrom >= request.ReservationFrom.Value.Date);

            if (request.ReservationTo.HasValue)
                query = query.Where(o => o.ReservationDateTo <= request.ReservationTo.Value.Date);

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var code = request.OrderCode.Trim();
                query = query.Where(o => o.OrderCode.Contains(code));
            }

            if (request.PaymentStates is { Count: > 0 })
            {
                query = query.Where(o => o.OrderPayments.Any(p => request.PaymentStates.Contains((int)p.State)));
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync(cancellationToken);

            var orderIds = orders.Select(o => o.OrderId).ToList();

            var refundOrderIds = await _context.RefundablePaypalAmounts
                .AsNoTracking()
                .Where(r => orderIds.Contains(r.OrderId))
                .Select(r => r.OrderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var feeWallets = await _context.CustomerWallets
                .AsNoTracking()
                .Where(cw =>
                    cw.OrderId.HasValue
                    && orderIds.Contains(cw.OrderId.Value)
                    && cw.Type == WalletType.OrderCancellationFees)
                .Select(cw => new { cw.OrderId, cw.Description })
                .ToListAsync(cancellationToken);

            var cancelledByFee = feeWallets
                .Where(w => w.OrderId.HasValue)
                .GroupBy(w => w.OrderId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Description).ToList());

            var refundSet = refundOrderIds.ToHashSet();

            var items = orders.Select(o =>
            {
                var payment = o.OrderPayments.FirstOrDefault();
                var marker = $"Order #{o.OrderCode}";
                var cancelledByWallet = cancelledByFee.TryGetValue(o.OrderId, out var descs)
                    && descs.Any(d => d.Contains(marker));
                var isCancelled = o.OrderState == OrderState.Cancelled
                    || o.MoneyRefunded
                    || refundSet.Contains(o.OrderId)
                    || cancelledByWallet;

                return new OrdersDetailsReportRowDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FullName,
                    CustomerMobile = o.Customer.MobileNumber,
                    CityId = o.CityId,
                    CityName = o.City.Name,
                    SubCategoryName = o.SubCategory.Name,
                    ReservationDateFrom = o.ReservationDateFrom,
                    ReservationDateTo = o.ReservationDateTo,
                    VehiclesCount = o.VehiclesCount,
                    OrderSubTotal = o.OrderSubTotal,
                    PreviousDebt = o.PreviousDebt,
                    OrderTotal = o.OrderTotal,
                    PaymentMethod = (PaymentMethod)o.PaymentMethodId,
                    PaymentState = payment?.State,
                    OrderState = o.OrderState,
                    MoneyRefunded = o.MoneyRefunded,
                    IsCancelled = isCancelled,
                    CreatedDate = o.CreatedDate
                };
            }).ToList();

            if (request.IsCancelled.HasValue)
            {
                items = items.Where(i => i.IsCancelled == request.IsCancelled.Value).ToList();
            }

            var report = new OrdersDetailsReportDto
            {
                Items = items,
                Totals = new OrdersDetailsReportTotalsDto
                {
                    OrdersCount = items.Count,
                    TotalSubTotal = items.Sum(i => i.OrderSubTotal),
                    TotalPreviousDebt = items.Sum(i => i.PreviousDebt),
                    TotalOrderAmount = items.Sum(i => i.OrderTotal),
                    TotalPaidAmount = items
                        .Where(i => i.PaymentState == PaymentState.Paid)
                        .Sum(i => i.OrderTotal)
                }
            };

            return Result.Success(report);
        }
    }
}
