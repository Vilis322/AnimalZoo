using System;

namespace AnimalZoo.App.Interfaces;

/// <summary>
/// Interface for pluggable logging functionality.
/// Supports different output formats (XML, JSON, etc.).
/// </summary>
public interface ILogger : IDisposable
{
    /// <summary>Log an informational message.</summary>
    void LogInfo(string message);

    /// <summary>Log a warning message.</summary>
    void LogWarning(string message);

    /// <summary>Log an error message with optional exception details.</summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>Write all buffered log entries to persistent storage.</summary>
    void Flush();
}
