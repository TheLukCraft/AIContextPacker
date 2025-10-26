using System.Diagnostics;
using System.Windows;
using AIContextPacker.Models;

namespace AIContextPacker.Views;

public partial class UpdateNotificationWindow : Window
{
    private readonly UpdateInfo _updateInfo;

    public UpdateNotificationWindow(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
        LoadUpdateInfo();
    }

    private void LoadUpdateInfo()
    {
        VersionText.Text = $"Version {_updateInfo.LatestVersion} is now available (you have {_updateInfo.CurrentVersion})";
        
        // Format release notes
        var notes = _updateInfo.ReleaseNotes;
        if (string.IsNullOrWhiteSpace(notes))
        {
            notes = "• New features and improvements\n• Bug fixes and performance enhancements";
        }
        
        ReleaseNotesText.Text = notes;
    }

    private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateInfo.ReleaseUrl,
                UseShellExecute = true
            });
            DialogResult = true;
            Close();
        }
        catch
        {
            MessageBox.Show($"Could not open browser. Please visit:\n{_updateInfo.ReleaseUrl}", 
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
