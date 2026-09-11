using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.MerchantNotification.Command.MarkAllMyMerchantNotificationsAsReadCommand
{
    public record MarkAllMyMerchantNotificationsAsReadCommand : IRequest<Result<bool>>;

    public class MarkAllMyMerchantNotificationsAsReadCommandHandler
        : IRequestHandler<MarkAllMyMerchantNotificationsAsReadCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public MarkAllMyMerchantNotificationsAsReadCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(
            MarkAllMyMerchantNotificationsAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var unread = await _context.MerchantNotifications
                .AsTracking()
                .Where(n => n.MerchantId == merchant.MerchantId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var n in unread)
                n.MarkAsRead();

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
