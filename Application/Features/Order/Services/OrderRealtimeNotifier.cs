using Domain.Enums;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Application.Features.Order.Services
{
    public interface IOrderRealtimeNotifier
    {
        Task NotifyAsync(
            int orderId,
            string title,
            string message,
            NotificationType notificationType,
            IEnumerable<int>? merchantIds = null,
            bool notifyMerchants = true,
            CancellationToken cancellationToken = default);
    }

    public class OrderRealtimeNotifier : IOrderRealtimeNotifier
    {
        private readonly DatabaseContext _context;
        private readonly IAdminNotificationHubService _adminNotifications;
        private readonly IMerchantNotificationHubService _merchantNotifications;

        public OrderRealtimeNotifier(
            DatabaseContext context,
            IAdminNotificationHubService adminNotifications,
            IMerchantNotificationHubService merchantNotifications)
        {
            _context = context;
            _adminNotifications = adminNotifications;
            _merchantNotifications = merchantNotifications;
        }

        public async Task NotifyAsync(
            int orderId,
            string title,
            string message,
            NotificationType notificationType,
            IEnumerable<int>? merchantIds = null,
            bool notifyMerchants = true,
            CancellationToken cancellationToken = default)
        {
            var orderCode = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            try
            {
                await _adminNotifications.SendNotificationAsync(
                    title,
                    message,
                    notificationType,
                    orderId,
                    orderCode);
            }
            catch
            {
                // Live notify must not fail the command.
            }

            if (!notifyMerchants)
                return;

            var ids = merchantIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();

            var invited = await _context.MerchantOrders
                .AsNoTracking()
                .Where(mo => mo.OrderId == orderId)
                .Select(mo => mo.MerchantId)
                .Distinct()
                .ToListAsync(cancellationToken);

            ids.AddRange(invited);

            if (ids.Count == 0)
            {
                ids = await _context.OrderVehicles
                    .AsNoTracking()
                    .Where(ov => ov.OrderId == orderId)
                    .Select(ov => ov.Vehicle.MerchantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            else
            {
                ids = ids.Distinct().ToList();
            }

            if (ids.Count == 0)
                return;

            try
            {
                await _merchantNotifications.SendToMerchantsAsync(
                    ids,
                    title,
                    message,
                    notificationType,
                    orderId,
                    orderCode);
            }
            catch
            {
            }
        }
    }
}
