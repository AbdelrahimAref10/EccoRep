using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Presentation.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation.Services
{
    public class MerchantNotificationHubService : IMerchantNotificationHubService
    {
        private readonly IHubContext<MerchantNotificationHub> _hubContext;
        private readonly DatabaseContext _context;

        public MerchantNotificationHubService(
            IHubContext<MerchantNotificationHub> hubContext,
            DatabaseContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public async Task SendToMerchantsAsync(
            IEnumerable<int> merchantIds,
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null,
            string? orderCode = null)
        {
            try
            {
                var ids = merchantIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();
                if (ids.Count == 0)
                    return;

                var notifications = ids.Select(merchantId =>
                    MerchantNotification.Create(
                        merchantId,
                        title,
                        message,
                        notificationType,
                        orderId,
                        "System")).ToList();

                _context.MerchantNotifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                foreach (var notification in notifications)
                {
                    var dto = new
                    {
                        MerchantNotificationId = notification.MerchantNotificationId,
                        MerchantId = notification.MerchantId,
                        Title = notification.Title,
                        Message = notification.Message,
                        OrderId = notification.OrderId,
                        OrderCode = orderCode,
                        NotificationType = notification.NotificationType,
                        IsRead = notification.IsRead,
                        CreatedDate = notification.CreatedDate
                    };

                    // Broadcast to all; merchant clients filter by their merchantId.
                    await _hubContext.Clients.All.SendAsync("NewMerchantNotification", dto);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[MerchantNotificationHubService] Error: {ex.Message}");
            }
        }
    }
}
