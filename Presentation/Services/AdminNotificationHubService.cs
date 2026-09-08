using Application.Features.AdminNotification.Command.CreateAdminNotificationCommand;
using Domain.Enums;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;
using System;
using System.Threading.Tasks;

namespace Presentation.Services
{
    public class AdminNotificationHubService : IAdminNotificationHubService
    {
        private readonly IHubContext<AdminNotificationHub> _hubContext;
        private readonly IMediator _mediator;

        public AdminNotificationHubService(
            IHubContext<AdminNotificationHub> hubContext,
            IMediator mediator)
        {
            _hubContext = hubContext;
            _mediator = mediator;
        }

        public async Task SendNotificationAsync(
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null)
        {
            try
            {
                // Step 1: Create notification in database
                var createCommand = new CreateAdminNotificationCommand
                {
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    OrderId = orderId
                };

                var result = await _mediator.Send(createCommand);

                if (!result.IsSuccess)
                {
                    System.Console.WriteLine($"[AdminNotificationHubService] Failed to create notification: {result.Error}");
                    return;
                }

                var notification = result.Value;

                // Step 2: Prepare notification DTO for SignalR
                var notificationDto = new
                {
                    AdminNotificationId = notification.AdminNotificationId,
                    Title = notification.Title,
                    Message = notification.Message,
                    OrderId = notification.OrderId,
                    NotificationType = notification.NotificationType,
                    IsRead = notification.IsRead,
                    CreatedDate = notification.CreatedDate
                };

                // Step 3: Send notification via SignalR to all connected admin clients
                System.Console.WriteLine("[AdminNotificationHubService] Sending notification to all connected admin clients");
                await _hubContext.Clients.All.SendAsync("NewAdminNotification", notificationDto);
                System.Console.WriteLine("[AdminNotificationHubService] Notification sent to all admin clients");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AdminNotificationHubService] Error sending notification: {ex.Message}");
                System.Console.WriteLine($"[AdminNotificationHubService] Stack trace: {ex.StackTrace}");
            }
        }
    }
}

