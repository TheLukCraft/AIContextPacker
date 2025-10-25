using System.Windows;
using AIContextPacker.Models;

namespace AIContextPacker.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow(GeneratedPart part)
    {
        InitializeComponent();
        DataContext = part;
    }
}
