using Application.Features.Customer.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Query.GetCustomerNotificationsQuery
{
    public record GetCustomerNotificationsQuery : IRequest<Result<List<CustomerNotificationDto>>>
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class GetCustomerNotificationsQueryHandler
        : IRequestHandler<GetCustomerNotificationsQuery, Result<List<CustomerNotificationDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetCustomerNotificationsQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<List<CustomerNotificationDto>>> Handle(
            GetCustomerNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
            {
                return Result.Failure<List<CustomerNotificationDto>>("Customer not found or not authenticated");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<List<CustomerNotificationDto>>("Customer not found");
            }

            // Existing notification store: VO_AdminNotification (linked to customer via Order)
            var query = _context.AdminNotifications
                .AsNoTracking()
                .Include(n => n.Order)
                .Where(n => n.Order != null && n.Order.CustomerId == customer.CustomerId)
                .OrderByDescending(n => n.CreatedDate)
                .AsQueryable();

            if (request.Skip.HasValue)
            {
                query = query.Skip(request.Skip.Value);
            }

            if (request.Take.HasValue)
            {
                query = query.Take(request.Take.Value);
            }

            var notifications = await query
                .Select(n => new CustomerNotificationDto
                {
                    CustomerNotificationId = n.AdminNotificationId,
                    CustomerId = customer.CustomerId,
                    Title = n.Title,
                    Message = n.Message,
                    OrderId = n.OrderId,
                    OrderCode = n.Order != null ? n.Order.OrderCode : null,
                    NotificationType = n.NotificationType,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Result.Success(notifications);
        }
    }
}
