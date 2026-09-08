using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Presentation.Hubs
{
    public class AdminNotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            System.Console.WriteLine($"[AdminNotificationHub] Client connected: {Context.ConnectionId}");
            System.Console.WriteLine($"[AdminNotificationHub] User: {Context.UserIdentifier}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            System.Console.WriteLine($"[AdminNotificationHub] Client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                System.Console.WriteLine($"[AdminNotificationHub] Disconnect error: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

