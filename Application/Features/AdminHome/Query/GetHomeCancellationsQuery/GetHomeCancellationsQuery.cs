using Application.Features.AdminHome.Common;
using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomeCancellationsQuery
{
    public record GetHomeCancellationsQuery : IRequest<Result<HomeCancellationsDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Granularity { get; set; } = "day";
    }

    public class GetHomeCancellationsQueryHandler : IRequestHandler<GetHomeCancellationsQuery, Result<HomeCancellationsDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeCancellationsQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeCancellationsDto>> Handle(GetHomeCancellationsQuery request, CancellationToken cancellationToken)
        {
            var granularity = (request.Granularity ?? "day").Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month"))
            {
                return Result.Failure<HomeCancellationsDto>("Granularity must be day, week, or month.");
            }

            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var entries = await _context.CustomerWallets.AsNoTracking()
                .Where(cw =>
                    cw.Type == WalletType.OrderCancellationFees &&
                    cw.CreatedDate >= from &&
                    cw.CreatedDate <= to)
                .Select(cw => new { cw.CreatedDate, cw.Withdraw, cw.State })
                .ToListAsync(cancellationToken);

            var buckets = new Dictionary<DateTime, HomeChartPointDto>();
            var cursor = BucketStart(from, granularity);
            var end = to.Date;
            while (cursor <= end)
            {
                buckets[cursor] = new HomeChartPointDto
                {
                    PeriodStart = cursor,
                    Period = granularity == "month" ? $"{cursor:yyyy-MM}" : $"{cursor:yyyy-MM-dd}",
                    Value = 0,
                    Count = 0
                };
                cursor = granularity switch
                {
                    "week" => cursor.AddDays(7),
                    "month" => cursor.AddMonths(1),
                    _ => cursor.AddDays(1)
                };
            }

            foreach (var entry in entries)
            {
                var key = BucketStart(entry.CreatedDate, granularity);
                if (!buckets.TryGetValue(key, out var point))
                {
                    continue;
                }

                point.Count += 1;
                point.Value += entry.Withdraw;
            }

            return Result.Success(new HomeCancellationsDto
            {
                CancelledOrders = entries.Count,
                TotalFees = entries.Sum(x => x.Withdraw),
                PaidFees = entries.Where(x => x.State == CustomerWalletState.Paid).Sum(x => x.Withdraw),
                UnpaidFees = entries.Where(x => x.State == CustomerWalletState.Pending).Sum(x => x.Withdraw),
                Series = buckets.Values.OrderBy(x => x.PeriodStart).ToList(),
                From = from,
                To = to
            });
        }

        private static DateTime BucketStart(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => date.Date.AddDays(-(int)date.DayOfWeek),
                "month" => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }
    }
}
