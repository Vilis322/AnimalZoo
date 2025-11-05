using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>
/// Wrapper for a localization key that should be translated dynamically.
/// Use this when passing parameters to LocalizableLogEntry that need to be re-translated.
/// </summary>
public sealed class LocalizationKey
{
    public string Key { get; }

    public LocalizationKey(string key)
    {
        Key = key;
    }
}

/// <summary>
/// Represents a log entry that can be localized dynamically.
/// Updates its display text when the application language changes.
/// </summary>
public sealed class LocalizableLogEntry : INotifyPropertyChanged
{
    private readonly ILocalizationService _loc;
    private readonly string? _localizationKey;
    private readonly object?[] _parameters;
    private readonly string? _staticText;

    /// <summary>
    /// Creates a localizable log entry with a localization key and parameters.
    /// </summary>
    /// <param name="localizationKey">The localization key (e.g., "Log.Added")</param>
    /// <param name="parameters">Parameters for string formatting</param>
    public LocalizableLogEntry(string localizationKey, params object?[] parameters)
    {
        _loc = Loc.Instance;
        _localizationKey = localizationKey;
        _parameters = parameters;
        _staticText = null;

        // Subscribe to language changes
        _loc.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Creates a static (non-localizable) log entry.
    /// Use this for messages that don't need localization (e.g., error messages with dynamic content).
    /// </summary>
    /// <param name="staticText">The static text to display</param>
    public LocalizableLogEntry(string staticText)
    {
        _loc = Loc.Instance;
        _localizationKey = null;
        _parameters = Array.Empty<object>();
        _staticText = staticText;

        // Subscribe to language changes (though static text won't change, we keep consistency)
        _loc.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// The display text for this log entry (localized if applicable).
    /// Supports nested LocalizationKey parameters that are translated dynamically.
    /// </summary>
    public string Text
    {
        get
        {
            if (_staticText is not null)
                return _staticText;

            if (_localizationKey is null)
                return string.Empty;

            try
            {
                var template = _loc[_localizationKey];

                // Resolve any LocalizationKey parameters dynamically
                var resolvedParams = _parameters.Select(p =>
                    p is LocalizationKey lk ? _loc[lk.Key] : p
                ).ToArray();

                return resolvedParams.Length > 0 ? string.Format(template, resolvedParams) : template;
            }
            catch
            {
                // Fallback if localization fails
                return $"[{_localizationKey}]";
            }
        }
    }

    private void OnLanguageChanged()
    {
        // Notify UI that Text property changed when language switches
        OnPropertyChanged(nameof(Text));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => Text;
}
