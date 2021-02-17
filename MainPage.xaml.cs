using System;
using System.Diagnostics;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace MyNotifications
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                // Listener supported!
                Debug.WriteLine("Listener Supported");
            }
            else
            {
                // Older version of Windows, no Listener
                Debug.WriteLine("Listener not Supported!");
            }

            LoadSettings();
        }

        private async void Button_ResetNotifications(object sender, RoutedEventArgs e)
        {
            await NotificationUtils.ClearNotifications();
            await NotificationUtils.SyncNotifications();

            ContentDialog dialog = new ContentDialog
            {
                Title = "Reset done",
                Content = "Your notifications have been resent to the api!",
                CloseButtonText = "Ok"
            };

            ContentDialogResult result = await dialog.ShowAsync();
        }

        private async void Button_SaveSettings(object sender, RoutedEventArgs e)
        {
            Settings.Set("APIURL", Input_APIURL.Text);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Settings saved",
                Content = "Your settings have been saved!",
                CloseButtonText = "Ok"
            };

            ContentDialogResult result = await dialog.ShowAsync();
        }

        private async void Button_CheckAPIConnection(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = await NotificationUtils.CheckAPIConnection();

                ContentDialog dialog = new ContentDialog
                {
                    Title = success ? "Check succeeded!" : "Check failed!",
                    Content = "API connection check finished.",
                    CloseButtonText = "Close"
                };

                _ = await dialog.ShowAsync();
            } 
            catch
            {

            }
        }

        private void LoadSettings()
        {
            Input_APIURL.Text = (string)Settings.Get("APIURL", NotificationUtils.DEFAULT_APIURL);
        }
    }
}