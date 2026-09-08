using Domain.Enums;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public interface IAdminNotificationHubService
    {
        Task SendNotificationAsync(
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null);
    }
}

