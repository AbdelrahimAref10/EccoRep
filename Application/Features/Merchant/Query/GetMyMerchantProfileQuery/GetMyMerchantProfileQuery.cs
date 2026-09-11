using Application.Features.Merchant.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Query.GetMyMerchantProfileQuery
{
    public record GetMyMerchantProfileQuery : IRequest<Result<MerchantDto>>;

    public class GetMyMerchantProfileQueryHandler : IRequestHandler<GetMyMerchantProfileQuery, Result<MerchantDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantProfileQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<MerchantDto>> Handle(
            GetMyMerchantProfileQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .Include(m => m.City)
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<MerchantDto>("Merchant profile not found for current user");

            return Result.Success(new MerchantDto
            {
                MerchantId = merchant.MerchantId,
                UserId = merchant.UserId,
                UserName = merchant.User?.UserName,
                CityId = merchant.CityId,
                CityName = merchant.City?.Name,
                FullName = merchant.FullName,
                MobileNumber = merchant.MobileNumber,
                Email = merchant.Email,
                PersonalImage = merchant.PersonalImage,
                IsActive = merchant.IsActive,
                CashOnReceive = merchant.CashOnReceive,
                IsDeleted = merchant.IsDeleted,
                CreatedDate = merchant.CreatedDate
            });
        }
    }
}
