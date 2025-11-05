namespace AnimalZoo.App.Models;

/// <summary>
/// Represents a neighbor reaction with localization key and parameters.
/// Used to create localizable log entries for animal greeting messages.
/// </summary>
public sealed class NeighborReaction
{
    public string LocalizationKey { get; }
    public object?[] Parameters { get; }

    public NeighborReaction(string localizationKey, params object?[] parameters)
    {
        LocalizationKey = localizationKey;
        Parameters = parameters;
    }
}
