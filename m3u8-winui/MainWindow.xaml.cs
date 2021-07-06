using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
//using CurlToCSharp.Services;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace m3u8_winui
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainViewModel HeaderModel { get; set; } = new MainViewModel();

        private string url;
        public string URL { get { return url; } set
            {
                url = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(URL)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;


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
    }

    
}
