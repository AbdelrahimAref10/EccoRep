using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminNotification.Command.MarkAdminNotificationAsReadCommand
{
    public record MarkAdminNotificationAsReadCommand : IRequest<Result>
    {
        public int AdminNotificationId { get; set; }
    }

    public class MarkAdminNotificationAsReadCommandHandler : IRequestHandler<MarkAdminNotificationAsReadCommand, Result>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserSession _userSession;

        public MarkAdminNotificationAsReadCommandHandler(
            DatabaseContext context,
            IDateTimeProvider dateTimeProvider,
            IUserSession userSession)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _userSession = userSession;
        }

        public async Task<Result> Handle(MarkAdminNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var notification = await _context.AdminNotifications
                    .AsTracking()
                    .FirstOrDefaultAsync(n => n.AdminNotificationId == request.AdminNotificationId, cancellationToken);

                if (notification == null)
                {
                    return Result.Failure("Notification not found");
                }

                var userId = _userSession.UserId;
                if (userId <= 0)
                {
                    return Result.Failure("User not authenticated");
                }

                notification.MarkAsRead(userId, _dateTimeProvider);
                notification.LastModifiedBy = _userSession.UserName;

                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to mark notification as read: {ex.Message}");
            }
        }
    }
}

