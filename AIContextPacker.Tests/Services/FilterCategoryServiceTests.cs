using AIContextPacker.Models;
using AIContextPacker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIContextPacker.Tests.Services;

public class FilterCategoryServiceTests
{
    private readonly Mock<ILogger<FilterCategoryService>> _mockLogger;
    private readonly FilterCategoryService _service;

    public FilterCategoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<FilterCategoryService>>();
        _service = new FilterCategoryService(_mockLogger.Object);
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_WithValidSettings_LoadsPredefinedCategories()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveFilters = new Dictionary<string, bool>(),
            CustomIgnoreFilters = []
        };
        Func<string, bool, Task> callback = (name, active) => Task.CompletedTask;

        // Act
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Assert
        _service.FilterCategories.Should().NotBeEmpty();
        _service.FilterCategories.Should().Contain(c => c.CategoryName == "Custom");
        _service.FilterCategories.Should().Contain(c => c.CategoryName != "Custom"); // At least one predefined category
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        Func<string, bool, Task> callback = (name, active) => Task.CompletedTask;

        // Act
        var act = async () => await _service.LoadFilterCategoriesAsync(null!, callback);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_WithNullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [], CustomIgnoreFilters = [] };

        // Act
        var act = async () => await _service.LoadFilterCategoriesAsync(settings, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_WithActiveFilters_LoadsActiveState()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveFilters = new Dictionary<string, bool>
            {
                ["Node.js"] = true,
                ["Python"] = false
            },
            CustomIgnoreFilters = []
        };
        Func<string, bool, Task> callback = (name, active) => Task.CompletedTask;

        // Act
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Assert
        var nodeFilter = _service.FilterCategories
            .SelectMany(c => c.Filters)
            .FirstOrDefault(f => f.Filter.Name == "Node.js");
        
        nodeFilter.Should().NotBeNull();
        nodeFilter!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_WithCustomFilters_LoadsCustomCategory()
    {
        // Arrange
        var customFilter = new IgnoreFilter
        {
            Name = "MyCustomFilter",
            Patterns = ["*.custom"]
        };
        var settings = new AppSettings
        {
            ActiveFilters = new Dictionary<string, bool> { ["MyCustomFilter"] = true },
            CustomIgnoreFilters = [customFilter]
        };
        Func<string, bool, Task> callback = (name, active) => Task.CompletedTask;

        // Act
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Assert
        var customCategory = _service.FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        customCategory.Should().NotBeNull();
        customCategory!.Filters.Should().Contain(f => f.Filter.Name == "MyCustomFilter");
        customCategory.Filters.First(f => f.Filter.Name == "MyCustomFilter").IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task LoadFilterCategoriesAsync_ClearsPreviousCategories()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [], CustomIgnoreFilters = [] };
        Func<string, bool, Task> callback = (name, active) => Task.CompletedTask;

        await _service.LoadFilterCategoriesAsync(settings, callback);
        var firstCount = _service.FilterCategories.Count;

        // Act
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Assert
        _service.FilterCategories.Count.Should().Be(firstCount); // Should not double
    }

    [Fact]
    public async Task AddCustomFilter_WithValidFilter_AddsToCategory()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [], CustomIgnoreFilters = [] };
        var callback = new Func<string, bool, Task>((name, active) => Task.CompletedTask);
        await _service.LoadFilterCategoriesAsync(settings, callback);

        var filter = new IgnoreFilter { Name = "TestFilter", Patterns = ["*.test"] };

        // Act
        _service.AddCustomFilter(filter, settings);

        // Assert
        var customCategory = _service.FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        customCategory.Should().NotBeNull();
        customCategory!.Filters.Should().Contain(f => f.Filter.Name == "TestFilter");
        settings.CustomIgnoreFilters.Should().Contain(filter);
        settings.ActiveFilters.Should().ContainKey("TestFilter");
    }

    [Fact]
    public void AddCustomFilter_WithNullFilter_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        var act = () => _service.AddCustomFilter(null!, settings);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCustomFilter_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var filter = new IgnoreFilter { Name = "Test", Patterns = ["*.test"] };

        // Act
        var act = () => _service.AddCustomFilter(filter, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddCustomFilter_WithDuplicateName_DoesNotAdd()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [], CustomIgnoreFilters = [] };
        var callback = new Func<string, bool, Task>((name, active) => Task.CompletedTask);
        await _service.LoadFilterCategoriesAsync(settings, callback);

        var filter1 = new IgnoreFilter { Name = "TestFilter", Patterns = ["*.test"] };
        var filter2 = new IgnoreFilter { Name = "TestFilter", Patterns = ["*.other"] };

        // Act
        _service.AddCustomFilter(filter1, settings);
        _service.AddCustomFilter(filter2, settings);

        // Assert
        var customCategory = _service.FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        customCategory!.Filters.Count(f => f.Filter.Name == "TestFilter").Should().Be(1);
    }

    [Fact]
    public async Task RemoveCustomFilter_WithExistingFilter_RemovesFromCategory()
    {
        // Arrange
        var filter = new IgnoreFilter { Name = "TestFilter", Patterns = ["*.test"] };
        var settings = new AppSettings
        {
            ActiveFilters = new Dictionary<string, bool> { ["TestFilter"] = true },
            CustomIgnoreFilters = [filter]
        };
        var callback = new Func<string, bool, Task>((name, active) => Task.CompletedTask);
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Act
        var result = _service.RemoveCustomFilter("TestFilter", settings);

        // Assert
        result.Should().BeTrue();
        var customCategory = _service.FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        customCategory!.Filters.Should().NotContain(f => f.Filter.Name == "TestFilter");
        settings.CustomIgnoreFilters.Should().NotContain(f => f.Name == "TestFilter");
        settings.ActiveFilters.Should().NotContainKey("TestFilter");
    }

    [Fact]
    public async Task RemoveCustomFilter_WithNonExistentFilter_ReturnsFalse()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [], CustomIgnoreFilters = [] };
        var callback = new Func<string, bool, Task>((name, active) => Task.CompletedTask);
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Act
        var result = _service.RemoveCustomFilter("NonExistent", settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveCustomFilter_WithNullOrEmptyFilterName_ThrowsArgumentException()
    {
        // Arrange
        var settings = new AppSettings();

        // Act & Assert
        var act1 = () => _service.RemoveCustomFilter(null!, settings);
        var act2 = () => _service.RemoveCustomFilter("", settings);
        var act3 = () => _service.RemoveCustomFilter("   ", settings);

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveCustomFilter_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.RemoveCustomFilter("Test", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateFilterActiveState_WithValidFilter_UpdatesSettings()
    {
        // Arrange
        var settings = new AppSettings { ActiveFilters = [] };

        // Act
        _service.UpdateFilterActiveState("TestFilter", true, settings);

        // Assert
        settings.ActiveFilters["TestFilter"].Should().BeTrue();
    }

    [Fact]
    public void UpdateFilterActiveState_WithNullOrEmptyFilterName_ThrowsArgumentException()
    {
        // Arrange
        var settings = new AppSettings();

        // Act & Assert
        var act1 = () => _service.UpdateFilterActiveState(null!, true, settings);
        var act2 = () => _service.UpdateFilterActiveState("", true, settings);
        var act3 = () => _service.UpdateFilterActiveState("   ", true, settings);

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateFilterActiveState_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.UpdateFilterActiveState("Test", true, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetActiveFilterNames_WithActiveFilters_ReturnsActiveNames()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveFilters = new Dictionary<string, bool>
            {
                ["Node.js"] = true,
                ["Python"] = false
            },
            CustomIgnoreFilters = []
        };
        var callback = new Func<string, bool, Task>((name, active) => Task.CompletedTask);
        await _service.LoadFilterCategoriesAsync(settings, callback);

        // Act
        var activeNames = _service.GetActiveFilterNames().ToList();

        // Assert
        activeNames.Should().Contain("Node.js");
        activeNames.Should().NotContain("Python");
    }

    [Fact]
    public void GetActiveFilterNames_WithNoActiveFilters_ReturnsEmpty()
    {
        // Act
        var activeNames = _service.GetActiveFilterNames().ToList();

        // Assert
        activeNames.Should().BeEmpty();
    }
}
