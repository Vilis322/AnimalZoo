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
/// This version wires localization for headers, logs, states and age suffix.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    // --- Localization service ---
    private readonly ILocalizationService _loc = Loc.Instance;

    // UI-visible collections
    public ObservableCollection<Animal> Animals { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();
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
    private readonly IRepository<Animal> _repo = new InMemoryRepository<Animal>();
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

                (MakeSoundCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (FeedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RemoveAnimalCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CrazyActionCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ToggleFlyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private string? _selectedLogEntry;
    public string? SelectedLogEntry
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
    public ICommand ChangeLanguageCommand { get; }

    private readonly Random _random = new();
    private readonly Dictionary<Animal, CancellationTokenSource> _flows = new();

    /// <summary>UI should display this alert text in a modal.</summary>
    public event Action<string>? AlertRequested;

    // Headers proxy (Type/Count/AverageAge)
    public LabelsProxy Labels { get; }

    public MainWindowViewModel()
    {
        Labels = new LabelsProxy(_loc);

        // Subscribe language changes
        _loc.LanguageChanged += () =>
        {
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

            OnPropertyChanged(nameof(SelectedAnimalStateL10n));

            Labels.RaiseAllChanged();
            UpdateStats();
        };

        // Seed demo
        var cat = new Cat("Murr", 3);
        var dog = new Dog("Rex", 5);
        var bird = new Bird("Kiwi", 1);

        AddAnimal(cat);
        AddAnimal(dog);
        AddAnimal(bird);

        // Enclosure events
        _enclosure.AnimalJoinedInSameEnclosure += OnAnimalJoinedInSameEnclosure;
        _enclosure.FoodDropped += (_, e) =>
            LogEntries.Add(string.Format(_loc["Log.FoodDropped"], e.When.ToShortTimeString()));

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
        ChangeLanguageCommand         = new RelayCommand(CycleLanguage);

        // Localized timeline logs
        HappyEvent   += a => LogEntries.Add(string.Format(_loc["Log.Happy"], a.Name));
        GamingEvent  += a => LogEntries.Add(string.Format(_loc["Log.Gaming"], a.Name));
        NightEvent   += a => LogEntries.Add(string.Format(_loc["Log.Night"], a.Name));
        HungryEvent  += a => LogEntries.Add(string.Format(_loc["Log.Hungry"], a.Name));

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

    /// <summary>Localized state of the selected animal.</summary>
    public string SelectedAnimalStateL10n
    {
        get
        {
            if (SelectedAnimal is null) return string.Empty;
            var mood = LocalizeMood(SelectedAnimal.Mood);
            return $"{SelectedAnimal.Name} • {mood}";
        }
    }

    /// <summary>Cycle language: ENG → RU → EST → ENG.</summary>
    private void CycleLanguage()
    {
        var next = _loc.CurrentLanguage switch
        {
            Language.ENG => Language.RU,
            Language.RU  => Language.EST,
            _            => Language.ENG
        };
        _loc.SetLanguage(next);
    }

    /// <summary>Add an animal to all storages and log.</summary>
    public void AddAnimal(Animal animal)
    {
        if (string.IsNullOrWhiteSpace(animal.Name) || animal.Name == "Unnamed")
        {
            AlertRequested?.Invoke(_loc["Alerts.NameMissing"]);
            return;
        }

        _repo.Add(animal);
        _enclosure.Add(animal);
        Animals.Add(animal);

        AnimalIds.Add(animal.Identifier);

        var typeName = LocalizeAnimalType(animal.GetType().Name);
        LogEntries.Add(string.Format(_loc["Log.Added"], animal.Name, typeName));
        UpdateStats();
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void RemoveAnimal()
    {
        if (SelectedAnimal is null) return;
        CancelFlow(SelectedAnimal);
        var name = SelectedAnimal.Name;

        _repo.Remove(SelectedAnimal);
        _enclosure.Remove(SelectedAnimal);

        var idStr = SelectedAnimal.Identifier;
        Animals.Remove(SelectedAnimal);
        var idx = AnimalIds.IndexOf(idStr);
        if (idx >= 0) AnimalIds.RemoveAt(idx);

        SelectedAnimal = Animals.FirstOrDefault();
        LogEntries.Add(string.Format(_loc["Log.Removed"], name));
        UpdateStats();
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async void MakeSound()
    {
        if (SelectedAnimal is null) return;

        var sound = SelectedAnimal.MakeSound();
        LogEntries.Add($"{SelectedAnimal.Name}: {sound}");

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
        LogEntries.Add(string.Format(_loc["Log.AteFood"], SelectedAnimal.Name, food));
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
                LogEntries.Add(text);   // animals already return localized strings
                AlertRequested?.Invoke(text);
            }
        }
        else
        {
            var msg = string.Format(_loc["Alerts.NothingCrazy"], SelectedAnimal.Name);
            LogEntries.Add(msg);
            AlertRequested?.Invoke(msg);
        }

        UpdateStats();
    }

    private void ToggleFly()
    {
        if (SelectedAnimal is null) return;

        if (SelectedAnimal is Flyable && SelectedAnimal.Mood == AnimalMood.Sleeping)
        {
            AlertRequested?.Invoke(string.Format(_loc["Alerts.SleepingNoFly"], SelectedAnimal.Name));
            return;
        }

        switch (SelectedAnimal)
        {
            case Bird b:
                {
                    var wasFlying = b.IsFlying;
                    b.ToggleFly();
                    if (b.IsFlying && !wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.BirdTakeOff"], b.Name));
                    else if (!b.IsFlying && wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.BirdLand"], b.Name));
                    break;
                }
            case Eagle e:
                {
                    var wasFlying = e.IsFlying;
                    e.ToggleFly();
                    if (e.IsFlying && !wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.EagleTakeOff"], e.Name));
                    else if (!e.IsFlying && wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.EagleLand"], e.Name));
                    break;
                }
            case Parrot p:
                {
                    var wasFlying = p.IsFlying;
                    p.Fly();
                    if (p.IsFlying && !wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.ParrotFly"], p.Name));
                    else if (!p.IsFlying && wasFlying)
                        LogEntries.Add(string.Format(_loc["Log.ParrotLand"], p.Name));
                    break;
                }
            case Flyable f:
                {
                    f.Fly();
                    break;
                }
        }

        UpdateCurrentImage();
    }

    private async Task DropFoodAsync()
    {
        if (_isFeeding) return;

        _isFeeding = true;
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();

        AlertRequested?.Invoke(_loc["Alerts.FeedingStarted"]);
        try
        {
            await _enclosure.DropFoodAsync(
                s => LogEntries.Add(s),
                onAte: animal =>
                {
                    if (animal.Mood == AnimalMood.Hungry)
                    {
                        StartPostFeedSequence(animal);
                        if (animal == SelectedAnimal) UpdateCurrentImage();
                    }
                    else
                    {
                        LogEntries.Add(string.Format(_loc["Log.IgnoredFood"], animal.Name));
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
        foreach (var resident in e.CurrentResidents)
        {
            var reaction = resident.OnNeighborJoined(e.Newcomer);
            if (!string.IsNullOrWhiteSpace(reaction))
                LogEntries.Add(reaction);
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

        var animals = _repo.GetAll().ToList();
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
            SelectedLogEntry = LogEntries.LastOrDefault();
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

        LogEntries.Add(_loc["Log.ResetAllHungry"]);

        if (SelectedAnimal is not null)
            UpdateCurrentImage();

        UpdateStats();
    }
}
