using CurlToCSharp.Models.Parsing;
using m3u8_downloader_photino.Libs;
using m3u8_downloader_photino.Libs.curl;
using m3u8_downloader_photino.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Photino.Blazor;
using Radzen;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace m3u8_downloader_photino.Pages
{
    public partial class Index
    {
        private string importCurlTextArea = "";
        private string downloadPath = "";
        private string url = "";
        private string overlayDisplay = "none";
        private double _downloadProgressBarValue;

        private string overlayHeight = "500px";

        private object threadLock = new object();
        private double DownloadProgressBarValue
        {
            get
            {
                var x = (int)_downloadProgressBarValue;
                return x;
            }
        }

        private bool doNotConnectChunks = false;

        private int parallelDownloadThreadsCount = 5;

        private List<string> progressLog = new List<string>();

        private RadzenDataGrid<HttpHeaderView> headersGrid;
        private List<HttpHeaderView> headers = new List<HttpHeaderView>();

        private List<M3u8Quality> qualities = new List<M3u8Quality>();
        private M3u8Quality selectedQuality;

        private Settings Settings;

        protected override Task OnInitializedAsync()
        {
            Program.MainWindow.WindowClosing += MainWindow_WindowClosing;

            if (File.Exists("settings.json"))
            {
                Settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(new FileStream("settings.json", FileMode.Open));
            }
            else
            {
                Settings = new Settings();
            }
            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var heightNum =  await jsRuntime.InvokeAsync<double>("getHeadersHeight");
            heightNum += 55;
            overlayHeight = $"{(int)heightNum}px";
        }

        private bool MainWindow_WindowClosing(object sender, EventArgs e)
        {
            var settingsStr = System.Text.Json.JsonSerializer.Serialize(Settings);
            File.WriteAllText("settings.json", settingsStr);
            return false;
        }

        private void ImportCurl()
        {
            var commandLineParser = new CommandLineParser(new ParsingOptions(int.MaxValue));
            var parserResult = commandLineParser.Parse(new Span<char>(importCurlTextArea.ToCharArray()));

            if (!parserResult.Success)
            {
                return;
            }
            url = parserResult.Data.Url.ToString();

            headers.Clear();
            foreach (var header in parserResult.Data.Headers)
            {
                headers.Add(new HttpHeaderView() { Name = header.Key, Value = header.Value });
            }
            headersGrid.Reload();
            StateHasChanged();
        }

        private async Task InsertHeaderRow()
        {
            /*
            headers.Add(new HttpHeaderView { Name = "", Value = "" });
            await headersGrid.Reload();
            */

            var header = new HttpHeaderView();
            await headersGrid.InsertRow(header);

            headers.Add(header);
        }

        private async Task EditRow(HttpHeaderView header)
        {
            await headersGrid.EditRow(header);
        }

        private async Task SaveRow(HttpHeaderView header)
        {
            await headersGrid.UpdateRow(header);
        }

        private void CancelEdit(HttpHeaderView header)
        {
            headersGrid.CancelEditRow(header);
        }

        private async Task DeleteRow(HttpHeaderView header)
        {
            headers.Remove(header);
            await headersGrid.Reload();
        }

        void OnUpdateRow(HttpHeaderView header)
        {
            //headers.Add(header);
        }

        private async Task StartDownload()
        {
            if (string.IsNullOrEmpty(url))
            {
                ShowNotification("URL cannot be empty.", NotificationSeverity.Warning);
                return;
            }
            if (string.IsNullOrEmpty(downloadPath))
            {
                ShowNotification("Download path cannot be empty.", NotificationSeverity.Warning);
                return;
            }
            overlayDisplay = "block";

            progressLog.Clear();

            HttpResponseMessage responseMsg_m3u8;
            try
            {
                responseMsg_m3u8 = await DownloadURL(url);
            }
            catch (Exception)
            {
                ShowNotification("URL unreachable.", NotificationSeverity.Error);
                overlayDisplay = "none";
                StateHasChanged();
                return;
            }

            progressLog.Add($"Content of {url} downloaded.");
            string response_m3u8Str = await responseMsg_m3u8.Content.ReadAsStringAsync();

            if (await DownloadChunks(response_m3u8Str, url))
            {
                ShowNotification("Download done");
                _downloadProgressBarValue = 0;

                overlayDisplay = "none";
            }
        }

        private void Browse_SelectDestication()
        {
            downloadPath = Program.MainWindow.ShowSaveFile();
        }

        private void ShowNotification(string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            NotificationMessage msg = new NotificationMessage { Style = "position: absolute; left: -98vw; top: 20px;", Severity = severity, Summary = message, Duration = 5000 };
            notificationService.Notify(msg);
        }

        private async Task<HttpResponseMessage> DownloadURL(string url)
        {
            using (var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), url))
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }

                    try
                    {
                        return await httpClient.SendAsync(request);
                    }
                    catch (Exception ex)
                    {
                        ShowNotification($"{ex.Message}", NotificationSeverity.Error);
                    }
                }
            }
            throw new ArgumentException(nameof(DownloadURL));
        }

        private async Task<bool> DownloadChunks(string responseMsg_m3u8, string url)
        {
            M3u8MediaContainer m3u8;
            try
            {
                m3u8 = M3u8Parser.Parse(responseMsg_m3u8);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error when parsing m3u: {ex.Message}");
                overlayDisplay = "none";
                return false;
            }

            if (m3u8?.Medias.Count == 0)
            {
                if (m3u8.Qualities.Count > 0)
                {
                    if ((m3u8.Qualities.Count == 1) && Settings.SelectFirstQuality)
                    {
                        selectedQuality = m3u8.Qualities[0];
                        progressLog.Add($"Autoselected quality {selectedQuality.Quality}");
                    }
                    else
                    {
                        qualities = m3u8.Qualities;
                        await ShowQualityDialog();

                        if (selectedQuality == null)
                        {
                            selectedQuality = qualities[0];
                        }
                        progressLog.Add($"Downloading quality {selectedQuality.Quality}");
                    }
                }
                else
                {
                    ShowNotification("URL does not contain any media.");
                    overlayDisplay = "none";
                    return false;
                }
            }

            string urlReplaced;
            if (selectedQuality != null)
            {
                string qualityUrl = selectedQuality.Path;
                if (!Uri.IsWellFormedUriString(selectedQuality.Path, UriKind.Absolute))
                {
                    qualityUrl = GetBaseURL(url) + "/" + selectedQuality.Path;
                }
                urlReplaced = GetBaseURL(qualityUrl);

                var resp = await DownloadURL(qualityUrl);
                responseMsg_m3u8 = await resp.Content.ReadAsStringAsync();
                m3u8 = M3u8Parser.Parse(responseMsg_m3u8);
            }
            else
            {
                urlReplaced = GetBaseURL(url);
            }

            _downloadProgressBarValue = 0;

            progressLog.Add($"Starting to download chunks...");

            var progressChunk = 100.0 / m3u8.Medias.Count;
            FileStream fileStream = new(downloadPath, FileMode.Create);
            BinaryWriter fileWriter = new BinaryWriter(fileStream);

            Program.Context = SynchronizationContext.Current;

            if (m3u8.Init != null)
            {
                var memStream = new MemoryStream();

                await DownloadChunk(urlReplaced, progressChunk, memStream, m3u8.Init, 1);

                memStream.Seek(0, SeekOrigin.Begin);
                fileWriter.Write(memStream.ToArray());
            }

            if (Settings.ParallelDownload)
            {
                await DownloadInParallel(m3u8, urlReplaced, progressChunk, fileWriter);
            }
            else
            {
                int i = 0;
                foreach (var media in m3u8.Medias)
                {
                    var memStream = new MemoryStream();
                    await DownloadChunk(urlReplaced, progressChunk, memStream, media.Path, i);
                    i++;

                    memStream.Seek(0, SeekOrigin.Begin);
                    fileWriter.Write(memStream.ToArray());

                    StateHasChanged();
                }
            }

            fileStream.Close();
            return true;
        }

        private async Task DownloadInParallel(M3u8MediaContainer m3u8, string urlReplaced, double progressChunk, BinaryWriter fileWriter)
        {
            int i = 0;
            while (i < m3u8.Medias.Count)
            {
                var medias = m3u8.Medias.Skip(i).Take(parallelDownloadThreadsCount).ToList();
                List<Task> tasks = new List<Task>();

                int to = i + parallelDownloadThreadsCount - 1;
                if (to > medias.Count - 1)
                {
                    to = medias.Count - 1;
                }

                var tmpChunks = Enumerable.Range(0, medias.Count).Select(i => new MemoryStream()).ToList();

                for (int j = 0; j <= to; j++)
                {
                    int tmpJ = j;

                    tasks.Add(Task.Run(async () => await DownloadChunk(urlReplaced, progressChunk, tmpChunks[tmpJ], medias[tmpJ].Path, i + tmpJ)));
                }

                //Task.WaitAll(tasks.ToArray()); // do not use - blocks UI thread
                await Task.WhenAll(tasks);

                foreach (var memStreamChunk in tmpChunks)
                {
                    memStreamChunk.Seek(0, SeekOrigin.Begin);
                    fileWriter.Write(memStreamChunk.ToArray());
                }

                StateHasChanged();
                i += parallelDownloadThreadsCount;
            }
        }

        /// <summary>
        /// Download individual video chunks
        /// </summary>
        /// <param name="urlReplaced"></param>
        /// <param name="progressChunk"></param>
        /// <param name="fileStream"></param>
        /// <param name="media"></param>
        /// <param name="chunkNumber"></param>
        /// <returns></returns>
        private async Task DownloadChunk(string urlReplaced, double progressChunk, MemoryStream chunkContent, string mediaPath, int chunkNumber)
        {
            progressLog.Add($"Starting to download chunk number {chunkNumber}");

            using (var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            }))
            {
                var chunkUrl = string.Empty;

                if (Uri.IsWellFormedUriString(mediaPath, UriKind.Absolute))
                {
                    chunkUrl = mediaPath;
                }
                else
                {
                    chunkUrl = urlReplaced + "/" + mediaPath;
                }

                try
                {
                    int retry = 0;
                    while (retry <= 3)
                    {
                        try
                        {
                            await SendChunkRequest(chunkContent, progressChunk, httpClient, chunkUrl);
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
                    ShowNotification($"{ex.Message}", NotificationSeverity.Error);
                }
            }
        }

        private string GetBaseURL(string url)
        {
            return url.Substring(0, url.LastIndexOf('/'));
        }

        private async Task SendChunkRequest(MemoryStream chunkContent, double progressChunk, HttpClient httpClient, string chunkUrl)
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), chunkUrl))
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }

                HttpResponseMessage? responseMsgVideoChunk = await httpClient.SendAsync(request);

                if (responseMsgVideoChunk != null)
                {
                    if (responseMsgVideoChunk.StatusCode != HttpStatusCode.OK)
                    {
                        progressLog.Add($"Download of {chunkUrl} failed with status code: {responseMsgVideoChunk.StatusCode}");
                    }
                    else
                    {
                        try
                        {
                            //var tmpStream = await responseMsgVideoChunk.Content.ReadAsStreamAsync();
                            var tmpBytes = await responseMsgVideoChunk.Content.ReadAsByteArrayAsync();
                            //await tmpStream.CopyToAsync(chunkContent);

                            chunkContent.Write(tmpBytes, 0, tmpBytes.Length);

                            chunkContent.Flush();
                        }
                        catch (Exception ex)
                        {

                        }
                        progressLog.Add($"Download of {chunkUrl} successful");

                        //var hasAccess = photinoWebViewManager.Dispatcher.CheckAccess();

                        //photinoWebViewManager.Dispatcher.AssertAccess();

                        /*
                        Program.Context.Post(new SendOrPostCallback(state =>
                        {
                            StateHasChanged();
                        }), null);
                        */

                        /*
                        photinoWebViewManager.TryDispatchAsync(sp =>
                        {
                            StateHasChanged();
                        });
                        */
                        /*
                        photinoWebViewManager.Dispatcher.InvokeAsync(async () =>
                        {
                            StateHasChanged();
                            int a = 0;
                            a++;
                            progressLog.Add($"{a} {progressChunk}");
                        });
                        */
                    }

                    lock(threadLock)
                    {
                        _downloadProgressBarValue += progressChunk;
                    }
                }
            }
        }
    }
}
