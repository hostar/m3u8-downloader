using Avalonia.Interactivity;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace m3u8_downloader_avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainViewModel HeaderModel { get; set; } = new MainViewModel();

        private string url = "";
        public string URL
        {
            get => url;
            set => this.RaiseAndSetIfChanged(ref url, value);
        }

        private string filePath = "";
        public string FilePath
        {
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref filePath, value);
        }

        private bool downloadButtonEnabled = true;
        public bool DownloadButtonEnabled
        {
            get => downloadButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref downloadButtonEnabled, value);
        }

        
    }
}
