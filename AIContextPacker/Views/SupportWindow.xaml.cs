using System.Diagnostics;
using System.Windows;

namespace AIContextPacker.Views;

public partial class SupportWindow : Window
{
    public SupportWindow()
    {
        InitializeComponent();
    }

    private void BuyMeCoffee_Click(object sender, RoutedEventArgs e)
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
            MessageBox.Show($"Could not open browser. Please visit:\n{url}", 
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
