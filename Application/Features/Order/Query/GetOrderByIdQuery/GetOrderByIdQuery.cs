using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetOrderByIdQuery
{
    public record GetOrderByIdQuery : IRequest<Result<OrderDetailDto>>
    {
        public int OrderId { get; set; }
    }

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IImageService _imageService;

        public GetOrderByIdQueryHandler(DatabaseContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<Result<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                        .ThenInclude(v => v.Merchant)
                .Include(o => o.OrderPayments)
                .Include(o => o.ReservedVehiclesPerDays)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<OrderDetailDto>($"Order with ID {request.OrderId} not found");
            }

            var refundablePaypal = await _context.RefundablePaypalAmounts
                .FirstOrDefaultAsync(rpa => rpa.OrderId == request.OrderId, cancellationToken);

            var orderTotals = await _context.OrderTotals
                .FirstOrDefaultAsync(ot => ot.OrderId == request.OrderId, cancellationToken);

            var orderCodeMarker = $"Order #{order.OrderCode}";
            var cancellationFeeEntry = await _context.CustomerWallets
                .Where(cw => cw.OrderId == request.OrderId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.Description.Contains(orderCodeMarker))
                .FirstOrDefaultAsync(cancellationToken);

            var merchantOrders = await _context.MerchantOrders
                .AsNoTracking()
                .Include(mo => mo.Merchant)
                .Where(mo => mo.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var merchantPaymentDetails = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .Include(p => p.Merchant)
                .Include(p => p.Vehicle)
                .Where(p => p.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var deliveryMenOrders = await _context.DeliveryMenOrders
                .AsNoTracking()
                .Include(d => d.Delivery)
                .Include(d => d.Vehicle)
                .Where(d => d.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var deliveryPaymentDetails = await _context.DeliveryOrderPaymentDetails
                .AsNoTracking()
                .Include(d => d.Delivery)
                .Include(d => d.Vehicle)
                .Where(d => d.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var journals = await _context.OrderJournals
                .AsNoTracking()
                .Where(j => j.OrderId == request.OrderId)
                .OrderBy(j => j.CreatedDate)
                .ThenBy(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);

            var orderDetailDto = new OrderDetailDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerMobileNumber = order.Customer.MobileNumber,
                SubCategoryId = order.SubCategoryId,
                SubCategoryName = order.SubCategory.Name,
                SubCategoryPrice = order.SubCategory.Price,
                CityId = order.CityId,
                CityName = order.City.Name,
                ReservationDateFrom = order.ReservationDateFrom,
                ReservationDateTo = order.ReservationDateTo,
                VehiclesCount = order.VehiclesCount,
                OrderSubTotal = order.OrderSubTotal,
                OrderTotal = order.OrderTotal,
                PreviousDebt = order.PreviousDebt,
                MoneyRefunded = order.MoneyRefunded,
                Notes = order.Notes,
                PassportImage = Domain.Models.Order.NormalizePassportImage(order.PassportImage),
                HotelName = order.HotelName,
                HotelAddress = order.HotelAddress,
                HotelPhone = order.HotelPhone,
                IsUrgent = order.IsUrgent,
                PaymentMethod = (PaymentMethod)order.PaymentMethodId,
                OrderState = order.OrderState,
                CreatedDate = order.CreatedDate,
                ReceiptFaultParty = order.ReceiptFaultParty,
                ReceiptRejectNote = order.ReceiptRejectNote,
                OrderVehicles = order.OrderVehicles.Select(ov => new OrderVehicleDto
                {
                    VehicleId = ov.VehicleId,
                    VehicleName = ov.Vehicle.Name,
                    VehicleCode = ov.Vehicle.VehicleCode,
                    ImageUrl = !string.IsNullOrWhiteSpace(ov.Vehicle.ImageUrl)
                        ? _imageService.GetImageUrl(ov.Vehicle.ImageUrl)
                        : null,
                    MerchantId = ov.Vehicle.MerchantId,
                    MerchantName = ov.Vehicle.Merchant?.FullName ?? string.Empty,
                    Status = (int)ov.Vehicle.Status
                }).ToList(),
                OrderPayments = order.OrderPayments.Select(op => new OrderPaymentDto
                {
                    Id = op.Id,
                    OrderId = op.OrderId,
                    PaymentMethod = (PaymentMethod)op.PaymentMethodId,
                    Total = op.Total,
                    State = op.State,
                    CreatedDate = op.CreatedDate
                }).ToList(),
                RefundablePaypalAmount = refundablePaypal != null ? new RefundablePaypalAmountDto
                {
                    Id = refundablePaypal.Id,
                    CustomerId = refundablePaypal.CustomerId,
                    OrderId = refundablePaypal.OrderId,
                    OrderTotal = refundablePaypal.OrderTotal,
                    CancellationFees = refundablePaypal.CancellationFees,
                    RefundableAmount = refundablePaypal.RefundableAmount,
                    State = refundablePaypal.State,
                    CreatedDate = refundablePaypal.CreatedDate
                } : null,
                OrderTotals = orderTotals != null ? new OrderTotalsDto
                {
                    Id = orderTotals.Id,
                    OrderId = orderTotals.OrderId,
                    SubTotal = orderTotals.SubTotal,
                    ServiceFees = orderTotals.ServiceFees,
                    DeliveryFees = orderTotals.DeliveryFees,
                    UrgentFees = orderTotals.UrgentFees,
                    TieredDiscount = orderTotals.TieredDiscount,
                    TotalAfterAllFees = orderTotals.TotalAfterAllFees
                } : null,
                OrderCancellationFee = cancellationFeeEntry != null ? new OrderCancellationFeeInfoDto
                {
                    WalletEntryId = cancellationFeeEntry.Id,
                    Amount = cancellationFeeEntry.Withdraw,
                    State = cancellationFeeEntry.State
                } : null,
                MerchantOrders = merchantOrders.Select(mo => new MerchantOrderDto
                {
                    MerchantOrderId = mo.MerchantOrderId,
                    OrderId = mo.OrderId,
                    MerchantId = mo.MerchantId,
                    MerchantName = mo.Merchant.FullName,
                    ResponseStatus = mo.ResponseStatus,
                    RejectReason = mo.RejectReason,
                    RespondedAt = mo.RespondedAt,
                    CreatedDate = mo.CreatedDate
                }).ToList(),
                MerchantOrderPaymentDetails = merchantPaymentDetails.Select(p => new MerchantOrderPaymentDetailDto
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
                DeliveryMenOrders = deliveryMenOrders.Select(d => new DeliveryMenOrderDto
                {
                    DeliveryMenOrderId = d.DeliveryMenOrderId,
                    OrderId = d.OrderId,
                    VehicleId = d.VehicleId,
                    VehicleCode = d.Vehicle.VehicleCode,
                    DeliveryId = d.DeliveryId,
                    DeliveryName = d.Delivery.FullName,
                    DeliveryReceivedFromMerchant = d.DeliveryReceivedFromMerchant,
                    ReceivedFromMerchantAt = d.ReceivedFromMerchantAt
                }).ToList(),
                DeliveryOrderPaymentDetails = deliveryPaymentDetails.Select(d => new DeliveryOrderPaymentDetailDto
                {
                    DeliveryOrderPaymentDetailId = d.DeliveryOrderPaymentDetailId,
                    OrderId = d.OrderId,
                    DeliveryId = d.DeliveryId,
                    DeliveryName = d.Delivery.FullName,
                    VehicleId = d.VehicleId,
                    VehicleCode = d.Vehicle.VehicleCode,
                    DeliveryFeeShare = d.DeliveryFeeShare
                }).ToList(),
                OrderJournals = journals.Select(j => new OrderJournalDto
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
                }).ToList()
            };

            return Result.Success(orderDetailDto);
        }
    }
}
