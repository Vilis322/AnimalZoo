using System;
using System.Collections.Generic;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Logging;

/// <summary>
/// Composite logger that writes to multiple logger implementations simultaneously.
/// </summary>
public sealed class CompositeLogger : ILogger
{
    private readonly List<ILogger> _loggers = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the CompositeLogger.
    /// </summary>
    /// <param name="loggers">Collection of logger implementations to use.</param>
    public CompositeLogger(params ILogger[] loggers)
    {
        if (loggers == null || loggers.Length == 0)
            throw new ArgumentException("At least one logger must be provided.", nameof(loggers));

        _loggers.AddRange(loggers);
    }

    /// <inheritdoc />
    public void LogInfo(string message)
    {
        foreach (var logger in _loggers)
        {
            logger.LogInfo(message);
        }
    }

    /// <inheritdoc />
    public void LogWarning(string message)
    {
        foreach (var logger in _loggers)
        {
            logger.LogWarning(message);
        }
    }

    /// <inheritdoc />
    public void LogError(string message, Exception? exception = null)
    {
        foreach (var logger in _loggers)
        {
            logger.LogError(message, exception);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        foreach (var logger in _loggers)
        {
            logger.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var logger in _loggers)
        {
            logger.Dispose();
        }

        _disposed = true;
    }
}
