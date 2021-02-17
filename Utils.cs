using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace MyNotifications
{
    public static class Requests
    {
        private static bool shown = false;

        private static async void ShowError()
        {
            if (!shown)
            {
                try
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Request failed",
                        Content = "The API endpoint could not be reached or another error occured!",
                        CloseButtonText = "Close"
                    };

                    ContentDialogResult result = await dialog.ShowAsync();

                    shown = true;
                }
                catch (Exception)
                {

                }
            }
        }

        public static async Task<string> Get(string url)
        {
            Debug.WriteLine("[Get] " + url);
            try
            {
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri(url);

                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                    uri);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                Debug.WriteLine("Request suceeded: " + httpResponseBody);
                return httpResponseBody;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request failed:");
                Debug.WriteLine(ex);

                ShowError();

                return "ERROR";
            }
        }

        public static async Task<bool> Delete(string url)
        {
            Debug.WriteLine("[Delete] " + url);
            try
            {
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri(url);

                HttpResponseMessage httpResponseMessage = await httpClient.DeleteAsync(
                    uri);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                Debug.WriteLine("Request suceeded: " + httpResponseBody);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request failed:");
                Debug.WriteLine(ex);

                ShowError();

                return false;
            }
        }

        public static async Task<bool> Post(string url, string json)
        {
            Debug.WriteLine("[Post] " + url);
            try
            {
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri(url);

                // Construct the JSON to post.
                HttpStringContent content = new HttpStringContent(
                    json,
                    UnicodeEncoding.Utf8,
                    "application/json");

                // Post the JSON and wait for a response.
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(
                    uri,
                    content);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                Debug.WriteLine("Request suceeded: " + httpResponseBody);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request failed:");
                Debug.WriteLine(ex);

                ShowError();

                return false;
            }
        }
    }

    public static class Settings
    {
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static object Get(string id, object defaultvalue = null)
        {
            object value = localSettings.Values[id];
            return value is null ? defaultvalue : value;
        }

        public static void Set(string id, object value)
        {
            Debug.WriteLine("[Save] '" + id + "':");
            Debug.WriteLine(value);
            localSettings.Values[id] = value;
        }
    }

    public static class TTS
    {
        public static async void Say(string text, string lang = "en")
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