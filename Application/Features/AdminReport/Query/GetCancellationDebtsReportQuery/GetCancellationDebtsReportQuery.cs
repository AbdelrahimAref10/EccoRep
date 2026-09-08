using Application.Features.AdminReport.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AdminReport.Query.GetCancellationDebtsReportQuery
{
    public record GetCancellationDebtsReportQuery : IRequest<Result<CancellationDebtsReportDto>>
    {
        public List<int>? CustomerIds { get; set; }
        public List<int>? States { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CustomerSearch { get; set; }
        public string? OrderCode { get; set; }
    }

    public class GetCancellationDebtsReportQueryHandler
        : IRequestHandler<GetCancellationDebtsReportQuery, Result<CancellationDebtsReportDto>>
    {
        private readonly DatabaseContext _context;

        public GetCancellationDebtsReportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<CancellationDebtsReportDto>> Handle(
            GetCancellationDebtsReportQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.CustomerWallets
                .AsNoTracking()
                .Include(cw => cw.Customer)
                .Where(cw => cw.Type == WalletType.OrderCancellationFees);

            if (request.CustomerIds is { Count: > 0 })
                query = query.Where(cw => request.CustomerIds.Contains(cw.CustomerId));

            if (request.States is { Count: > 0 })
                query = query.Where(cw => request.States.Contains((int)cw.State));

            if (request.FromDate.HasValue)
                query = query.Where(cw => cw.CreatedDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(cw => cw.CreatedDate < to);
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerSearch))
            {
                var term = request.CustomerSearch.Trim();
                query = query.Where(cw =>
                    cw.Customer.FullName.Contains(term)
                    || cw.Customer.MobileNumber.Contains(term));
            }

            var wallets = await query
                .OrderByDescending(cw => cw.CreatedDate)
                .ToListAsync(cancellationToken);

            var orderIds = wallets
                .Where(w => w.OrderId.HasValue)
                .Select(w => w.OrderId!.Value)
                .Distinct()
                .ToList();

            var orderCodes = await _context.Orders
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.OrderId))
                .Select(o => new { o.OrderId, o.OrderCode })
                .ToDictionaryAsync(x => x.OrderId, x => x.OrderCode, cancellationToken);

            var items = wallets.Select(w =>
            {
                string? orderCode = null;
                if (w.OrderId.HasValue)
                    orderCodes.TryGetValue(w.OrderId.Value, out orderCode);

                return new CancellationDebtsReportRowDto
                {
                    WalletId = w.Id,
                    CustomerId = w.CustomerId,
                    CustomerName = w.Customer.FullName,
                    CustomerMobile = w.Customer.MobileNumber,
                    OrderId = w.OrderId,
                    OrderCode = orderCode,
                    Amount = w.Withdraw,
                    State = w.State,
                    Description = w.Description,
                    CreatedDate = w.CreatedDate
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var code = request.OrderCode.Trim();
                items = items
                    .Where(i =>
                        (!string.IsNullOrEmpty(i.OrderCode) && i.OrderCode.Contains(code, StringComparison.OrdinalIgnoreCase))
                        || i.Description.Contains(code, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var report = new CancellationDebtsReportDto
            {
                Items = items,
                Totals = new CancellationDebtsReportTotalsDto
                {
                    EntriesCount = items.Count,
                    CustomersCount = items.Select(i => i.CustomerId).Distinct().Count(),
                    TotalAmount = items.Sum(i => i.Amount),
                    TotalPending = items.Where(i => i.State == CustomerWalletState.Pending).Sum(i => i.Amount),
                    TotalUnderPayment = items.Where(i => i.State == CustomerWalletState.UnderPayment).Sum(i => i.Amount),
                    TotalPaid = items.Where(i => i.State == CustomerWalletState.Paid).Sum(i => i.Amount)
                }
            };

            return Result.Success(report);
        }
    }
}
