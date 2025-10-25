using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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

    [ObservableProperty]
    private FileTreeNode? rootNode;

    [ObservableProperty]
    private ObservableCollection<FileTreeNode> pinnedFiles = new();

    [ObservableProperty]
    private ObservableCollection<GeneratedPart> generatedParts = new();

    [ObservableProperty]
    private ObservableCollection<FilterViewModel> availableFilters = new();

    [ObservableProperty]
    private ObservableCollection<GlobalPrompt> globalPrompts = new();

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
    private string currentProjectPath = string.Empty;

    [ObservableProperty]
    private AppSettings settings = new();

    private List<string> _localGitignorePatterns = new();

    public MainViewModel(
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        INotificationService notificationService,
        IClipboardService clipboardService)
    {
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Settings = await _settingsService.LoadSettingsAsync();
        MaxCharsLimit = Settings.MaxCharsLimit;

        foreach (var filter in Settings.IgnoreFilters)
        {
            var isActive = Settings.ActiveFilters.ContainsKey(filter.Name) && Settings.ActiveFilters[filter.Name];
            var filterVm = new FilterViewModel(filter, isActive);
            filterVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterViewModel.IsActive))
                {
                    Settings.ActiveFilters[filter.Name] = filterVm.IsActive;
                    ApplyFilters();
                }
            };
            AvailableFilters.Add(filterVm);
        }

        foreach (var prompt in Settings.GlobalPrompts)
        {
            GlobalPrompts.Add(prompt);
        }

        var sessionState = await _settingsService.LoadSessionStateAsync();
        UseDetectedGitignore = sessionState.UseDetectedGitignore;

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
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _notificationService.ShowWarning("Please select a valid folder.");
                return;
            }

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                _notificationService.ShowError($"Directory does not exist: {folderPath}");
                return;
            }

            var structure = await _fileSystemService.LoadProjectAsync(folderPath);
            CurrentProjectPath = folderPath;
            RootNode = structure.RootNode;
            HasLocalGitignore = structure.HasLocalGitignore;

            if (HasLocalGitignore)
            {
                _localGitignorePatterns = await _fileSystemService.ReadGitignoreAsync(structure.LocalGitignorePath);
            }
            else
            {
                _localGitignorePatterns.Clear();
            }

            ApplyFilters();
            await _settingsService.AddRecentProjectAsync(folderPath);
            MarkAsChanged();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to load project: {ex.Message}");
        }
    }

    [RelayCommand]
    private void TogglePinFile(FileTreeNode node)
    {
        if (node.IsDirectory)
            return;

        node.IsPinned = !node.IsPinned;

        if (node.IsPinned)
        {
            PinnedFiles.Add(node);
            node.IsSelected = false;
        }
        else
        {
            PinnedFiles.Remove(node);
        }

        MarkAsChanged();
    }

    [RelayCommand]
    private void SelectAll()
    {
        if (RootNode != null)
        {
            SelectAllRecursive(RootNode, true);
        }
        MarkAsChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        if (RootNode != null)
        {
            SelectAllRecursive(RootNode, false);
        }
        MarkAsChanged();
    }

    private void SelectAllRecursive(FileTreeNode node, bool select)
    {
        if (node.IsVisible && !node.IsPinned)
        {
            node.IsSelected = select;
        }

        foreach (var child in node.Children)
        {
            SelectAllRecursive(child, select);
        }
    }

    [RelayCommand]
    private async Task GeneratePartsAsync()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath))
        {
            _notificationService.ShowWarning("Please load a project first.");
            return;
        }

        var selectedFiles = GetSelectedFilePaths(RootNode).ToList();
        var pinnedFilePaths = PinnedFiles.Select(f => f.FullPath).ToList();

        if (!selectedFiles.Any() && !pinnedFilePaths.Any())
        {
            _notificationService.ShowWarning("Please select at least one file.");
            return;
        }

        var globalPrompt = GlobalPrompts.FirstOrDefault(p => p.Id == SelectedGlobalPromptId)?.Content;

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
        _notificationService.ShowSuccess($"Part {part.PartNumber} copied to clipboard!");
    }

    [RelayCommand]
    private async Task CopyStructureAsync()
    {
        if (RootNode == null)
        {
            _notificationService.ShowWarning("Please load a project first.");
            return;
        }

        var selectedFiles = GetSelectedFilePaths(RootNode).ToList();
        var pinnedFilePaths = PinnedFiles.Select(f => f.FullPath).ToList();

        var generator = new PartGeneratorService(_fileSystemService, _notificationService);
        var structure = generator.GenerateStructure(RootNode, selectedFiles, pinnedFilePaths);

        await _clipboardService.SetTextAsync(structure);
        _notificationService.ShowSuccess("Project structure copied to clipboard!");
    }

    public void ApplyFilters()
    {
        if (RootNode == null || string.IsNullOrEmpty(CurrentProjectPath))
            return;

        var activeFilters = AvailableFilters
            .Where(f => f.IsActive)
            .Select(f => f.Filter)
            .ToList();
        var gitignorePatterns = UseDetectedGitignore ? _localGitignorePatterns : new List<string>();

        System.Diagnostics.Debug.WriteLine($"ApplyFilters: Active filters count: {activeFilters.Count}");
        System.Diagnostics.Debug.WriteLine($"ApplyFilters: Gitignore patterns count: {gitignorePatterns.Count}");

        var filterService = new FilterService(
            Settings.AllowedExtensions,
            activeFilters,
            gitignorePatterns,
            CurrentProjectPath);

        ApplyFiltersRecursive(RootNode, filterService);
        MarkAsChanged();
    }

    private void ApplyFiltersRecursive(FileTreeNode node, FilterService filterService)
    {
        if (!node.IsDirectory)
        {
            var wasVisible = node.IsVisible;
            node.IsVisible = filterService.ShouldIncludeFile(node.FullPath);
            
            if (wasVisible != node.IsVisible)
            {
                System.Diagnostics.Debug.WriteLine($"File visibility changed: {node.Name} -> {node.IsVisible}");
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                ApplyFiltersRecursive(child, filterService);
            }

            // Directory is visible if it has any visible children
            node.IsVisible = node.Children.Any(c => c.IsVisible);
        }
    }

    private IEnumerable<string> GetSelectedFilePaths(FileTreeNode node)
    {
        if (!node.IsDirectory && node.IsSelected && node.IsVisible)
        {
            yield return node.FullPath;
        }

        foreach (var child in node.Children)
        {
            foreach (var path in GetSelectedFilePaths(child))
            {
                yield return path;
            }
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

    partial void OnUseDetectedGitignoreChanged(bool value)
    {
        ApplyFilters();
    }

    public async Task SaveStateAsync()
    {
        var sessionState = new SessionState
        {
            LastProjectPath = CurrentProjectPath,
            PinnedFiles = PinnedFiles.Select(f => f.FullPath).ToList(),
            SelectedFiles = RootNode != null ? GetSelectedFilePaths(RootNode).ToList() : new List<string>(),
            UseDetectedGitignore = UseDetectedGitignore,
            SelectedGlobalPrompt = SelectedGlobalPromptId ?? string.Empty
        };

        await _settingsService.SaveSessionStateAsync(sessionState);
        await _settingsService.SaveSettingsAsync(Settings);
    }
}
