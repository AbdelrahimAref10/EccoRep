using Application.Common;
using Application.Features.Vehicle.Common;
using Application.Features.Vehicle.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetMyMerchantVehiclesQuery
{
    public record GetMyMerchantVehiclesQuery : IRequest<Result<PagedResult<VehicleDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public int? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
    }

    public class GetMyMerchantVehiclesQueryHandler
        : IRequestHandler<GetMyMerchantVehiclesQuery, Result<PagedResult<VehicleDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public GetMyMerchantVehiclesQueryHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<PagedResult<VehicleDto>>> Handle(
            GetMyMerchantVehiclesQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<PagedResult<VehicleDto>>("Merchant profile not found for current user");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

            var query = _context.Vehicles
                .AsNoTracking()
                .Where(v => v.MerchantId == merchant.MerchantId);

            if (request.CategoryId.HasValue)
                query = query.Where(v => v.SubCategory.CategoryId == request.CategoryId.Value);

            if (request.SubCategoryId.HasValue)
                query = query.Where(v => v.SubCategoryId == request.SubCategoryId.Value);

            if (request.Status.HasValue && VehicleStatusMapper.TryToEnum(request.Status.Value, out var statusEnum))
                query = query.Where(v => v.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(v =>
                    v.Name.ToLower().Contains(searchTerm) ||
                    v.VehicleCode.ToLower().Contains(searchTerm) ||
                    v.SubCategory.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderBy(v => v.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.VehicleId,
                    v.Name,
                    v.VehicleCode,
                    v.ImageUrl,
                    v.Status,
                    v.SubCategoryId,
                    SubCategoryName = v.SubCategory.Name,
                    v.Color,
                    v.Type,
                    v.Model,
                    v.Price,
                    v.SpeedKmh,
                    v.EngineCapacityCc,
                    CategoryId = v.SubCategory.CategoryId,
                    CategoryName = v.SubCategory.Category.Name,
                    CityId = v.SubCategory.Category.CityId,
                    CityName = v.SubCategory.Category.City.Name,
                    v.MerchantId,
                    MerchantName = v.Merchant.FullName
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
                Color = v.Color,
                Type = v.Type,
                Model = v.Model,
                Price = v.Price,
                SpeedKmh = v.SpeedKmh,
                EngineCapacityCc = v.EngineCapacityCc,
                CategoryId = v.CategoryId,
                CategoryName = v.CategoryName,
                CityId = v.CityId,
                CityName = v.CityName,
                MerchantId = v.MerchantId,
                MerchantName = v.MerchantName
            }).ToList();

            return Result.Success(new PagedResult<VehicleDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }
}
