using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Logging;

/// <summary>
/// Logger implementation that outputs log entries in JSON format.
/// </summary>
public sealed class JsonLogger : ILogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the JsonLogger.
    /// </summary>
    /// <param name="logFilePath">Path to the JSON log file.</param>
    public JsonLogger(string logFilePath)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public void LogInfo(string message)
    {
        lock (_lock)
        {
            _entries.Add(new LogEntry
            {
                Level = "Info",
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <inheritdoc />
    public void LogWarning(string message)
    {
        lock (_lock)
        {
            _entries.Add(new LogEntry
            {
                Level = "Warning",
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <inheritdoc />
    public void LogError(string message, Exception? exception = null)
    {
        lock (_lock)
        {
            _entries.Add(new LogEntry
            {
                Level = "Error",
                Message = message,
                Exception = exception?.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (_lock)
        {
            if (_entries.Count == 0)
                return;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_entries, options);
            File.WriteAllText(_logFilePath, json);

            _entries.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        Flush();
        _disposed = true;
    }

    private sealed class LogEntry
    {
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
