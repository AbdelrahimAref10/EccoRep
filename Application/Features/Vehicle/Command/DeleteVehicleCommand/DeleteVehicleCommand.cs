using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Command.DeleteVehicleCommand
{
    public record DeleteVehicleCommand : IRequest<Result<bool>>
    {
        public int VehicleId { get; set; }
    }

    public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;

        public DeleteVehicleCommandHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles
                .AsTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId, cancellationToken);

            if (vehicle == null)
            {
                return Result.Failure<bool>($"Vehicle with ID {request.VehicleId} not found");
            }

            var isLinkedToOrders = await _context.OrderVehicles
                .AnyAsync(ov => ov.VehicleId == request.VehicleId, cancellationToken);

            if (isLinkedToOrders)
            {
                return Result.Failure<bool>(
                    "Cannot delete this vehicle because it is linked to orders. Please keep it or reassign related orders first.");
            }

            var hasReservations = await _context.ReservedVehiclesPerDays
                .AnyAsync(rv => rv.VehicleId == request.VehicleId, cancellationToken);

            if (hasReservations)
            {
                return Result.Failure<bool>(
                    "Cannot delete this vehicle because it has reservation history. Please keep it for records.");
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
    }
}
