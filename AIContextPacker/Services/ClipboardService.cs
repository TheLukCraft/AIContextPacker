using System;
using System.Threading.Tasks;
using System.Windows;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

public class ClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Clipboard.SetText(text);
        }).Task;
    }

    public Task<string> GetTextAsync()
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        }).Task;
    }

    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Clipboard.Clear();
        });
    }
}
