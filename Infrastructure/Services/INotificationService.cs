namespace Infrastructure.Services
{
    public interface INotificationService
    {
        Task SendNotificationForSingleDevice(NotificationBody notificationBody);
        Task SendNotificationAsyncToMultipleDevices(NotificationBodyForMultipleDevices notificationBody);
    }

    public class NotificationBody
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string FireBaseToken { get; set; } = string.Empty;
        public Dictionary<string, string> PayLoad { get; set; } = new Dictionary<string, string>();
    }

    public class NotificationBodyForMultipleDevices
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> FireBaseTokens { get; set; } = new List<string>();
        public Dictionary<string, string> PayLoad { get; set; } = new Dictionary<string, string>();
    }
}


