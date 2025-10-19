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
using AnimalZoo.App.Utils;
using Avalonia.Media;                 
using Avalonia.Media.Imaging;         
using Avalonia.Platform;              

namespace AnimalZoo.App.ViewModels;

/// <summary>
/// Main view model holding animals, selected animal, actions, log and current image binding.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Animal> Animals { get; } = new();

    private Animal? _selectedAnimal;
    private Animal? _subscribedAnimal;

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
            }
        }
    }

    /// <summary>Log entries displayed at the bottom.</summary>
    public ObservableCollection<string> LogEntries { get; } = new();

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

    public ICommand RemoveAnimalCommand { get; }
    public ICommand MakeSoundCommand { get; }
    public ICommand FeedCommand { get; }
    public ICommand CrazyActionCommand { get; }
    public ICommand ToggleFlyCommand { get; }
    public ICommand ClearFoodCommand { get; }

    public ICommand ClearLogCommand { get; }
    public ICommand RemoveLogEntryByValueCommand { get; }

    private readonly Random _random = new();
    private readonly TimeSpan _phaseDuration = TimeSpan.FromSeconds(20);

    private readonly System.Collections.Generic.Dictionary<Animal, CancellationTokenSource> _moodFlows = new();

    /// <summary>Raised when UI should show a modal alert.</summary>
    public event Action<string>? AlertRequested;

    public MainWindowViewModel()
    {
        var cat = new Cat("Murr", 3);
        var dog = new Dog("Rex", 5);
        var bird = new Bird("Kiwi", 1);

        Animals.Add(cat);
        Animals.Add(dog);
        Animals.Add(bird);

        SelectedAnimal = Animals.FirstOrDefault();

        RemoveAnimalCommand = new RelayCommand(RemoveAnimal, () => SelectedAnimal is not null);
        MakeSoundCommand = new RelayCommand(MakeSound,    () => SelectedAnimal is not null);
        FeedCommand = new RelayCommand(Feed,         () => SelectedAnimal is not null);
        CrazyActionCommand = new RelayCommand(CrazyAction,  () => SelectedAnimal is not null);
        ToggleFlyCommand = new RelayCommand(ToggleFly,    () => SelectedAnimal is Bird);
        ClearFoodCommand = new RelayCommand(() => FoodInput = string.Empty);

        ClearLogCommand = new RelayCommand(ClearLog);
        RemoveLogEntryByValueCommand = new RelayCommand(RemoveLogEntryByValue);
    }

    public void AddAnimal(Animal animal)
    {
        Animals.Add(animal);
        SelectedAnimal = animal;
        LogEntries.Add($"Added {animal.Name} ({animal.GetType().Name}).");
    }

    private void RemoveAnimal()
    {
        if (SelectedAnimal is null) return;
        CancelMoodFlow(SelectedAnimal);
        var name = SelectedAnimal.Name;
        Animals.Remove(SelectedAnimal);
        SelectedAnimal = Animals.FirstOrDefault();
        LogEntries.Add($"Removed {name}.");
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

        SelectedAnimal.SetMood(AnimalMood.Happy);
        StartMoodFlow(SelectedAnimal);
        UpdateCurrentImage();
    }

    private void CrazyAction()
    {
        if (SelectedAnimal is null) return;
        
        if (SelectedAnimal is Bird bird1 && bird1.Mood == AnimalMood.Sleeping)
        {
            // Bird's crazy action toggles flight; disallow while sleeping
            AlertRequested?.Invoke($"{bird1.Name} is sleeping and cannot fly now.");
            return;
        }

        if (SelectedAnimal is ICrazyAction actor)
        {
            var text = actor.ActCrazy(Animals.ToList());
            if (!string.IsNullOrWhiteSpace(text))
                LogEntries.Add(text);
        }
        else
        {
            LogEntries.Add($"{SelectedAnimal.Name} has nothing crazy to do.");
        }
    }

    private void ToggleFly()
    {
        if (SelectedAnimal is Bird b)
        {
            if (b.Mood == AnimalMood.Sleeping)
            {
                AlertRequested?.Invoke($"{b.Name} is sleeping and cannot fly now.");
                return;
            }

            var wasFlying = b.IsFlying;
            b.ToggleFly();

            if (b.IsFlying && !wasFlying)
                LogEntries.Add($"{b.Name} took off and shouts 'CHIRP!!!'");
            else if (!b.IsFlying && wasFlying)
                LogEntries.Add($"{b.Name} landed.");
        }
    }

    private void StartMoodFlow(Animal animal)
    {
        CancelMoodFlow(animal);
        var cts = new CancellationTokenSource();
        _moodFlows[animal] = cts;

        _ = RunMoodFlowAsync(animal, cts.Token);
    }

    private async Task RunMoodFlowAsync(Animal animal, CancellationToken token)
    {
        try
        {
            await Task.Delay(_phaseDuration, token);
            var mid = _random.Next(2) == 0 ? AnimalMood.Sleeping : AnimalMood.Gaming;
            animal.SetMood(mid);
            if (animal == SelectedAnimal) UpdateCurrentImage();

            await Task.Delay(_phaseDuration, token);
            animal.SetMood(AnimalMood.Hungry);
            if (animal == SelectedAnimal) UpdateCurrentImage();
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
    }

    private void CancelMoodFlow(Animal animal)
    {
        if (_moodFlows.TryGetValue(animal, out var old))
        {
            old.Cancel();
            old.Dispose();
            _moodFlows.Remove(animal);
        }
    }

    // --- Image handling ---

    private void OnSelectedAnimalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Animal.DisplayState) || e.PropertyName == nameof(Animal.Mood))
        {
            UpdateCurrentImage();
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

        var names = new System.Collections.Generic.List<string>();
        if (SelectedAnimal is Bird bird && bird.IsFlying)
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
