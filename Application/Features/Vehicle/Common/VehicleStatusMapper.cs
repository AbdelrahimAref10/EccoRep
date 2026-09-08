using Domain.Enums;

namespace Application.Features.Vehicle.Common
{
    public static class VehicleStatusMapper
    {
        public static bool TryToEnum(int status, out VehicleStatus vehicleStatus)
        {
            if (Enum.IsDefined(typeof(VehicleStatus), status))
            {
                vehicleStatus = (VehicleStatus)status;
                return true;
            }

            vehicleStatus = default;
            return false;
        }

        public static VehicleStatus ToEnum(int status)
        {
            if (!TryToEnum(status, out var vehicleStatus))
            {
                throw new ArgumentException($"Invalid vehicle status: {status}", nameof(status));
            }

            return vehicleStatus;
        }
    }
}
