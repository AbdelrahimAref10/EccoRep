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

namespace Application.Features.AdminHome.Query.GetHomeCustomerGrowthQuery
{
    public record GetHomeCustomerGrowthQuery : IRequest<Result<HomeCustomerGrowthDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
        public string Granularity { get; set; } = "day";
    }

    public class GetHomeCustomerGrowthQueryHandler : IRequestHandler<GetHomeCustomerGrowthQuery, Result<HomeCustomerGrowthDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeCustomerGrowthQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeCustomerGrowthDto>> Handle(GetHomeCustomerGrowthQuery request, CancellationToken cancellationToken)
        {
            var granularity = (request.Granularity ?? "day").Trim().ToLowerInvariant();
            if (granularity is not ("day" or "week" or "month"))
            {
                return Result.Failure<HomeCustomerGrowthDto>("Granularity must be day, week, or month.");
            }

            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var customers = _context.Customers.AsNoTracking().AsQueryable();
            if (request.CityId.HasValue)
            {
                customers = customers.Where(c => c.CityId == request.CityId.Value);
            }

            var total = await customers.CountAsync(cancellationToken);
            var active = await customers.CountAsync(c => c.State == CustomerState.Active, cancellationToken);
            var inactive = await customers.CountAsync(c => c.State == CustomerState.InActive, cancellationToken);
            var blocked = await customers.CountAsync(c => c.State == CustomerState.Blocked, cancellationToken);
            var individual = await customers.CountAsync(c => c.RegisterAs == 0, cancellationToken);
            var institution = await customers.CountAsync(c => c.RegisterAs == 1, cancellationToken);
            var cashBlocked = await customers.CountAsync(c => c.CashBlock, cancellationToken);

            var createdInRange = await customers
                .Where(c => c.CreatedDate >= from && c.CreatedDate <= to)
                .Select(c => c.CreatedDate)
                .ToListAsync(cancellationToken);

            var buckets = BuildBuckets(from, to, granularity);
            foreach (var created in createdInRange)
            {
                var key = BucketStart(created, granularity);
                if (buckets.TryGetValue(key, out var point))
                {
                    point.Count += 1;
                    point.Value += 1;
                }
            }

            return Result.Success(new HomeCustomerGrowthDto
            {
                Granularity = granularity,
                TotalCustomers = total,
                ActiveCustomers = active,
                InactiveCustomers = inactive,
                BlockedCustomers = blocked,
                IndividualCustomers = individual,
                InstitutionCustomers = institution,
                NewInRange = createdInRange.Count,
                CashBlockedCustomers = cashBlocked,
                Series = buckets.Values.OrderBy(x => x.PeriodStart).ToList(),
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
    }
}
