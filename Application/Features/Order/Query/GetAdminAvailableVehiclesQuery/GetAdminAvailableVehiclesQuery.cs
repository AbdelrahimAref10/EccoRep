using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetAdminAvailableVehiclesQuery
{

    public record GetAdminAvailableVehiclesQuery : IRequest<Result<AdminAvailableVehiclesDto>>
    {
        public int SubCategoryId { get; set; }
        public int CityId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        /// <summary>When set, reservations for this order are ignored (for replace-vehicle on existing order).</summary>
        public int? ExcludeOrderId { get; set; }
        public int DestinationZoneId { get; set; }
    }

    public class GetAdminAvailableVehiclesQueryHandler
        : IRequestHandler<GetAdminAvailableVehiclesQuery, Result<AdminAvailableVehiclesDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IVehicleReservationQueryService _reservationQueryService;
        private readonly IImageService _imageService;

        public GetAdminAvailableVehiclesQueryHandler(
            DatabaseContext context,
            IVehicleReservationQueryService reservationQueryService,
            IImageService imageService)
        {
            _context = context;
            _reservationQueryService = reservationQueryService;
            _imageService = imageService;
        }

        public async Task<Result<AdminAvailableVehiclesDto>> Handle(
            GetAdminAvailableVehiclesQuery request,
            CancellationToken cancellationToken)
        {
            var availability = await _reservationQueryService.GetAvailableVehiclesAsync(
                request.SubCategoryId,
                request.CityId,
                request.ReservationDateFrom,
                request.ReservationDateTo,
                cancellationToken,
                request.ExcludeOrderId);

            if (request.DestinationZoneId <= 0)
                return Result.Failure<AdminAvailableVehiclesDto>("Destination zone is required");

            if (!await OrderZoneFeeHelper.ZoneBelongsToCityAsync(
                    _context, request.CityId, request.DestinationZoneId, cancellationToken))
            {
                return Result.Failure<AdminAvailableVehiclesDto>(
                    "Destination zone must belong to the selected city group");
            }

            if (availability.IsFailure)
            {
                return Result.Failure<AdminAvailableVehiclesDto>(availability.Error);
            }

            var origin = await _context.Zones.AsNoTracking()
                .FirstOrDefaultAsync(z => z.ZoneId == request.DestinationZoneId, cancellationToken);
            if (origin == null)
                return Result.Failure<AdminAvailableVehiclesDto>("Destination zone not found");

            var zoneIds = availability.Value.Select(v => v.MerchantZoneId).Append(origin.ZoneId).Distinct().ToList();
            var zones = await _context.Zones.AsNoTracking()
                .Where(z => zoneIds.Contains(z.ZoneId))
                .ToDictionaryAsync(z => z.ZoneId, cancellationToken);

            var sorted = ZoneProximitySorter.SortByMerchantZoneDistance(
                availability.Value,
                origin,
                v => v.MerchantZoneId,
                zones);

            var subCategory = await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                    .ThenInclude(c => c.City)
                .FirstAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            var city = subCategory.Category.City!;
            var from = request.ReservationDateFrom.Date;
            var to = request.ReservationDateTo.Date;
            var days = Domain.Models.Order.InclusiveReservationDays(from, to);

            var items = sorted.Select(v =>
            {
                var isAvailable = v.AvailabilityStatus == VehicleAvailabilityStatus.Available;
                return new AdminAvailableVehicleItemDto
                {
                    VehicleId = v.VehicleId,
                    Name = v.Name,
                    VehicleCode = v.VehicleCode,
                    ImageUrl = !string.IsNullOrWhiteSpace(v.ImagePath)
                        ? _imageService.GetImageUrl(v.ImagePath)
                        : null,
                    MerchantId = v.MerchantId,
                    MerchantZoneId = v.MerchantZoneId,
                    MerchantName = v.MerchantName,
                    Status = (int)v.VehicleStatus,
                    IsAvailable = isAvailable,
                    UnavailableReason = isAvailable ? null : "Reserved",
                    ConflictingDates = v.ConflictingDates.ToList(),
                    Color = v.Color,
                    Type = v.Type,
                    Model = v.Model,
                    Price = v.Price,
                    SpeedKmh = v.SpeedKmh,
                    EngineCapacityCc = v.EngineCapacityCc
                };
            }).ToList();

            return Result.Success(new AdminAvailableVehiclesDto
            {
                SubCategoryId = subCategory.SubCategoryId,
                SubCategoryName = subCategory.Name,
                CityId = city.CityId,
                CityName = city.Name,
                ReservationDateFrom = from,
                ReservationDateTo = to,
                Days = days,
                AvailableCount = items.Count(v => v.IsAvailable),
                UnavailableCount = items.Count(v => !v.IsAvailable),
                Vehicles = items
            });
        }
    }
}
