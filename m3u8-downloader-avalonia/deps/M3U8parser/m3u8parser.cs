using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace m3u8_downloader_avalonia.deps.M3U8parser
{
    // source: https://github.com/ramtinak/SimpleM3U8Parser
    public class M3u8Parser
    {
        const string M3U8_TAG = "#EXTM3U";

        const string MEDIA_PATTERN = @"EXTINF:(?<duration>.*),\n(?<link>(\S+))";

        const string QUALITY_PATTERN = @"#EXT-X-STREAM-INF.*RESOLUTION=(?<quality>[0-9]+x[0-9]+).*\n(?<link>(\S+))";

        static public M3u8MediaContainer Parse(string content)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException("M3u8Parser.Parse(content)");
            if (!content.Contains(M3U8_TAG))
                throw new Exception("'content' is not a `m3u/m3u8` file.");
            var mediaList = new List<M3u8Media>();
            foreach (Match m in Regex.Matches(content, MEDIA_PATTERN))
            {
                var path = m.Groups["link"]?.Value;
                var duration = m.Groups["duration"]?.Value;
                if (!string.IsNullOrEmpty(path) && double.TryParse(duration, out double durationAsDouble))
                    mediaList.Add(new M3u8Media { Duration = durationAsDouble, Path = path });
            }

            var qualityList = new List<M3u8Quality>();
            foreach (Match m in Regex.Matches(content, QUALITY_PATTERN))
            {
                var path = m.Groups["link"]?.Value;
                var quality = m.Groups["quality"]?.Value;
                if (!string.IsNullOrEmpty(path) )
                    qualityList.Add(new M3u8Quality { Quality = quality, Path = path });
            }
            var durations = mediaList.Select(m => m.Duration).ToArray();
            var container = new M3u8MediaContainer
            {
                Medias = mediaList,
                Qualities = qualityList,
                Duration = TimeSpan.FromSeconds(durations.Sum())
            };
            return container;
        }
    }

    public class M3u8MediaContainer
    {
        public List<M3u8Media> Medias { get; internal set; } = new List<M3u8Media>();
        public List<M3u8Quality> Qualities { get; internal set; } = new List<M3u8Quality>();

        public TimeSpan Duration { get; internal set; }
    }

    public class M3u8Media
    {
        public double Duration { get; set; }

        public string Path { get; set; }
    }

    public class M3u8Quality
    {
        public string Quality { get; set; }

        public string Path { get; set; }
    }
}
