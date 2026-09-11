using Application.Features.Vehicle.Common;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Command.UpdateVehicleCommand
{
    public record UpdateVehicleCommand : IRequest<Result<int>>
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public int MerchantId { get; set; }
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public UpdateVehicleCommandHandler(DatabaseContext context, IUserSession userSession, IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<int>> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            if (!VehicleStatusMapper.TryToEnum(request.Status, out var status))
            {
                return Result.Failure<int>("Invalid vehicle status");
            }

            if (request.MerchantId <= 0)
            {
                return Result.Failure<int>("Merchant ID is required");
            }

            var vehicle = await _context.Vehicles
                .AsTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId, cancellationToken);

            if (vehicle == null)
            {
                return Result.Failure<int>($"Vehicle with ID {request.VehicleId} not found");
            }

            var merchantExists = await _context.Merchants
                .AnyAsync(m => m.MerchantId == request.MerchantId && m.IsActive && !m.IsDeleted, cancellationToken);

            if (!merchantExists)
            {
                return Result.Failure<int>($"Active merchant with ID {request.MerchantId} not found");
            }

            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<int>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            string? oldImageUrl = vehicle.ImageUrl;
            string? imageUrl = vehicle.ImageUrl;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl) && _imageService.IsBase64String(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "vehicles");
                if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl != imageUrl)
                {
                    _imageService.DeleteImage(oldImageUrl);
                }
            }

            vehicle.Update(
                request.Name,
                request.VehicleCode,
                request.SubCategoryId,
                request.MerchantId,
                status,
                imageUrl,
                _userSession.UserName ?? "System"
            );

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(vehicle.VehicleId);
        }
    }
}
