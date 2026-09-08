using Domain.Common;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public NotificationService(ILogger<NotificationService> logger, IDateTimeProvider dateTimeProvider)
        {
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task SendNotificationForSingleDevice(NotificationBody notificationBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notificationBody.FireBaseToken))
                {
                    _logger.LogWarning("Firebase token is empty, skipping notification");
                    return;
                }

                var message = new Message()
                {
                    Token = notificationBody.FireBaseToken,
                    Notification = new Notification
                    {
                        Title = notificationBody.Title,
                        Body = notificationBody.Body
                    },
                    Data = notificationBody.PayLoad,
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            ChannelId = "1",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                            DefaultSound = true,
                            Priority = NotificationPriority.HIGH,
                            EventTimestamp = _dateTimeProvider.Now
                        }
                    }
                };

                var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Firebase result is {0} for device {1}", result, notificationBody.FireBaseToken);
            }
            catch (FirebaseMessagingException fireBaseEx)
            {
                _logger.LogError(fireBaseEx, "Error in Sending Notifications: {ErrorCode}, {ErrorMessage}",
                    fireBaseEx.ErrorCode, fireBaseEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Sending Notifications");
            }
        }

        public async Task SendNotificationAsyncToMultipleDevices(NotificationBodyForMultipleDevices notificationBody)
        {
            try
            {
                var deviceIds = notificationBody.FireBaseTokens
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (deviceIds.Count == 0)
                {
                    _logger.LogWarning("No valid Firebase tokens provided, skipping notification");
                    return;
                }

                var message = new MulticastMessage()
                {
                    Tokens = deviceIds,
                    Notification = new Notification
                    {
                        Title = notificationBody.Title,
                        Body = notificationBody.Body
                    },
                    Data = notificationBody.PayLoad,
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            ChannelId = "1",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                            DefaultSound = true,
                            Priority = NotificationPriority.HIGH,
                            EventTimestamp = _dateTimeProvider.Now
                        }
                    }
                };

                var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation(
                    "Firebase success count: {0}, Firebase failed count: {1}",
                    result.SuccessCount,
                    result.FailureCount
                );

                // Log failed tokens for cleanup
                for (int i = 0; i < result.Responses.Count; i++)
                {
                    if (!result.Responses[i].IsSuccess)
                    {
                        _logger.LogWarning("Failed to send notification to token {Token}: {Error}",
                            deviceIds[i], result.Responses[i].Exception?.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Sending Notifications to Multiple Devices");
            }
        }
    }
}


