using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using MyNotifications;

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
        }

        private async void Button_NotificationAccess(object sender, RoutedEventArgs e)
        {
            await NotificationUtils.GetBackgroundPermission();
            await NotificationUtils.GetNotificationPermission();
        }

        private void Button_TTS(object sender, RoutedEventArgs e)
        {
            TTSUtils.TTS("Hello world! This is just a test!", "en");
        }
    }
}
