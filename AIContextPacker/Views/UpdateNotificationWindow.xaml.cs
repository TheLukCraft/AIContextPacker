using System.Diagnostics;
using System.Windows;
using AIContextPacker.Converters;
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
        
        // Set release notes using converter
        var markdown = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
            ? "• New features and improvements\n• Bug fixes and performance enhancements"
            : _updateInfo.ReleaseNotes;
            
        var converter = new MarkdownToFlowDocumentConverter();
        var document = converter.Convert(markdown, typeof(System.Windows.Documents.FlowDocument), null, System.Globalization.CultureInfo.CurrentCulture);
        
        if (document is System.Windows.Documents.FlowDocument flowDoc)
        {
            ReleaseNotesRichTextBox.Document = flowDoc;
        }
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
