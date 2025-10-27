using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AIContextPacker.Exceptions;
using AIContextPacker.Helpers;
using AIContextPacker.Models;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIContextPacker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly IClipboardService _clipboardService;
    private readonly IProjectService _projectService;
    private readonly IFileSelectionService _fileSelectionService;
    private readonly IPinService _pinService;
    private readonly IFilterCategoryService _filterCategoryService;
    private readonly ISessionStateService _sessionStateService;
    private readonly ISearchService _searchService;

    private CancellationTokenSource? _searchCts;
    private readonly Debouncer _searchDebouncer;

    public event Action<string>? ToastRequested;

    [ObservableProperty]
    private FileTreeNode? rootNode;

    // Delegate to PinService for pinned files management
    public IReadOnlyList<FileTreeNode> PinnedFiles => _pinService.PinnedFiles;

    [ObservableProperty]
    private ObservableCollection<GeneratedPart> generatedParts = new();

    [ObservableProperty]
    private ObservableCollection<FilterCategoryViewModel> filterCategories = new();

    [ObservableProperty]
    private ObservableCollection<GlobalPrompt> globalPrompts = new();

    [ObservableProperty]
    private ObservableCollection<string> recentProjects = new();

    [ObservableProperty]
    private string? selectedGlobalPromptId;

    [ObservableProperty]
    private int maxCharsLimit = 10000;

    [ObservableProperty]
    private bool hasLocalGitignore;

    [ObservableProperty]
    private bool useDetectedGitignore = true;

    [ObservableProperty]
    private bool isGenerateEnabled = true;

    [ObservableProperty]
    private bool isLoadingProject = false;

    [ObservableProperty]
    private string loadingStatus = string.Empty;

    [ObservableProperty]
    private string currentProjectPath = string.Empty;

    [ObservableProperty]
    private AppSettings settings = new();

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private bool isCaseSensitive;

    [ObservableProperty]
    private bool useRegex;

    [ObservableProperty]
    private bool matchWholeWord;

    [ObservableProperty]
    private string searchResultStatus = string.Empty;

    [ObservableProperty]
    private bool isPinnedFilesExpanded = true;

    [ObservableProperty]
    private bool isFiltersExpanded = true;

    [ObservableProperty]
    private bool isGeneratedPartsExpanded = true;

    private List<string> _localGitignorePatterns = new();

    public MainViewModel(
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        INotificationService notificationService,
        IClipboardService clipboardService,
        IProjectService projectService,
        IFileSelectionService fileSelectionService,
        IPinService pinService,
        IFilterCategoryService filterCategoryService,
        ISessionStateService sessionStateService,
        ISearchService searchService)
    {
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;
        _projectService = projectService;
        _fileSelectionService = fileSelectionService;
        _pinService = pinService;
        _filterCategoryService = filterCategoryService;
        _sessionStateService = sessionStateService;
        _searchService = searchService;
        _searchDebouncer = new Debouncer(TimeSpan.FromMilliseconds(400));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Settings = await _settingsService.LoadSettingsAsync();
        MaxCharsLimit = Settings.MaxCharsLimit;

        // Load filter categories using service
        await _filterCategoryService.LoadFilterCategoriesAsync(Settings, OnFilterActiveChangedAsync);
        
        // Synchronize FilterCategories observable collection with service
        FilterCategories.Clear();
        foreach (var category in _filterCategoryService.FilterCategories)
        {
            FilterCategories.Add(category);
        }

        // Add "None" option for global prompts
        GlobalPrompts.Add(new GlobalPrompt
        {
            Id = null,
            Name = "(None)",
            Content = string.Empty
        });

        foreach (var prompt in Settings.GlobalPrompts)
        {
            GlobalPrompts.Add(prompt);
        }

        // Load recent projects
        var recentProjectsList = await _settingsService.GetRecentProjectsAsync();
        RecentProjects.Clear();
        foreach (var project in recentProjectsList.Take(5))
        {
            if (Directory.Exists(project))
            {
                RecentProjects.Add(project);
            }
        }

        var sessionState = await _sessionStateService.RestoreSessionStateAsync(
            LoadProjectAsync,
            TogglePinFile,
            FindNodeByPath);
        
        UseDetectedGitignore = sessionState.UseDetectedGitignore;
        
        // Restore selected global prompt (defaults to null/"(None)" if not set)
        SelectedGlobalPromptId = string.IsNullOrEmpty(sessionState.SelectedGlobalPrompt) 
            ? null 
            : sessionState.SelectedGlobalPrompt;
        
        // Notify UI that pinned files may have been restored
        OnPropertyChanged(nameof(PinnedFiles));
    }

    private async Task OnFilterActiveChangedAsync(string filterName, bool isActive)
    {
        _filterCategoryService.UpdateFilterActiveState(filterName, isActive, Settings);
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task LoadProjectAsync(string folderPath)
    {
        try
        {
            IsLoadingProject = true;

            var progress = new ProgressReporter((status, percent) =>
            {
                LoadingStatus = status;
            });

            var structure = await _projectService.LoadProjectAsync(folderPath, progress);
            
            CurrentProjectPath = folderPath;
            RootNode = structure.RootNode;
            HasLocalGitignore = structure.HasLocalGitignore;

            LoadingStatus = "Reading .gitignore...";

            if (HasLocalGitignore)
            {
                _localGitignorePatterns = await _fileSystemService.ReadGitignoreAsync(structure.LocalGitignorePath);
            }
            else
            {
                _localGitignorePatterns.Clear();
            }

            LoadingStatus = "Applying filters...";

            await ApplyFiltersAsync();
            await _settingsService.AddRecentProjectAsync(folderPath);
            
            // Update recent projects list
            var recentProjectsList = await _settingsService.GetRecentProjectsAsync();
            RecentProjects.Clear();
            foreach (var project in recentProjectsList.Take(5))
            {
                if (Directory.Exists(project))
                {
                    RecentProjects.Add(project);
                }
            }
            
            MarkAsChanged();
            LoadingStatus = "Project loaded successfully!";
        }
        catch (ProjectLoadException ex)
        {
            _notificationService.ShowError($"Failed to load project:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"An unexpected error occurred:\n{ex.Message}");
        }
        finally
        {
            IsLoadingProject = false;
            LoadingStatus = string.Empty;
            await ClearSearchAsync();
        }
    }

    [RelayCommand]
    private void TogglePinFile(FileTreeNode node)
    {
        _pinService.TogglePin(node);
        OnPropertyChanged(nameof(PinnedFiles));
        MarkAsChanged();
    }

    [RelayCommand]
    private void SelectAll()
    {
        if (RootNode != null)
        {
            _fileSelectionService.SelectAll(RootNode);
        }
        MarkAsChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        if (RootNode != null)
        {
            _fileSelectionService.DeselectAll(RootNode);
        }
        MarkAsChanged();
    }

    [RelayCommand]
    private async Task GeneratePartsAsync()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath) || RootNode == null)
        {
            _notificationService.ShowWarning("Please load a project first.");
            return;
        }

        var selectedFiles = _fileSelectionService.GetSelectedFilePaths(RootNode).ToList();
        var pinnedFilePaths = _pinService.GetPinnedFilePaths().ToList();

        if (!selectedFiles.Any() && !pinnedFilePaths.Any())
        {
            _notificationService.ShowWarning("Please select at least one file.");
            return;
        }

        // Only use global prompt if a non-null ID is selected
        var globalPrompt = string.IsNullOrEmpty(SelectedGlobalPromptId) 
            ? null 
            : GlobalPrompts.FirstOrDefault(p => p.Id == SelectedGlobalPromptId)?.Content;

        var generator = new PartGeneratorService(_fileSystemService, _notificationService);
        var parts = await generator.GeneratePartsAsync(
            pinnedFilePaths,
            selectedFiles,
            CurrentProjectPath,
            MaxCharsLimit,
            Settings.IncludeFileHeaders,
            globalPrompt);

        GeneratedParts.Clear();
        foreach (var part in parts)
        {
            GeneratedParts.Add(part);
        }

        if (parts.Any())
        {
            IsGenerateEnabled = false;
            _notificationService.ShowSuccess($"Generated {parts.Count} part(s) successfully!");
        }
    }

    [RelayCommand]
    private async Task CopyPartAsync(GeneratedPart part)
    {
        await _clipboardService.SetTextAsync(part.Content);
        part.WasCopied = true;
        ToastRequested?.Invoke($"Part {part.PartNumber} copied to clipboard!");
    }

    [RelayCommand]
    private async Task CopyStructureAsync()
    {
        if (RootNode == null)
        {
            _notificationService.ShowWarning("Please load a project first.");
            return;
        }

        var selectedFiles = _fileSelectionService.GetSelectedFilePaths(RootNode).ToList();
        var pinnedFilePaths = _pinService.GetPinnedFilePaths().ToList();

        var generator = new PartGeneratorService(_fileSystemService, _notificationService);
        var structure = generator.GenerateStructure(RootNode, selectedFiles, pinnedFilePaths);

        await _clipboardService.SetTextAsync(structure);
        ToastRequested?.Invoke("Project structure copied to clipboard!");
    }

    public async Task ApplyFiltersAsync()
    {
        if (RootNode == null || string.IsNullOrEmpty(CurrentProjectPath))
            return;

        try
        {
            IsLoadingProject = true;
            
            var progress = new ProgressReporter((status, percent) =>
            {
                LoadingStatus = status;
            });

            // Aggregate all active filters from all categories
            var activeFilters = FilterCategories
                .SelectMany(cat => cat.Filters)
                .Where(f => f.IsActive)
                .Select(f => f.Filter)
                .ToList();
            
            var gitignorePatterns = UseDetectedGitignore ? _localGitignorePatterns : new List<string>();

            var filterService = new FilterService(
                Settings.AllowedExtensions,
                activeFilters,
                gitignorePatterns,
                CurrentProjectPath);

            await filterService.ApplyFiltersAsync(RootNode, progress);
            MarkAsChanged();
        }
        finally
        {
            IsLoadingProject = false;
            LoadingStatus = string.Empty;
            await ClearSearchAsync();
        }
    }

    public void RefreshFiltersAndPrompts()
    {
        // Refresh custom filters
        var customCategory = FilterCategories.FirstOrDefault(c => c.CategoryName == "Custom");
        if (customCategory != null)
        {
            // Remove filters that no longer exist
            var filtersToRemove = customCategory.Filters
                .Where(fvm => !Settings.CustomIgnoreFilters.Contains(fvm.Filter))
                .ToList();
            
            foreach (var filter in filtersToRemove)
            {
                customCategory.Filters.Remove(filter);
            }

            // Update existing filters and add new ones
            foreach (var filter in Settings.CustomIgnoreFilters)
            {
                var existingVm = customCategory.Filters.FirstOrDefault(fvm => fvm.Filter == filter);
                if (existingVm != null)
                {
                    // Filter already exists, trigger property change notification by reassigning
                    existingVm.Filter = filter;
                }
                else
                {
                    // New filter, add it
                    var isActive = Settings.ActiveFilters.ContainsKey(filter.Name) && Settings.ActiveFilters[filter.Name];
                    var filterVm = new FilterViewModel(filter, isActive, isReadOnly: false);
                    filterVm.PropertyChanged += async (s, e) =>
                    {
                        if (e.PropertyName == nameof(FilterViewModel.IsActive) && s is FilterViewModel fvm)
                        {
                            Settings.ActiveFilters[fvm.Filter.Name] = fvm.IsActive;
                            await ApplyFiltersAsync();
                        }
                    };
                    customCategory.Filters.Add(filterVm);
                }
            }
        }

        // Refresh global prompts
        GlobalPrompts.Clear();
        GlobalPrompts.Add(new GlobalPrompt
        {
            Id = null,
            Name = "(None)",
            Content = string.Empty
        });

        foreach (var prompt in Settings.GlobalPrompts)
        {
            GlobalPrompts.Add(prompt);
        }
    }

    private FileTreeNode? FindNodeByPath(FileTreeNode? node, string path)
    {
        if (node == null)
            return null;

        if (node.FullPath == path)
            return node;

        foreach (var child in node.Children)
        {
            var found = FindNodeByPath(child, path);
            if (found != null)
                return found;
        }

        return null;
    }

    private void MarkAsChanged()
    {
        IsGenerateEnabled = true;
    }

    partial void OnMaxCharsLimitChanged(int value)
    {
        Settings.MaxCharsLimit = value;
        MarkAsChanged();
    }

    partial void OnSelectedGlobalPromptIdChanged(string? value)
    {
        MarkAsChanged();
    }

    async partial void OnUseDetectedGitignoreChanged(bool value)
    {
        await ApplyFiltersAsync();
    }

    public async Task SaveStateAsync()
    {
        await _sessionStateService.SaveSessionStateAsync(
            CurrentProjectPath,
            RootNode,
            SelectedGlobalPromptId,
            UseDetectedGitignore);
        
        await _settingsService.SaveSettingsAsync(Settings);
    }

    partial void OnSearchTermChanged(string value)
    {
        _searchDebouncer.Debounce(async () => await SearchAsync());
    }

    partial void OnIsCaseSensitiveChanged(bool value)
    {
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            _searchDebouncer.Debounce(async () => await SearchAsync());
        }
    }

    partial void OnUseRegexChanged(bool value)
    {
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            _searchDebouncer.Debounce(async () => await SearchAsync());
        }
    }

    partial void OnMatchWholeWordChanged(bool value)
    {
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            _searchDebouncer.Debounce(async () => await SearchAsync());
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (RootNode == null || string.IsNullOrEmpty(SearchTerm))
        {
            await ClearSearchAsync();
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        SearchResultStatus = "Searching...";
        IsLoadingProject = true;

        try
        {
            // First, hide all nodes
            SetAllSearchVisible(RootNode, false);

            var options = new SearchOptions
            {
                SearchTerm = SearchTerm,
                IsCaseSensitive = IsCaseSensitive,
                UseRegex = UseRegex,
                MatchWholeWord = MatchWholeWord
            };

            // Run search in background
            var searchResult = await Task.Run(async () =>
                await _searchService.SearchAsync(RootNode, options, token), token);

            if (token.IsCancellationRequested)
                return;

            // Make matched nodes and their parents visible
            foreach (var node in searchResult.MatchedNodes)
            {
                node.IsSearchVisible = true;
                SetParentsSearchVisible(node);
                ExpandParents(node);
            }

            SearchResultStatus = searchResult.FilesMatched > 0
                ? $"Found {searchResult.FilesMatched} matching item{(searchResult.FilesMatched != 1 ? "s" : "")}"
                : "No matches found";
        }
        catch (OperationCanceledException)
        {
            SearchResultStatus = "Search cancelled";
        }
        catch (Exception ex)
        {
            SearchResultStatus = "Search error";
            _notificationService.ShowError($"Search failed: {ex.Message}");
        }
        finally
        {
            IsLoadingProject = false;
        }
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        _searchCts?.Cancel();
        SearchTerm = string.Empty;
        SearchResultStatus = string.Empty;
        
        if (RootNode != null)
        {
            await Task.Run(() => SetAllSearchVisible(RootNode, true));
        }
    }

    /// <summary>
    /// Sets IsSearchVisible for all nodes in the tree.
    /// </summary>
    private void SetAllSearchVisible(FileTreeNode node, bool isVisible)
    {
        node.IsSearchVisible = isVisible;
        foreach (var child in node.Children)
        {
            SetAllSearchVisible(child, isVisible);
        }
    }

    /// <summary>
    /// Makes all parent nodes up to the root visible in search.
    /// </summary>
    private void SetParentsSearchVisible(FileTreeNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            current.IsSearchVisible = true;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Expands all parent nodes up to the root to make the node visible.
    /// </summary>
    private void ExpandParents(FileTreeNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }
}
