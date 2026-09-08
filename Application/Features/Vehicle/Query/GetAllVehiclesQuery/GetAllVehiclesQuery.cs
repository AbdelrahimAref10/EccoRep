using Application.Common;
using Application.Features.Vehicle.Common;
using Application.Features.Vehicle.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Query.GetAllVehiclesQuery
{
    public record GetAllVehiclesQuery : IRequest<Result<PagedResult<VehicleDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int>? CategoryIds { get; set; }
        public int? SubCategoryId { get; set; }
        public List<int>? SubCategoryIds { get; set; }
        public List<int>? CityIds { get; set; }
        /// <summary>VehicleStatus as int. Prefer Statuses for multi-select.</summary>
        public int? Status { get; set; }
        public List<int>? Statuses { get; set; }
    }

    public class GetAllVehiclesQueryHandler : IRequestHandler<GetAllVehiclesQuery, Result<PagedResult<VehicleDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllVehiclesQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<PagedResult<VehicleDto>>> Handle(GetAllVehiclesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Vehicles.AsNoTracking().AsQueryable();

            var categoryIds = ResolveIds(request.CategoryIds, request.CategoryId);
            if (categoryIds is { Count: > 0 })
            {
                query = query.Where(v => categoryIds.Contains(v.SubCategory.CategoryId));
            }

            var subCategoryIds = ResolveIds(request.SubCategoryIds, request.SubCategoryId);
            if (subCategoryIds is { Count: > 0 })
            {
                query = query.Where(v => subCategoryIds.Contains(v.SubCategoryId));
            }

            if (request.CityIds is { Count: > 0 })
            {
                var cityIds = request.CityIds.Distinct().ToList();
                query = query.Where(v => cityIds.Contains(v.SubCategory.Category.CityId));
            }

            var statuses = ResolveStatuses(request);
            if (statuses is { Count: > 0 })
            {
                query = query.Where(v => statuses.Contains(v.Status));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(v =>
                    v.Name.ToLower().Contains(searchTerm) ||
                    v.VehicleCode.ToLower().Contains(searchTerm) ||
                    v.SubCategory.Name.ToLower().Contains(searchTerm) ||
                    v.SubCategory.Category.Name.ToLower().Contains(searchTerm) ||
                    v.SubCategory.Category.City.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Do not cast Status to int in SQL — DB column is nvarchar enum name.
            var rows = await query
                .OrderBy(v => v.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(v => new
                {
                    v.VehicleId,
                    v.Name,
                    v.VehicleCode,
                    v.ImageUrl,
                    v.Status,
                    v.SubCategoryId,
                    SubCategoryName = v.SubCategory.Name,
                    SubCategoryPrice = v.SubCategory.Price,
                    CategoryId = v.SubCategory.CategoryId,
                    CategoryName = v.SubCategory.Category.Name,
                    CityId = v.SubCategory.Category.CityId,
                    CityName = v.SubCategory.Category.City.Name
                })
                .ToListAsync(cancellationToken);

            var items = rows.Select(v => new VehicleDto
            {
                VehicleId = v.VehicleId,
                Name = v.Name,
                VehicleCode = v.VehicleCode,
                ImageUrl = _imageService.GetImageUrl(v.ImageUrl),
                Status = (int)v.Status,
                SubCategoryId = v.SubCategoryId,
                SubCategoryName = v.SubCategoryName,
                SubCategoryPrice = v.SubCategoryPrice,
                CategoryId = v.CategoryId,
                CategoryName = v.CategoryName,
                CityId = v.CityId,
                CityName = v.CityName
            }).ToList();

            return Result.Success(new PagedResult<VehicleDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        private static List<int>? ResolveIds(List<int>? ids, int? singleId)
        {
            if (ids is { Count: > 0 })
            {
                return ids.Distinct().ToList();
            }

            if (singleId.HasValue)
            {
                return new List<int> { singleId.Value };
            }

            return null;
        }

        private static List<VehicleStatus>? ResolveStatuses(GetAllVehiclesQuery request)
        {
            IEnumerable<int> raw = request.Statuses is { Count: > 0 }
                ? request.Statuses
                : request.Status.HasValue
                    ? new[] { request.Status.Value }
                    : Enumerable.Empty<int>();

            var statuses = raw
                .Where(s => VehicleStatusMapper.TryToEnum(s, out _))
                .Select(s => (VehicleStatus)s)
                .Distinct()
                .ToList();

            return statuses.Count > 0 ? statuses : null;
        }
    }
}
