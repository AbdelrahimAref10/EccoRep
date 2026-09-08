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

namespace Application.Features.AdminHome.Query.GetHomeSummaryQuery
{
    public record GetHomeSummaryQuery : IRequest<Result<HomeSummaryDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
    }

    public class GetHomeSummaryQueryHandler : IRequestHandler<GetHomeSummaryQuery, Result<HomeSummaryDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeSummaryQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeSummaryDto>> Handle(GetHomeSummaryQuery request, CancellationToken cancellationToken)
        {
            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);
            var (prevFrom, prevTo) = AdminHomeDateRange.PreviousPeriod(from, to);

            var orders = _context.Orders.AsNoTracking().AsQueryable();
            if (request.CityId.HasValue)
            {
                orders = orders.Where(o => o.CityId == request.CityId.Value);
            }

            var revenue = await PaidRevenueAsync(orders, from, to, cancellationToken);
            var prevRevenue = await PaidRevenueAsync(orders, prevFrom, prevTo, cancellationToken);

            var ordersCount = await orders.CountAsync(o => o.CreatedDate >= from && o.CreatedDate <= to, cancellationToken);
            var prevOrdersCount = await orders.CountAsync(o => o.CreatedDate >= prevFrom && o.CreatedDate <= prevTo, cancellationToken);

            var activeRentals = await orders.CountAsync(o =>
                o.OrderState == OrderState.Confirmed ||
                o.OrderState == OrderState.OnWay ||
                o.OrderState == OrderState.CustomerReceived, cancellationToken);

            var pendingOrders = await orders.CountAsync(o => o.OrderState == OrderState.Pending, cancellationToken);

            var vehicles = _context.Vehicles.AsNoTracking();
            var totalVehicles = await vehicles.CountAsync(cancellationToken);
            var availableVehicles = await vehicles.CountAsync(v => v.Status == VehicleStatus.Available, cancellationToken);
            var rentedVehicles = await vehicles.CountAsync(v => v.Status == VehicleStatus.Rented, cancellationToken);
            var utilization = totalVehicles == 0 ? 0 : Math.Round((decimal)rentedVehicles / totalVehicles * 100m, 2);

            var customers = _context.Customers.AsNoTracking().AsQueryable();
            if (request.CityId.HasValue)
            {
                customers = customers.Where(c => c.CityId == request.CityId.Value);
            }

            var newCustomers = await customers.CountAsync(c => c.CreatedDate >= from && c.CreatedDate <= to, cancellationToken);
            var prevNewCustomers = await customers.CountAsync(c => c.CreatedDate >= prevFrom && c.CreatedDate <= prevTo, cancellationToken);

            var totalDebit = await _context.CompanyTreasuries.AsNoTracking().SumAsync(t => (decimal?)t.DebitAmount, cancellationToken) ?? 0;
            var totalCredit = await _context.CompanyTreasuries.AsNoTracking().SumAsync(t => (decimal?)t.CreditAmount, cancellationToken) ?? 0;

            var unpaidCancellationFeesCount = await _context.CustomerWallets.AsNoTracking()
                .CountAsync(cw =>
                    cw.Type == WalletType.OrderCancellationFees &&
                    cw.State == CustomerWalletState.Pending &&
                    cw.CreatedDate >= from &&
                    cw.CreatedDate <= to, cancellationToken);

            var dto = new HomeSummaryDto
            {
                Revenue = revenue,
                RevenueChangePercent = AdminHomeDateRange.PercentChange(revenue, prevRevenue),
                OrdersCount = ordersCount,
                OrdersChangePercent = AdminHomeDateRange.PercentChange(ordersCount, prevOrdersCount),
                AverageOrderValue = ordersCount == 0 ? 0 : Math.Round(revenue / ordersCount, 2),
                ActiveRentals = activeRentals,
                PendingOrders = pendingOrders,
                AvailableVehicles = availableVehicles,
                TotalVehicles = totalVehicles,
                VehicleUtilizationPercent = utilization,
                NewCustomers = newCustomers,
                NewCustomersChangePercent = AdminHomeDateRange.PercentChange(newCustomers, prevNewCustomers),
                TreasuryBalance = totalDebit - totalCredit,
                UnpaidCancellationFeesCount = unpaidCancellationFeesCount,
                From = from,
                To = to
            };

            return Result.Success(dto);
        }

        private static async Task<decimal> PaidRevenueAsync(
            IQueryable<Domain.Models.Order> orders,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            return await orders
                .Where(o => o.OrderState == OrderState.Completed && o.CreatedDate >= from && o.CreatedDate <= to)
                .SelectMany(o => o.OrderPayments)
                .Where(p => p.State == PaymentState.Paid)
                .SumAsync(p => (decimal?)p.Total, cancellationToken) ?? 0;
        }
    }
}
