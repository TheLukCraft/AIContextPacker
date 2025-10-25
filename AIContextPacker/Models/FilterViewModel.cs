using CommunityToolkit.Mvvm.ComponentModel;

namespace AIContextPacker.Models;

public partial class FilterViewModel : ObservableObject
{
    [ObservableProperty]
    private IgnoreFilter filter = new();

    [ObservableProperty]
    private bool isActive;

    public string Name => Filter.Name;

    public FilterViewModel(IgnoreFilter filter, bool isActive = false)
    {
        Filter = filter;
        IsActive = isActive;
    }
}
