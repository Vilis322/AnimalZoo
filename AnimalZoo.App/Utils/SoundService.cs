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
    /// The WAV file is expected to be located alongside images in the existing animal asset folder.
    /// </summary>
    public static class SoundService
    {
        /// <summary>
        /// Plays the default "voice.wav" for a given animal type (e.g., "Cat", "Dog") from Avalonia resources.
        /// Creates a temporary file for native playback tools. Does not block the UI thread.
        /// Throws a descriptive exception if the resource is missing or no backend is available.
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
        /// Returns a unique temp file path with .wav extension.
        /// </summary>
        private static string CreateTempWavPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"animalzoo_{Guid.NewGuid():N}.wav");
            return path;
        }

        /// <summary>
        /// Plays a WAV file cross-platform using the appropriate backend.
        /// Non-blocking: playback is run on a background thread when needed.
        /// </summary>
        private static Task PlayFileAsync(string absolutePath)
        {
            if (!Path.IsPathRooted(absolutePath))
                throw new ArgumentException("Path must be absolute.", nameof(absolutePath));
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Sound file does not exist on disk.", absolutePath);

            // Use a guard method recognized by CA1416
            if (IsWindows())
            {
                // Direct call to the Windows-only API to satisfy CA1416 (no lambda boundary).
                PlayWithSoundPlayer(absolutePath);
                return Task.CompletedTask;
            }

            if (OperatingSystem.IsMacOS())
            {
                return RunDetachedAsync("afplay", $"\"{absolutePath}\"");
            }

            if (OperatingSystem.IsLinux())
            {
                return TryRunFirstAvailableAsync(
                    new[]
                    {
                        ("paplay", $"\"{absolutePath}\""),
                        ("aplay",  $"\"{absolutePath}\"")
                    },
                    onAllFailed: () => throw new PlatformNotSupportedException(
                        "Neither 'paplay' nor 'aplay' found on PATH. Please install one of them to enable sound playback.")
                );
            }

            throw new PlatformNotSupportedException("Unsupported OS for sound playback.");
        }

        /// <summary>
        /// Windows-only WAV playback using System.Media.SoundPlayer.
        /// The attribute informs analyzers that this API is supported on Windows only.
        /// </summary>
        /// <param name="absolutePath">Absolute path to the WAV file.</param>
        [SupportedOSPlatform("windows")]
        private static void PlayWithSoundPlayer(string absolutePath)
        {
            using var player = new System.Media.SoundPlayer(absolutePath);
            player.Play(); // fire-and-forget
        }

        /// <summary>
        /// CA1416 guard: returns true only on Windows and marks the condition as a platform guard.
        /// </summary>
        [SupportedOSPlatformGuard("windows")]
        private static bool IsWindows() => OperatingSystem.IsWindows();

        /// <summary>
        /// Tries to run the first available command from the provided list.
        /// </summary>
        private static async Task TryRunFirstAvailableAsync((string exe, string args)[] commands, Action onAllFailed)
        {
            foreach (var (exe, args) in commands)
            {
                if (IsOnPath(exe))
                {
                    await RunDetachedAsync(exe, args);
                    return;
                }
            }
            onAllFailed?.Invoke();
        }

        /// <summary>
        /// Runs a process detached (non-blocking). Arguments must be pre-escaped by caller.
        /// </summary>
        private static Task RunDetachedAsync(string fileName, string arguments)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
                using var _ = Process.Start(psi);
            });
        }

        /// <summary>
        /// Checks whether an executable is available on PATH.
        /// </summary>
        private static bool IsOnPath(string exeName)
        {
            try
            {
                if (IsWindows())
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = exeName,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p!.WaitForExit(1000);
                    return p.ExitCode == 0;
                }
                else
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = exeName,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p!.WaitForExit(1000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

