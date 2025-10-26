using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    private List<string> _localGitignorePatterns = new();

    public MainViewModel(
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        INotificationService notificationService,
        IClipboardService clipboardService,
        IProjectService projectService,
        IFileSelectionService fileSelectionService,
        IPinService pinService)
    {
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;
        _projectService = projectService;
        _fileSelectionService = fileSelectionService;
        _pinService = pinService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Settings = await _settingsService.LoadSettingsAsync();
        MaxCharsLimit = Settings.MaxCharsLimit;

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
                var isActive = Settings.ActiveFilters.ContainsKey(filter.Name) && Settings.ActiveFilters[filter.Name];
                var filterVm = new FilterViewModel(filter, isActive, isReadOnly: true);
                filterVm.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterViewModel.IsActive) && s is FilterViewModel fvm)
                    {
                        Settings.ActiveFilters[fvm.Filter.Name] = fvm.IsActive;
                        await ApplyFiltersAsync();
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

        foreach (var filter in Settings.CustomIgnoreFilters)
        {
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

        FilterCategories.Add(customCategory);

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

        var sessionState = await _settingsService.LoadSessionStateAsync();
        UseDetectedGitignore = sessionState.UseDetectedGitignore;
        
        // Restore selected global prompt (defaults to null/"(None)" if not set)
        SelectedGlobalPromptId = string.IsNullOrEmpty(sessionState.SelectedGlobalPrompt) 
            ? null 
            : sessionState.SelectedGlobalPrompt;

        if (!string.IsNullOrEmpty(sessionState.LastProjectPath) && 
            Directory.Exists(sessionState.LastProjectPath))
        {
            await LoadProjectAsync(sessionState.LastProjectPath);
            
            // Restore pinned files
            foreach (var pinnedPath in sessionState.PinnedFiles)
            {
                var node = FindNodeByPath(RootNode, pinnedPath);
                if (node != null)
                {
                    TogglePinFile(node);
                }
            }
        }
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
        }
    }

    [RelayCommand]
    private void TogglePinFile(FileTreeNode node)
    {
        _pinService.TogglePin(node);
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
        var sessionState = new SessionState
        {
            LastProjectPath = CurrentProjectPath,
            PinnedFiles = _pinService.GetPinnedFilePaths().ToList(),
            SelectedFiles = RootNode != null ? _fileSelectionService.GetSelectedFilePaths(RootNode).ToList() : new List<string>(),
            UseDetectedGitignore = UseDetectedGitignore,
            SelectedGlobalPrompt = SelectedGlobalPromptId ?? string.Empty
        };

        await _settingsService.SaveSessionStateAsync(sessionState);
        await _settingsService.SaveSettingsAsync(Settings);
    }
}
