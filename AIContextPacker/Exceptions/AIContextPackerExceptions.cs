using System;

namespace AIContextPacker.Exceptions;

/// <summary>
/// Base exception for all AI Context Packer exceptions.
/// </summary>
public class AIContextPackerException : Exception
{
    public AIContextPackerException(string message) : base(message)
    {
    }

    public AIContextPackerException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when project loading fails.
/// </summary>
public class ProjectLoadException : AIContextPackerException
{
    public string? ProjectPath { get; }

    public ProjectLoadException(string message, string? projectPath = null) 
        : base(message)
    {
        ProjectPath = projectPath;
    }

    public ProjectLoadException(string message, string? projectPath, Exception innerException) 
        : base(message, innerException)
    {
        ProjectPath = projectPath;
    }
}

/// <summary>
/// Exception thrown when filter application fails.
/// </summary>
public class FilterApplicationException : AIContextPackerException
{
    public string? FilterName { get; }

    public FilterApplicationException(string message, string? filterName = null) 
        : base(message)
    {
        FilterName = filterName;
    }

    public FilterApplicationException(string message, string? filterName, Exception innerException) 
        : base(message, innerException)
    {
        FilterName = filterName;
    }
}

/// <summary>
/// Exception thrown when part generation fails.
/// </summary>
public class PartGenerationException : AIContextPackerException
{
    public string? FilePath { get; }

    public PartGenerationException(string message, string? filePath = null) 
        : base(message)
    {
        FilePath = filePath;
    }

    public PartGenerationException(string message, string? filePath, Exception innerException) 
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Exception thrown when file system operations fail.
/// </summary>
public class FileSystemException : AIContextPackerException
{
    public string? Path { get; }

    public FileSystemException(string message, string? path = null) 
        : base(message)
    {
        Path = path;
    }

    public FileSystemException(string message, string? path, Exception innerException) 
        : base(message, innerException)
    {
        Path = path;
    }
}
