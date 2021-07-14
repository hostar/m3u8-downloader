using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;
using m3u8_downloader_avalonia.Models;
using m3u8_downloader_avalonia.ViewModels;
using SimpleM3U8Parser;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;

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

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ctx.URL))
            {
                /*
                var errorDialog = new ContentDialog()
                {
                    Title = "Missing entry",
                    Content = "Must enter URL.",
                    CloseButtonText = "Ok"
                };

                await errorDialog.ShowAsync(ContentDialogPlacement.Popup);
                */
                return;
            }
            ctx.DownloadButtonEnabled = false;

            HttpResponseMessage responseMsg_m3u8;
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), ctx.URL))
                {
                    foreach (var header in ctx.HeaderModel.Items)
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }

                    responseMsg_m3u8 = await httpClient.SendAsync(request);
                }
            }

            FileStream fileStream = new FileStream(ctx.FilePath, FileMode.Create);

            string response_m3u8 = await responseMsg_m3u8.Content.ReadAsStringAsync();
            var m3u8 = M3u8Parser.Parse(response_m3u8);

            var urlReplaced = ctx.URL.Substring(0, ctx.URL.LastIndexOf('/'));

            var progressChunk = 100.0 / m3u8.Medias.Count;
            foreach (var media in m3u8.Medias)
            {
                // download individual video chunks
                using (var httpClient = new HttpClient())
                {
                    var chunkUrl = urlReplaced + "/" + media.Path;
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
            // ShowNotification();

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