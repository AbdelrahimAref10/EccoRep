using Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public interface IMerchantNotificationHubService
    {
        Task SendToMerchantsAsync(
            IEnumerable<int> merchantIds,
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null,
            string? orderCode = null);
    }
}
