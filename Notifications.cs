using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MyNotifications
{
    [Serializable]
    public class MyNotification
    {
        public readonly uint id;
        public readonly string appName;
        public readonly WriteableBitmap appLogo;

        public readonly string title;
        public readonly string description;

        public MyNotification(uint id, string appName, WriteableBitmap appLogo, string title = "", string description = "")
        {
            this.id = id;
            this.appName = appName;
            this.appLogo = appLogo;
            this.title = title;
            this.description = description;
        }

        public static async Task<MyNotification> FromUserNotification(UserNotification notification)
        {
            string appName = notification.AppInfo.DisplayInfo.DisplayName;

            // Get the app's logo
            WriteableBitmap appLogo = new WriteableBitmap(8, 8);
            try
            {
                var appLogoStream = notification.AppInfo.DisplayInfo.GetLogo(new Size(8, 8));
                var stream = await appLogoStream.OpenReadAsync();
                await appLogo.SetSourceAsync(stream);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Error getting BitmapImage!");
            }

            // Get the toast binding, if present
            NotificationBinding toastBinding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);

            if (toastBinding != null)
            {
                IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();

                string title = textElements.FirstOrDefault()?.Text;
                string description = string.Join("\n", textElements.Skip(1).Select(t => t.Text));

                return new MyNotification(notification.Id, appName, appLogo, title, description);
            }
            else
            {
                return new MyNotification(notification.Id, appName, appLogo);
            }
        }

        public override string ToString()
        {
            return $"[{id} by {appName}] {title}: {description}";
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public static class NotificationUtils
    {
        public static readonly string DEFAULT_APIURL = "http://localhost:80/notifications";

        private static List<uint> CurrentNotificationIds
        {
            get { return ((Newtonsoft.Json.Linq.JArray) Settings.GetSerialized("CURRENTNOTIFICATIONIDS", new Newtonsoft.Json.Linq.JArray())).ToObject<List<uint>>(); }
            set { Settings.SetSerialized("CURRENTNOTIFICATIONIDS", value); }
        }

        private static async Task<bool> AddNotification(UserNotification notification)
        {
            MyNotification notif = await MyNotification.FromUserNotification(notification);
            string content = notif.ToJson();
            string url = ((string)Settings.Get("APIURL", DEFAULT_APIURL)) + "?mode=add";
            bool success = await Requests.Post(url, content);
            return success;
        }

        private static async Task<bool> RemoveNotification(uint notificationId)
        {
            string url = ((string)Settings.Get("APIURL", DEFAULT_APIURL)) + "?mode=delete&id=" + notificationId.ToString();
            bool success = await Requests.Delete(url);
            return success;
        }
        
        public static async Task<bool> ClearNotifications()
        {
            CurrentNotificationIds = new List<uint>();

            string url = ((string)Settings.Get("APIURL", DEFAULT_APIURL)) + "?mode=clear";
            string content = "{\"clear\": true}";
            bool success = await Requests.Post(url, content);
            return success;
        }

        public static async Task SyncNotifications()
        {
            List<uint> notificationIds = CurrentNotificationIds;
            List<uint> toBeRemoved = new List<uint>(notificationIds);

            UserNotificationListener listener = UserNotificationListener.Current;

            IReadOnlyList<UserNotification> userNotifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);

            foreach (UserNotification userNotification in userNotifications)
            {
                if (notificationIds.Contains(userNotification.Id))
                {
                    toBeRemoved.Remove(userNotification.Id);
                }
                else
                {
                    _ = AddNotification(userNotification);
                    notificationIds.Add(userNotification.Id);
                }
            }

            foreach (uint id in toBeRemoved)
            {
                _ = RemoveNotification(id);
                notificationIds.Remove(id);
            }

            CurrentNotificationIds = notificationIds;
            return;
        }

        // API Check

        public static async Task<bool> CheckAPIConnection()
        {
            string url = ((string)Settings.Get("APIURL", DEFAULT_APIURL)) + "?mode=test";
            string content = await Requests.Get(url);
            return (bool)(content != "ERROR");
        }


        // Permissions & Background task

        public static async Task<bool> RegisterBackgroundProcess()
        {
            bool backgroundPermission = await GetBackgroundPermission();
            bool notificationPermission = await GetNotificationPermission();

            if (backgroundPermission && notificationPermission)
            {
                // If background task isn't registered yet
                if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals("UserNotificationChanged")))
                {
                    // Specify the background task
                    var builder = new BackgroundTaskBuilder()
                    {
                        Name = "UserNotificationChanged"
                    };

                    builder.SetTrigger(new UserNotificationChangedTrigger(NotificationKinds.Toast));
                    builder.Register();

                    Debug.WriteLine("Background process registered!");
                }

                return true;
            }
            else
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "No permissions",
                    Content = "I'm not usable if I don't have Notification and Background permissions!",
                    CloseButtonText = "Cancel"
                };

                ContentDialogResult result = await dialog.ShowAsync();

                return false;
            }
        }

        public static async Task<bool> GetBackgroundPermission()
        {
            BackgroundAccessStatus accessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            switch (accessStatus)
            {
                case BackgroundAccessStatus.DeniedBySystemPolicy:
                    Debug.WriteLine("Background Access DENIED by SystemPolicy!");
                    return false;

                case BackgroundAccessStatus.DeniedByUser:
                    Debug.WriteLine("Background Access DENIED by User!");
                    return false;

                case BackgroundAccessStatus.Unspecified:
                    Debug.WriteLine("Background Access Unspecified!");
                    return false;

                default:
                    Debug.WriteLine("Background Access ALLOWED: " + accessStatus.ToString());
                    return true;
            }
        }

        public static async Task<bool> GetNotificationPermission()
        {
            UserNotificationListener listener = UserNotificationListener.Current;
            UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

            switch (accessStatus)
            {
                case UserNotificationListenerAccessStatus.Allowed:
                    Debug.WriteLine("Notification Access ALLOWED!");
                    return true;

                case UserNotificationListenerAccessStatus.Denied:
                    Debug.WriteLine("Notification Access DENIED!");
                    return false;

                case UserNotificationListenerAccessStatus.Unspecified:
                    Debug.WriteLine("Notification Access UNSPECIFIED!");
                    return false;

                default:
                    Debug.WriteLine("This shouldn't be happening!");
                    return false;
            }
        }
    }
}