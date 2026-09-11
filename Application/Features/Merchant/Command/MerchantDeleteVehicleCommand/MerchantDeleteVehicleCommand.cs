using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Command.MerchantDeleteVehicleCommand
{
    /// <summary>Merchant deletes only their own vehicle (same rules as admin delete).</summary>
    public record MerchantDeleteVehicleCommand : IRequest<Result<bool>>
    {
        public int VehicleId { get; set; }
    }

    public class MerchantDeleteVehicleCommandHandler : IRequestHandler<MerchantDeleteVehicleCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public MerchantDeleteVehicleCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(MerchantDeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var vehicle = await _context.Vehicles
                .AsTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId, cancellationToken);

            if (vehicle == null)
                return Result.Failure<bool>($"Vehicle with ID {request.VehicleId} not found");

            if (vehicle.MerchantId != merchant.MerchantId)
                return Result.Failure<bool>("You can only delete your own vehicles");

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
