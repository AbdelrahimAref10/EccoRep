using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.SearchCustomersByMobileQuery
{
    public record SearchCustomersByMobileQuery : IRequest<Result<List<CustomerLookupDto>>>
    {
        /// <summary>Full or partial mobile number (min 4 digits after trim).</summary>
        public string MobileNumber { get; set; } = string.Empty;
    }

    public class SearchCustomersByMobileQueryHandler
        : IRequestHandler<SearchCustomersByMobileQuery, Result<List<CustomerLookupDto>>>
    {
        private const int MinDigits = 4;
        private const int MaxResults = 10;

        private readonly DatabaseContext _context;

        public SearchCustomersByMobileQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<CustomerLookupDto>>> Handle(
            SearchCustomersByMobileQuery request,
            CancellationToken cancellationToken)
        {
            var mobile = NormalizeMobile(request.MobileNumber);
            if (string.IsNullOrWhiteSpace(mobile) || CountDigits(mobile) < MinDigits)
            {
                return Result.Failure<List<CustomerLookupDto>>(
                    "Enter at least 4 digits of the mobile number.");
            }

            // Exact match first, then contains — capped for performance.
            var exact = await _context.Customers
                .AsNoTracking()
                .Where(c => c.MobileNumber == mobile)
                .Select(c => new CustomerLookupDto
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    MobileNumber = c.MobileNumber,
                    CityId = c.CityId,
                    ZoneId = c.ZoneId,
                    CityName = c.City.Name,
                    State = c.State,
                    CashBlock = c.CashBlock
                })
                .ToListAsync(cancellationToken);

            if (exact.Count > 0)
            {
                return Result.Success(exact);
            }

            var partial = await _context.Customers
                .AsNoTracking()
                .Where(c => c.MobileNumber.Contains(mobile))
                .OrderBy(c => c.FullName)
                .Take(MaxResults)
                .Select(c => new CustomerLookupDto
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    MobileNumber = c.MobileNumber,
                    CityId = c.CityId,
                    ZoneId = c.ZoneId,
                    CityName = c.City.Name,
                    State = c.State,
                    CashBlock = c.CashBlock
                })
                .ToListAsync(cancellationToken);

            return Result.Success(partial);
        }

        private static string NormalizeMobile(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Replace(" ", string.Empty);
        }

        private static int CountDigits(string value)
        {
            var count = 0;
            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
