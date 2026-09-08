using Application.Features.AdminReport.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AdminReport.Query.GetPayPalRefundsReportQuery
{
    public record GetPayPalRefundsReportQuery : IRequest<Result<PayPalRefundsReportDto>>
    {
        public List<int>? CustomerIds { get; set; }
        public List<int>? RefundStates { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderCode { get; set; }
        public bool? MoneyRefunded { get; set; }
    }

    public class GetPayPalRefundsReportQueryHandler
        : IRequestHandler<GetPayPalRefundsReportQuery, Result<PayPalRefundsReportDto>>
    {
        private readonly DatabaseContext _context;

        public GetPayPalRefundsReportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<PayPalRefundsReportDto>> Handle(
            GetPayPalRefundsReportQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.RefundablePaypalAmounts
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Order)
                .AsQueryable();

            if (request.CustomerIds is { Count: > 0 })
                query = query.Where(r => request.CustomerIds.Contains(r.CustomerId));

            if (request.RefundStates is { Count: > 0 })
                query = query.Where(r => request.RefundStates.Contains((int)r.State));

            if (request.FromDate.HasValue)
                query = query.Where(r => r.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(r => r.CreatedDate < to);
            }

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var code = request.OrderCode.Trim();
                query = query.Where(r => r.Order.OrderCode.Contains(code));
            }

            if (request.MoneyRefunded.HasValue)
                query = query.Where(r => r.Order.MoneyRefunded == request.MoneyRefunded.Value);

            var refunds = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);

            var items = refunds.Select(r => new PayPalRefundsReportRowDto
            {
                RefundId = r.Id,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer.FullName,
                CustomerMobile = r.Customer.MobileNumber,
                OrderTotal = r.OrderTotal,
                CancellationFees = r.CancellationFees,
                RefundableAmount = r.RefundableAmount,
                State = r.State,
                MoneyRefunded = r.Order.MoneyRefunded,
                CreatedDate = r.CreatedDate
            }).ToList();

            var report = new PayPalRefundsReportDto
            {
                Items = items,
                Totals = new PayPalRefundsReportTotalsDto
                {
                    EntriesCount = items.Count,
                    TotalOrderAmount = items.Sum(i => i.OrderTotal),
                    TotalCancellationFees = items.Sum(i => i.CancellationFees),
                    TotalRefundable = items.Sum(i => i.RefundableAmount),
                    TotalPendingRefundable = items
                        .Where(i => i.State == RefundState.Pending)
                        .Sum(i => i.RefundableAmount),
                    TotalCompletedRefundable = items
                        .Where(i => i.State == RefundState.Success || i.MoneyRefunded)
                        .Sum(i => i.RefundableAmount)
                }
            };

            return Result.Success(report);
        }
    }
}
