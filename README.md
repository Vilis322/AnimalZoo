# AnimalZoo (Avalonia + OOP Demo, .NET 9)

---

##  Project Description

**AnimalZoo** is a simple desktop application created for educational purposes.  
The goal is to practice **object-oriented programming (OOP)** principles and build a GUI using **Avalonia UI**.
Each animal is an object with its own properties, behavior, and dynamic state transitions.

 ---

## ***Features***

- **OOP core**
    - `abstract class Animal { Name, Age; virtual Describe(); abstract MakeSound(); }`
    - Interfaces: `Flyable`, `ICrazyAction`.
    - Implementations: `Cat`, `Dog`, `Bird`.
- **States / moods**
    - `Hungry → Happy → Sleeping/Gaming → Hungry` (auto-cycle via timers).
    - **Bird** cannot fly while `Sleeping`, auto-lands on sleep.
- **UI (Avalonia)**
    - Left: list of animals (add/remove).
    - Right: details of selected (name, age, state) and actions:
        - Make sound, Crazy action, Toggle fly (bird)
        - Feed with inline clear button
    - **Dynamic image** for selected animal based on its mood:
        - `Assets/<Type>/<mood>.png` (png/jpg/jpeg/gif/webp)
        - Bird also supports `flying.png` (and optionally `flying_<mood>.png`)
    - Bottom: action **log** with right-click **delete** per entry, **clear** button.
- **Add animal dialog**
    - Reflection-based type discovery (no hardcoding).
    - Name + age + type; supports future animal classes out of the box.
- **Localization**
    - Built-in multilingual support with **3 languages**: English 🇬🇧, Russian 🇷🇺, Estonian 🇪🇪.
    - All UI labels, buttons, alerts, logs, and messages are localized.
    - Language can be changed at runtime via the **language dropdown** in the main window.
- **Sound system**
    - Each animal has its own **voice.wav** stored under `Assets/<Animal>/`.
    - Pressing **Make Sound** plays the correct sound file for the selected animal.
    - Some animals also support additional effect sounds (e.g., **Dog** plays `crazy_action.wav` during crazy action).
- **Crazy actions**
    - Each animal can have unique “crazy” behavior, for example:
        - **Dog**: performs a multi-bark with a dedicated sound effect.
        - **Monkey**: swaps names between animals.
        - **Parrot**: mimics sounds of other animals; if no suitable targets are available, shows a localized alert.
    - All crazy actions are fully localized and logged.
- **Flight mechanics**
    - Any animal implementing `Flyable` can toggle between flying and grounded states.
    - Unified logic for all flying animals (no class-specific hardcoding).
    - Dynamic UI state updates and localized flight status display.
- **LINQ-based statistics**
    - Real-time table summarizing animal counts and average ages by type.
    - Separate lists for hungry animals and the oldest one.
    - Fully scrollable statistics panel with live updates.
- **Alerts and dialogs**
    - All alert windows use localized messages and button labels.
    - Centralized alert request mechanism integrated with ViewModel layer.

---

## **Architecture** 
```bash
AnimalZoo/
├─ AnimalZoo.sln
├─ Makefile
├─ README.md
└─ AnimalZoo.App/
   ├─ AnimalZoo.App.csproj
   ├─ app.manifest  
   ├─ Program.cs
   ├─ App.axaml
   ├─ App.axaml.cs
   ├─ Views/
   │  ├─ MainWindow.axaml
   │  ├─ MainWindow.axaml.cs
   │  ├─ AddAnimalWindow.axaml
   │  ├─ AddAnimalWindow.axaml.cs
   │  ├─ AlertWindow.axaml
   │  └─ AlertWindow.axaml.cs
   ├─ ViewModels/
   │  ├─ MainWindowViewModel.cs        
   │  └─ AddAnimalViewModel.cs         
   ├─ Models/
   │  ├─ Enclosure/
   │  │  ├─ Enclosure.cs
   │  │  └─ EnclosureEvents.cs
   │  ├─ Animal.cs                     
   │  ├─ AnimalMood.cs                 
   │  ├─ AnimalText.cs   
   │  ├─ Bat.cs
   │  ├─ Bird.cs
   │  ├─ Cat.cs
   │  ├─ Dog.cs
   │  ├─ Eagle.cs
   │  ├─ Fox.cs
   │  ├─ Lion.cs
   │  ├─ Monkey.cs
   │  ├─ Parrot.cs
   │  ├─ Penguin.cs
   │  ├─ Raccoon.cs
   │  └─ Turtle.cs                        
   ├─ Interfaces/
   │  ├─ Flyable.cs                    
   │  ├─ ICrazyAction.cs  
   │  └─ IRepository.cs 
   ├─ Localization/
   │  ├─ ILocalizationService.cs
   │  ├─ Language.cs
   │  ├─ Loc.cs
   │  └─ LocalizationService.cs  
   ├─ Repositories/
   │  └─ InMemoryRepository.cs      
   ├─ Utils/
   │  ├─ AnimalFactory.cs
   │  ├─ AssetService.cs            
   │  ├─ RelayCommand.cs     
   │  └─ SoundService.cs               
   └─ Assets/
      ├─ Bat/
      ├─ Bird/
      ├─ Cat/
      ├─ Dog/
      ├─ Eagle/
      ├─ Fox/      
      ├─ Lion/
      ├─ Monkey/
      ├─ Parrot/
      ├─ Penguin/
      ├─ Raccoon/
      ├─ Turtle/
      └─ i18n/  # .json files with supported localization keys              
```
---

## **Quick Start**
## 🚀 Quick Start

```bash
# Clone repository
git clone https://github.com/<your-user>/AnimalZoo.git
cd AnimalZoo

# Build
make build

# Run the app
make run

# Clean build files
make clean
```
 ---

## **First Issue** (university task) -> CLOSED 
- Create three new animal types that inherit from Animal and optionally implement Flyable or ICrazyAction.
- Each class must:
  - Implement its own MakeSound() method.
  - Optionally override Describe().
  - Have at least one unique “crazy” action.
  - Provide corresponding images in Assets/<AnimalName>/. (optional)

## **Second Issue** (university task, issue is the same as the previous one) -> CLOSED
- Create three new animal types that inherit from Animal and optionally implement Flyable or ICrazyAction.
- Each class must:
    - Implement its own MakeSound() method.
    - Optionally override Describe().
    - Have at least one unique “crazy” action.
    - Provide corresponding images in Assets/<AnimalName>/. (optional)