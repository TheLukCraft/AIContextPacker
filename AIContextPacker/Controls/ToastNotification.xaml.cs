using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AIContextPacker.Controls;

public partial class ToastNotification : UserControl
{
    private DispatcherTimer? _timer;

    public ToastNotification()
    {
        InitializeComponent();
    }

    public void Show(string message, ToastType type = ToastType.Success, int durationSeconds = 3)
    {
        MessageText.Text = message;
        
        // Set colors based on type
        var border = (Border)Content;
        switch (type)
        {
            case ToastType.Success:
                border.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                IconText.Text = "✓";
                break;
            case ToastType.Info:
                border.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                IconText.Text = "ℹ";
                break;
            case ToastType.Warning:
                border.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                IconText.Text = "⚠";
                break;
            case ToastType.Error:
                border.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                IconText.Text = "✕";
                break;
        }

        // Play show animation
        var showStoryboard = (Storyboard)Resources["ShowAnimation"];
        showStoryboard.Begin(this);

        // Auto-hide after duration
        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(durationSeconds)
        };
        _timer.Tick += (s, e) =>
        {
            _timer.Stop();
            Hide();
        };
        _timer.Start();
    }

    public void Hide()
    {
        _timer?.Stop();
        var hideStoryboard = (Storyboard)Resources["HideAnimation"];
        hideStoryboard.Completed += (s, e) =>
        {
            if (Parent is Panel panel)
            {
                panel.Children.Remove(this);
            }
        };
        hideStoryboard.Begin(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}

public enum ToastType
{
    Success,
    Info,
    Warning,
    Error
}
