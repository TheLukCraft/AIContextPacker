using CommunityToolkit.Mvvm.ComponentModel;

namespace AIContextPacker.Models;

public partial class GeneratedPart : ObservableObject
{
    public int PartNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public int MaxChars { get; set; }

    [ObservableProperty]
    private bool wasCopied;
}
