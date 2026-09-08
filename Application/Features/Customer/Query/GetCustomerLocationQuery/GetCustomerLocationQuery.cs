using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.GetCustomerLocationQuery
{
    public record GetCustomerLocationQuery : IRequest<Result<CustomerLocationDto>>
    {
    }

    public class GetCustomerLocationQueryHandler : IRequestHandler<GetCustomerLocationQuery, Result<CustomerLocationDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetCustomerLocationQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<CustomerLocationDto>> Handle(GetCustomerLocationQuery request, CancellationToken cancellationToken)
        {
            var userId = _userSession.UserId;

            if (userId <= 0)
            {
                return Result.Failure<CustomerLocationDto>("User not found or not authenticated");
            }

            var customer = await _context.Customers
                .Include(c => c.CustomerLocation)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CustomerLocationDto>("Customer not found");
            }

            if (customer.CustomerLocation == null)
            {
                return Result.Failure<CustomerLocationDto>("Customer location not available");
            }

            var locationDto = new CustomerLocationDto
            {
                CustomerId = customer.CustomerId,
                Longitude = customer.CustomerLocation.Longitude,
                Latitude = customer.CustomerLocation.Latitude
            };

            return Result.Success(locationDto);
        }
    }
}

