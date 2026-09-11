using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.MerchantNotification.Query.GetMyMerchantUnreadNotificationsCountQuery
{
    public record GetMyMerchantUnreadNotificationsCountQuery : IRequest<Result<int>>;

    public class GetMyMerchantUnreadNotificationsCountQueryHandler
        : IRequestHandler<GetMyMerchantUnreadNotificationsCountQuery, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantUnreadNotificationsCountQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<int>> Handle(
            GetMyMerchantUnreadNotificationsCountQuery request,
            CancellationToken cancellationToken)
        {
            var merchantId = await _context.Merchants
                .AsNoTracking()
                .Where(m => m.UserId == _userSession.UserId && !m.IsDeleted)
                .Select(m => (int?)m.MerchantId)
                .FirstOrDefaultAsync(cancellationToken);

            if (merchantId == null)
                return Result.Failure<int>("Merchant profile not found for current user");

            var count = await _context.MerchantNotifications
                .AsNoTracking()
                .CountAsync(n => n.MerchantId == merchantId && !n.IsRead, cancellationToken);

            return Result.Success(count);
        }
    }
}
