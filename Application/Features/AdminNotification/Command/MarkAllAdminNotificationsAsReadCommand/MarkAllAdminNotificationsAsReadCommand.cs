using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminNotification.Command.MarkAllAdminNotificationsAsReadCommand
{
    public record MarkAllAdminNotificationsAsReadCommand : IRequest<Result>;

    public class MarkAllAdminNotificationsAsReadCommandHandler : IRequestHandler<MarkAllAdminNotificationsAsReadCommand, Result>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserSession _userSession;

        public MarkAllAdminNotificationsAsReadCommandHandler(
            DatabaseContext context,
            IDateTimeProvider dateTimeProvider,
            IUserSession userSession)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _userSession = userSession;
        }

        public async Task<Result> Handle(MarkAllAdminNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _userSession.UserId;
                if (userId <= 0)
                {
                    return Result.Failure("User not authenticated");
                }

                var unreadNotifications = await _context.AdminNotifications
                    .AsTracking()
                    .Where(n => !n.IsRead)
                    .ToListAsync(cancellationToken);

                if (unreadNotifications.Count == 0)
                {
                    return Result.Success();
                }

                var actor = _userSession.UserName ?? "System";

                foreach (var notification in unreadNotifications)
                {
                    notification.MarkAsRead(userId, _dateTimeProvider);
                    notification.LastModifiedBy = actor;
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to mark all notifications as read: {ex.Message}");
            }
        }
    }
}
