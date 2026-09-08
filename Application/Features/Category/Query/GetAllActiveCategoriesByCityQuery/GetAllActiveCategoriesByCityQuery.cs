using Application.Features.Category.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Query.GetAllActiveCategoriesByCityQuery
{
    public record GetAllActiveCategoriesByCityQuery : IRequest<Result<List<CategoryDto>>>
    {
        public int CustomerId { get; set; }
    }

    public class GetAllActiveCategoriesByCityQueryHandler : IRequestHandler<GetAllActiveCategoriesByCityQuery, Result<List<CategoryDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllActiveCategoriesByCityQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<List<CategoryDto>>> Handle(GetAllActiveCategoriesByCityQuery request, CancellationToken cancellationToken)
        {
            // Get Customer to retrieve CityId
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<List<CategoryDto>>("Customer not found");
            }

            var categories = await _context.Categories
                .Include(c => c.City)
                .Where(c => c.IsActive && c.CityId == customer.CityId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    SubCategoryCount = c.SubCategories.Count(sc => sc.IsActive),
                    CityId = c.CityId,
                    CityName = c.City.Name
                })
                .ToListAsync(cancellationToken);

            foreach (var dto in categories)
            {
                dto.ImageUrl = _imageService.GetImageUrl(dto.ImageUrl);
            }

            return Result.Success(categories);
        }
    }
}

