using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Models;
using AnimalZoo.App.Repositories;
using AnimalZoo.App.Utils;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Generic;

namespace AnimalZoo.App.ViewModels;

/// <summary>Row for "By type" statistics table.</summary>
public sealed class AnimalTypeStat
{
    /// <summary>Type name (e.g., Cat, Dog).</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Total animals of this type.</summary>
    public int Count { get; init; }
    /// <summary>Average age across this type.</summary>
    public double AverageAge { get; init; }
}

/// <summary>
/// Main view model with MVVM bindings, repository, enclosure usage,
/// event-driven state machine per animal (Happy → Random(Night|Gaming) → Hungry),
/// and LINQ statistics.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    // UI-visible collections
    public ObservableCollection<Animal> Animals { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

    // Stats (table + lists)
    public ObservableCollection<AnimalTypeStat> ByTypeStats { get; } = new();
    public ObservableCollection<string> HungryStats { get; } = new();
    private string _oldestStat = string.Empty;
    public string OldestStat
    {
        get => _oldestStat;
        private set { if (_oldestStat != value) { _oldestStat = value; OnPropertyChanged(); } }
    }

    // Repository and enclosure
    private readonly IRepository<Animal> _repo = new InMemoryRepository<Animal>();
    private readonly Enclosure<Animal> _enclosure = new();

    // Event-driven states
    public event Action<Animal>? HappyEvent;
    public event Action<Animal>? GamingEvent;
    public event Action<Animal>? NightEvent;

    // Durations
    private static readonly TimeSpan HappyDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NextPhaseDuration = TimeSpan.FromSeconds(10);

    private Animal? _selectedAnimal;
    private Animal? _subscribedAnimal;

    // Feeding re-entrance guard
    private bool _isFeeding = false;

    /// <summary>Currently selected animal.</summary>
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

    private readonly Random _random = new();

    // Per-animal sequence cancellation
    private readonly Dictionary<Animal, CancellationTokenSource> _flows = new();

    /// <summary>Raised when UI should show a modal alert.</summary>
    public event Action<string>? AlertRequested;

    public MainWindowViewModel()
    {
        // Seed data
        var cat = new Cat("Murr", 3);
        var dog = new Dog("Rex", 5);
        var bird = new Bird("Kiwi", 1);

        AddAnimal(cat);
        AddAnimal(dog);
        AddAnimal(bird);

        // Enclosure event: neighbors react
        _enclosure.AnimalJoinedInSameEnclosure += OnAnimalJoinedInSameEnclosure;
        _enclosure.FoodDropped += (_, e) =>
            LogEntries.Add($"Food dropped at {e.When:T}. Feeding order will be displayed...");

        SelectedAnimal = Animals.FirstOrDefault();

        RemoveAnimalCommand           = new RelayCommand(RemoveAnimal,       () => SelectedAnimal is not null);
        MakeSoundCommand              = new RelayCommand(MakeSound,          () => SelectedAnimal is not null);
        FeedCommand                   = new RelayCommand(Feed,               () => SelectedAnimal is not null);
        CrazyActionCommand            = new RelayCommand(CrazyAction,        () => SelectedAnimal is not null);
        ToggleFlyCommand              = new RelayCommand(ToggleFly,          () => SelectedAnimal is Flyable);
        ClearFoodCommand              = new RelayCommand(() => FoodInput = string.Empty);
        ClearLogCommand               = new RelayCommand(ClearLog);
        RemoveLogEntryByValueCommand  = new RelayCommand(RemoveLogEntryByValue);
        // Guarded command: disabled while feeding is in progress
        DropFoodCommand               = new RelayCommand(async () => await DropFoodAsync(), () => !_isFeeding && Animals.Count > 0);
        RefreshStatsCommand           = new RelayCommand(ResetAllToHungryAndRefresh);
        
        HappyEvent  += a => LogEntries.Add($"{a.Name} is happy.");
        GamingEvent += a => LogEntries.Add($"{a.Name} is gaming.");
        NightEvent  += a => LogEntries.Add($"{a.Name} fell asleep for the night.");

        UpdateStats();
    }

    /// <summary>Adds an animal to repository, enclosure and UI list.</summary>
    public void AddAnimal(Animal animal)
    {
        _repo.Add(animal);
        _enclosure.Add(animal);
        Animals.Add(animal);
        LogEntries.Add($"Added {animal.Name} ({animal.GetType().Name}).");
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
        Animals.Remove(SelectedAnimal);

        SelectedAnimal = Animals.FirstOrDefault();
        LogEntries.Add($"Removed {name}.");
        UpdateStats();
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void MakeSound()
    {
        if (SelectedAnimal is null) return;
        var sound = SelectedAnimal.MakeSound();
        LogEntries.Add($"{SelectedAnimal.Name}: {sound}");
    }

    private void Feed()
    {
        if (SelectedAnimal is null) return;

        if (SelectedAnimal.Mood != AnimalMood.Hungry)
        {
            AlertRequested?.Invoke("You have already fed the animal and it is not hungry now.");
            return;
        }

        var food = string.IsNullOrWhiteSpace(FoodInput) ? "food" : FoodInput.Trim();
        LogEntries.Add($"{SelectedAnimal.Name} ate {food}.");
        FoodInput = string.Empty;

        StartPostFeedSequence(SelectedAnimal);
        UpdateCurrentImage();
        UpdateStats();
    }

    private void CrazyAction()
    {
        if (SelectedAnimal is null) return;

        // Unified rule: sleeping animals cannot perform crazy actions
        if (SelectedAnimal.Mood == AnimalMood.Sleeping)
        {
            AlertRequested?.Invoke($"{SelectedAnimal.Name} is sleeping and cannot perform a crazy action now.");
            return;
        }

        if (SelectedAnimal is ICrazyAction actor)
        {
            var text = actor.ActCrazy(Animals.ToList());
            if (!string.IsNullOrWhiteSpace(text))
            {
                LogEntries.Add(text);
                AlertRequested?.Invoke(text); // also show alert
            }
        }
        else
        {
            var msg = $"{SelectedAnimal.Name} has nothing crazy to do.";
            LogEntries.Add(msg);
            AlertRequested?.Invoke(msg);
        }

        UpdateStats();
    }

    private void ToggleFly()
    {
        if (SelectedAnimal is null) return;

        if ((SelectedAnimal is Bird bird && bird.Mood == AnimalMood.Sleeping) ||
            (SelectedAnimal is Eagle eagle && eagle.Mood == AnimalMood.Sleeping))
        {
            AlertRequested?.Invoke($"{SelectedAnimal.Name} is sleeping and cannot fly now.");
            return;
        }

        if (SelectedAnimal is Bird b)
        {
            var wasFlying = b.IsFlying;
            b.ToggleFly();

            if (b.IsFlying && !wasFlying)
                LogEntries.Add($"{b.Name} took off and shouts 'CHIRP!!!'");
            else if (!b.IsFlying && wasFlying)
                LogEntries.Add($"{b.Name} landed.");
        }
        else if (SelectedAnimal is Eagle e)
        {
            var wasFlying = e.IsFlying;
            e.ToggleFly();

            if (e.IsFlying && !wasFlying)
                LogEntries.Add($"{e.Name} soars into the sky with a mighty screech!");
            else if (!e.IsFlying && wasFlying)
                LogEntries.Add($"{e.Name} folds its wings and perches.");
        }
        else if (SelectedAnimal is Flyable f)
        {
            f.Fly();
        }

        UpdateCurrentImage();
    }

    private async Task DropFoodAsync()
    {
        if (_isFeeding) return; // safety double-click guard

        _isFeeding = true;
        (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();

        AlertRequested?.Invoke("Feeding started. Watch the log for the order and progress.");
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
                        LogEntries.Add($"{animal.Name} was not hungry and ignored the food.");
                    }
                }
            );
            AlertRequested?.Invoke("Feeding finished.");
        }
        catch (Exception ex)
        {
            LogEntries.Add($"Feeding error: {ex.Message}");
            AlertRequested?.Invoke("Feeding failed. See log for details.");
        }
        finally
        {
            _isFeeding = false;
            (DropFoodCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        UpdateStats();
    }

    // ---- Event-driven sequence per animal ----

    /// <summary>
    /// Start: Happy (5s) → Random: Night(Sleeping 10s) or Gaming(10s) → Hungry (stop).
    /// Cancels any prior flow for that animal.
    /// </summary>
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
            // Happy phase
            animal.SetMood(AnimalMood.Happy);
            HappyEvent?.Invoke(animal);
            if (animal == SelectedAnimal) UpdateCurrentImage();
            await Task.Delay(HappyDuration, token);

            // Random next: Night (Sleeping) OR Gaming
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

            // End at Hungry and stop
            animal.SetMood(AnimalMood.Hungry);
            if (animal == SelectedAnimal) UpdateCurrentImage();

            CancelFlow(animal);
        }
        catch (TaskCanceledException)
        {
            // interrupted — do nothing
        }
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

    // --- Enclosure neighbor reactions ---

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
        if (e.PropertyName == nameof(Animal.DisplayState) || e.PropertyName == nameof(Animal.Mood) || e.PropertyName == nameof(Animal.Name))
        {
            UpdateCurrentImage();
            UpdateStats();
        }
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
        if ((SelectedAnimal is Bird bird && bird.IsFlying) ||
            (SelectedAnimal is Eagle eagle && eagle.IsFlying))
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

    // --- LINQ Stats & reset ---

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
            .Select(g => new AnimalTypeStat { Type = g.Key, Count = g.Count(), AverageAge = g.Average(a => a.Age) }))
        {
            ByTypeStats.Add(row);
        }

        foreach (var s in animals
            .Where(a => a.Mood == AnimalMood.Hungry)
            .OrderByDescending(a => a.Age)
            .Select(a => $"{a.Name} ({a.GetType().Name}, {a.Age}y)"))
        {
            HungryStats.Add(s);
        }
        if (HungryStats.Count == 0) HungryStats.Add("— none —");

        var oldest = animals.OrderByDescending(a => a.Age).First();
        OldestStat = $"{oldest.Name} ({oldest.GetType().Name}, {oldest.Age}y)";
    }

    /// <summary>
    /// Reset all animals to Hungry, cancel all flows, refresh stats and image.
    /// Used by Refresh Stats per assignment.
    /// </summary>
    private void ResetAllToHungryAndRefresh()
    {
        foreach (var a in Animals.ToList())
        {
            CancelFlow(a);
            a.SetMood(AnimalMood.Hungry);
        }
        UpdateCurrentImage();
        UpdateStats();
        LogEntries.Add("All animals reset to Hungry (via Refresh Stats).");
    }

    // --- Log management ---

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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

