using Application.Features.City.DTOs;
using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetCityFeesQuery
{
    public record GetCityFeesQuery : IRequest<Result<CityFeesDto>>
    {
        // No parameters needed - will get from logged-in customer
    }

    public class GetCityFeesQueryHandler : IRequestHandler<GetCityFeesQuery, Result<CityFeesDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetCityFeesQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<CityFeesDto>> Handle(GetCityFeesQuery request, CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
            {
                return Result.Failure<CityFeesDto>("Customer not found or not authenticated");
            }

            var customer = await _context.Customers
                .Include(c => c.City)
                    .ThenInclude(city => city.TieredDiscounts)
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CityFeesDto>("Customer not found");
            }

            if (customer.City == null)
            {
                return Result.Failure<CityFeesDto>("Customer city not found");
            }

            var pendingFees = await CancellationDebtHelper.GetPendingCancellationFeesAsync(
                _context,
                customer.CustomerId,
                cancellationToken);
            var previousDebt = CancellationDebtHelper.SumWithdraw(pendingFees);

            var cityFeesDto = new CityFeesDto
            {
                CityId = customer.City.CityId,
                ServiceFees = customer.City.ServiceFees,
                UrgentFees = customer.City.UrgentDelivery,
                CancellationFees = customer.City.CancellationFees,
                PreviousDebt = previousDebt,
                TieredDiscounts = customer.City.TieredDiscounts
                    .OrderBy(td => td.From)
                    .Select(td => new TieredDiscountDto
                    {
                        Id = td.Id,
                        CityId = td.CityId,
                        From = td.From,
                        To = td.To,
                        Discount = td.Discount
                    })
                    .ToList()
            };

            return Result.Success(cityFeesDto);
        }
    }
}
