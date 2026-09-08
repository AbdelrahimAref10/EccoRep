using Application.Features.AdminHome.Common;
using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomeTreasurySnapshotQuery
{
    public record GetHomeTreasurySnapshotQuery : IRequest<Result<HomeTreasurySnapshotDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Granularity { get; set; } = "day";
    }

    public class GetHomeTreasurySnapshotQueryHandler : IRequestHandler<GetHomeTreasurySnapshotQuery, Result<HomeTreasurySnapshotDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeTreasurySnapshotQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeTreasurySnapshotDto>> Handle(GetHomeTreasurySnapshotQuery request, CancellationToken cancellationToken)
        {
            var granularity = (request.Granularity ?? "day").Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month"))
            {
                return Result.Failure<HomeTreasurySnapshotDto>("Granularity must be day, week, or month.");
            }

            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var all = _context.CompanyTreasuries.AsNoTracking();
            var totalDebit = await all.SumAsync(t => (decimal?)t.DebitAmount, cancellationToken) ?? 0;
            var totalCredit = await all.SumAsync(t => (decimal?)t.CreditAmount, cancellationToken) ?? 0;
            var lastUpdated = await all.Select(t => (DateTime?)t.CreatedDate).MaxAsync(cancellationToken);

            var inRange = await all
                .Where(t => t.CreatedDate >= from && t.CreatedDate <= to)
                .Select(t => new { t.CreatedDate, t.DebitAmount, t.CreditAmount })
                .ToListAsync(cancellationToken);

            var buckets = new Dictionary<DateTime, HomeTreasuryMovementPointDto>();
            var cursor = BucketStart(from, granularity);
            var end = to.Date;
            while (cursor <= end)
            {
                buckets[cursor] = new HomeTreasuryMovementPointDto
                {
                    PeriodStart = cursor,
                    Period = granularity == "month" ? $"{cursor:yyyy-MM}" : $"{cursor:yyyy-MM-dd}",
                    Debit = 0,
                    Credit = 0,
                    Net = 0
                };
                cursor = granularity switch
                {
                    "week" => cursor.AddDays(7),
                    "month" => cursor.AddMonths(1),
                    _ => cursor.AddDays(1)
                };
            }

            foreach (var row in inRange)
            {
                var key = BucketStart(row.CreatedDate, granularity);
                if (!buckets.TryGetValue(key, out var point))
                {
                    continue;
                }

                point.Debit += row.DebitAmount;
                point.Credit += row.CreditAmount;
                point.Net = point.Debit - point.Credit;
            }

            return Result.Success(new HomeTreasurySnapshotDto
            {
                Balance = totalDebit - totalCredit,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                LastUpdated = lastUpdated,
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
