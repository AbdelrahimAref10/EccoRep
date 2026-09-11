using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Services
{
    public interface IVehicleReservationQueryService
    {
        Task<Result<IReadOnlyList<VehicleReservationAvailabilityItem>>> GetAvailableVehiclesAsync(
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            CancellationToken cancellationToken = default,
            int? excludeOrderId = null);
    }
}
