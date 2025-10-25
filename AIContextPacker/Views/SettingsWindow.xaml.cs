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
        FiltersListBox.ItemsSource = _settings.IgnoreFilters;
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
            _settings.IgnoreFilters.Add(dialog.Filter);
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
                _settings.IgnoreFilters.Remove(filter);
                FiltersListBox.Items.Refresh();
            }
        }
    }
}
