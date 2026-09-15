using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.GetCustomerInfoQuery
{
    public record GetCustomerInfoQuery : IRequest<Result<CustomerDto>>;

    public class GetCustomerInfoQueryHandler : IRequestHandler<GetCustomerInfoQuery, Result<CustomerDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public GetCustomerInfoQueryHandler(DatabaseContext context, IUserSession userSession, IImageService imageService)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<CustomerDto>> Handle(GetCustomerInfoQuery request, CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
            {
                return Result.Failure<CustomerDto>("Customer not found or not authenticated");
            }

            var customer = await _context.Customers
                .Include(c => c.City)
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CustomerDto>("Customer not found");
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                MobileNumber = customer.MobileNumber,
                FullName = customer.FullName,
                Gender = customer.Gender,
                PersonalImage = _imageService.GetImageUrl(customer.PersonalImage),
                Email = customer.Email,
                CommercialRegisterImage = _imageService.GetImageUrl(customer.CommercialRegisterImage),
                RegisterAs = customer.RegisterAs,
                VerificationBy = customer.VerificationBy,
                CityId = customer.CityId,
                ZoneId = customer.ZoneId,
                CityName = customer.City != null ? customer.City.Name : string.Empty,
                State = customer.State,
                CashBlock = customer.CashBlock,
                CreatedDate = customer.CreatedDate
            };

            return Result.Success(dto);
        }
    }
}
