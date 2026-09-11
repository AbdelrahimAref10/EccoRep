using Application.Features.Merchant.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetAllMerchantsQuery
{
    public record GetAllMerchantsQuery : IRequest<Result<List<MerchantDto>>>
    {
        public string? Search { get; set; }
        /// <summary>When null, returns non-deleted only. When set, filters by that value.</summary>
        public bool? IsDeleted { get; set; }
    }

    public class GetAllMerchantsQueryHandler : IRequestHandler<GetAllMerchantsQuery, Result<List<MerchantDto>>>
    {
        private readonly DatabaseContext _context;

        public GetAllMerchantsQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<MerchantDto>>> Handle(
            GetAllMerchantsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Merchants.AsNoTracking().AsQueryable();

            if (request.IsDeleted.HasValue)
                query = query.Where(m => m.IsDeleted == request.IsDeleted.Value);
            else
                query = query.Where(m => !m.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(m =>
                    m.FullName.Contains(term) ||
                    m.MobileNumber.Contains(term) ||
                    (m.Email != null && m.Email.Contains(term)) ||
                    (m.User.UserName != null && m.User.UserName.Contains(term)));
            }

            var list = await query
                .OrderByDescending(m => m.CreatedDate)
                .Select(m => new MerchantDto
                {
                    MerchantId = m.MerchantId,
                    UserId = m.UserId,
                    UserName = m.User.UserName,
                    CityId = m.CityId,
                    CityName = m.City.Name,
                    FullName = m.FullName,
                    MobileNumber = m.MobileNumber,
                    Email = m.Email,
                    PersonalImage = m.PersonalImage,
                    IsActive = m.IsActive,
                    CashOnReceive = m.CashOnReceive,
                    IsDeleted = m.IsDeleted,
                    CreatedDate = m.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Result.Success(list);
        }
    }
}
