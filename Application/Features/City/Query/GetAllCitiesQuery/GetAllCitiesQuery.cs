using Application.Common;
using Application.Features.City.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Query.GetAllCitiesQuery
{
    public record GetAllCitiesQuery : IRequest<Result<PagedResult<CityDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAllCitiesQueryHandler : IRequestHandler<GetAllCitiesQuery, Result<PagedResult<CityDto>>>
    {
        private readonly DatabaseContext _context;

        public GetAllCitiesQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedResult<CityDto>>> Handle(GetAllCitiesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Cities.AsQueryable();

            // Apply IsActive filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            // Apply search filter by name
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                       (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CityDto
                {
                    CityId = c.CityId,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CustomerCount = c.Customers.Count,
                    CreatedDate = c.CreatedDate,
                    DeliveryFees = c.DeliveryFees,
                    UrgentDelivery = c.UrgentDelivery,
                    ServiceFees = c.ServiceFees,
                    CancellationFees = c.CancellationFees
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResult<CityDto>
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

