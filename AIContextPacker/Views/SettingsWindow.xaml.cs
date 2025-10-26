using System.Linq;
using System.Windows;
using AIContextPacker.Models;

namespace AIContextPacker.Views;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        DataContext = _settings;

        LoadData();
    }

    private void LoadData()
    {
        ExtensionsTextBox.Text = string.Join(", ", _settings.AllowedExtensions);
        PromptsListBox.ItemsSource = _settings.GlobalPrompts;
        FiltersListBox.ItemsSource = _settings.CustomIgnoreFilters;
        
        // Load theme setting
        switch (_settings.Theme)
        {
            case Models.ThemeMode.Light:
                LightThemeRadio.IsChecked = true;
                break;
            case Models.ThemeMode.Dark:
                DarkThemeRadio.IsChecked = true;
                break;
            case Models.ThemeMode.System:
                SystemThemeRadio.IsChecked = true;
                break;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Save extensions
        var extensions = ExtensionsTextBox.Text
            .Split(',')
            .Select(ext => ext.Trim())
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .ToList();

        _settings.AllowedExtensions = extensions;
        
        // Save theme setting
        if (LightThemeRadio.IsChecked == true)
            _settings.Theme = Models.ThemeMode.Light;
        else if (DarkThemeRadio.IsChecked == true)
            _settings.Theme = Models.ThemeMode.Dark;
        else if (SystemThemeRadio.IsChecked == true)
            _settings.Theme = Models.ThemeMode.System;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AddPrompt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PromptEditorWindow();
        if (dialog.ShowDialog() == true)
        {
            _settings.GlobalPrompts.Add(dialog.Prompt);
            PromptsListBox.Items.Refresh();
        }
    }

    private void EditPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (PromptsListBox.SelectedItem is GlobalPrompt prompt)
        {
            var dialog = new PromptEditorWindow(prompt);
            if (dialog.ShowDialog() == true)
            {
                PromptsListBox.Items.Refresh();
            }
        }
        else
        {
            MessageBox.Show("Please select a prompt to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (PromptsListBox.SelectedItem is GlobalPrompt prompt)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{prompt.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _settings.GlobalPrompts.Remove(prompt);
                PromptsListBox.Items.Refresh();
            }
        }
    }

    private void AddFilter_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FilterEditorWindow();
        if (dialog.ShowDialog() == true)
        {
            _settings.CustomIgnoreFilters.Add(dialog.Filter);
            FiltersListBox.Items.Refresh();
        }
    }

    private void EditFilter_Click(object sender, RoutedEventArgs e)
    {
        if (FiltersListBox.SelectedItem is IgnoreFilter filter)
        {
            var dialog = new FilterEditorWindow(filter);
            if (dialog.ShowDialog() == true)
            {
                FiltersListBox.Items.Refresh();
            }
        }
        else
        {
            MessageBox.Show("Please select a filter to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void DeleteFilter_Click(object sender, RoutedEventArgs e)
    {
        if (FiltersListBox.SelectedItem is IgnoreFilter filter)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{filter.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _settings.CustomIgnoreFilters.Remove(filter);
                FiltersListBox.Items.Refresh();
            }
        }
    }
}
