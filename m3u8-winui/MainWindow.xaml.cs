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

            StreamWriter streamWriter = new StreamWriter(FilePath, true, Encoding.UTF8);

            string response_m3u8 = await responseMsg_m3u8.Content.ReadAsStringAsync();
            var m3u8 = M3u8Parser.Parse(response_m3u8);

            var urlReplaced = URL.Substring(0, URL.LastIndexOf('/'));
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
                        var tmpStream = await responseMsgVideoChunk.Content.ReadAsStreamAsync();
                        StreamReader streamReader = new StreamReader(tmpStream);
                        streamWriter.Write(streamReader.ReadToEnd());
                        streamReader.Close();
                    }
                }
            }

            streamWriter.Close();
        }
    }

    
}
