using System.Windows;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

public class NotificationService : INotificationService
{
    public void ShowError(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    public void ShowSuccess(string message)
    {
        // For now using MessageBox, can be replaced with custom toast notification
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    public void ShowWarning(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    public void ShowInfo(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }
}
