using Application.Common;
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

namespace Application.Features.SubCategory.Query.GetAllSubCategoriesQuery
{
    public record GetAllSubCategoriesQuery : IRequest<Result<PagedResult<SubCategoryDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? CityIds { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsOffer { get; set; }
    }

    public class GetAllSubCategoriesQueryHandler : IRequestHandler<GetAllSubCategoriesQuery, Result<PagedResult<SubCategoryDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllSubCategoriesQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<PagedResult<SubCategoryDto>>> Handle(GetAllSubCategoriesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.SubCategories.AsNoTracking().AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(sc => sc.IsActive == request.IsActive.Value);
            }

            if (request.IsOffer.HasValue)
            {
                query = query.Where(sc => sc.IsOffer == request.IsOffer.Value);
            }

            var categoryIds = ResolveCategoryIds(request);
            if (categoryIds is { Count: > 0 })
            {
                query = query.Where(sc => categoryIds.Contains(sc.CategoryId));
            }

            if (request.CityIds is { Count: > 0 })
            {
                var cityIds = request.CityIds.Distinct().ToList();
                query = query.Where(sc => cityIds.Contains(sc.Category.CityId));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(sc =>
                    sc.Name.ToLower().Contains(searchTerm) ||
                    (sc.Description != null && sc.Description.ToLower().Contains(searchTerm)) ||
                    sc.Category.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(sc => sc.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(sc => new SubCategoryDto
                {
                    SubCategoryId = sc.SubCategoryId,
                    Name = sc.Name,
                    Description = sc.Description,
                    ImageUrl = sc.ImageUrl,
                    IsActive = sc.IsActive,
                    IsOffer = sc.IsOffer,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    CityId = sc.Category.CityId,
                    CityName = sc.Category.City.Name,
                    VehicleCount = sc.Vehicles.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.ImageUrl = _imageService.GetImageUrl(item.ImageUrl);
            }

            var result = new PagedResult<SubCategoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }

        private static List<int>? ResolveCategoryIds(GetAllSubCategoriesQuery request)
        {
            if (request.CategoryIds is { Count: > 0 })
            {
                return request.CategoryIds.Distinct().ToList();
            }

            if (request.CategoryId.HasValue)
            {
                return new List<int> { request.CategoryId.Value };
            }

            return null;
        }
    }
}
