using Application.Features.Vehicle.Common;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Command.CreateVehicleCommand
{
    public record CreateVehicleCommand : IRequest<Result<int>>
    {
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public int MerchantId { get; set; }
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

    public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public CreateVehicleCommandHandler(DatabaseContext context, IUserSession userSession, IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<int>> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {
            if (!VehicleStatusMapper.TryToEnum(request.Status, out var status))
            {
                return Result.Failure<int>("Invalid vehicle status");
            }

            if (request.MerchantId <= 0)
            {
                return Result.Failure<int>("Merchant ID is required");
            }

            var merchantExists = await _context.Merchants
                .AnyAsync(m => m.MerchantId == request.MerchantId && m.IsActive && !m.IsDeleted, cancellationToken);

            if (!merchantExists)
            {
                return Result.Failure<int>($"Active merchant with ID {request.MerchantId} not found");
            }

            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                    .ThenInclude(c => c.City)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<int>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "vehicles");
            }

            var vehicle = Domain.Models.Vehicle.Create(
                request.Name,
                request.VehicleCode,
                request.SubCategoryId,
                request.MerchantId,
                status,
                request.Color,
                request.Type,
                request.Model,
                request.Price,
                request.SpeedKmh,
                request.EngineCapacityCc,
                imageUrl,
                _userSession.UserName ?? "System"
            );

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(vehicle.VehicleId);
        }
    }
}
