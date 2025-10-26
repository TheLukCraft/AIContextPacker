using AIContextPacker.Helpers;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace AIContextPacker.Services;

/// <summary>
/// Service for managing filter categories and their active state.
/// </summary>
public class FilterCategoryService : IFilterCategoryService
{
    private readonly ILogger<FilterCategoryService> _logger;

    public ObservableCollection<FilterCategoryViewModel> FilterCategories { get; } = [];

    public FilterCategoryService(ILogger<FilterCategoryService> logger)
    {
        _logger = logger;
    }

    public async Task LoadFilterCategoriesAsync(AppSettings settings, Func<string, bool, Task> onFilterActiveChanged)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(onFilterActiveChanged);

        _logger.LogInformation("Loading filter categories");

        FilterCategories.Clear();

        // Load predefined categorized filters
        var categories = GitIgnoreCategories.GetAllCategories();
        foreach (var category in categories)
        {
            var categoryVm = new FilterCategoryViewModel
            {
                CategoryName = category
            };

            var filters = GitIgnoreCategories.GetFiltersForCategory(category);
            foreach (var filter in filters)
            {
                var isActive = settings.ActiveFilters.ContainsKey(filter.Name) && settings.ActiveFilters[filter.Name];
                var filterVm = new FilterViewModel(filter, isActive, isReadOnly: true);
                
                // Set up PropertyChanged handler
                filterVm.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterViewModel.IsActive) && s is FilterViewModel fvm)
                    {
                        await onFilterActiveChanged(fvm.Filter.Name, fvm.IsActive);
                    }
                };
                
                categoryVm.Filters.Add(filterVm);
            }

            FilterCategories.Add(categoryVm);
        }

        // Load custom filters
        var customCategory = new FilterCategoryViewModel
        {
            CategoryName = "Custom"
        };

        foreach (var filter in settings.CustomIgnoreFilters)
        {
            var isActive = settings.ActiveFilters.ContainsKey(filter.Name) && settings.ActiveFilters[filter.Name];
            var filterVm = new FilterViewModel(filter, isActive, isReadOnly: false);
            
            // Set up PropertyChanged handler
            filterVm.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(FilterViewModel.IsActive) && s is FilterViewModel fvm)
                {
                    await onFilterActiveChanged(fvm.Filter.Name, fvm.IsActive);
                }
            };
            
            customCategory.Filters.Add(filterVm);
        }

        FilterCategories.Add(customCategory);

        _logger.LogInformation("Loaded {CategoryCount} filter categories with {FilterCount} total filters",
            FilterCategories.Count,
            FilterCategories.Sum(c => c.Filters.Count));

        await Task.CompletedTask;
    }

    public void AddCustomFilter(IgnoreFilter filter, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(settings);

        _logger.LogInformation("Adding custom filter: {FilterName}", filter.Name);

        // Find custom category
        var customCategory = FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        if (customCategory == null)
        {
            _logger.LogWarning("Custom category not found, cannot add filter");
            return;
        }

        // Check if filter already exists
        if (customCategory.Filters.Any(f => f.Filter.Name == filter.Name))
        {
            _logger.LogWarning("Filter {FilterName} already exists in custom category", filter.Name);
            return;
        }

        // Add to settings
        settings.CustomIgnoreFilters.Add(filter);
        settings.ActiveFilters[filter.Name] = false; // Default to inactive

        // Add to UI
        var filterVm = new FilterViewModel(filter, isActive: false, isReadOnly: false);
        customCategory.Filters.Add(filterVm);

        _logger.LogInformation("Added custom filter: {FilterName}", filter.Name);
    }

    public bool RemoveCustomFilter(string filterName, AppSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterName);
        ArgumentNullException.ThrowIfNull(settings);

        _logger.LogInformation("Removing custom filter: {FilterName}", filterName);

        // Find custom category
        var customCategory = FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        if (customCategory == null)
        {
            _logger.LogWarning("Custom category not found");
            return false;
        }

        // Find filter in UI
        var filterVm = customCategory.Filters.FirstOrDefault(f => f.Filter.Name == filterName);
        if (filterVm == null)
        {
            _logger.LogWarning("Filter {FilterName} not found in custom category", filterName);
            return false;
        }

        // Remove from settings
        var filter = settings.CustomIgnoreFilters.FirstOrDefault(f => f.Name == filterName);
        if (filter != null)
        {
            settings.CustomIgnoreFilters.Remove(filter);
        }

        settings.ActiveFilters.Remove(filterName);

        // Remove from UI
        customCategory.Filters.Remove(filterVm);

        _logger.LogInformation("Removed custom filter: {FilterName}", filterName);
        return true;
    }

    public void UpdateFilterActiveState(string filterName, bool isActive, AppSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterName);
        ArgumentNullException.ThrowIfNull(settings);

        settings.ActiveFilters[filterName] = isActive;

        _logger.LogDebug("Updated filter {FilterName} active state to {IsActive}", filterName, isActive);
    }

    public IEnumerable<string> GetActiveFilterNames()
    {
        return FilterCategories
            .SelectMany(category => category.Filters)
            .Where(filter => filter.IsActive)
            .Select(filter => filter.Filter.Name)
            .ToList();
    }
}
