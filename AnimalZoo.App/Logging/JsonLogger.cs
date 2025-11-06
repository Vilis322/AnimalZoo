using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Logging;

/// <summary>
/// Logger implementation that outputs log entries in JSON format.
/// Supports rolling log files by size.
/// </summary>
public sealed class JsonLogger : ILogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private bool _disposed;

    // Rolling configuration
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxBackupFiles = 5;

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

            // Check if rolling is needed
            RollFileIfNeeded();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // Read existing entries if file exists
            var allEntries = new List<LogEntry>();
            if (File.Exists(_logFilePath))
            {
                try
                {
                    var existingJson = File.ReadAllText(_logFilePath);
                    var existingEntries = JsonSerializer.Deserialize<List<LogEntry>>(existingJson);
                    if (existingEntries != null)
                    {
                        allEntries.AddRange(existingEntries);
                    }
                }
                catch
                {
                    // If deserialization fails, start fresh
                }
            }

            // Append new entries
            allEntries.AddRange(_entries);

            // Write all entries
            var json = JsonSerializer.Serialize(allEntries, options);
            File.WriteAllText(_logFilePath, json);

            _entries.Clear();
        }
    }

    /// <summary>
    /// Rolls the log file if it exceeds the maximum size.
    /// </summary>
    private void RollFileIfNeeded()
    {
        if (!File.Exists(_logFilePath))
            return;

        var fileInfo = new FileInfo(_logFilePath);
        if (fileInfo.Length < MaxFileSizeBytes)
            return;

        // Roll existing backup files (app.log.4 -> app.log.5, app.log.3 -> app.log.4, etc.)
        for (int i = MaxBackupFiles - 1; i >= 1; i--)
        {
            var sourceFile = $"{_logFilePath}.{i}";
            var destFile = $"{_logFilePath}.{i + 1}";

            if (File.Exists(destFile))
                File.Delete(destFile);

            if (File.Exists(sourceFile))
                File.Move(sourceFile, destFile);
        }

        // Move current file to .1
        var backupFile = $"{_logFilePath}.1";
        if (File.Exists(backupFile))
            File.Delete(backupFile);

        File.Move(_logFilePath, backupFile);
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
