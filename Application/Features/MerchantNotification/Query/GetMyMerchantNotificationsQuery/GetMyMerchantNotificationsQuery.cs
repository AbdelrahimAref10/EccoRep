using Application.Features.MerchantNotification.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.MerchantNotification.Query.GetMyMerchantNotificationsQuery
{
    public record GetMyMerchantNotificationsQuery : IRequest<Result<List<MerchantNotificationDto>>>
    {
        public bool? IsRead { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class GetMyMerchantNotificationsQueryHandler
        : IRequestHandler<GetMyMerchantNotificationsQuery, Result<List<MerchantNotificationDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantNotificationsQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<List<MerchantNotificationDto>>> Handle(
            GetMyMerchantNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<List<MerchantNotificationDto>>("Merchant profile not found for current user");

            var query = _context.MerchantNotifications
                .AsNoTracking()
                .Include(n => n.Order)
                .Where(n => n.MerchantId == merchant.MerchantId);

            if (request.IsRead.HasValue)
                query = query.Where(n => n.IsRead == request.IsRead.Value);

            query = query.OrderByDescending(n => n.CreatedDate);

            if (request.Skip.HasValue)
                query = query.Skip(Math.Max(0, request.Skip.Value));
            if (request.Take.HasValue)
                query = query.Take(Math.Clamp(request.Take.Value, 1, 100));
            else
                query = query.Take(50);

            var items = await query.Select(n => new MerchantNotificationDto
            {
                MerchantNotificationId = n.MerchantNotificationId,
                MerchantId = n.MerchantId,
                Title = n.Title,
                Message = n.Message,
                OrderId = n.OrderId,
                OrderCode = n.Order != null ? n.Order.OrderCode : null,
                NotificationType = n.NotificationType,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedDate = n.CreatedDate
            }).ToListAsync(cancellationToken);

            return Result.Success(items);
        }
    }
}
