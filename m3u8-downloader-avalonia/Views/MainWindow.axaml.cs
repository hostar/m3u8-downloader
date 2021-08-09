using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Dialogs;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;
using m3u8_downloader_avalonia.Models;
using m3u8_downloader_avalonia.ViewModels;
using m3u8_downloader_avalonia.deps.M3U8parser;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DialogHost;

namespace m3u8_downloader_avalonia.Views
{
    public partial class MainWindow : Window
    {
        MainWindowViewModel ctx;

        public MainWindow()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void DoTheThing()
        {
            ctx = (MainWindowViewModel)DataContext;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var commandLineParser = new CommandLineParser(new ParsingOptions(int.MaxValue));

            var tbCurl = this.FindControl<TextBox>("tbCurl");
            var parserResult = commandLineParser.Parse(new Span<char>($"curl {tbCurl.Text}".ToCharArray()));

            if (!parserResult.Success)
            {
                return;
            }
            ctx.URL = parserResult.Data.Url.ToString();
            foreach (var header in parserResult.Data.Headers)
            {
                ctx.HeaderModel.Items.Add(new HeaderView() { Name = header.Key, Value = header.Value });
            }
        }

        private void DeleteHeader_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            ctx.HeaderModel.Items.Remove((HeaderView)btn.Tag);
        }

        private void AddHeader_Click(object sender, RoutedEventArgs e)
        {
            ctx.HeaderModel.Items.Add(new HeaderView());
        }

        private async Task<HttpResponseMessage> Download_URL(string url)
        {
            HttpResponseMessage responseMsg_m3u8;
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), url))
                {
                    foreach (var header in ctx.HeaderModel.Items)
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }

                    responseMsg_m3u8 = await httpClient.SendAsync(request);
                }
            }
            return responseMsg_m3u8;
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ctx.URL))
            {
                ShowNotification("Must enter URL");
                return;
            }
            ctx.DownloadButtonEnabled = false;

            HttpResponseMessage responseMsg_m3u8;
            try
            {
                responseMsg_m3u8 = await Download_URL(ctx.URL);
            }
            catch (Exception)
            {
                ShowNotification("URL unreachable.");
                ctx.DownloadButtonEnabled = true;
                return;
            }

            if (string.IsNullOrEmpty(ctx.FilePath))
            {
                await OpenSaveDialog();
            }

            if (await DownloadChunks(responseMsg_m3u8))
            {
                ShowNotification("Download done");
                ctx.DownloadProgress = 0;

                ctx.DownloadButtonEnabled = true;
            }
        }

        private async Task OpenSaveDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            ctx.FilePath = await saveFileDialog.ShowAsync(this);
        }

        private async void OpenSaveDialog_Click(object sender, RoutedEventArgs e)
        {
            await OpenSaveDialog();
        }

        private async Task<bool> DownloadChunks(HttpResponseMessage responseMsg_m3u8, bool showModalIfMediaMissing = true)
        {
            FileStream fileStream = new FileStream(ctx.FilePath, FileMode.Create);
            string response_m3u8 = await responseMsg_m3u8.Content.ReadAsStringAsync();
            var m3u8 = M3u8Parser.Parse(response_m3u8);

            if (m3u8.Medias.Count == 0)
            {
                if (m3u8.Qualities.Count > 0)
                {
                    if (showModalIfMediaMissing)
                    {
                        ctx.QualityList = m3u8.Qualities;
                        ctx.QualityModalOpened = true;
                    }
                    return false;
                }
                ShowNotification("URL does not contain any media."); // TODO: this method should not show any GUI
                ctx.DownloadButtonEnabled = true;
                return false;
            }
            string urlReplaced = GetBaseURL(ctx.URL);

            var progressChunk = 100.0 / m3u8.Medias.Count;
            foreach (var media in m3u8.Medias)
            {
                // download individual video chunks
                using (var httpClient = new HttpClient())
                {
                    var chunkUrl = string.Empty;

                    if (Uri.IsWellFormedUriString(media.Path, UriKind.Absolute))
                    {
                        chunkUrl = media.Path;
                    }
                    else
                    {
                        chunkUrl = urlReplaced + "/" + media.Path;
                    }

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), chunkUrl))
                    {
                        foreach (var header in ctx.HeaderModel.Items)
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                        }

                        var responseMsgVideoChunk = await httpClient.SendAsync(request);
                        ctx.downloadProgressDouble += progressChunk;
                        ctx.DownloadProgress = (int)ctx.downloadProgressDouble;

                        using (var memstream = new MemoryStream())
                        {
                            await responseMsgVideoChunk.Content.CopyToAsync(memstream);
                            var bytes = default(byte[]);
                            bytes = memstream.ToArray();
                            fileStream.Write(bytes);
                        }
                    }
                }
            }

            fileStream.Close();
            return true;
        }

        private string GetBaseURL(string url)
        {
            return ctx.URL.Substring(0, url.LastIndexOf('/'));
        }

        private void ShowNotification(string text)
        {
            ctx.MsgBoxModalText = text;
            ctx.MsgBoxModalOpened = true;
        }

        private async void QualitySelector_OnDialogClosing(object? sender, DialogClosingEventArgs e)
        {
            var quality = e.Parameter as M3u8Quality;
            string urlReplaced = GetBaseURL(ctx.URL);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var responseMsg_m3u8 = await Download_URL(urlReplaced + "/" + quality.Path);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            await DownloadChunks(responseMsg_m3u8);
            ShowNotification("Download done");

            ctx.DownloadProgress = 0;

            ctx.DownloadButtonEnabled = true;
        }

        /*
        private void ShowNotification()
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode("Download done"));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode($"File {FilePath} was downloaded"));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }
        */
    }
}