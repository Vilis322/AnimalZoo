using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;

namespace AnimalZoo.App.Localization
{
    /// <summary>
    /// JSON-based localization loader for Avalonia assets.
    /// Looks up strings under avares://AnimalZoo.App/Assets/i18n/{fileName}.json
    /// File names expected:
    ///   - ENG -> eng.json
    ///   - RU  -> ru.json
    ///   - EST -> est.json
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        public Language CurrentLanguage { get; private set; } = Language.ENG;

        public event Action? LanguageChanged;

        /// <summary>
        /// Indexer returning a localized string by key; falls back to [key] if missing.
        /// </summary>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key))
                    return string.Empty;
                if (_cache.TryGetValue(key, out var value))
                    return value;
                return $"[{key}]";
            }
        }

        /// <summary>
        /// Sets current language, loads its dictionary, and notifies subscribers.
        /// </summary>
        public void SetLanguage(Language lang)
        {
            if (lang == CurrentLanguage) return;

            LoadLanguage(lang);
            CurrentLanguage = lang;
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// Loads the JSON dictionary for the given language into the cache.
        /// </summary>
        private void LoadLanguage(Language lang)
        {
            _cache.Clear();

            // UPDATED: match user-provided filenames (eng.json / ru.json / est.json)
            var fileName = lang switch
            {
                Language.RU  => "ru.json",
                Language.EST => "est.json",
                _            => "eng.json" // ENG
            };

            var uri = new Uri($"avares://AnimalZoo.App/Assets/i18n/{fileName}");
            if (!AssetLoader.Exists(uri))
                throw new FileNotFoundException($"Localization file not found: {fileName}");

            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict is null)
                throw new InvalidDataException($"Invalid localization JSON: {fileName}");

            foreach (var (k, v) in dict)
                _cache[k] = v;
        }

        /// <summary>
        /// Factory creating a service with the default language preloaded.
        /// </summary>
        public static LocalizationService CreateDefault()
        {
            var svc = new LocalizationService();
            svc.LoadLanguage(Language.ENG);
            return svc;
        }
    }
}

