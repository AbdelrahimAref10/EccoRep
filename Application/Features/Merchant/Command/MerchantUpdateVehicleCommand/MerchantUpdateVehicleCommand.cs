using Application.Features.Vehicle.Common;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Command.MerchantUpdateVehicleCommand
{
    /// <summary>Merchant updates only their own vehicle. MerchantId cannot be changed.</summary>
    public record MerchantUpdateVehicleCommand : IRequest<Result<int>>
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        /// <summary>Top speed in km/h. Optional.</summary>
        public int? SpeedKmh { get; set; }
        /// <summary>Motor engine capacity in CC. Optional.</summary>
        public int? EngineCapacityCc { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MerchantUpdateVehicleCommandHandler : IRequestHandler<MerchantUpdateVehicleCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public MerchantUpdateVehicleCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<int>> Handle(MerchantUpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            if (!VehicleStatusMapper.TryToEnum(request.Status, out var status))
                return Result.Failure<int>("Invalid vehicle status");

            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<int>("Merchant profile not found for current user");

            var vehicle = await _context.Vehicles
                .AsTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId, cancellationToken);

            if (vehicle == null)
                return Result.Failure<int>($"Vehicle with ID {request.VehicleId} not found");

            if (vehicle.MerchantId != merchant.MerchantId)
                return Result.Failure<int>("You can only update your own vehicles");

            var subCategory = await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
                return Result.Failure<int>($"SubCategory with ID {request.SubCategoryId} not found");

            if (subCategory.Category.CityId != merchant.CityId)
                return Result.Failure<int>("SubCategory must belong to a category in your city");

            string? oldImageUrl = vehicle.ImageUrl;
            string? imageUrl = vehicle.ImageUrl;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl) && _imageService.IsBase64String(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "vehicles");
                if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl != imageUrl)
                    _imageService.DeleteImage(oldImageUrl);
            }

            vehicle.Update(
                request.Name,
                request.VehicleCode,
                request.SubCategoryId,
                merchant.MerchantId,
                status,
                request.Color,
                request.Type,
                request.Model,
                request.Price,
                request.SpeedKmh,
                request.EngineCapacityCc,
                imageUrl,
                _userSession.UserName ?? merchant.FullName
            );

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(vehicle.VehicleId);
        }
    }
}
