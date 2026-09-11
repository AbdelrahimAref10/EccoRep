using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.MarkMerchantHandoverToDeliveryCommand
{
    public record MarkMerchantHandoverToDeliveryCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public List<int> VehicleIds { get; set; } = new();
    }

    public class MarkMerchantHandoverToDeliveryCommandHandler
        : IRequestHandler<MarkMerchantHandoverToDeliveryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;

        public MarkMerchantHandoverToDeliveryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
        }

        public async Task<Result<bool>> Handle(
            MarkMerchantHandoverToDeliveryCommand request,
            CancellationToken cancellationToken)
        {
            if (request.VehicleIds == null || request.VehicleIds.Count == 0)
                return Result.Failure<bool>("At least one vehicle is required");

            var vehicleIds = request.VehicleIds.Distinct().ToList();
            var isAdmin = _userSession.Roles.Contains(AppRoleNames.SuperAdmin);

            Domain.Models.Merchant? sessionMerchant = null;
            if (!isAdmin)
            {
                sessionMerchant = await _context.Merchants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId, cancellationToken);

                if (sessionMerchant == null)
                    return Result.Failure<bool>("Merchant profile not found for current user");
            }

            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.DeliveryAssigned && order.OrderState != OrderState.OnWay)
                return Result.Failure<bool>($"Cannot mark handover in {order.OrderState} state. Delivery must be assigned first.");

            var deliveryAssignments = await _context.DeliveryMenOrders
                .AsTracking()
                .Where(d => d.OrderId == request.OrderId && vehicleIds.Contains(d.VehicleId))
                .ToListAsync(cancellationToken);

            if (deliveryAssignments.Count != vehicleIds.Count)
                return Result.Failure<bool>("One or more vehicles do not have a delivery assignment");

            var paymentDetails = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .Where(p => p.OrderId == request.OrderId && vehicleIds.Contains(p.VehicleId))
                .ToListAsync(cancellationToken);

            if (paymentDetails.Count != vehicleIds.Count)
                return Result.Failure<bool>("Merchant payment details missing for one or more vehicles");

            if (!isAdmin && paymentDetails.Any(p => p.MerchantId != sessionMerchant!.MerchantId))
                return Result.Failure<bool>("You can only hand over vehicles belonging to your merchant account");

            var merchantIds = paymentDetails.Select(p => p.MerchantId).Distinct().ToList();
            var merchants = await _context.Merchants
                .AsNoTracking()
                .Where(m => merchantIds.Contains(m.MerchantId))
                .ToDictionaryAsync(m => m.MerchantId, cancellationToken);

            var createdBy = _userSession.UserName ?? "System";

            foreach (var assignment in deliveryAssignments)
                assignment.MarkReceivedFromMerchant(createdBy);

            foreach (var merchantGroup in paymentDetails.GroupBy(p => p.MerchantId))
            {
                var merchantId = merchantGroup.Key;
                var merchantVehicleIds = merchantGroup.Select(p => p.VehicleId).OrderBy(id => id).ToList();
                var netAmount = merchantGroup.Sum(p => p.NetAmount);
                if (netAmount <= 0)
                    continue;

                if (!merchants.TryGetValue(merchantId, out var merchant))
                    return Result.Failure<bool>($"Merchant {merchantId} not found");

                var sortedIds = string.Join(",", merchantVehicleIds);

                var rentalResult = await _journal.PostCreditAsync(
                    request.OrderId,
                    LedgerPartyType.Merchant,
                    merchantId,
                    netAmount,
                    OrderJournalEntryKind.MerchantRentalAccrued,
                    OrderJournalKeys.Build(request.OrderId, $"merchant-rental:{merchantId}:vehicles:{sortedIds}"),
                    note: $"Handover vehicles {sortedIds}",
                    createdBy: createdBy,
                    cancellationToken: cancellationToken);

                if (rentalResult.IsFailure)
                    return Result.Failure<bool>(rentalResult.Error);

                if (!merchant.CashOnReceive)
                    continue;

                var cashPaidResult = await _journal.PostDebitAsync(
                    request.OrderId,
                    LedgerPartyType.Merchant,
                    merchantId,
                    netAmount,
                    OrderJournalEntryKind.MerchantPaidByDeliveryCashOnReceive,
                    OrderJournalKeys.Build(request.OrderId, $"merchant-cash-on-receive:{merchantId}:vehicles:{sortedIds}"),
                    note: $"Cash on receive vehicles {sortedIds}",
                    createdBy: createdBy,
                    cancellationToken: cancellationToken);

                if (cashPaidResult.IsFailure)
                    return Result.Failure<bool>(cashPaidResult.Error);

                var vehiclesForMerchant = merchantVehicleIds.ToHashSet();
                foreach (var deliveryGroup in deliveryAssignments
                    .Where(d => vehiclesForMerchant.Contains(d.VehicleId))
                    .GroupBy(d => d.DeliveryId))
                {
                    var deliveryId = deliveryGroup.Key;
                    var deliveryVehicleIds = deliveryGroup.Select(d => d.VehicleId).OrderBy(id => id).ToList();
                    var deliveryNet = merchantGroup
                        .Where(p => deliveryVehicleIds.Contains(p.VehicleId))
                        .Sum(p => p.NetAmount);

                    if (deliveryNet <= 0)
                        continue;

                    var advanceResult = await _journal.PostCreditAsync(
                        request.OrderId,
                        LedgerPartyType.Delivery,
                        deliveryId,
                        deliveryNet,
                        OrderJournalEntryKind.DeliveryCashAdvanceToMerchant,
                        OrderJournalKeys.Build(
                            request.OrderId,
                            $"delivery-cash-advance:{deliveryId}:merchant:{merchantId}:vehicles:{string.Join(",", deliveryVehicleIds)}"),
                        note: $"Cash advance to merchant {merchantId}",
                        createdBy: createdBy,
                        cancellationToken: cancellationToken);

                    if (advanceResult.IsFailure)
                        return Result.Failure<bool>(advanceResult.Error);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
