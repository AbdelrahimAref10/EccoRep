using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Common
{
    public static class OrderFleetFinancialHelper
    {
        public static async Task RecalculateFromAssignedVehiclesAsync(
            DatabaseContext context,
            Domain.Models.Order order,
            string actor,
            CancellationToken cancellationToken)
        {
            var city = await context.Cities
                .AsNoTracking()
                .Include(c => c.TieredDiscounts)
                .FirstOrDefaultAsync(c => c.CityId == order.CityId, cancellationToken);

            if (city == null)
                throw new InvalidOperationException("City not found for order pricing");

            var pricing = order.RecalculateTotals(city, actor);

            var orderTotals = await context.OrderTotals
                .AsTracking()
                .FirstOrDefaultAsync(ot => ot.OrderId == order.OrderId, cancellationToken);

            if (orderTotals != null)
                orderTotals.Apply(pricing);
            else
            {
                await context.OrderTotals.AddAsync(
                    OrderTotals.FromPricing(order.OrderId, pricing),
                    cancellationToken);
            }

            var payment = order.OrderPayments?.FirstOrDefault()
                ?? await context.OrderPayments
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.OrderId == order.OrderId, cancellationToken);

            if (payment != null && payment.State is PaymentState.Pending or PaymentState.Failed)
            {
                payment.Update(payment.PaymentMethodId, pricing.Total, actor);
            }

            await RebuildMerchantPaymentDetailsIfPresentAsync(context, order, actor, cancellationToken);
        }

        public static async Task RebuildMerchantPaymentDetailsIfPresentAsync(
            DatabaseContext context,
            Domain.Models.Order order,
            string actor,
            CancellationToken cancellationToken)
        {
            var existing = await context.MerchantOrderPaymentDetails
                .AsTracking()
                .Where(p => p.OrderId == order.OrderId)
                .ToListAsync(cancellationToken);

            if (existing.Count == 0)
                return;

            context.MerchantOrderPaymentDetails.RemoveRange(existing);

            foreach (var ov in order.OrderVehicles)
            {
                if (ov.Vehicle.MerchantId <= 0)
                    throw new InvalidOperationException($"Vehicle {ov.Vehicle.VehicleCode} has no merchant assigned");

                await context.MerchantOrderPaymentDetails.AddAsync(
                    MerchantOrderPaymentDetail.Create(
                        order.OrderId,
                        ov.Vehicle.MerchantId,
                        ov.VehicleId,
                        order.CalculateVehicleRental(ov.Vehicle.Price),
                        actor),
                    cancellationToken);
            }
        }

        public static void SyncInvitation(
            MerchantOrder invitation,
            IReadOnlyCollection<OrderVehicle> merchantVehicles,
            string actor)
        {
            if (merchantVehicles.Count == 0)
                return;

            var allConfirmed = merchantVehicles.All(v => v.MerchantResponseStatus == MerchantVehicleResponseStatus.Confirmed);
            var allDeclined = merchantVehicles.All(v => v.MerchantResponseStatus == MerchantVehicleResponseStatus.Declined);
            var anyConfirmed = merchantVehicles.Any(v => v.MerchantResponseStatus == MerchantVehicleResponseStatus.Confirmed);
            var anyPending = merchantVehicles.Any(v => v.MerchantResponseStatus == MerchantVehicleResponseStatus.Pending);

            if (allConfirmed)
                invitation.Accept(actor);
            else if (allDeclined)
            {
                if (invitation.ResponseStatus != MerchantOrderResponseStatus.Rejected)
                    invitation.Reject("All assigned vehicles declined by merchant", actor);
            }
            else if (anyConfirmed && !anyPending)
                invitation.AcceptPartial(actor);
            else if (anyConfirmed && anyPending)
                invitation.AcceptPartial(actor);
            else
                invitation.ResetToPending(actor);
        }

        public static bool CanMarkMerchantConfirmed(
            Domain.Models.Order order,
            IReadOnlyCollection<MerchantOrder> invitations)
        {
            if (order.OrderVehicles.Count == 0)
                return false;

            if (order.OrderVehicles.Any(ov => ov.MerchantResponseStatus != MerchantVehicleResponseStatus.Confirmed))
                return false;

            var activeMerchantIds = order.OrderVehicles
                .Select(ov => ov.Vehicle.MerchantId)
                .ToHashSet();

            var activeInvites = invitations.Where(mo => activeMerchantIds.Contains(mo.MerchantId)).ToList();
            if (activeInvites.Count == 0)
                return false;

            return activeInvites.All(mo => mo.ResponseStatus == MerchantOrderResponseStatus.Accepted);
        }
    }
}
