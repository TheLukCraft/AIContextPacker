using System;
using System.Linq;
using System.Windows;
using AIContextPacker.Models;

namespace AIContextPacker.Views;

public partial class FilterEditorWindow : Window
{
    public IgnoreFilter Filter { get; private set; }

    public FilterEditorWindow(IgnoreFilter? existingFilter = null)
    {
        InitializeComponent();

        if (existingFilter != null)
        {
            Filter = existingFilter;
            NameTextBox.Text = Filter.Name;
            PatternsTextBox.Text = string.Join(Environment.NewLine, Filter.Patterns);
            Title = "Edit Filter";
        }
        else
        {
            Filter = new IgnoreFilter();
            Title = "New Filter";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a filter name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Filter.Name = NameTextBox.Text.Trim();
        Filter.Patterns = PatternsTextBox.Text
            .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
