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

namespace Application.Features.AdminHome.Query.GetHomeRevenueTrendQuery
{
    public record GetHomeRevenueTrendQuery : IRequest<Result<HomeRevenueTrendDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
        /// <summary>day | week | month</summary>
        public string Granularity { get; set; } = "day";
    }

    public class GetHomeRevenueTrendQueryHandler : IRequestHandler<GetHomeRevenueTrendQuery, Result<HomeRevenueTrendDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeRevenueTrendQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeRevenueTrendDto>> Handle(GetHomeRevenueTrendQuery request, CancellationToken cancellationToken)
        {
            var granularity = (request.Granularity ?? "day").Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month"))
            {
                return Result.Failure<HomeRevenueTrendDto>("Granularity must be day, week, or month.");
            }

            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var ordersQuery = _context.Orders.AsNoTracking()
                .Where(o => o.OrderState == OrderState.Completed && o.CreatedDate >= from && o.CreatedDate <= to);

            if (request.CityId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.CityId == request.CityId.Value);
            }

            var rows = await ordersQuery
                .Select(o => new
                {
                    o.CreatedDate,
                    Paid = o.OrderPayments.Where(p => p.State == PaymentState.Paid).Sum(p => (decimal?)p.Total) ?? 0
                })
                .ToListAsync(cancellationToken);

            var buckets = BuildBuckets(from, to, granularity);
            foreach (var row in rows)
            {
                var key = BucketStart(row.CreatedDate, granularity);
                if (buckets.TryGetValue(key, out var point))
                {
                    point.Value += row.Paid;
                    point.Count += 1;
                }
            }

            var series = buckets.Values.OrderBy(x => x.PeriodStart).ToList();
            var totalRevenue = series.Sum(x => x.Value);
            var totalOrders = series.Sum(x => x.Count);

            return Result.Success(new HomeRevenueTrendDto
            {
                Granularity = granularity,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = totalOrders == 0 ? 0 : Math.Round(totalRevenue / totalOrders, 2),
                Series = series,
                From = from,
                To = to
            });
        }

        private static Dictionary<DateTime, HomeChartPointDto> BuildBuckets(DateTime from, DateTime to, string granularity)
        {
            var map = new Dictionary<DateTime, HomeChartPointDto>();
            var cursor = BucketStart(from, granularity);
            var end = to.Date;

            while (cursor <= end)
            {
                map[cursor] = new HomeChartPointDto
                {
                    PeriodStart = cursor,
                    Period = FormatPeriod(cursor, granularity),
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

            return map;
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

        private static string FormatPeriod(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => $"{date:yyyy-MM-dd}",
                "month" => $"{date:yyyy-MM}",
                _ => $"{date:yyyy-MM-dd}"
            };
        }
    }
}
