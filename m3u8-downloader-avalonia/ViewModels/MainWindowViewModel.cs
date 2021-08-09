using Avalonia.Interactivity;
using m3u8_downloader_avalonia.deps.M3U8parser;
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

        private bool qualityModalOpened = false;
        public bool QualityModalOpened
        {
            get => qualityModalOpened;
            set => this.RaiseAndSetIfChanged(ref qualityModalOpened, value);
        }

        private bool msgBoxModalOpened = false;
        public bool MsgBoxModalOpened
        {
            get => msgBoxModalOpened;
            set => this.RaiseAndSetIfChanged(ref msgBoxModalOpened, value);
        }

        private string msgBoxModalText = string.Empty;
        public string MsgBoxModalText
        {
            get => msgBoxModalText;
            set => this.RaiseAndSetIfChanged(ref msgBoxModalText, value);
        }

        private int downloadProgress = 0;

        public int DownloadProgress
        {
            get => downloadProgress;
            set => this.RaiseAndSetIfChanged(ref downloadProgress, value);
        }
        public double downloadProgressDouble { get; set; } = 0;

        private List<M3u8Quality> qualityList = new List<M3u8Quality>();
        public List<M3u8Quality> QualityList
        {
            get => qualityList;
            set => this.RaiseAndSetIfChanged(ref qualityList, value);
        }
    }
}
