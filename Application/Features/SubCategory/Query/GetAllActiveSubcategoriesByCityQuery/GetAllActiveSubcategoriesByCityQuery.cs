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

namespace Application.Features.SubCategory.Query.GetAllActiveSubcategoriesByCityQuery
{
    public record GetAllActiveSubcategoriesByCityQuery : IRequest<Result<List<SubCategoryDto>>>
    {
        public int CustomerId { get; set; }
    }

    public class GetAllActiveSubcategoriesByCityQueryHandler : IRequestHandler<GetAllActiveSubcategoriesByCityQuery, Result<List<SubCategoryDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllActiveSubcategoriesByCityQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<List<SubCategoryDto>>> Handle(GetAllActiveSubcategoriesByCityQuery request, CancellationToken cancellationToken)
        {
            // Get Customer to retrieve CityId
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<List<SubCategoryDto>>("Customer not found");
            }

            var subCategories = await _context.SubCategories
                .Include(sc => sc.Category)
                    .ThenInclude(c => c.City)
                .Where(sc => sc.IsActive && sc.Category.CityId == customer.CityId)
                .OrderBy(sc => sc.Name)
                .Select(sc => new SubCategoryDto
                {
                    SubCategoryId = sc.SubCategoryId,
                    Name = sc.Name,
                    Description = sc.Description,
                    ImageUrl = sc.ImageUrl,
                    IsActive = sc.IsActive,
                    IsOffer = sc.IsOffer,
                    Price = sc.Price,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    CityId = sc.Category.CityId,
                    CityName = sc.Category.City.Name,
                    VehicleCount = sc.Vehicles.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var dto in subCategories)
            {
                dto.ImageUrl = _imageService.GetImageUrl(dto.ImageUrl);
            }

            return Result.Success(subCategories);
        }
    }
}

