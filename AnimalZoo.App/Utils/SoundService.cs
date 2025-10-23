using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace AnimalZoo.App.Utils
{
    /// <summary>
    /// Cross-platform sound playback service that reads WAV files from Avalonia resources,
    /// writes them into a temporary file and plays using the OS-appropriate backend.
    /// Resource path convention:
    ///   avares://AnimalZoo.App/Assets/{AnimalType}/voice.wav
    /// </summary>
    public static class SoundService
    {
        /// <summary>
        /// Plays the default 'voice.wav' for a given animal type from the embedded Assets folder.
        /// </summary>
        /// <param name="animalTypeName">Type name (matches the asset folder under Assets/).</param>
        public static async Task PlayAnimalVoiceAsync(string animalTypeName)
        {
            if (string.IsNullOrWhiteSpace(animalTypeName))
                throw new ArgumentException("Animal type name must be provided.", nameof(animalTypeName));

            // Use existing per-animal folder under Assets/
            var uri = new Uri($"avares://AnimalZoo.App/Assets/{animalTypeName}/voice.wav");

            if (!AssetLoader.Exists(uri))
                throw new FileNotFoundException($"Sound resource not found for '{animalTypeName}'.", uri.ToString());

            // Dump resource to a temp .wav file for native playback
            string tempFile = CreateTempWavPath();
            await using (var dst = File.Create(tempFile))
            await using (var src = AssetLoader.Open(uri))
            {
                await src.CopyToAsync(dst);
            }

            await PlayFileAsync(tempFile);
            // NOTE: We do not delete the temp file immediately to avoid races with the OS player.
            // Temp cleaner or system cleanup will handle it eventually.
        }

        /// <summary>
        /// Plays a specific WAV effect file for a given animal from the embedded Assets folder.
        /// Example: PlayAnimalEffectAsync("Dog", "crazy_action.wav") -> Assets/Dog/crazy_action.wav
        /// </summary>
        /// <param name="animalTypeName">Type name (matches the asset folder under Assets/).</param>
        /// <param name="fileName">WAV file name inside the animal's folder.</param>
        public static async Task PlayAnimalEffectAsync(string animalTypeName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(animalTypeName))
                throw new ArgumentException("Animal type name must be provided.", nameof(animalTypeName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Effect file name must be provided.", nameof(fileName));

            var uri = new Uri($"avares://AnimalZoo.App/Assets/{animalTypeName}/{fileName}");
            if (!AssetLoader.Exists(uri))
                throw new FileNotFoundException($"Sound resource not found: '{animalTypeName}/{fileName}'.", uri.ToString());

            string tempFile = CreateTempWavPath();
            await using (var dst = File.Create(tempFile))
            await using (var src = AssetLoader.Open(uri))
            {
                await src.CopyToAsync(dst);
            }

            await PlayFileAsync(tempFile);
        }

        /// <summary>
        /// Returns a unique temp file path with .wav extension.
        /// </summary>
        private static string CreateTempWavPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"animalzoo_{Guid.NewGuid():N}.wav");
            return path;
        }

        private static async Task PlayFileAsync(string absolutePath)
        {
            // (Existing implementation that calls platform-specific players)
            // ... keep your original body here ...
            if (OperatingSystem.IsWindows())
            {
                await RunDetachedAsync("powershell",
                    $"-NoProfile -WindowStyle Hidden -Command " +
                    $"\"(New-Object Media.SoundPlayer '{absolutePath}').PlaySync()\"");
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                await RunDetachedAsync("afplay", $"\"{absolutePath}\"");
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                // Try paplay or aplay
                if (!await TryRunFirstAvailableAsync(
                        new[]
                        {
                            ("paplay", $"\"{absolutePath}\""),
                            ("aplay",  $"\"{absolutePath}\"")
                        }))
                {
                    throw new PlatformNotSupportedException(
                        "Neither 'paplay' nor 'aplay' is available to play WAV files.");
                }
                return;
            }

            throw new PlatformNotSupportedException("Unsupported OS for sound playback.");
        }

        private static async Task<bool> TryRunFirstAvailableAsync((string exe, string args)[] candidates)
        {
            foreach (var (exe, args) in candidates)
            {
                if (IsOnPath(exe))
                {
                    try
                    {
                        await RunDetachedAsync(exe, args);
                        return true;
                    }
                    catch
                    {
                        // try next
                    }
                }
            }
            return false;
        }

        private static Task RunDetachedAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            return Task.CompletedTask;
        }

        private static bool IsOnPath(string exe)
        {
            try
            {
                var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

                foreach (var p in paths)
                {
                    var full = Path.Combine(p, exe);
                    if (File.Exists(full))
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }
}


