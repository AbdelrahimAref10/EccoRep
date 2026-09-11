using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class MerchantNotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            System.Console.WriteLine($"[MerchantNotificationHub] Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            System.Console.WriteLine($"[MerchantNotificationHub] Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
