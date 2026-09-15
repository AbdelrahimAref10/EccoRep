using Application.Features.Merchant.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetActiveMerchantsLookupQuery
{
    public record GetActiveMerchantsLookupQuery : IRequest<Result<List<MerchantLookupDto>>>;

    public class GetActiveMerchantsLookupQueryHandler
        : IRequestHandler<GetActiveMerchantsLookupQuery, Result<List<MerchantLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetActiveMerchantsLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<MerchantLookupDto>>> Handle(
            GetActiveMerchantsLookupQuery request,
            CancellationToken cancellationToken)
        {
            var merchants = await _context.Merchants
                .AsNoTracking()
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.FullName)
                .Select(m => new MerchantLookupDto
                {
                    MerchantId = m.MerchantId,
                    CityId = m.CityId,
                    ZoneId = m.ZoneId,
                    FullName = m.FullName,
                    MobileNumber = m.MobileNumber,
                    CashOnReceive = m.CashOnReceive
                })
                .ToListAsync(cancellationToken);

            return Result.Success(merchants);
        }
    }
}
