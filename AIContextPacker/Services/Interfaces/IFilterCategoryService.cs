using AIContextPacker.Models;
using System.Collections.ObjectModel;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Service for managing filter categories and their active state.
/// </summary>
public interface IFilterCategoryService
{
    /// <summary>
    /// Gets the collection of filter categories.
    /// </summary>
    ObservableCollection<FilterCategoryViewModel> FilterCategories { get; }

    /// <summary>
    /// Loads predefined and custom filter categories from settings.
    /// </summary>
    /// <param name="settings">Application settings containing custom filters and active states</param>
    /// <param name="onFilterActiveChanged">Callback invoked when a filter's IsActive state changes</param>
    Task LoadFilterCategoriesAsync(AppSettings settings, Func<string, bool, Task> onFilterActiveChanged);

    /// <summary>
    /// Adds a new custom filter.
    /// </summary>
    /// <param name="filter">The filter to add</param>
    /// <param name="settings">Application settings to persist to</param>
    void AddCustomFilter(IgnoreFilter filter, AppSettings settings);

    /// <summary>
    /// Removes a custom filter by name.
    /// </summary>
    /// <param name="filterName">Name of the filter to remove</param>
    /// <param name="settings">Application settings to persist to</param>
    /// <returns>True if filter was found and removed, false otherwise</returns>
    bool RemoveCustomFilter(string filterName, AppSettings settings);

    /// <summary>
    /// Updates the active state of a filter.
    /// </summary>
    /// <param name="filterName">Name of the filter</param>
    /// <param name="isActive">New active state</param>
    /// <param name="settings">Application settings to persist to</param>
    void UpdateFilterActiveState(string filterName, bool isActive, AppSettings settings);

    /// <summary>
    /// Gets all currently active filter names.
    /// </summary>
    /// <returns>Collection of active filter names</returns>
    IEnumerable<string> GetActiveFilterNames();
}
