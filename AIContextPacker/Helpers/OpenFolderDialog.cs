using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AIContextPacker.Helpers;

public class OpenFolderDialog
{
    public string Title { get; set; } = "Select Folder";
    public string FolderName { get; private set; } = string.Empty;

    public bool ShowDialog()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Title,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        
        if (result == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            FolderName = dialog.FolderName;
            return true;
        }

        return false;
    }
}
