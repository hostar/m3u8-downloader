using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;
using m3u8_winui.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using m3u8_winui.deps.M3u8Parser;
using System.Diagnostics;
using System.Text;
using Windows.UI.Notifications;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace m3u8_winui
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel HeaderModel { get; set; } = new MainViewModel();

        private string url;
        public string URL { get { return url; } set
            {
                url = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(URL)));
            }
        }

        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }

        public int downloadProgress { get; set; } = 0;
        public double downloadProgressDouble { get; set; } = 0;

        public bool downloadButtonEnabled { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            Title = "M3U8 Downloader - WinUI";
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var commandLineParser = new CommandLineParser(new ParsingOptions(int.MaxValue));
            //var converterService = new ConverterService();
            var parserResult = commandLineParser.Parse(new Span<char>($"curl {tbCurl.Text}".ToCharArray()));

            URL = parserResult.Data.Url.ToString();
            foreach (var header in parserResult.Data.Headers)
            {
                HeaderModel.Items.Add(new Models.HeaderView() { Name = header.Key, Value = header.Value });
            }

            //var csharp = converterService.ToCsharp(parserResult.Data);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            HeaderModel.Items.Remove((HeaderView)btn.Tag);
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(URL))
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
            downloadButtonEnabled = false;
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(downloadButtonEnabled)));
            HttpResponseMessage responseMsg_m3u8;
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), URL))
                {
                    foreach (var header in HeaderModel.Items)
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }

                    responseMsg_m3u8 = await httpClient.SendAsync(request);
                }
            }

            FileStream fileStream = new FileStream(FilePath, FileMode.Create);

            string response_m3u8 = await responseMsg_m3u8.Content.ReadAsStringAsync();
            var m3u8 = M3u8Parser.Parse(response_m3u8);

            var urlReplaced = URL.Substring(0, URL.LastIndexOf('/'));

            var progressChunk = 100.0 / m3u8.Medias.Count;
            foreach (var media in m3u8.Medias)
            {
                // download individual video chunks
                using (var httpClient = new HttpClient())
                {
                    var chunkUrl = urlReplaced + "/" + media.Path;
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), chunkUrl))
                    {
                        foreach (var header in HeaderModel.Items)
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                        }

                        var responseMsgVideoChunk = await httpClient.SendAsync(request);
                        downloadProgressDouble += progressChunk;
                        downloadProgress = (int)downloadProgressDouble;
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(downloadProgress)));

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
            ShowNotification();

            downloadProgress = 0;
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(downloadProgress)));
            downloadButtonEnabled = true;
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(downloadButtonEnabled)));
        }

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
    }

    
}
