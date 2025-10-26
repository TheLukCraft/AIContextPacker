using System.Diagnostics;
using System.Windows;

namespace AIContextPacker.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OpenWebsite_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.capala.pl/");
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/TheLukCraft");
    }

    private void OpenYouTube_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.youtube.com/@capalapl");
    }

    private void OpenTikTok_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.tiktok.com/@capalapl");
    }

    private void OpenBuyMeCoffee_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://buymeacoffee.com/thelukcraft");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show($"Could not open {url}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
