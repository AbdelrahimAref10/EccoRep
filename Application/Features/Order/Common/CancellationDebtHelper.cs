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
    public static class CancellationDebtHelper
    {
        public static async Task<List<CustomerWallet>> GetPendingCancellationFeesAsync(
            DatabaseContext context,
            int customerId,
            CancellationToken cancellationToken)
        {
            return await context.CustomerWallets
                .AsTracking()
                .Where(cw =>
                    cw.CustomerId == customerId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.State == CustomerWalletState.Pending)
                .ToListAsync(cancellationToken);
        }

        public static decimal SumWithdraw(IEnumerable<CustomerWallet> wallets)
            => wallets.Sum(w => w.Withdraw);

        public static void AttachPendingFeesToOrder(IEnumerable<CustomerWallet> pendingFees, int newOrderId)
        {
            foreach (var wallet in pendingFees)
            {
                wallet.MarkAsUnderPayment(newOrderId);
            }
        }

        public static async Task MarkUnderPaymentFeesAsPaidAsync(
            DatabaseContext context,
            int orderId,
            CancellationToken cancellationToken)
        {
            var wallets = await context.CustomerWallets
                .AsTracking()
                .Where(cw =>
                    cw.OrderId == orderId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.State == CustomerWalletState.UnderPayment)
                .ToListAsync(cancellationToken);

            foreach (var wallet in wallets)
            {
                wallet.MarkAsPaid();
            }
        }

        public static async Task RevertUnderPaymentFeesToPendingAsync(
            DatabaseContext context,
            int orderId,
            CancellationToken cancellationToken)
        {
            var wallets = await context.CustomerWallets
                .AsTracking()
                .Where(cw =>
                    cw.OrderId == orderId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.State == CustomerWalletState.UnderPayment)
                .ToListAsync(cancellationToken);

            foreach (var wallet in wallets)
            {
                wallet.RevertToPending();
            }
        }

        /// <summary>
        /// True when this order was cancelled/rejected (not merely carrying prior debt).
        /// </summary>
        public static async Task<bool> IsOrderCancelledAsync(
            DatabaseContext context,
            Domain.Models.Order order,
            CancellationToken cancellationToken)
        {
            if (order.OrderState == OrderState.Cancelled)
                return true;

            // Legacy cancelled rows (before OrderState.Cancelled existed)
            if (order.MoneyRefunded)
                return true;

            if (await context.RefundablePaypalAmounts.AnyAsync(r => r.OrderId == order.OrderId, cancellationToken))
                return true;

            var orderCodeMarker = $"Order #{order.OrderCode}";
            return await context.CustomerWallets.AnyAsync(
                cw => cw.OrderId == order.OrderId
                    && cw.Type == WalletType.OrderCancellationFees
                    && cw.State == CustomerWalletState.Pending
                    && cw.Description.Contains(orderCodeMarker),
                cancellationToken);
        }
    }
}
