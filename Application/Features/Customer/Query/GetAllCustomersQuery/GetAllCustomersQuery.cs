using Application.Common;
using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.GetAllCustomersQuery
{
    public record GetAllCustomersQuery : IRequest<Result<PagedResult<CustomerDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Domain.Enums.CustomerState? State { get; set; }
        public int? CityId { get; set; }
        public int? RegisterAs { get; set; } // 0 = Individual, 1 = Institution
    }

    public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, Result<PagedResult<CustomerDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetAllCustomersQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<PagedResult<CustomerDto>>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Customers
                .Include(c => c.City)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(searchTerm) ||
                                       c.MobileNumber.Contains(searchTerm) ||
                                       (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                                       (c.City != null && c.City.Name.ToLower().Contains(searchTerm)));
            }

            if (request.State.HasValue)
            {
                query = query.Where(c => c.State == request.State.Value);
            }

            if (request.CityId.HasValue && request.CityId.Value > 0)
            {
                query = query.Where(c => c.CityId == request.CityId.Value);
            }

            if (request.RegisterAs.HasValue)
            {
                query = query.Where(c => c.RegisterAs == request.RegisterAs.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = customers.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                MobileNumber = c.MobileNumber,
                FullName = c.FullName,
                Gender = c.Gender,
                PersonalImage = _imageService.GetImageUrl(c.PersonalImage),
                Email = c.Email,
                CommercialRegisterImage = _imageService.GetImageUrl(c.CommercialRegisterImage),
                RegisterAs = c.RegisterAs,
                VerificationBy = c.VerificationBy,
                CityId = c.CityId,
                CityName = c.City != null ? c.City.Name : string.Empty,
                State = c.State,
                CashBlock = c.CashBlock,
                CreatedDate = c.CreatedDate
            }).ToList();

            var result = new PagedResult<CustomerDto>
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

