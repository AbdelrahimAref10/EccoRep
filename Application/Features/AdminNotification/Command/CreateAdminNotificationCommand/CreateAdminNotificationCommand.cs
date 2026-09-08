using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminNotification.Command.CreateAdminNotificationCommand
{
    public record CreateAdminNotificationCommand : IRequest<Result<Domain.Models.AdminNotification>>
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public int? OrderId { get; set; }
    }

    public class CreateAdminNotificationCommandHandler : IRequestHandler<CreateAdminNotificationCommand, Result<Domain.Models.AdminNotification>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserSession _userSession;

        public CreateAdminNotificationCommandHandler(
            DatabaseContext context,
            IDateTimeProvider dateTimeProvider,
            IUserSession userSession)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _userSession = userSession;
        }

        public async Task<Result<Domain.Models.AdminNotification>> Handle(CreateAdminNotificationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var notification = Domain.Models.AdminNotification.Create(
                    request.Title,
                    request.Message,
                    request.NotificationType,
                    request.OrderId,
                    _userSession.UserName ?? "System"
                );

                // Set audit fields
                notification.CreatedDate = _dateTimeProvider.Now;
                notification.LastModifiedDate = _dateTimeProvider.Now;

                _context.AdminNotifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success(notification);
            }
            catch (Exception ex)
            {
                return Result.Failure<Domain.Models.AdminNotification>($"Failed to create notification: {ex.Message}");
            }
        }
    }
}

