using Application.Features.Merchant.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetMerchantByIdQuery
{
    public record GetMerchantByIdQuery : IRequest<Result<MerchantDto>>
    {
        public int MerchantId { get; set; }
    }

    public class GetMerchantByIdQueryHandler : IRequestHandler<GetMerchantByIdQuery, Result<MerchantDto>>
    {
        private readonly DatabaseContext _context;

        public GetMerchantByIdQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<MerchantDto>> Handle(GetMerchantByIdQuery request, CancellationToken cancellationToken)
        {
            if (request.MerchantId <= 0)
                return Result.Failure<MerchantDto>("MerchantId is required");

            var merchant = await _context.Merchants
                .AsNoTracking()
                .Where(m => m.MerchantId == request.MerchantId && !m.IsDeleted)
                .Select(m => new MerchantDto
                {
                    MerchantId = m.MerchantId,
                    UserId = m.UserId,
                    UserName = m.User.UserName,
                    CityId = m.CityId,
                    ZoneId = m.ZoneId,
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
                .FirstOrDefaultAsync(cancellationToken);

            if (merchant == null)
                return Result.Failure<MerchantDto>("Merchant not found");

            return Result.Success(merchant);
        }
    }
}
