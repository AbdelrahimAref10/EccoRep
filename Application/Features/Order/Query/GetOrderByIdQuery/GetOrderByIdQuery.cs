using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
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

        public GetOrderByIdQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .Include(o => o.OrderPayments)
                .Include(o => o.ReservedVehiclesPerDays)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<OrderDetailDto>($"Order with ID {request.OrderId} not found");
            }

            // Get RefundablePaypalAmount if exists
            var refundablePaypal = await _context.RefundablePaypalAmounts
                .FirstOrDefaultAsync(rpa => rpa.OrderId == request.OrderId, cancellationToken);

            // Get OrderTotals if exists
            var orderTotals = await _context.OrderTotals
                .FirstOrDefaultAsync(ot => ot.OrderId == request.OrderId, cancellationToken);

            // Get cancellation fee wallet entry if order was cancelled with fee (not prior debt UnderPayment)
            var orderCodeMarker = $"Order #{order.OrderCode}";
            var cancellationFeeEntry = await _context.CustomerWallets
                .Where(cw => cw.OrderId == request.OrderId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.Description.Contains(orderCodeMarker))
                .FirstOrDefaultAsync(cancellationToken);

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
                OrderVehicles = order.OrderVehicles.Select(ov => new OrderVehicleDto
                {
                    VehicleId = ov.VehicleId,
                    VehicleName = ov.Vehicle.Name,
                    VehicleCode = ov.Vehicle.VehicleCode,
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
                } : null
            };

            return Result.Success(orderDetailDto);
        }
    }
}

