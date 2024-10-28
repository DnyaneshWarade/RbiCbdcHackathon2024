using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BharatEpaisaApp.Database.Models
{
    public class PushNotificationReceived : ValueChangedMessage<string>
    {
        public PushNotificationReceived(string message) : base(message) { }
    }
}
