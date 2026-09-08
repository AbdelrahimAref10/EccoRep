using Application.Features.SubCategory.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Query.GetSubCategoriesByCategoryQuery
{
    public record GetSubCategoriesByCategoryQuery : IRequest<Result<List<SubCategoryDto>>>
    {
        public int CategoryId { get; set; }
    }

    public class GetSubCategoriesByCategoryQueryHandler : IRequestHandler<GetSubCategoriesByCategoryQuery, Result<List<SubCategoryDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetSubCategoriesByCategoryQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<List<SubCategoryDto>>> Handle(GetSubCategoriesByCategoryQuery request, CancellationToken cancellationToken)
        {
            // Verify category exists
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == request.CategoryId && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return Result.Failure<List<SubCategoryDto>>($"Category with ID {request.CategoryId} not found");
            }

            var subCategories = await _context.SubCategories
                .Include(sc => sc.Category)
                    .ThenInclude(c => c.City)
                .Where(sc => sc.CategoryId == request.CategoryId && sc.IsActive)
                .OrderBy(sc => sc.Name)
                .ToListAsync(cancellationToken);

            var result = subCategories.Select(sc => new SubCategoryDto
            {
                SubCategoryId = sc.SubCategoryId,
                Name = sc.Name,
                Description = sc.Description,
                ImageUrl = _imageService.GetImageUrl(sc.ImageUrl),
                IsActive = sc.IsActive,
                IsOffer = sc.IsOffer,
                Price = sc.Price,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category.Name,
                CityId = sc.Category.CityId,
                CityName = sc.Category.City.Name,
                VehicleCount = sc.Vehicles.Count
            }).ToList();

            return Result.Success(result);
        }
    }
}

