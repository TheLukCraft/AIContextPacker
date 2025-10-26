# AI Context Packer - Developer Guide (Post-Refactoring)

## Quick Reference

### Logging Patterns

#### Basic Logging
```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Operation started");
        
        try
        {
            // Do work
            _logger.LogDebug("Processing step completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed with {ErrorCode}", errorCode);
            throw;
        }
    }
}
```

#### Performance Logging
```csharp
public async Task<Result> ExpensiveOperationAsync()
{
    _logger.LogInformation("Starting expensive operation");
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var result = await DoWorkAsync();
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Operation completed in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
            
        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, 
            "Operation failed after {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

### Exception Handling Patterns

#### Throwing Custom Exceptions
```csharp
public async Task LoadProjectAsync(string path)
{
    _logger.LogInformation("Loading project from {Path}", path);
    
    try
    {
        if (!Directory.Exists(path))
        {
            throw new ProjectLoadException(
                $"Directory not found: {path}", 
                projectPath: path);
        }
        
        // Load project...
    }
    catch (Exception ex) when (ex is not ProjectLoadException)
    {
        _logger.LogError(ex, "Failed to load project from {Path}", path);
        throw new ProjectLoadException(
            $"Failed to load project: {ex.Message}", 
            projectPath: path, 
            innerException: ex);
    }
}
```

#### Catching and Handling
```csharp
try
{
    await _projectService.LoadProjectAsync(path);
}
catch (ProjectLoadException ex)
{
    _logger.LogWarning(ex, "Project load failed: {ProjectPath}", ex.ProjectPath);
    _notificationService.ShowError($"Could not load project from:\n{ex.ProjectPath}\n\n{ex.Message}");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during project load");
    _notificationService.ShowError("An unexpected error occurred. Please check the logs.");
}
```

### Progress Reporting Pattern

#### Service with Progress Reporting
```csharp
public async Task ProcessFilesAsync(
    List<string> files, 
    IProgressReporter? progress = null)
{
    _logger.LogInformation("Processing {Count} files", files.Count);
    
    for (int i = 0; i < files.Count; i++)
    {
        // Check for cancellation
        progress?.CancellationToken.ThrowIfCancellationRequested();
        
        // Report progress
        var percentComplete = (double)(i + 1) / files.Count * 100;
        progress?.Report($"Processing file {i + 1} of {files.Count}...", percentComplete);
        
        await ProcessFileAsync(files[i]);
    }
    
    progress?.Clear();
    _logger.LogInformation("All files processed successfully");
}
```

#### ViewModel Using Progress Reporter
```csharp
[RelayCommand]
private async Task ProcessFilesAsync()
{
    var cts = new CancellationTokenSource();
    var progress = new ProgressReporter(
        (status, percent) =>
        {
            LoadingStatus = status;
            ProgressPercentage = percent ?? 0;
        },
        cts.Token);
    
    try
    {
        IsLoading = true;
        await _fileService.ProcessFilesAsync(files, progress);
    }
    finally
    {
        IsLoading = false;
        progress.Clear();
    }
}
```

### Testing Patterns

#### Service Test with Mocks
```csharp
public class MyServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ILogger<MyService>> _loggerMock;
    private readonly MyService _sut;

    public MyServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _loggerMock = new Mock<ILogger<MyService>>();
        _sut = new MyService(_fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DoSomething_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        _fileSystemMock
            .Setup(x => x.ReadFileAsync(It.IsAny<string>()))
            .ReturnsAsync("test content");

        // Act
        var result = await _sut.DoSomething();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("test content");
        
        _fileSystemMock.Verify(
            x => x.ReadFileAsync(It.IsAny<string>()), 
            Times.Once);
    }
}
```

#### File System Test with Cleanup
```csharp
public class FileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileService _sut;

    public FileServiceTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(), 
            $"Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _sut = new FileService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ReadFile_WithExistingFile_ReturnsContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        // Act
        var result = await _sut.ReadFileAsync(testFile);

        // Assert
        result.Should().Be("content");
    }
}
```

### Async/Await Best Practices

#### DO: Always await async methods
```csharp
// Good ✅
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    ProcessData(data);
}
```

#### DON'T: Fire and forget
```csharp
// Bad ❌
public void LoadData()
{
    _ = _service.GetDataAsync(); // Fire and forget
}
```

#### DO: Use ConfigureAwait(false) in libraries
```csharp
// Good for library code ✅
public async Task<Data> GetDataAsync()
{
    var result = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return await result.Content.ReadAsAsync<Data>().ConfigureAwait(false);
}
```

#### DON'T: Use ConfigureAwait(false) in WPF/UI code
```csharp
// Bad for WPF ❌
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync().ConfigureAwait(false);
    // This may throw if trying to update UI from background thread!
    StatusText = "Loaded"; 
}

// Good for WPF ✅
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    StatusText = "Loaded"; // Safe - back on UI thread
}
```

### SOLID Principles Checklist

#### Single Responsibility Principle (SRP)
- ✅ Each class has one reason to change
- ✅ Services handle one domain area
- ❌ God objects with multiple responsibilities

#### Open/Closed Principle (OCP)
- ✅ Use interfaces for extensibility
- ✅ Plugin architecture where appropriate
- ❌ Hardcoded dependencies

#### Liskov Substitution Principle (LSP)
- ✅ Derived classes can replace base classes
- ✅ Interface implementations behave consistently
- ❌ Breaking contracts in derived classes

#### Interface Segregation Principle (ISP)
- ✅ Small, focused interfaces
- ✅ Clients don't depend on unused methods
- ❌ Fat interfaces with many methods

#### Dependency Inversion Principle (DIP)
- ✅ Depend on abstractions (interfaces)
- ✅ Use dependency injection
- ❌ Depend on concrete implementations

### Code Review Checklist

Before submitting code:
- [ ] All public APIs have XML documentation
- [ ] Appropriate log statements added (Info, Debug, Error)
- [ ] Custom exceptions used instead of generic Exception
- [ ] Unit tests written and passing
- [ ] Async methods properly awaited
- [ ] Progress reporting added for long operations
- [ ] No Debug.WriteLine() statements
- [ ] SOLID principles followed
- [ ] No compiler warnings

### Common Mistakes to Avoid

#### 1. Async Void
```csharp
// Bad ❌
public async void LoadData()
{
    await _service.GetDataAsync();
}

// Good ✅
public async Task LoadDataAsync()
{
    await _service.GetDataAsync();
}
```

#### 2. Blocking Async Code
```csharp
// Bad ❌
public void LoadData()
{
    var data = _service.GetDataAsync().Result; // Deadlock risk!
}

// Good ✅
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
}
```

#### 3. Swallowing Exceptions
```csharp
// Bad ❌
try
{
    await DoSomethingAsync();
}
catch
{
    // Silent failure
}

// Good ✅
try
{
    await DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    throw; // Or handle appropriately
}
```

#### 4. Not Disposing Resources
```csharp
// Bad ❌
var stream = File.OpenRead(path);
// ... use stream ...

// Good ✅
using var stream = File.OpenRead(path);
// ... use stream ...
// Automatically disposed
```

### Performance Tips

1. **Use async for I/O operations**: File, network, database
2. **Don't use async for CPU-bound work**: Use Task.Run instead
3. **Cache where appropriate**: Avoid repeated expensive operations
4. **Use lazy loading**: Load data only when needed
5. **Profile before optimizing**: Use tools like dotTrace or PerfView

### Resources

- [Microsoft Async Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Clean Code](https://www.oreilly.com/library/view/clean-code-a/9780136083238/)
- [xUnit Documentation](https://xunit.net/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Configuration-Basics)

---

**Last Updated:** October 26, 2025  
**Version:** 1.0 (Post-Refactoring Phase 1)
