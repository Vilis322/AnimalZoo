using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Logging;

/// <summary>
/// Logger implementation that outputs log entries in XML format.
/// Supports rolling log files by size.
/// </summary>
public sealed class XmlLogger : ILogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private bool _disposed;

    // Rolling configuration
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxBackupFiles = 5;

    /// <summary>
    /// Initializes a new instance of the XmlLogger.
    /// </summary>
    /// <param name="logFilePath">Path to the XML log file.</param>
    public XmlLogger(string logFilePath)
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

            var allEntries = new List<LogEntry>();

            // Read existing entries if file exists
            if (File.Exists(_logFilePath))
            {
                try
                {
                    using var reader = XmlReader.Create(_logFilePath);
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "LogEntry")
                        {
                            var timestamp = reader.GetAttribute("Timestamp");
                            var level = reader.GetAttribute("Level");

                            reader.ReadToDescendant("Message");
                            var message = reader.ReadElementContentAsString();

                            string? exception = null;
                            if (reader.Name == "Exception")
                            {
                                exception = reader.ReadElementContentAsString();
                            }

                            if (!string.IsNullOrEmpty(timestamp) && !string.IsNullOrEmpty(level))
                            {
                                allEntries.Add(new LogEntry
                                {
                                    Timestamp = DateTime.Parse(timestamp),
                                    Level = level,
                                    Message = message,
                                    Exception = exception
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // If parsing fails, start fresh
                }
            }

            // Append new entries
            allEntries.AddRange(_entries);

            // Write all entries
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };

            using (var writer = XmlWriter.Create(_logFilePath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("LogEntries");

                foreach (var entry in allEntries)
                {
                    writer.WriteStartElement("LogEntry");
                    writer.WriteAttributeString("Timestamp", entry.Timestamp.ToString("o"));
                    writer.WriteAttributeString("Level", entry.Level);
                    writer.WriteElementString("Message", entry.Message);
                    if (!string.IsNullOrEmpty(entry.Exception))
                    {
                        writer.WriteElementString("Exception", entry.Exception);
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

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
