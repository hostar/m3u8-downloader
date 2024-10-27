using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Photino.NET;
using Radzen;

namespace m3u8_downloader_photino
{
    class Program
    {
        public static PhotinoWindow MainWindow { get; private set; }
        public static SynchronizationContext Context { get; set; }

        [STAThread]
        static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
                .AddLogging()
                .AddRadzenComponents();

            // register root component and selector
            appBuilder.RootComponents.Add<App>("app");

            var app = appBuilder.Build();

            // customize window
            app.MainWindow
                .SetSize(1000, 1000)
                .SetMinSize(600, 500)
                //.SetResizable(false)
                .SetFileSystemAccessEnabled(true)
                .SetIconFile("favicon.ico")
                .SetBrowserControlInitParameters("--unsafely-disable-devtools-self-xss-warnings")
                .SetTitle("M3U8 downloader");

            MainWindow = app.MainWindow;

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();

        }
    }
}