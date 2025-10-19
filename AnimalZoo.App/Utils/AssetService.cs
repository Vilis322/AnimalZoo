using System;
using System.IO;
using Avalonia.Platform;

namespace AnimalZoo.App.Utils;

/// <summary>
/// Abstraction over Avalonia asset loading to allow unit testing of image resolution logic.
/// </summary>
public interface IAssetService
{
    bool Exists(Uri uri);
    Stream Open(Uri uri);
}

/// <summary>
/// Default implementation using Avalonia's AssetLoader.
/// </summary>
public sealed class AvaloniaAssetService : IAssetService
{
    public bool Exists(Uri uri) => AssetLoader.Exists(uri);
    public Stream Open(Uri uri) => AssetLoader.Open(uri);
}

/// <summary>
/// Global asset service accessor. In tests you can override <see cref="Instance"/> with a fake.
/// </summary>
public static class AssetService
{
    private static IAssetService? _instance;

    /// <summary>
    /// Current asset service. Defaults to <see cref="AvaloniaAssetService"/>.
    /// </summary>
    public static IAssetService Instance
    {
        get => _instance ??= new AvaloniaAssetService();
        set => _instance = value;
    }
}