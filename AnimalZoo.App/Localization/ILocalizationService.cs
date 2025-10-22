using System;

namespace AnimalZoo.App.Localization
{
    /// <summary>
    /// Abstraction for a simple runtime localization service.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>Current UI language.</summary>
        Language CurrentLanguage { get; }

        /// <summary>
        /// Returns a localized string by key. If key is missing, returns the key in brackets.
        /// </summary>
        string this[string key] { get; }

        /// <summary>
        /// Sets current language and notifies subscribers.
        /// </summary>
        void SetLanguage(Language lang);

        /// <summary>
        /// Triggered when the language changes so that UI can refresh bindings.
        /// </summary>
        event Action? LanguageChanged;
    }
}