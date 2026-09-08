using Application.Features.City.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Query.GetCityByIdQuery
{
    public record GetCityByIdQuery : IRequest<Result<CityDto>>
    {
        public int CityId { get; set; }
    }

    public class GetCityByIdQueryHandler : IRequestHandler<GetCityByIdQuery, Result<CityDto>>
    {
        private readonly DatabaseContext _context;

        public GetCityByIdQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<CityDto>> Handle(GetCityByIdQuery request, CancellationToken cancellationToken)
        {
            var city = await _context.Cities
                .Include(c => c.TieredDiscounts)
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<CityDto>($"City with ID {request.CityId} not found");
            }

            var customerCount = await _context.Customers
                .CountAsync(c => c.CityId == city.CityId, cancellationToken);

            var cityDto = new CityDto
            {
                CityId = city.CityId,
                Name = city.Name,
                Description = city.Description,
                IsActive = city.IsActive,
                CustomerCount = customerCount,
                CreatedDate = city.CreatedDate,
                DeliveryFees = city.DeliveryFees,
                UrgentDelivery = city.UrgentDelivery,
                ServiceFees = city.ServiceFees,
                CancellationFees = city.CancellationFees,
                TieredDiscounts = city.TieredDiscounts.Select(td => new DTOs.TieredDiscountDto
                {
                    Id = td.Id,
                    CityId = td.CityId,
                    From = td.From,
                    To = td.To,
                    Discount = td.Discount
                }).ToList()
            };

            return Result.Success(cityDto);
        }
    }
}

