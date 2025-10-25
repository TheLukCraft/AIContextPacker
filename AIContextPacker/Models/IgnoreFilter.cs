using System.Collections.Generic;

namespace AIContextPacker.Models;

public class IgnoreFilter
{
    public string Name { get; set; } = string.Empty;
    public List<string> Patterns { get; set; } = new();
}
