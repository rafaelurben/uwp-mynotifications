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

                Debug.WriteLine("Request suceeded: "+httpResponseBody);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request failed:");
                Debug.WriteLine(ex);
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
                return false;
            }
        }
    }

    public static class Settings
    {
        static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static object Get(string id)
        {
            return localSettings.Values[id];
        }

        public static void Set(string id, object value)
        {
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