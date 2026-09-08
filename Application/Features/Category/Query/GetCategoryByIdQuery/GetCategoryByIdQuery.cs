using Application.Features.Category.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Query.GetCategoryByIdQuery
{
    public record GetCategoryByIdQuery : IRequest<Result<CategoryDto>>
    {
        public int CategoryId { get; set; }
    }

    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetCategoryByIdQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.City)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result.Failure<CategoryDto>($"Category with ID {request.CategoryId} not found");
            }

            var categoryDto = new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = _imageService.GetImageUrl(category.ImageUrl),
                IsActive = category.IsActive,
                SubCategoryCount = category.SubCategories.Count(sc => sc.IsActive),
                CityId = category.CityId,
                CityName = category.City.Name
            };

            return Result.Success(categoryDto);
        }
    }
}

