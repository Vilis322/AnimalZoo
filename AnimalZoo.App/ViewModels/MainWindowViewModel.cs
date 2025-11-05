using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Models;
using AnimalZoo.App.Repositories;
using AnimalZoo.App.Utils;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Generic;
using System.Reflection;

namespace AnimalZoo.App.ViewModels;

/// <summary>Row for "By type" statistics table.</summary>
public sealed class AnimalTypeStat
{
    /// <summary>Type name (localized).</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Total animals of this type.</summary>
    public int Count { get; init; }
    /// <summary>Average age across this type.</summary>
    public double AverageAge { get; init; }
}

/// <summary>
/// Main view model: animals list, actions, log, images, and LINQ stats.
/// Handles localization of headers, logs, states and age suffix.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    // --- Localization service ---
    private readonly ILocalizationService _loc = Loc.Instance;

    // --- Logger ---
    private readonly ILogger? _logger;

    // UI-visible collections
    public ObservableCollection<Animal> Animals { get; } = new();
    public ObservableCollection<LocalizableLogEntry> LogEntries { get; } = new();
    public ObservableCollection<string> AnimalIds { get; } = new();

    // Stats (table + lists)
    public ObservableCollection<AnimalTypeStat> ByTypeStats { get; } = new();
    public ObservableCollection<string> HungryStats { get; } = new();
    private string _oldestStat = string.Empty;
    public string OldestStat
    {
        get => _oldestStat;
        private set { if (_oldestStat != value) { _oldestStat = value; OnPropertyChanged(); } }
    }

    // Repo + enclosure
    private readonly IAnimalsRepository _animalsRepo;
    private readonly IEnclosureRepository _enclosureRepo;
    private readonly Enclosure<Animal> _enclosure = new();

    // Timeline events
    public event Action<Animal>? HappyEvent;
    public event Action<Animal>? GamingEvent;
    public event Action<Animal>? NightEvent;
    public event Action<Animal>? HungryEvent;

    // Durations
    private static readonly TimeSpan HappyDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NextPhaseDuration = TimeSpan.FromSeconds(10);

    private Animal? _selectedAnimal;
    private Animal? _subscribedAnimal;

    // Feeding guard
    private bool _isFeeding;

    // Loading guard (prevents false greetings during database load)
    private bool _isLoadingFromDatabase;

    // IDs panel visibility
    private bool _isIdListVisible;
    public bool IsIdListVisible
    {
        get => _isIdListVisible;
        set { if (_isIdListVisible != value) { _isIdListVisible = value; OnPropertyChanged(); } }
    }

    public ICommand ToggleIdListCommand { get; }

    /// <summary>Selected animal.</summary>
    public Animal? SelectedAnimal
    {
        get => _selectedAnimal;
        set
        {
            if (!Equals(_selectedAnimal, value))
            {
                if (_subscribedAnimal is not null)
                    _subscribedAnimal.PropertyChanged -= OnSelectedAnimalPropertyChanged;

                _selectedAnimal = value;
                OnPropertyChanged();

                _subscribedAnimal = _selectedAnimal;
                if (_subscribedAnimal is not null)
                    _subscribedAnimal.PropertyChanged += OnSelectedAnimalPropertyChanged;

                UpdateCurrentImage();
                OnPropertyChanged(nameof(SelectedAnimalStateL10n));
                OnPropertyChanged(nameof(SelectedAnimalTypeL10n));

                (MakeSoundCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (FeedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RemoveAnimalCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CrazyActionCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ToggleFlyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private LocalizableLogEntry? _selectedLogEntry;
    public LocalizableLogEntry? SelectedLogEntry
    {
        get => _selectedLogEntry;
        set { if (_selectedLogEntry != value) { _selectedLogEntry = value; OnPropertyChanged(); } }
    }

    private string _foodInput = string.Empty;
    public string FoodInput
    {
        get => _foodInput;
        set { if (_foodInput != value) { _foodInput = value; OnPropertyChanged(); } }
    }

    private IImage? _currentImage;
    /// <summary>Current image for the selected animal and its mood.</summary>
    public IImage? CurrentImage
    {
        get => _currentImage;
        private set { if (!ReferenceEquals(_currentImage, value)) { _currentImage = value; OnPropertyChanged(); } }
    }

    // Commands
    public ICommand RemoveAnimalCommand { get; }
    public ICommand MakeSoundCommand { get; }
    public ICommand FeedCommand { get; }
    public ICommand CrazyActionCommand { get; }
    public ICommand ToggleFlyCommand { get; }
    public ICommand ClearFoodCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand RemoveLogEntryByValueCommand { get; }
    public ICommand DropFoodCommand { get; }
    public ICommand RefreshStatsCommand { get; }
    // NOTE: legacy language cycle command removed; property kept nullable to avoid CS8618
    public ICommand? ChangeLanguageCommand { get; }

    private readonly Random _random = new();
    private readonly Dictionary<Animal, CancellationTokenSource> _flows = new();

    /// <summary>UI should display this alert text in a modal.</summary>
    public event Action<string>? AlertRequested;

    // --- Language picker (for ComboBox in XAML) ---
    /// <summary>
    /// Presentation model for a language entry in the ComboBox.
    /// </summary>
    public sealed class LanguageOption
    {
        public Language Code { get; }
        public string Display { get; }
        public LanguageOption(Language code, string display)
        {
            Code = code;
            Display = display;
        }
    }

    /// <summary>Available languages for selection in the UI.</summary>
    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();

    private LanguageOption? _selectedLanguageOption;

    /// <summary>
    /// Currently selected language option in the ComboBox.
    /// Setting this property immediately applies the language via ILocalizationService.
    /// </summary>
    public LanguageOption? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (!Equals(_selectedLanguageOption, value) && value is not null)
            {
                _selectedLanguageOption = value;
                OnPropertyChanged();
                // Apply language right away to provide instant feedback in the UI.
                _loc.SetLanguage(value.Code);
            }
        }
    }

    // Headers proxy (Type/Count/AverageAge)
    public LabelsProxy Labels { get; }

    /// <summary>
    /// Initializes a new instance with optional dependency injection.
    /// Falls back to InMemoryRepository if no repositories are provided.
    /// </summary>
    public MainWindowViewModel(IAnimalsRepository? animalsRepo = null, IEnclosureRepository? enclosureRepo = null, ILogger? logger = null)
    {
        _animalsRepo = animalsRepo ?? new InMemoryRepositoryAdapter();
        _enclosureRepo = enclosureRepo ?? new InMemoryEnclosureRepositoryAdapter();
        _logger = logger;

        Labels = new LabelsProxy(_loc);

        // --- Initialize language options (shown in ComboBox) ---
        LanguageOptions.Add(new LanguageOption(Language.ENG, "ENG"));
        LanguageOptions.Add(new LanguageOption(Language.RU,  "RU"));
        LanguageOptions.Add(new LanguageOption(Language.EST, "EST"));
        SelectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Code == _loc.CurrentLanguage)
                                 ?? LanguageOptions.FirstOrDefault();

        // Subscribe language changes
        _loc.LanguageChanged += () =>
        {
            // Keep ComboBox in sync if language set programmatically
            var need = LanguageOptions.FirstOrDefault(o => o.Code == _loc.CurrentLanguage);
            if (need is not null && !Equals(SelectedLanguageOption, need))
            {
                _selectedLanguageOption = need; // avoid reentrancy when raising PropertyChanged
                OnPropertyChanged(nameof(SelectedLanguageOption));
            }

            // Simple labels/buttons
            OnPropertyChanged(nameof(TextMakeSound));
            OnPropertyChanged(nameof(TextFeed));
            OnPropertyChanged(nameof(TextCrazyAction));
            OnPropertyChanged(nameof(TextToggleFly));
            OnPropertyChanged(nameof(TextClearLog));
            OnPropertyChanged(nameof(TextDropFood));
            OnPropertyChanged(nameof(TextRefreshStats));
            OnPropertyChanged(nameof(TextToggleIds));
            OnPropertyChanged(nameof(TextLanguageButton));
            OnPropertyChanged(nameof(TextFoodLabel));

            OnPropertyChanged(nameof(TextAnimalsHeader));
            OnPropertyChanged(nameof(TextDetailsHeader));
            OnPropertyChanged(nameof(TextAnimalIdsHeader));
            OnPropertyChanged(nameof(TextNameColon));
            OnPropertyChanged(nameof(TextAgeColon));
            OnPropertyChanged(nameof(TextStateColon));
            OnPropertyChanged(nameof(TextAddAnimal));
            OnPropertyChanged(nameof(TextRemoveAnimal));
            OnPropertyChanged(nameof(TextDropFoodFull));
            OnPropertyChanged(nameof(TextRefreshStatsFull));
            OnPropertyChanged(nameof(TextLogHeader));
            OnPropertyChanged(nameof(TextDeleteSelected));
            OnPropertyChanged(nameof(TextStatsHeader));
            OnPropertyChanged(nameof(TextHungryHeader));
            OnPropertyChanged(nameof(TextOldestHeader));
            OnPropertyChanged(nameof(TextFooterTips));
            OnPropertyChanged(nameof(TextAgeListSuffix));

            // Derived localized fields depending on selected animal
            OnPropertyChanged(nameof(SelectedAnimalStateL10n));
            OnPropertyChanged(nameof(SelectedAnimalTypeL10n));

            Labels.RaiseAllChanged();
            UpdateStats();
        };

        // Enclosure events
        _enclosure.AnimalJoinedInSameEnclosure += OnAnimalJoinedInSameEnclosure;
        _enclosure.FoodDropped += (_, e) =>
            LogEntries.Insert(0, new LocalizableLogEntry("Log.FoodDropped", e.When.ToShortTimeString()));

        // Load existing animals from database
        LoadAnimalsFromDatabase();

        SelectedAnimal = Animals.FirstOrDefault();

        RemoveAnimalCommand           = new RelayCommand(RemoveAnimal,       () => SelectedAnimal is not null);
        MakeSoundCommand              = new RelayCommand(MakeSound,          () => SelectedAnimal is not null);
        FeedCommand                   = new RelayCommand(Feed,               () => SelectedAnimal is not null);
        CrazyActionCommand            = new RelayCommand(CrazyAction,        () => SelectedAnimal is not null);
        ToggleFlyCommand              = new RelayCommand(ToggleFly,          () => SelectedAnimal is Flyable);
        ClearFoodCommand              = new RelayCommand(() => FoodInput = string.Empty);
        ClearLogCommand               = new RelayCommand(ClearLog);
        RemoveLogEntryByValueCommand  = new RelayCommand(RemoveLogEntryByValue);
        DropFoodCommand               = new RelayCommand(async () => await DropFoodAsync(), () => !_isFeeding && Animals.Count > 0);
        RefreshStatsCommand           = new RelayCommand(ResetAllToHungryAndRefresh);
        ToggleIdListCommand           = new RelayCommand(() => IsIdListVisible = !IsIdListVisible);
        // ChangeLanguageCommand intentionally not initialized (nullable) - selection is via ComboBox.

        // Localized timeline logs
        HappyEvent   += a => LogEntries.Insert(0, new LocalizableLogEntry("Log.Happy", a.Name));
        GamingEvent  += a => LogEntries.Insert(0, new LocalizableLogEntry("Log.Gaming", a.Name));
        NightEvent   += a => LogEntries.Insert(0, new LocalizableLogEntry("Log.Night", a.Name));
        HungryEvent  += a => LogEntries.Insert(0, new LocalizableLogEntry("Log.Hungry", a.Name));

        RebuildIdList();
        UpdateStats();
    }

    // --- Localized UI text (bind to these from XAML) ---
    public string TextMakeSound        => _loc["Buttons.MakeSound"];
    public string TextFeed             => _loc["Buttons.Feed"];
    public string TextCrazyAction      => _loc["Buttons.CrazyAction"];
    public string TextToggleFly        => _loc["Buttons.ToggleFly"];
    public string TextClearLog         => _loc["Buttons.ClearLog"];
    public string TextDropFood         => _loc["Buttons.DropFood"];
    public string TextRefreshStats     => _loc["Buttons.RefreshStats"];
    public string TextToggleIds        => _loc["Buttons.ToggleIds"];
    public string TextLanguageButton   => _loc["Buttons.Language"];
    public string TextFoodLabel        => _loc["Labels.Food"];

    public string TextAnimalsHeader    => _loc["Headers.Animals"];
    public string TextDetailsHeader    => _loc["Headers.Details"];
    public string TextAnimalIdsHeader  => _loc["Headers.AnimalIds"];
    public string TextNameColon        => _loc["Labels.Name"] + ":";
    public string TextAgeColon         => _loc["Labels.Age"] + ":";
    public string TextStateColon       => _loc["Labels.State"] + ":";

    public string TextAddAnimal        => _loc["Buttons.AddAnimal"];
    public string TextRemoveAnimal     => _loc["Buttons.RemoveAnimal"];
    public string TextDropFoodFull     => _loc["Buttons.DropFoodFull"];
    public string TextRefreshStatsFull => _loc["Buttons.RefreshStatsFull"];

    public string TextLogHeader        => _loc["Headers.Log"];
    public string TextDeleteSelected   => _loc["Buttons.DeleteSelected"];
    public string TextStatsHeader      => _loc["Headers.Stats"];
    public string TextHungryHeader     => _loc["Headers.HungryList"];
    public string TextOldestHeader     => _loc["Headers.Oldest"];
    public string TextFooterTips       => _loc["Footer.Tips"];

    /// <summary>Localized suffix for the animals list "( N y.o.)" part.</summary>
    public string TextAgeListSuffix    => _loc["List.AgeSuffixList"];

    /// <summary>
    /// Localized state line for the selected animal.
    /// Shows just the mood; for flyable animals adds a localized flight status (flying/on the ground).
    /// </summary>
    public string SelectedAnimalStateL10n
    {
        get
        {
            if (SelectedAnimal is null) return string.Empty;
            var mood = LocalizeMood(SelectedAnimal.Mood);
            var fly = GetFlyStatus(SelectedAnimal);
            return fly is null ? mood : $"{mood} • {fly}";
        }
    }

    /// <summary>
    /// Localized type name for the selected animal (e.g., "Dog", "Parrot").
    /// </summary>
    public string SelectedAnimalTypeL10n
    {
        get
        {
            if (SelectedAnimal is null) return string.Empty;
            return _loc[$"AnimalType.{SelectedAnimal.GetType().Name}"];
        }
    }

    /// <summary>Add an animal to all storages and log.</summary>
    public void AddAnimal(Animal animal)
    {
        if (string.IsNullOrWhiteSpace(animal.Name) || animal.Name == "Unnamed")
        {
            AlertRequested?.Invoke(_loc["Alerts.NameMissing"]);
            return;
        }

        _animalsRepo.Save(animal);
        _enclosureRepo.AssignToEnclosure(animal.UniqueId, "Main");
        _enclosure.Add(animal);
        Animals.Add(animal);

        AnimalIds.Add(animal.Identifier);

        var typeName = LocalizeAnimalType(animal.GetType().Name);
        LogEntries.Insert(0, new LocalizableLogEntry("Log.Added", animal.Name, typeName));
        _logger?.LogInfo($"Added animal: {animal.Name} ({typeName})");
        UpdateStats();
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void RemoveAnimal()
    {
        if (SelectedAnimal is null) return;
        CancelFlow(SelectedAnimal);
        var name = SelectedAnimal.Name;
        var uniqueId = SelectedAnimal.UniqueId;

        _animalsRepo.Delete(uniqueId);
        _enclosureRepo.RemoveFromEnclosure(uniqueId);
        _enclosure.Remove(SelectedAnimal);

        var idStr = SelectedAnimal.Identifier;
        Animals.Remove(SelectedAnimal);
        var idx = AnimalIds.IndexOf(idStr);
        if (idx >= 0) AnimalIds.RemoveAt(idx);

        SelectedAnimal = Animals.FirstOrDefault();
        LogEntries.Insert(0, new LocalizableLogEntry("Log.Removed", name));
        _logger?.LogInfo($"Removed animal: {name}");
        UpdateStats();
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async void MakeSound()
    {
        if (SelectedAnimal is null) return;

        var sound = SelectedAnimal.MakeSound();
        LogEntries.Insert(0, new LocalizableLogEntry($"{SelectedAnimal.Name}: {sound}"));

        try
        {
            var typeName = SelectedAnimal.GetType().Name;
            await SoundService.PlayAnimalVoiceAsync(typeName);
        }
        catch (Exception ex)
        {
            AlertRequested?.Invoke(string.Format(_loc["Alerts.SoundError"], ex.Message));
        }
    }

    private void Feed()
    {
        if (SelectedAnimal is null) return;

        if (SelectedAnimal.Mood != AnimalMood.Hungry)
        {
            AlertRequested?.Invoke(_loc["Alerts.AlreadyFed"]);
            return;
        }

        var food = string.IsNullOrWhiteSpace(FoodInput) ? _loc["Labels.Food"].ToLowerInvariant() : FoodInput.Trim();
        LogEntries.Insert(0, new LocalizableLogEntry("Log.AteFood", SelectedAnimal.Name, food));
        FoodInput = string.Empty;

        StartPostFeedSequence(SelectedAnimal);
        UpdateCurrentImage();
        UpdateStats();
    }

    private void CrazyAction()
    {
        if (SelectedAnimal is null) return;

        if (SelectedAnimal.Mood == AnimalMood.Sleeping)
        {
            AlertRequested?.Invoke(string.Format(_loc["Alerts.SleepingNoCrazy"], SelectedAnimal.Name));
            return;
        }

        if (SelectedAnimal is ICrazyAction actor)
        {
            var text = actor.ActCrazy(Animals.ToList());
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Animals already return localized strings - store as static
                LogEntries.Insert(0, new LocalizableLogEntry(text));
                AlertRequested?.Invoke(text);
            }
        }
        else
        {
            var msg = string.Format(_loc["Alerts.NothingCrazy"], SelectedAnimal.Name);
            LogEntries.Insert(0, new LocalizableLogEntry(msg));
            AlertRequested?.Invoke(msg);
        }

        UpdateStats();
    }

    /// <summary>
    /// Toggles flight state for any Flyable animal without hardcoding concrete classes.
    /// Discovers the best available operation in this order:
    /// ToggleFly() → Fly() → (TakeOff()/Land()) → direct write to IsFlying (fallback).
    /// Logs via generic keys (Log.FlyStart/Log.FlyStop) and updates image/state.
    /// </summary>
    private void ToggleFly()
    {
        if (SelectedAnimal is null) return;

        // Do not allow toggling while sleeping (UX rule).
        if (SelectedAnimal is Flyable && SelectedAnimal.Mood == AnimalMood.Sleeping)
        {
            AlertRequested?.Invoke(string.Format(_loc["Alerts.SleepingNoFly"], SelectedAnimal.Name));
            return;
        }

        // Only handle Flyable; otherwise nothing to toggle.
        if (SelectedAnimal is not Flyable)
            return;

        var type = SelectedAnimal.GetType();
        var wasFlying = IsAnimalFlying(SelectedAnimal);

        // Preferred method: ToggleFly()
        var toggle = type.GetMethod("ToggleFly", BindingFlags.Public | BindingFlags.Instance);

        // Alternative: Fly() (some classes implement "toggle" behavior via Fly())
        var fly = type.GetMethod("Fly", BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>());

        // Pair methods: TakeOff() / Land() — use depending on current state
        var takeOff = type.GetMethod("TakeOff", BindingFlags.Public | BindingFlags.Instance);
        var land    = type.GetMethod("Land",    BindingFlags.Public | BindingFlags.Instance);

        bool invoked = false;

        if (toggle is not null)
        {
            toggle.Invoke(SelectedAnimal, null);
            invoked = true;
        }
        else if (fly is not null)
        {
            fly.Invoke(SelectedAnimal, null);
            invoked = true;
        }
        else if (!wasFlying && takeOff is not null)
        {
            takeOff.Invoke(SelectedAnimal, null);
            invoked = true;
        }
        else if (wasFlying && land is not null)
        {
            land.Invoke(SelectedAnimal, null);
            invoked = true;
        }
        else
        {
            // Fallback: try flipping IsFlying if it is writable
            var isFlyingProp = type.GetProperty("IsFlying", BindingFlags.Public | BindingFlags.Instance);
            if (isFlyingProp?.CanWrite == true)
            {
                isFlyingProp.SetValue(SelectedAnimal, !wasFlying);
                invoked = true;
            }
        }

        // If something was invoked, evaluate the result and log via generic keys
        if (invoked)
        {
            var nowFlying = IsAnimalFlying(SelectedAnimal);
            if (nowFlying != wasFlying)
            {
                var key = nowFlying ? "Log.FlyStart" : "Log.FlyStop";
                LogEntries.Insert(0, new LocalizableLogEntry(key, SelectedAnimal.Name));
            }
        }

        UpdateCurrentImage();
        OnPropertyChanged(nameof(SelectedAnimalStateL10n));
        OnPropertyChanged(nameof(SelectedAnimalTypeL10n));
    }

    private async Task DropFoodAsync()
    {
        if (_isFeeding) return;

        // Check if any animals are hungry before starting the feeding process
        if (!Animals.Any(a => a.Mood == AnimalMood.Hungry))
        {
            AlertRequested?.Invoke(_loc["Alerts.NoHungryAnimals"]);
            return;
        }

        _isFeeding = true;
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();

        AlertRequested?.Invoke(_loc["Alerts.FeedingStarted"]);
        try
        {
            await _enclosure.DropFoodAsync(
                s => LogEntries.Insert(0, new LocalizableLogEntry(s)),
                onAte: animal =>
                {
                    if (animal.Mood == AnimalMood.Hungry)
                    {
                        StartPostFeedSequence(animal);
                        if (animal == SelectedAnimal) UpdateCurrentImage();
                    }
                    else
                    {
                        LogEntries.Insert(0, new LocalizableLogEntry("Log.IgnoredFood", animal.Name));
                    }
                }
            );
            AlertRequested?.Invoke(_loc["Alerts.FeedingFinished"]);
        }
        catch
        {
            AlertRequested?.Invoke(_loc["Alerts.FeedingFailed"]);
        }
        finally
        {
            _isFeeding = false;
            (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        UpdateStats();
    }

    // --- Post-feed sequence ---
    private void StartPostFeedSequence(Animal animal)
    {
        CancelFlow(animal);
        var cts = new CancellationTokenSource();
        _flows[animal] = cts;
        _ = RunSequenceAsync(animal, cts.Token);
    }

    private async Task RunSequenceAsync(Animal animal, CancellationToken token)
    {
        try
        {
            animal.SetMood(AnimalMood.Happy);
            HappyEvent?.Invoke(animal);
            if (animal == SelectedAnimal) UpdateCurrentImage();
            await Task.Delay(HappyDuration, token);

            var nextIsNight = _random.Next(2) == 0;

            if (nextIsNight)
            {
                animal.SetMood(AnimalMood.Sleeping);
                NightEvent?.Invoke(animal);
                if (animal == SelectedAnimal) UpdateCurrentImage();
                await Task.Delay(NextPhaseDuration, token);
            }
            else
            {
                animal.SetMood(AnimalMood.Gaming);
                GamingEvent?.Invoke(animal);
                if (animal == SelectedAnimal) UpdateCurrentImage();
                await Task.Delay(NextPhaseDuration, token);
            }

            animal.SetMood(AnimalMood.Hungry);
            HungryEvent?.Invoke(animal);
            if (animal == SelectedAnimal) UpdateCurrentImage();

            CancelFlow(animal);
        }
        catch (TaskCanceledException) { }
    }

    private void CancelFlow(Animal animal)
    {
        if (_flows.TryGetValue(animal, out var old))
        {
            old.Cancel();
            old.Dispose();
            _flows.Remove(animal);
        }
    }

    private void OnAnimalJoinedInSameEnclosure(object? sender, AnimalJoinedEventArgs e)
    {
        // Skip greetings during initial database load to prevent false triggers
        if (_isLoadingFromDatabase)
            return;

        foreach (var resident in e.CurrentResidents)
        {
            var reaction = resident.OnNeighborJoined(e.Newcomer);
            if (reaction is not null)
                LogEntries.Insert(0, new LocalizableLogEntry(reaction.LocalizationKey, reaction.Parameters));
        }
    }

    // --- Image handling ---
    private void OnSelectedAnimalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Animal.DisplayState) ||
            e.PropertyName == nameof(Animal.Mood) ||
            e.PropertyName == nameof(Animal.Name))
        {
            UpdateCurrentImage();
            UpdateStats();
            OnPropertyChanged(nameof(SelectedAnimalStateL10n));
            OnPropertyChanged(nameof(SelectedAnimalTypeL10n));
        }
    }

    private static bool IsAnimalFlying(Animal a)
    {
        if (a is not Flyable) return false;
        var prop = a.GetType().GetProperty("IsFlying", BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || prop.PropertyType != typeof(bool) || !prop.CanRead) return false;
        var value = prop.GetValue(a);
        return value is bool b && b;
    }

    /// <summary>Returns a localized fly status for flyable animals; otherwise null.</summary>
    private string? GetFlyStatus(Animal a)
    {
        if (a is not Flyable) return null;
        var isFlying = IsAnimalFlying(a);
        return _loc[isFlying ? "FlyingState.Flying" : "FlyingState.Perched"];
    }

    private void UpdateCurrentImage()
    {
        if (SelectedAnimal is null)
        {
            CurrentImage = null;
            return;
        }

        var typeName = SelectedAnimal.GetType().Name;
        var mood = SelectedAnimal.Mood.ToString().ToLowerInvariant();
        var exts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

        var names = new List<string>();
        if (IsAnimalFlying(SelectedAnimal))
        {
            names.Add($"flying_{mood}");
            names.Add("flying");
        }
        names.Add(mood);

        foreach (var name in names)
        {
            foreach (var ext in exts)
            {
                var uri = new Uri($"avares://AnimalZoo.App/Assets/{typeName}/{name}{ext}");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    CurrentImage = new Bitmap(stream);
                    return;
                }
            }
        }

        CurrentImage = null;
    }

    // --- LINQ Stats ---
    private void UpdateStats()
    {
        ByTypeStats.Clear();
        HungryStats.Clear();
        OldestStat = string.Empty;

        var animals = _animalsRepo.GetAll().ToList();
        if (animals.Count == 0) return;

        foreach (var row in animals
            .GroupBy(a => a.GetType().Name)
            .OrderBy(g => g.Key)
            .Select(g => new AnimalTypeStat
            {
                Type = LocalizeAnimalType(g.Key),
                Count = g.Count(),
                AverageAge = g.Average(a => a.Age)
            }))
        {
            ByTypeStats.Add(row);
        }

        var yearsShort = _loc["Labels.YearsShort"];

        foreach (var s in animals
            .Where(a => a.Mood == AnimalMood.Hungry)
            .OrderByDescending(a => a.Age)
            .Select(a => $"{a.Name} ({LocalizeAnimalType(a.GetType().Name)}, {a.Age}{yearsShort})"))
        {
            HungryStats.Add(s);
        }

        if (HungryStats.Count == 0)
            HungryStats.Add(_loc["Stats.NoHungry"]);

        var oldest = animals.OrderByDescending(a => a.Age).First();
        OldestStat = $"{oldest.Name} ({LocalizeAnimalType(oldest.GetType().Name)}, {oldest.Age}{yearsShort})";
    }

    private void ClearLog()
    {
        LogEntries.Clear();
        SelectedLogEntry = null;
    }

    private void RemoveLogEntryByValue()
    {
        if (SelectedLogEntry is null) return;
        var entry = SelectedLogEntry;
        var toRemove = LogEntries.FirstOrDefault(x => x == entry);
        if (toRemove != null)
        {
            LogEntries.Remove(toRemove);
            // After removal, keep selection at the top (newest entry first).
            SelectedLogEntry = LogEntries.FirstOrDefault();
        }
    }

    private void RebuildIdList()
    {
        AnimalIds.Clear();
        foreach (var a in Animals)
            AnimalIds.Add(a.Identifier);
    }

    // Helpers
    private string LocalizeMood(AnimalMood mood) => _loc[$"Mood.{mood}"];
    private string LocalizeAnimalType(string typeName) => _loc[$"AnimalType.{typeName}"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    /// <summary>
    /// Labels proxy so XAML can bind to localized column headers.
    /// Implements INotifyPropertyChanged to refresh on language switch.
    /// </summary>
    public sealed class LabelsProxy : INotifyPropertyChanged
    {
        private readonly ILocalizationService _loc;
        public LabelsProxy(ILocalizationService loc) => _loc = loc;

        public string Type => _loc["Labels.Type"];
        public string Count => _loc["Labels.Count"];
        public string AverageAge => _loc["Labels.AverageAge"];

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void RaiseAllChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AverageAge)));
        }
    }

    /// <summary>
    /// Resets all animals to Hungry, stops their running sequences,
    /// forces flying animals to land (if applicable), updates image/stats,
    /// and writes a localized log entry.
    /// </summary>
    private void ResetAllToHungryAndRefresh()
    {
        foreach (var a in Animals.ToList())
        {
            // Stop any running state machine
            CancelFlow(a);

            // Force land for flyables if they expose IsFlying/land operations
            if (a is Flyable)
            {
                var type = a.GetType();
                var isFlyingProp = type.GetProperty("IsFlying", BindingFlags.Public | BindingFlags.Instance);
                var landMethod = type.GetMethod("Land", BindingFlags.Public | BindingFlags.Instance);

                if (isFlyingProp is not null && landMethod is not null)
                {
                    var isFlying = isFlyingProp.GetValue(a) as bool? ?? false;
                    if (isFlying) landMethod.Invoke(a, null);
                }
            }

            a.SetMood(AnimalMood.Hungry);
        }

        LogEntries.Insert(0, new LocalizableLogEntry("Log.ResetAllHungry"));

        if (SelectedAnimal is not null)
            UpdateCurrentImage();

        UpdateStats();
    }

    /// <summary>
    /// Loads all animals from the database and populates the UI collections.
    /// Called during initialization to restore the previous state.
    /// </summary>
    private void LoadAnimalsFromDatabase()
    {
        try
        {
            // Set loading flag to prevent false greeting triggers
            _isLoadingFromDatabase = true;

            var animalsFromDb = _animalsRepo.GetAll().ToList();

            foreach (var animal in animalsFromDb)
            {
                // Add to UI collection (without saving to DB again)
                Animals.Add(animal);

                // Add to enclosure (greetings are suppressed during loading)
                _enclosure.Add(animal);

                // Add to ID list
                AnimalIds.Add(animal.Identifier);

                // Log the loaded animal
                _logger?.LogInfo($"Loaded animal from database: {animal.Name} ({animal.GetType().Name})");
            }

            if (animalsFromDb.Count > 0)
            {
                LogEntries.Insert(0, new LocalizableLogEntry($"Loaded {animalsFromDb.Count} animal(s) from database"));
                _logger?.LogInfo($"Successfully loaded {animalsFromDb.Count} animals from database");
            }

            UpdateStats();
        }
        catch (Exception ex)
        {
            // If database loading fails, log the error but don't crash the app
            LogEntries.Insert(0, new LocalizableLogEntry($"Failed to load animals from database: {ex.Message}"));
            _logger?.LogError($"Failed to load animals from database", ex);
        }
        finally
        {
            // Clear loading flag so new animals can trigger greetings
            _isLoadingFromDatabase = false;
        }
    }
}
