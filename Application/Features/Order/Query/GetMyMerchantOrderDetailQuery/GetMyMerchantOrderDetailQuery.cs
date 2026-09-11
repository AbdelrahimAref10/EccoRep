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

namespace Application.Features.Order.Query.GetMyMerchantOrderDetailQuery
{
    public record GetMyMerchantOrderDetailQuery : IRequest<Result<MerchantPortalOrderDetailDto>>
    {
        public int OrderId { get; set; }
    }

    public class GetMyMerchantOrderDetailQueryHandler
        : IRequestHandler<GetMyMerchantOrderDetailQuery, Result<MerchantPortalOrderDetailDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantOrderDetailQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<MerchantPortalOrderDetailDto>> Handle(
            GetMyMerchantOrderDetailQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<MerchantPortalOrderDetailDto>("Merchant profile not found for current user");

            var invitation = await _context.MerchantOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    mo => mo.OrderId == request.OrderId && mo.MerchantId == merchant.MerchantId,
                    cancellationToken);

            if (invitation == null)
                return Result.Failure<MerchantPortalOrderDetailDto>("Order not found for this merchant");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<MerchantPortalOrderDetailDto>($"Order with ID {request.OrderId} not found");

            var myVehicles = order.OrderVehicles
                .Where(ov => ov.Vehicle.MerchantId == merchant.MerchantId)
                .Select(ov => new MerchantPortalVehicleDto
                {
                    VehicleId = ov.VehicleId,
                    VehicleName = ov.Vehicle.Name,
                    VehicleCode = ov.Vehicle.VehicleCode,
                    Status = (int)ov.Vehicle.Status
                })
                .ToList();

            var myVehicleIds = myVehicles.Select(v => v.VehicleId).ToHashSet();

            var paymentDetails = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .Include(p => p.Merchant)
                .Include(p => p.Vehicle)
                .Where(p => p.OrderId == request.OrderId && p.MerchantId == merchant.MerchantId)
                .ToListAsync(cancellationToken);

            var deliveryAssignments = await _context.DeliveryMenOrders
                .AsNoTracking()
                .Include(d => d.Delivery)
                .Include(d => d.Vehicle)
                .Where(d => d.OrderId == request.OrderId && myVehicleIds.Contains(d.VehicleId))
                .ToListAsync(cancellationToken);

            var journals = await _context.OrderJournals
                .AsNoTracking()
                .Where(j =>
                    j.OrderId == request.OrderId &&
                    j.PartyType == LedgerPartyType.Merchant &&
                    j.PartyId == merchant.MerchantId)
                .OrderBy(j => j.CreatedDate)
                .ThenBy(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);

            var canActOnInvite = order.OrderState == OrderState.MerchantPending
                && invitation.ResponseStatus == MerchantOrderResponseStatus.Pending;

            var pendingHandoverIds = deliveryAssignments
                .Where(d => !d.DeliveryReceivedFromMerchant)
                .Select(d => d.VehicleId)
                .ToList();

            var canHandover = pendingHandoverIds.Count > 0
                && (order.OrderState == OrderState.DeliveryAssigned || order.OrderState == OrderState.OnWay);

            var handovers = myVehicles.Select(v =>
            {
                var assignment = deliveryAssignments.FirstOrDefault(d => d.VehicleId == v.VehicleId);
                return new MerchantPortalHandoverDto
                {
                    VehicleId = v.VehicleId,
                    VehicleCode = v.VehicleCode,
                    DeliveryId = assignment?.DeliveryId,
                    DeliveryName = assignment?.Delivery?.FullName,
                    DeliveryReceivedFromMerchant = assignment?.DeliveryReceivedFromMerchant ?? false,
                    ReceivedFromMerchantAt = assignment?.ReceivedFromMerchantAt,
                    AssignedToDelivery = assignment != null
                };
            }).ToList();

            return Result.Success(new MerchantPortalOrderDetailDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                SubCategoryName = order.SubCategory.Name,
                SubCategoryPrice = order.SubCategory.Price,
                CityName = order.City.Name,
                ReservationDateFrom = order.ReservationDateFrom,
                ReservationDateTo = order.ReservationDateTo,
                VehiclesCount = order.VehiclesCount,
                IsUrgent = order.IsUrgent,
                OrderState = order.OrderState,
                CreatedDate = order.CreatedDate,
                Notes = order.Notes,
                HotelName = order.HotelName,
                HotelAddress = order.HotelAddress,
                HotelPhone = order.HotelPhone,
                CashOnReceive = merchant.CashOnReceive,
                MyResponseStatus = invitation.ResponseStatus,
                MyRejectReason = invitation.RejectReason,
                MyRespondedAt = invitation.RespondedAt,
                CanAccept = canActOnInvite,
                CanReject = canActOnInvite,
                CanHandover = canHandover,
                MyVehicles = myVehicles,
                MyPaymentDetails = paymentDetails.Select(p => new MerchantOrderPaymentDetailDto
                {
                    MerchantOrderPaymentDetailId = p.MerchantOrderPaymentDetailId,
                    OrderId = p.OrderId,
                    MerchantId = p.MerchantId,
                    MerchantName = p.Merchant.FullName,
                    VehicleId = p.VehicleId,
                    VehicleCode = p.Vehicle.VehicleCode,
                    VehicleRental = p.VehicleRental,
                    ServiceFeeShare = p.ServiceFeeShare,
                    NetAmount = p.NetAmount
                }).ToList(),
                MyHandovers = handovers,
                MyJournals = journals.Select(j => new OrderJournalDto
                {
                    OrderJournalId = j.OrderJournalId,
                    OrderId = j.OrderId,
                    PartyType = j.PartyType,
                    PartyId = j.PartyId,
                    Direction = j.Direction,
                    Amount = j.Amount,
                    EntryKind = j.EntryKind,
                    IdempotencyKey = j.IdempotencyKey,
                    FaultParty = j.FaultParty,
                    Note = j.Note,
                    CreatedBy = j.CreatedBy,
                    CreatedDate = j.CreatedDate
                }).ToList(),
                MyRentalTotal = paymentDetails.Sum(p => p.VehicleRental),
                MyServiceFeeTotal = paymentDetails.Sum(p => p.ServiceFeeShare),
                MyNetTotal = paymentDetails.Sum(p => p.NetAmount)
            });
        }
    }
}
