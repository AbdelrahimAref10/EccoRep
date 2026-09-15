using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetCustomerAvailableVehiclesQuery
{
    /// <summary>
    /// Mobile wrapper around shared IVehicleReservationQueryService.
    /// </summary>
    public record GetCustomerAvailableVehiclesQuery : IRequest<Result<List<CustomerAvailableVehicleItemDto>>>
    {
        public int SubCategoryId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int DestinationZoneId { get; set; }
    }

    public class GetCustomerAvailableVehiclesQueryHandler
        : IRequestHandler<GetCustomerAvailableVehiclesQuery, Result<List<CustomerAvailableVehicleItemDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IVehicleReservationQueryService _reservationQueryService;
        private readonly IImageService _imageService;

        public GetCustomerAvailableVehiclesQueryHandler(
            DatabaseContext context,
            IUserSession userSession,
            IVehicleReservationQueryService reservationQueryService,
            IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _reservationQueryService = reservationQueryService;
            _imageService = imageService;
        }

        public async Task<Result<List<CustomerAvailableVehicleItemDto>>> Handle(
            GetCustomerAvailableVehiclesQuery request,
            CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
            {
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>(
                    "Customer not found or not authenticated");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>("Customer not found");
            }

            if (request.DestinationZoneId <= 0)
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>("Destination zone is required");

            if (!await OrderZoneFeeHelper.ZoneBelongsToCityAsync(
                    _context, customer.CityId, request.DestinationZoneId, cancellationToken))
            {
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>(
                    "Destination zone must belong to the customer city group");
            }

            var availability = await _reservationQueryService.GetAvailableVehiclesAsync(
                request.SubCategoryId,
                customer.CityId,
                request.ReservationDateFrom,
                request.ReservationDateTo,
                cancellationToken);

            if (availability.IsFailure)
            {
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>(availability.Error);
            }

            var origin = await _context.Zones.AsNoTracking()
                .FirstOrDefaultAsync(z => z.ZoneId == request.DestinationZoneId, cancellationToken);
            if (origin == null)
                return Result.Failure<List<CustomerAvailableVehicleItemDto>>("Destination zone not found");

            var zoneIds = availability.Value.Select(v => v.MerchantZoneId).Append(origin.ZoneId).Distinct().ToList();
            var zones = await _context.Zones.AsNoTracking()
                .Where(z => zoneIds.Contains(z.ZoneId))
                .ToDictionaryAsync(z => z.ZoneId, cancellationToken);

            var sorted = ZoneProximitySorter.SortByMerchantZoneDistance(
                availability.Value,
                origin,
                v => v.MerchantZoneId,
                zones);

            var items = sorted.Select(v => new CustomerAvailableVehicleItemDto
            {
                VehicleId = v.VehicleId,
                Name = v.Name,
                VehicleCode = v.VehicleCode,
                ImageUrl = !string.IsNullOrWhiteSpace(v.ImagePath)
                    ? _imageService.GetImageUrl(v.ImagePath)
                    : null,
                Status = (int)v.AvailabilityStatus,
                ConflictingDates = v.ConflictingDates.ToList(),
                Color = v.Color,
                Type = v.Type,
                Model = v.Model,
                Price = v.Price,
                MerchantZoneId = v.MerchantZoneId,
                SpeedKmh = v.SpeedKmh,
                EngineCapacityCc = v.EngineCapacityCc
            }).ToList();

            return Result.Success(items);
        }
    }
}
