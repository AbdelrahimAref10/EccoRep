using Application.Features.Vehicle.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Query.GetVehicleByIdQuery
{
    public record GetVehicleByIdQuery : IRequest<Result<VehicleDto>>
    {
        public int VehicleId { get; set; }
    }

    public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, Result<VehicleDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetVehicleByIdQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<VehicleDto>> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.SubCategory)
                    .ThenInclude(sc => sc.Category)
                        .ThenInclude(c => c.City)
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId, cancellationToken);

            if (vehicle == null)
            {
                return Result.Failure<VehicleDto>($"Vehicle with ID {request.VehicleId} not found");
            }

            var vehicleDto = new VehicleDto
            {
                VehicleId = vehicle.VehicleId,
                Name = vehicle.Name,
                ImageUrl = _imageService.GetImageUrl(vehicle.ImageUrl),
                Status = (int)vehicle.Status,
                SubCategoryId = vehicle.SubCategoryId,
                SubCategoryName = vehicle.SubCategory.Name,
                SubCategoryPrice = vehicle.SubCategory.Price,
                CategoryId = vehicle.SubCategory.CategoryId,
                CategoryName = vehicle.SubCategory.Category.Name,
                CityId = vehicle.SubCategory.Category.CityId,
                CityName = vehicle.SubCategory.Category.City.Name
            };

            return Result.Success(vehicleDto);
        }
    }
}


