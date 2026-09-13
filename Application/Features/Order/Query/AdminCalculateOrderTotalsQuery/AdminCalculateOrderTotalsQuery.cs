using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.AdminCalculateOrderTotalsQuery
{
    public record AdminCalculateOrderTotalsQuery : IRequest<Result<AdminOrderTotalsPreviewDto>>
    {
        public int CustomerId { get; set; }
        public int SubCategoryId { get; set; }
        public int CityId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public bool IsUrgent { get; set; }
        public List<int> VehicleIds { get; set; } = new();
    }

    public class AdminCalculateOrderTotalsQueryHandler : IRequestHandler<AdminCalculateOrderTotalsQuery, Result<AdminOrderTotalsPreviewDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public AdminCalculateOrderTotalsQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<AdminOrderTotalsPreviewDto>> Handle(
            AdminCalculateOrderTotalsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.VehicleIds == null || request.VehicleIds.Count == 0)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("At least one vehicle must be selected");
            }

            var distinctVehicleIds = request.VehicleIds.Distinct().ToList();
            if (distinctVehicleIds.Count != request.VehicleIds.Count)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("Duplicate vehicle IDs are not allowed");
            }

            if (request.ReservationDateFrom > request.ReservationDateTo)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("Reservation date from must be on or before reservation date to");
            }

            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("SubCategory not found or inactive");
            }

            var city = await _context.Cities
                .Include(c => c.TieredDiscounts)
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("City not found");
            }

            var vehicles = await _context.Vehicles
                .Include(v => v.Merchant)
                .Where(v => distinctVehicleIds.Contains(v.VehicleId))
                .ToListAsync(cancellationToken);

            if (vehicles.Count != distinctVehicleIds.Count)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>("One or more vehicles not found");
            }

            var from = request.ReservationDateFrom.Date;
            var to = request.ReservationDateTo.Date;

            var stillBookedOverlaps = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Include(rv => rv.Order)
                .Where(rv => distinctVehicleIds.Contains(rv.VehicleId)
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from)
                .Select(rv => new ValueTuple<int, DateTime, DateTime>(rv.VehicleId, rv.DateFrom, rv.DateTo))
                .ToListAsync(cancellationToken);

            var availability = Domain.Models.Order.EnsureSelectedVehiclesAreAvailable(
                vehicles,
                request.SubCategoryId,
                from,
                to,
                stillBookedOverlaps);

            if (availability.IsFailure)
            {
                return Result.Failure<AdminOrderTotalsPreviewDto>(availability.Error);
            }

            var days = Domain.Models.Order.InclusiveReservationDays(from, to);

            decimal previousDebt = 0;
            if (request.CustomerId > 0)
            {
                var pendingFees = await CancellationDebtHelper.GetPendingCancellationFeesAsync(
                    _context,
                    request.CustomerId,
                    cancellationToken);
                previousDebt = CancellationDebtHelper.SumWithdraw(pendingFees);
            }

            var pricing = Domain.Models.Order.CalculatePricing(
                vehicles.Select(v => v.Price).ToList(),
                city,
                request.IsUrgent,
                days,
                previousDebt);

            return Result.Success(new AdminOrderTotalsPreviewDto
            {
                CityId = city.CityId,
                CityName = city.Name,
                SubCategoryId = subCategory.SubCategoryId,
                SubCategoryName = subCategory.Name,
                ReservationDateFrom = request.ReservationDateFrom,
                ReservationDateTo = request.ReservationDateTo,
                Days = days,
                IsUrgent = request.IsUrgent,
                VehiclesCount = pricing.VehiclesCount,
                Vehicles = vehicles.Select(v => new AdminOrderPreviewVehicleDto
                {
                    VehicleId = v.VehicleId,
                    Name = v.Name,
                    VehicleCode = v.VehicleCode,
                    ImageUrl = !string.IsNullOrWhiteSpace(v.ImageUrl) ? _imageService.GetImageUrl(v.ImageUrl) : null,
                    MerchantId = v.MerchantId,
                    MerchantName = v.Merchant?.FullName ?? string.Empty,
                    Color = v.Color,
                    Type = v.Type,
                    Model = v.Model,
                    Price = v.Price,
                    SpeedKmh = v.SpeedKmh,
                    EngineCapacityCc = v.EngineCapacityCc
                }).ToList(),
                UnitPrice = pricing.UnitPrice,
                SubTotal = pricing.SubTotal,
                DeliveryFees = pricing.DeliveryFees,
                ServiceFees = pricing.ServiceFees,
                UrgentFees = pricing.UrgentFees,
                TieredDiscountPercentage = pricing.TieredDiscountPercentage,
                TieredDiscountAmount = pricing.TieredDiscountAmount,
                PreviousDebt = pricing.PreviousDebt,
                Total = pricing.Total,
                PaymentMethod = nameof(PaymentMethod.Cash)
            });
        }
    }
}
