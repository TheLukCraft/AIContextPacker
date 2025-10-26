using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AIContextPacker.Models;

public partial class FilterCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string categoryName = string.Empty;

    public ObservableCollection<FilterViewModel> Filters { get; set; } = new();
}
