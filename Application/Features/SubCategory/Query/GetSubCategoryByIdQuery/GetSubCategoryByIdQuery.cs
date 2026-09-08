using Application.Features.SubCategory.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Query.GetSubCategoryByIdQuery
{
    public record GetSubCategoryByIdQuery : IRequest<Result<SubCategoryDto>>
    {
        public int SubCategoryId { get; set; }
    }

    public class GetSubCategoryByIdQueryHandler : IRequestHandler<GetSubCategoryByIdQuery, Result<SubCategoryDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetSubCategoryByIdQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<SubCategoryDto>> Handle(GetSubCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                    .ThenInclude(c => c.City)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<SubCategoryDto>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            var vehicleCount = await _context.Vehicles
                .CountAsync(v => v.SubCategoryId == subCategory.SubCategoryId, cancellationToken);

            var subCategoryDto = new SubCategoryDto
            {
                SubCategoryId = subCategory.SubCategoryId,
                Name = subCategory.Name,
                Description = subCategory.Description,
                ImageUrl = _imageService.GetImageUrl(subCategory.ImageUrl),
                IsActive = subCategory.IsActive,
                IsOffer = subCategory.IsOffer,
                Price = subCategory.Price,
                CategoryId = subCategory.CategoryId,
                CategoryName = subCategory.Category.Name,
                CityId = subCategory.Category.CityId,
                CityName = subCategory.Category.City.Name,
                VehicleCount = vehicleCount
            };

            return Result.Success(subCategoryDto);
        }
    }
}

