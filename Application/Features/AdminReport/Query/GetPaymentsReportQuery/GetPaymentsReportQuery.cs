using Application.Features.AdminReport.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AdminReport.Query.GetPaymentsReportQuery
{
    public record GetPaymentsReportQuery : IRequest<Result<PaymentsReportDto>>
    {
        public List<int>? CityIds { get; set; }
        public List<int>? CustomerIds { get; set; }
        public List<int>? PaymentMethodIds { get; set; }
        public List<int>? PaymentStates { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderCode { get; set; }
    }

    public class GetPaymentsReportQueryHandler
        : IRequestHandler<GetPaymentsReportQuery, Result<PaymentsReportDto>>
    {
        private readonly DatabaseContext _context;

        public GetPaymentsReportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<PaymentsReportDto>> Handle(
            GetPaymentsReportQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.OrderPayments
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Include(p => p.Order)
                    .ThenInclude(o => o.City)
                .AsQueryable();

            if (request.CityIds is { Count: > 0 })
                query = query.Where(p => request.CityIds.Contains(p.Order.CityId));

            if (request.CustomerIds is { Count: > 0 })
                query = query.Where(p => request.CustomerIds.Contains(p.Order.CustomerId));

            if (request.PaymentMethodIds is { Count: > 0 })
                query = query.Where(p => request.PaymentMethodIds.Contains(p.PaymentMethodId));

            if (request.PaymentStates is { Count: > 0 })
                query = query.Where(p => request.PaymentStates.Contains((int)p.State));

            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(p => p.CreatedDate < to);
            }

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var code = request.OrderCode.Trim();
                query = query.Where(p => p.Order.OrderCode.Contains(code));
            }

            var payments = await query
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync(cancellationToken);

            var items = payments.Select(p => new PaymentsReportRowDto
            {
                PaymentId = p.Id,
                OrderId = p.OrderId,
                OrderCode = p.Order.OrderCode,
                CustomerId = p.Order.CustomerId,
                CustomerName = p.Order.Customer.FullName,
                CityName = p.Order.City.Name,
                PaymentMethod = (PaymentMethod)p.PaymentMethodId,
                State = p.State,
                Amount = p.Total,
                PreviousDebt = p.Order.PreviousDebt,
                OrderState = p.Order.OrderState,
                CreatedDate = p.CreatedDate
            }).ToList();

            var report = new PaymentsReportDto
            {
                Items = items,
                Totals = new PaymentsReportTotalsDto
                {
                    PaymentsCount = items.Count,
                    TotalAmount = items.Sum(i => i.Amount),
                    TotalPaid = items.Where(i => i.State == PaymentState.Paid).Sum(i => i.Amount),
                    TotalPending = items.Where(i => i.State == PaymentState.Pending).Sum(i => i.Amount),
                    TotalFailed = items.Where(i => i.State == PaymentState.Failed).Sum(i => i.Amount),
                    TotalRefunded = items.Where(i => i.State == PaymentState.Refunded).Sum(i => i.Amount),
                    TotalCashPaid = items
                        .Where(i => i.PaymentMethod == PaymentMethod.Cash && i.State == PaymentState.Paid)
                        .Sum(i => i.Amount),
                    TotalPayPalPaid = items
                        .Where(i => i.PaymentMethod == PaymentMethod.PayPal && i.State == PaymentState.Paid)
                        .Sum(i => i.Amount)
                }
            };

            return Result.Success(report);
        }
    }
}
