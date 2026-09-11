using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.MerchantNotification.Command.MarkMerchantNotificationAsReadCommand
{
    public record MarkMerchantNotificationAsReadCommand : IRequest<Result<bool>>
    {
        public int MerchantNotificationId { get; set; }
    }

    public class MarkMerchantNotificationAsReadCommandHandler
        : IRequestHandler<MarkMerchantNotificationAsReadCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public MarkMerchantNotificationAsReadCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(
            MarkMerchantNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var notification = await _context.MerchantNotifications
                .AsTracking()
                .FirstOrDefaultAsync(
                    n => n.MerchantNotificationId == request.MerchantNotificationId
                        && n.MerchantId == merchant.MerchantId,
                    cancellationToken);

            if (notification == null)
                return Result.Failure<bool>("Notification not found");

            notification.MarkAsRead();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
