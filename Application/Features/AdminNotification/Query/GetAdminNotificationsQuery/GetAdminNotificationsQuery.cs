using Application.Features.AdminNotification.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminNotification.Query.GetAdminNotificationsQuery
{
    public record GetAdminNotificationsQuery : IRequest<Result<List<AdminNotificationDto>>>
    {
        public bool? IsRead { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class GetAdminNotificationsQueryHandler : IRequestHandler<GetAdminNotificationsQuery, Result<List<AdminNotificationDto>>>
    {
        private readonly DatabaseContext _context;

        public GetAdminNotificationsQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<AdminNotificationDto>>> Handle(GetAdminNotificationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.AdminNotifications
                    .Include(n => n.Order)
                    .AsQueryable();

                // Filter by read status if provided
                if (request.IsRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == request.IsRead.Value);
                }

                // Order by creation date (newest first)
                query = query.OrderByDescending(n => n.CreatedDate);

                // Apply pagination
                if (request.Skip.HasValue)
                {
                    query = query.Skip(request.Skip.Value);
                }

                if (request.Take.HasValue)
                {
                    query = query.Take(request.Take.Value);
                }

                var notifications = await query
                    .Select(n => new AdminNotificationDto
                    {
                        AdminNotificationId = n.AdminNotificationId,
                        Title = n.Title,
                        Message = n.Message,
                        OrderId = n.OrderId,
                        OrderCode = n.Order != null ? n.Order.OrderCode : null,
                        NotificationType = n.NotificationType,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt,
                        ReadByUserId = n.ReadByUserId,
                        CreatedDate = n.CreatedDate
                    })
                    .ToListAsync(cancellationToken);

                return Result.Success(notifications);
            }
            catch (Exception ex)
            {
                return Result.Failure<List<AdminNotificationDto>>($"Failed to get notifications: {ex.Message}");
            }
        }
    }
}

