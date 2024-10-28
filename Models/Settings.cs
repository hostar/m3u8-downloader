using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3u8_downloader_photino.Models
{
    public class Settings
    {
        public bool ParallelDownload { get; set; } = false;
        public int ParallelDownloadThreadsCount { get; set; } = 5;
        public bool SelectFirstQuality { get; set; } = false;
    }
}
