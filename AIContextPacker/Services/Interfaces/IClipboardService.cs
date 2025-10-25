using System.Threading.Tasks;

namespace AIContextPacker.Services.Interfaces;

public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task<string> GetTextAsync();
    void Clear();
}
