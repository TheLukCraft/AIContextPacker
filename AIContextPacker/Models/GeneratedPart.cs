namespace AIContextPacker.Models;

public class GeneratedPart
{
    public int PartNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public int MaxChars { get; set; }
    public bool WasCopied { get; set; }
}
