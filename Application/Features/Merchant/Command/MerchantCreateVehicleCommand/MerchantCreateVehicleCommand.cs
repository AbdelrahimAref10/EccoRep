using Application.Features.Vehicle.Common;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Command.MerchantCreateVehicleCommand
{
    /// <summary>
    /// Merchant creates a vehicle into the shared Vehicles table.
    /// MerchantId is always taken from the logged-in merchant (not from the client body).
    /// </summary>
    public record MerchantCreateVehicleCommand : IRequest<Result<int>>
    {
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

    public class MerchantCreateVehicleCommandHandler : IRequestHandler<MerchantCreateVehicleCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public MerchantCreateVehicleCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<int>> Handle(MerchantCreateVehicleCommand request, CancellationToken cancellationToken)
        {
            if (!VehicleStatusMapper.TryToEnum(request.Status, out var status))
                return Result.Failure<int>("Invalid vehicle status");

            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<int>("Merchant profile not found for current user");

            if (!merchant.IsActive)
                return Result.Failure<int>("Merchant account is inactive");

            var subCategory = await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
                return Result.Failure<int>($"SubCategory with ID {request.SubCategoryId} not found");

            if (subCategory.Category.CityId != merchant.CityId)
                return Result.Failure<int>("SubCategory must belong to a category in your city");

            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "vehicles");

            var vehicle = Domain.Models.Vehicle.Create(
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

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(vehicle.VehicleId);
        }
    }
}
