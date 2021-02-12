using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace MyNotifications
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Zustand von zuvor angehaltener Anwendung laden
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Wenn der Navigationsstapel nicht wiederhergestellt wird, zur ersten Seite navigieren
                    // und die neue Seite konfigurieren, indem die erforderlichen Informationen als Navigationsparameter
                    // übergeben werden
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }

            if (await NotificationUtils.RegisterBackgroundProcess())
            {
                NotificationUtils.SyncNotifications();
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Navigation auf eine bestimmte Seite fehlschlägt
        /// </summary>
        /// <param name="sender">Der Rahmen, bei dem die Navigation fehlgeschlagen ist</param>
        /// <param name="e">Details über den Navigationsfehler</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            deferral.Complete();
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            switch (args.TaskInstance.Task.Name)
            {
                case "UserNotificationChanged":
                    NotificationUtils.SyncNotifications();
                    break;
            }

            deferral.Complete();
        }
    }

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
                RandomAccessStreamReference appLogoStream = notification.AppInfo.DisplayInfo.GetLogo(new Size(8, 8));
                await appLogo.SetSourceAsync(await appLogoStream.OpenReadAsync());
            }
            catch
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

        public async void PostToUrl(string url = "http://localhost:80")
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri(url);

                // Construct the JSON to post.
                HttpStringContent content = new HttpStringContent(
                    this.ToJson(),
                    UnicodeEncoding.Utf8,
                    "application/json");

                // Post the JSON and wait for a response.
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(
                    uri,
                    content);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                Debug.WriteLine(httpResponseBody);
            }
            catch (Exception ex)
            {
                // Write out any exceptions.
                Debug.WriteLine(ex);
            }
        }
    }

    public static class NotificationUtils
    {
        private static List<uint> currentNotificationIds = new List<uint>();

        private static async void AddNotification(UserNotification notification)
        {
            currentNotificationIds.Add(notification.Id);

            MyNotification notif = await MyNotification.FromUserNotification(notification);
            Debug.WriteLine(notif.ToJson());
            notif.PostToUrl();
        }

        private static void RemoveNotification(uint notificationId)
        {
            Debug.WriteLine("[Delete] " + notificationId.ToString());
            currentNotificationIds.Remove(notificationId);
        }

        public static async void SyncNotifications()
        {
            UserNotificationListener listener = UserNotificationListener.Current;

            IReadOnlyList<UserNotification> userNotifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);
            var toBeRemoved = new List<uint>(currentNotificationIds);

            foreach (UserNotification userNotification in userNotifications)
            {
                if (currentNotificationIds.Contains(userNotification.Id))
                {
                    toBeRemoved.Remove(userNotification.Id);
                }
                else
                {
                    AddNotification(userNotification);
                }
            }

            foreach (uint id in toBeRemoved)
            {
                RemoveNotification(id);
            }
        }

        public static async Task<bool> RegisterBackgroundProcess()
        {
            bool success = await GetBackgroundPermission();
            bool success2 = await GetNotificationPermission();

            if (success && success2)
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

    public class TTSUtils
    {
        public static async void TTS(string text, string lang = "en")
        {
            MediaElement mediaElement = new MediaElement();
            var synth = new SpeechSynthesizer();

            var list = from a in SpeechSynthesizer.AllVoices
                       where a.Language.Contains(lang)
                       select a;

            if (list.Count() > 0)
            {
                synth.Voice = list.Last();
            }

            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);
            mediaElement.SetSource(stream, stream.ContentType);
            mediaElement.Play();
        }
    }
}