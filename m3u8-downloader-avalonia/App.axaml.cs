using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using m3u8_downloader_avalonia.ViewModels;
using m3u8_downloader_avalonia.Views;

namespace m3u8_downloader_avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                (desktop.MainWindow as MainWindow).DoTheThing();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}