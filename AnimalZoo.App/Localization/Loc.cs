namespace AnimalZoo.App.Localization
{
    /// <summary>
    /// Static accessor to the app-wide localization service.
    /// Keeps changes minimal to avoid wiring DI into existing code.
    /// </summary>
    public static class Loc
    {
        private static ILocalizationService? _instance;

        /// <summary>
        /// Gets the singleton instance (lazily created).
        /// </summary>
        public static ILocalizationService Instance
            => _instance ??= LocalizationService.CreateDefault();

        /// <summary>
        /// Helper method to fetch a localized string by key.
        /// </summary>
        public static string Get(string key) => Instance[key];
    }
}