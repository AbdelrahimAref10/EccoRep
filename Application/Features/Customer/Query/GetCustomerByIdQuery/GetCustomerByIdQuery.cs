using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.GetCustomerByIdQuery
{
    public record GetCustomerByIdQuery : IRequest<Result<CustomerDto>>
    {
        public int CustomerId { get; set; }
    }

    public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetCustomerByIdQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .Include(c => c.City)
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

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
                CityName = customer.City != null ? customer.City.Name : string.Empty,
                State = customer.State,
                CashBlock = customer.CashBlock,
                CreatedDate = customer.CreatedDate
            };

            return Result.Success(dto);
        }
    }
}

