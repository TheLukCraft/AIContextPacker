using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AIContextPacker.Helpers;
using AIContextPacker.Models;
using AIContextPacker.ViewModels;
using AIContextPacker.Views;

namespace AIContextPacker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
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
            // Settings were saved
            _viewModel.ApplyFilters();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
}