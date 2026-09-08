using Application.Common;
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

namespace Application.Features.Category.Query.GetAllCategoriesQuery
{
    public record GetAllCategoriesQuery : IRequest<Result<PagedResult<CategoryDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public List<int>? CityIds { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<PagedResult<CategoryDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllCategoriesQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<PagedResult<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories.AsNoTracking().AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            if (request.CityIds is { Count: > 0 })
            {
                var cityIds = request.CityIds.Distinct().ToList();
                query = query.Where(c => cityIds.Contains(c.CityId));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchTerm)) ||
                    c.City.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
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

            foreach (var item in items)
            {
                item.ImageUrl = _imageService.GetImageUrl(item.ImageUrl);
            }

            var result = new PagedResult<CategoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }
    }
}
