using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;
using m3u8_downloader_avalonia.Models;
using m3u8_downloader_avalonia.ViewModels;
using m3u8_downloader_avalonia.deps.M3U8parser;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DialogHost;
using System.Net;
using Avalonia;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            ctx.UploadData = parserResult.Data.UploadData;

            ctx.HeaderModel.Items.Clear();
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
            using (var httpClient = new HttpClient(new HttpClientHandler
                        {
                            AutomaticDecompression = DecompressionMethods.GZip
                                                 | DecompressionMethods.Deflate
                        }))
            {
                using (var request = new HttpRequestMessage(new HttpMethod(ctx.IsPostRequest ? "POST" : "GET"), url))
                {
                    foreach (var header in ctx.HeaderModel.Items)
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }

                    if (ctx.IsPostRequest)
                    {
                        //var dict = ctx.UploadData.ToDictionary((u) => u.Content, (e) => e.Content);
                        //request.Content = new FormUrlEncodedContent(dict);
                        
                        foreach (CurlToCSharp.Models.UploadData uploadData in ctx.UploadData)
                        {
                            request.Content = new StringContent(uploadData.Content.Substring(1), Encoding.UTF8, "application/x-www-form-urlencoded");
                        }
                        
                    }

                    try
                    {
                        return await httpClient.SendAsync(request);
                    }
                    catch (Exception ex)
                    {
                        ShowNotification($"{ex.Message}");
                    }
                }
            }
            throw new ArgumentException(nameof(Download_URL));
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

            string response_m3u8Str = await responseMsg_m3u8.Content.ReadAsStringAsync();

            if (response_m3u8Str[0] == '<')
            {
                ShowNotification("URL does not return JSON");
                return;
            }

            if (await DownloadChunks(response_m3u8Str))
            {
                ShowNotification("Download done");
                ctx.DownloadProgress = 0;

                ctx.DownloadButtonEnabled = true;
            }
        }

        private async void QualitySelector_OnDialogClosing(object? sender, DialogClosingEventArgs e)
        {
            var quality = e.Parameter as M3u8Quality;
            string urlBase = GetBaseURL(quality.Path);

            var qualityUrl = new Uri(quality.Path);
            if (!qualityUrl.IsAbsoluteUri)
            {
                urlBase += "/" + quality.Path;
            }
            else
            {
                urlBase = quality.Path;
            }

            var responseMsg_m3u8 = await Download_URL(urlBase);
            string response_m3u8Str = await responseMsg_m3u8.Content.ReadAsStringAsync();

            await DownloadChunks(response_m3u8Str, qualityUrl: qualityUrl.ToString());
            ShowNotification("Download done");

            ctx.DownloadProgress = 0;

            ctx.DownloadButtonEnabled = true;
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

        private async Task<bool> DownloadChunks(string responseMsg_m3u8, bool showModalIfMediaMissing = true, string qualityUrl = "")
        {
            FileStream fileStream = new(ctx.FilePath, FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);

            //System.Text.Json.JsonSerializer.Deserialize<string>(responseMsg_m3u8);
            System.Text.Json.JsonDocument jsonDocument = System.Text.Json.JsonDocument.Parse(responseMsg_m3u8);

            string? domainrd = "";
            if (jsonDocument.RootElement.TryGetProperty("domainrd", out JsonElement domainrdElem))
            {
                domainrd = domainrdElem.GetString();
            }

            List<string> chunks = new List<string>();

            if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement jsonElement))
            {
                foreach (JsonElement arrElem in jsonElement[1].EnumerateArray())
                {
                    var arr = arrElem.GetString()?.Split("|");
                    if (arr != null)
                    {
                        if (arr[0][0] == '2')
                        {
                            chunks.Add(arr[1]);
                        }
                        if (arr[0][0] == '1')
                        {
                            chunks.Add(domainrd + "/rdv1/" + arr[1]);
                        }
                    }
                }
            }

            /*
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
            */

            string urlReplaced;
            if (qualityUrl != string.Empty)
            {
                urlReplaced = GetBaseURL(qualityUrl);
            }
            else
            {
                urlReplaced = GetBaseURL(ctx.URL);
            }

            var progressChunk = 100.0 / chunks.Count;
            ctx.downloadProgressDouble = 0;

            foreach (string chunkUrl in chunks)
            {
                using (var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                }))
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), chunkUrl))
                    {
                        foreach (var header in ctx.HeaderModel.Items)
                        {
                            if ((header.Name != "Content-Length") || (header.Name != "Content-Type"))
                            {
                                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                            }                            
                        }
                        HttpResponseMessage? responseMsgVideoChunk = await httpClient.SendAsync(request);

                        if (responseMsgVideoChunk.StatusCode == HttpStatusCode.OK)
                        {
                            var str = await responseMsgVideoChunk.Content.ReadAsStringAsync();
                            var index = str.IndexOf("G@");
                            str = str.Substring(index, str.Length - index);

                            streamWriter.Write(str);

                            ctx.downloadProgressDouble += progressChunk;
                            ctx.DownloadProgress = (int)ctx.downloadProgressDouble;
                        }
                    }
                }
            }

            /*
            var progressChunk = 100.0 / m3u8.Medias.Count;
            foreach (var media in m3u8.Medias)
            {
                // download individual video chunks
                using (var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                                            | DecompressionMethods.Deflate
                                            | DecompressionMethods.Brotli
                }))
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

                    try
                    {
                        int retry = 0;
                        while (retry <= 3)
                        {
                            try
                            {
                                await SendChunkRequest(fileStream, progressChunk, httpClient, chunkUrl);
                                break;
                            }
                            catch (Exception ex)
                            {
                                break;
                                retry++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowNotification($"{ex.Message}");
                    }
                }
            }
            */

            streamWriter.Close();
            return true;
        }

        private async Task SendChunkRequest(FileStream fileStream, double progressChunk, HttpClient httpClient, string chunkUrl)
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), chunkUrl))
            {
                foreach (var header in ctx.HeaderModel.Items)
                {
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }

                HttpResponseMessage? responseMsgVideoChunk = await httpClient.SendAsync(request);

                if (responseMsgVideoChunk != null)
                {
                    if (responseMsgVideoChunk.StatusCode != HttpStatusCode.OK)
                    {

                    }

                    try
                    {
                        using (var memstream = new MemoryStream())
                        {
                            await responseMsgVideoChunk.Content.CopyToAsync(memstream);
                            var bytes = default(byte[]);
                            bytes = memstream.ToArray();
                            fileStream.Write(bytes);
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    ctx.downloadProgressDouble += progressChunk;
                    ctx.DownloadProgress = (int)ctx.downloadProgressDouble;
                }
            }
        }

        private string GetBaseURL(string url)
        {
            return url.Substring(0, url.LastIndexOf('/'));
        }

        private void ShowNotification(string text)
        {
            ctx.MsgBoxModalText = text;
            ctx.MsgBoxModalOpened = true;
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