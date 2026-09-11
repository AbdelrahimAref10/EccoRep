using Application.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetMyMerchantOrdersQuery
{
    public record GetMyMerchantOrdersQuery : IRequest<Result<PagedResult<MerchantPortalOrderListItemDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public OrderState? State { get; set; }
        public string? OrderCode { get; set; }
        /// <summary>Filter by this merchant's invitation status.</summary>
        public MerchantOrderResponseStatus? MyResponseStatus { get; set; }
        /// <summary>Only invitations awaiting Accept/Reject.</summary>
        public bool? PendingOnly { get; set; }
        /// <summary>Only orders with vehicles waiting for handover.</summary>
        public bool? AwaitingHandoverOnly { get; set; }
    }

    public class GetMyMerchantOrdersQueryHandler
        : IRequestHandler<GetMyMerchantOrdersQuery, Result<PagedResult<MerchantPortalOrderListItemDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantOrdersQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<PagedResult<MerchantPortalOrderListItemDto>>> Handle(
            GetMyMerchantOrdersQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<PagedResult<MerchantPortalOrderListItemDto>>(
                    "Merchant profile not found for current user");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

            var invitationsQuery = _context.MerchantOrders
                .AsNoTracking()
                .Where(mo => mo.MerchantId == merchant.MerchantId);

            if (request.MyResponseStatus.HasValue)
                invitationsQuery = invitationsQuery.Where(mo => mo.ResponseStatus == request.MyResponseStatus.Value);

            if (request.PendingOnly == true)
                invitationsQuery = invitationsQuery.Where(mo => mo.ResponseStatus == MerchantOrderResponseStatus.Pending);

            var orderIdsQuery = invitationsQuery.Select(mo => mo.OrderId).Distinct();

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Where(o => orderIdsQuery.Contains(o.OrderId));

            if (request.State.HasValue)
                query = query.Where(o => o.OrderState == request.State.Value);

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
                query = query.Where(o => o.OrderCode.Contains(request.OrderCode.Trim()));

            if (request.AwaitingHandoverOnly == true)
            {
                var awaitingOrderIds = await _context.DeliveryMenOrders
                    .AsNoTracking()
                    .Where(d => !d.DeliveryReceivedFromMerchant)
                    .Join(
                        _context.MerchantOrderPaymentDetails.AsNoTracking()
                            .Where(p => p.MerchantId == merchant.MerchantId),
                        d => new { d.OrderId, d.VehicleId },
                        p => new { p.OrderId, p.VehicleId },
                        (d, _) => d.OrderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                query = query.Where(o =>
                    awaitingOrderIds.Contains(o.OrderId) &&
                    (o.OrderState == OrderState.DeliveryAssigned || o.OrderState == OrderState.OnWay));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pageOrderIds = orders.Select(o => o.OrderId).ToList();

            var invitations = await _context.MerchantOrders
                .AsNoTracking()
                .Where(mo => mo.MerchantId == merchant.MerchantId && pageOrderIds.Contains(mo.OrderId))
                .ToListAsync(cancellationToken);

            var invitationByOrder = invitations.ToDictionary(mo => mo.OrderId);

            var myVehiclesByOrder = await _context.OrderVehicles
                .AsNoTracking()
                .Where(ov => pageOrderIds.Contains(ov.OrderId) && ov.Vehicle.MerchantId == merchant.MerchantId)
                .GroupBy(ov => ov.OrderId)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count, cancellationToken);

            var payments = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .Where(p => p.MerchantId == merchant.MerchantId && pageOrderIds.Contains(p.OrderId))
                .ToListAsync(cancellationToken);

            var paymentsByOrder = payments
                .GroupBy(p => p.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var pendingHandovers = await _context.DeliveryMenOrders
                .AsNoTracking()
                .Where(d => pageOrderIds.Contains(d.OrderId) && !d.DeliveryReceivedFromMerchant)
                .Join(
                    _context.MerchantOrderPaymentDetails.AsNoTracking()
                        .Where(p => p.MerchantId == merchant.MerchantId),
                    d => new { d.OrderId, d.VehicleId },
                    p => new { p.OrderId, p.VehicleId },
                    (d, _) => d)
                .GroupBy(d => d.OrderId)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count, cancellationToken);

            var items = orders.Select(o =>
            {
                invitationByOrder.TryGetValue(o.OrderId, out var invitation);
                paymentsByOrder.TryGetValue(o.OrderId, out var orderPayments);
                orderPayments ??= new List<Domain.Models.MerchantOrderPaymentDetail>();

                var myResponse = invitation?.ResponseStatus ?? MerchantOrderResponseStatus.Pending;
                var pendingHandover = pendingHandovers.GetValueOrDefault(o.OrderId);
                var canActOnInvite = o.OrderState == OrderState.MerchantPending
                    && myResponse == MerchantOrderResponseStatus.Pending;
                var canHandover = pendingHandover > 0
                    && (o.OrderState == OrderState.DeliveryAssigned || o.OrderState == OrderState.OnWay);

                return new MerchantPortalOrderListItemDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    SubCategoryName = o.SubCategory.Name,
                    CityName = o.City.Name,
                    ReservationDateFrom = o.ReservationDateFrom,
                    ReservationDateTo = o.ReservationDateTo,
                    OrderState = o.OrderState,
                    CreatedDate = o.CreatedDate,
                    IsUrgent = o.IsUrgent,
                    MyResponseStatus = myResponse,
                    MyRejectReason = invitation?.RejectReason,
                    MyRespondedAt = invitation?.RespondedAt,
                    MyVehiclesCount = myVehiclesByOrder.GetValueOrDefault(o.OrderId),
                    MyRentalTotal = orderPayments.Sum(p => p.VehicleRental),
                    MyServiceFeeTotal = orderPayments.Sum(p => p.ServiceFeeShare),
                    MyNetTotal = orderPayments.Sum(p => p.NetAmount),
                    CanAccept = canActOnInvite,
                    CanReject = canActOnInvite,
                    PendingHandoverVehicleCount = pendingHandover,
                    CanHandover = canHandover
                };
            }).ToList();

            return Result.Success(new PagedResult<MerchantPortalOrderListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }
}
