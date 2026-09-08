using Application.Features.AdminHome.Common;
using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomeCityPerformanceQuery
{
    public record GetHomeCityPerformanceQuery : IRequest<Result<HomeCityPerformanceDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class GetHomeCityPerformanceQueryHandler : IRequestHandler<GetHomeCityPerformanceQuery, Result<HomeCityPerformanceDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeCityPerformanceQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeCityPerformanceDto>> Handle(GetHomeCityPerformanceQuery request, CancellationToken cancellationToken)
        {
            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var cities = await _context.Cities.AsNoTracking()
                .Select(c => new { c.CityId, c.Name })
                .ToListAsync(cancellationToken);

            var orderStats = await _context.Orders.AsNoTracking()
                .Where(o => o.CreatedDate >= from && o.CreatedDate <= to)
                .GroupBy(o => o.CityId)
                .Select(g => new
                {
                    CityId = g.Key,
                    OrdersCount = g.Count(),
                    Revenue = g.Where(o => o.OrderState == OrderState.Completed)
                        .SelectMany(o => o.OrderPayments)
                        .Where(p => p.State == PaymentState.Paid)
                        .Sum(p => (decimal?)p.Total) ?? 0
                })
                .ToListAsync(cancellationToken);

            var vehicleStats = await _context.Vehicles.AsNoTracking()
                .GroupBy(v => v.SubCategory.Category.CityId)
                .Select(g => new
                {
                    CityId = g.Key,
                    VehiclesCount = g.Count(),
                    AvailableVehicles = g.Count(v => v.Status == VehicleStatus.Available)
                })
                .ToListAsync(cancellationToken);

            var customerStats = await _context.Customers.AsNoTracking()
                .Where(c => c.CreatedDate >= from && c.CreatedDate <= to)
                .GroupBy(c => c.CityId)
                .Select(g => new { CityId = g.Key, NewCustomers = g.Count() })
                .ToListAsync(cancellationToken);

            var items = cities.Select(city =>
            {
                var orders = orderStats.FirstOrDefault(x => x.CityId == city.CityId);
                var vehicles = vehicleStats.FirstOrDefault(x => x.CityId == city.CityId);
                var customers = customerStats.FirstOrDefault(x => x.CityId == city.CityId);

                return new HomeCityPerformanceItemDto
                {
                    CityId = city.CityId,
                    CityName = city.Name,
                    OrdersCount = orders?.OrdersCount ?? 0,
                    Revenue = orders?.Revenue ?? 0,
                    VehiclesCount = vehicles?.VehiclesCount ?? 0,
                    AvailableVehicles = vehicles?.AvailableVehicles ?? 0,
                    NewCustomers = customers?.NewCustomers ?? 0
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.OrdersCount)
            .ToList();

            return Result.Success(new HomeCityPerformanceDto
            {
                Cities = items,
                From = from,
                To = to
            });
        }
    }
}
