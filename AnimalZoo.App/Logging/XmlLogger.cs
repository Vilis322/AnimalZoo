using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Logging;

/// <summary>
/// Logger implementation that outputs log entries in XML format.
/// </summary>
public sealed class XmlLogger : ILogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly string _logFilePath;

    /// <summary>
    /// Initializes a new instance of the XmlLogger.
    /// </summary>
    /// <param name="logFilePath">Path to the XML log file.</param>
    public XmlLogger(string logFilePath)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
    }

    /// <inheritdoc />
    public void LogInfo(string message)
    {
        _entries.Add(new LogEntry
        {
            Level = "Info",
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <inheritdoc />
    public void LogWarning(string message)
    {
        _entries.Add(new LogEntry
        {
            Level = "Warning",
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <inheritdoc />
    public void LogError(string message, Exception? exception = null)
    {
        _entries.Add(new LogEntry
        {
            Level = "Error",
            Message = message,
            Exception = exception?.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (_entries.Count == 0)
            return;

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  "
        };

        using var writer = XmlWriter.Create(_logFilePath, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("LogEntries");

        foreach (var entry in _entries)
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

        _entries.Clear();
    }

    private sealed class LogEntry
    {
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
