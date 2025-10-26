using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIContextPacker.Controls;
using AIContextPacker.Helpers;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using AIContextPacker.ViewModels;
using AIContextPacker.Views;

namespace AIContextPacker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IUpdateService _updateService;
    private bool _isClosing = false;

    public MainWindow(MainViewModel viewModel, IUpdateService updateService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _updateService = updateService;
        DataContext = _viewModel;
        
        // Subscribe to toast notifications
        _viewModel.ToastRequested += ShowToast;
        
        // Save state when closing - must handle async properly
        Closing += OnWindowClosing;
        
        // Apply saved theme and check for updates after window is loaded
        Loaded += async (s, e) =>
        {
            App.ApplyTheme(_viewModel.Settings.Theme);
            await CheckForUpdatesAsync();
        };
    }

    private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            _isClosing = true;
            
            await _viewModel.SaveStateAsync();
            
            // Unsubscribe to prevent memory leaks
            _viewModel.ToastRequested -= ShowToast;
            Closing -= OnWindowClosing;
            
            Application.Current.Shutdown();
        }
    }

    private async void SelectProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog())
        {
            await _viewModel.LoadProjectCommand.ExecuteAsync(dialog.FolderName);
        }
    }

    private async void RecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Header is string projectPath)
        {
            if (Directory.Exists(projectPath))
            {
                await _viewModel.LoadProjectCommand.ExecuteAsync(projectPath);
            }
            else
            {
                MessageBox.Show($"Project folder no longer exists:\n{projectPath}", 
                    "Project Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            var folder = files.FirstOrDefault();

            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                _ = _viewModel.LoadProjectCommand.ExecuteAsync(folder);
            }
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            var folder = files.FirstOrDefault();

            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_viewModel.Settings);
        if (settingsWindow.ShowDialog() == true)
        {
            // Apply theme change
            App.ApplyTheme(_viewModel.Settings.Theme);
            
            // Refresh filters and prompts to show newly added items
            _viewModel.RefreshFiltersAndPrompts();
            
            // Settings were saved
            _viewModel.ApplyFilters();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Tutorial_Click(object sender, RoutedEventArgs e)
    {
        var tutorialWindow = new TutorialWindow();
        tutorialWindow.Owner = this;
        tutorialWindow.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = this;
        aboutWindow.ShowDialog();
    }

    private void SupportMe_Click(object sender, RoutedEventArgs e)
    {
        var supportWindow = new SupportWindow();
        supportWindow.Owner = this;
        supportWindow.ShowDialog();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(true);
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdateMessage = false)
    {
        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();

            if (updateInfo.IsUpdateAvailable)
            {
                var updateWindow = new UpdateNotificationWindow(updateInfo);
                updateWindow.Owner = this;
                updateWindow.ShowDialog();
            }
            else if (showNoUpdateMessage)
            {
                MessageBox.Show(
                    $"You are running the latest version ({updateInfo.CurrentVersion}).",
                    "No Updates Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch
        {
            if (showNoUpdateMessage)
            {
                MessageBox.Show(
                    "Could not check for updates. Please check your internet connection.",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void PreviewPart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is GeneratedPart part)
        {
            var previewWindow = new PreviewWindow(part);
            previewWindow.Owner = this;
            previewWindow.ShowDialog();
        }
    }

    public void ShowToast(string message)
    {
        var toast = new ToastNotification();
        ToastContainer.Children.Add(toast);
        toast.Show(message, ToastType.Success);
    }

    private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is TreeView && !e.Handled)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            ProjectFilesScrollViewer.RaiseEvent(eventArg);
        }
    }
}
