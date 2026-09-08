using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminNotification.Query.GetUnreadAdminNotificationsCountQuery
{
    public record GetUnreadAdminNotificationsCountQuery : IRequest<Result<int>>;

    public class GetUnreadAdminNotificationsCountQueryHandler : IRequestHandler<GetUnreadAdminNotificationsCountQuery, Result<int>>
    {
        private readonly DatabaseContext _context;

        public GetUnreadAdminNotificationsCountQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<int>> Handle(GetUnreadAdminNotificationsCountQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var count = await _context.AdminNotifications
                    .CountAsync(n => !n.IsRead, cancellationToken);

                return Result.Success(count);
            }
            catch (Exception ex)
            {
                return Result.Failure<int>($"Failed to get unread count: {ex.Message}");
            }
        }
    }
}

