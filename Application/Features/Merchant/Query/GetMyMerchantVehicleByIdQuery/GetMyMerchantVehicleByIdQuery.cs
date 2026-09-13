using Application.Features.Vehicle.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetMyMerchantVehicleByIdQuery
{
    public record GetMyMerchantVehicleByIdQuery : IRequest<Result<VehicleDto>>
    {
        public int VehicleId { get; set; }
    }

    public class GetMyMerchantVehicleByIdQueryHandler : IRequestHandler<GetMyMerchantVehicleByIdQuery, Result<VehicleDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public GetMyMerchantVehicleByIdQueryHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<VehicleDto>> Handle(
            GetMyMerchantVehicleByIdQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<VehicleDto>("Merchant profile not found for current user");

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .Include(v => v.Merchant)
                .Include(v => v.SubCategory)
                    .ThenInclude(sc => sc.Category)
                        .ThenInclude(c => c.City)
                .FirstOrDefaultAsync(
                    v => v.VehicleId == request.VehicleId && v.MerchantId == merchant.MerchantId,
                    cancellationToken);

            if (vehicle == null)
                return Result.Failure<VehicleDto>($"Vehicle with ID {request.VehicleId} not found");

            return Result.Success(new VehicleDto
            {
                VehicleId = vehicle.VehicleId,
                Name = vehicle.Name,
                VehicleCode = vehicle.VehicleCode,
                ImageUrl = _imageService.GetImageUrl(vehicle.ImageUrl),
                Status = (int)vehicle.Status,
                SubCategoryId = vehicle.SubCategoryId,
                SubCategoryName = vehicle.SubCategory.Name,
                Color = vehicle.Color,
                Type = vehicle.Type,
                Model = vehicle.Model,
                Price = vehicle.Price,
                SpeedKmh = vehicle.SpeedKmh,
                EngineCapacityCc = vehicle.EngineCapacityCc,
                CategoryId = vehicle.SubCategory.CategoryId,
                CategoryName = vehicle.SubCategory.Category.Name,
                CityId = vehicle.SubCategory.Category.CityId,
                CityName = vehicle.SubCategory.Category.City.Name,
                MerchantId = vehicle.MerchantId,
                MerchantName = vehicle.Merchant.FullName
            });
        }
    }
}
