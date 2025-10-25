using System;
using System.Windows;
using AIContextPacker.Models;

namespace AIContextPacker.Views;

public partial class PromptEditorWindow : Window
{
    public GlobalPrompt Prompt { get; private set; }

    public PromptEditorWindow(GlobalPrompt? existingPrompt = null)
    {
        InitializeComponent();

        if (existingPrompt != null)
        {
            Prompt = existingPrompt;
            NameTextBox.Text = Prompt.Name;
            ContentTextBox.Text = Prompt.Content;
            Title = "Edit Prompt";
        }
        else
        {
            Prompt = new GlobalPrompt { Id = Guid.NewGuid().ToString() };
            Title = "New Prompt";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a prompt name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Prompt.Name = NameTextBox.Text.Trim();
        Prompt.Content = ContentTextBox.Text;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
