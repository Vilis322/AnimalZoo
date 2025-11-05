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
    - Implementations: `Cat`, `Dog`, `Bird`, `Eagle`, `Bat`, `Lion`, `Penguin`, `Fox`, `Parrot`, `Monkey`, `Raccoon`, `Turtle`.
- **States / moods**
    - `Hungry → Happy → Sleeping/Gaming → Hungry` (auto-cycle via timers).
    - Flyable animals cannot fly while `Sleeping`, auto-land on sleep.
- **UI (Avalonia)**
    - Modern, colorful interface with proper spacing and responsive layout.
    - Left panel: list of animals with add/remove buttons.
    - Right panel: details of selected animal and interactive actions:
        - Make sound, Crazy action, Toggle fly (for flyable animals)
        - Feed with inline clear button
    - **Dynamic image** for selected animal based on its mood:
        - `Assets/<Type>/<mood>.png` (supports png/jpg/jpeg/gif/webp)
        - Flyable animals also support `flying.png` and `flying_<mood>.png`
    - Action **log** with localized messages that update when language changes.
    - Buttons use Grid layout to prevent overlapping in non-fullscreen mode.
- **Add animal dialog**
    - Reflection-based type discovery (no hardcoding).
    - Name + age + type; supports future animal classes out of the box.
- **Localization**
    - Built-in multilingual support with **3 languages**: English 🇬🇧, Russian 🇷🇺, Estonian 🇪🇪.
    - All UI labels, buttons, alerts, logs, and messages are localized.
    - **Dynamic log localization**: log messages automatically update when language changes.
    - Language can be changed at runtime via the **language dropdown** in the main window.
- **Database persistence**
    - **SQL Server 2022** database running in Docker for data persistence.
    - **ADO.NET-based repositories** with CRUD operations for animals and enclosures.
    - All animal data persists between application restarts.
    - **Pluggable logging** with JSON or XML format support.
    - Automatic fallback to in-memory storage if database is unavailable.
    - Docker management via **Makefile** commands or manual Docker/dotnet commands.
- **Sound system**
    - Each animal has its own **voice.wav** stored under `Assets/<Animal>/`.
    - Pressing **Make Sound** plays the correct sound file for the selected animal.
    - Some animals also support additional effect sounds (e.g., **Dog** plays `crazy_action.wav` during crazy action).
- **Crazy actions**
    - Each animal can have unique "crazy" behavior, for example:
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
├─ Makefile                          # Build and Docker management automation
├─ docker-compose.yml                # SQL Server 2022 container configuration
├─ database-init.sql                 # Database schema initialization
├─ README.md, QUICK_START.md, MAKE.md, DATABASE_SETUP.md
└─ AnimalZoo.App/
   ├─ AnimalZoo.App.csproj
   ├─ appsettings.json               # Database and logging configuration
   ├─ Program.cs, App.axaml.cs
   ├─ Views/                         # Avalonia UI windows (MainWindow, AddAnimalWindow, AlertWindow)
   ├─ ViewModels/                    # MVVM view models with business logic
   ├─ Models/                        # Animal classes (Cat, Dog, Bird, Eagle, Bat, etc.)
   │  ├─ Enclosure/                  # Enclosure management and events
   │  └─ LocalizableLogEntry.cs      # Dynamic localization for log messages
   ├─ Interfaces/                    # Core interfaces (Flyable, ICrazyAction, ILogger, IAnimalsRepository, etc.)
   ├─ Localization/                  # Multilingual support (ENG, RU, EST)
   ├─ Repositories/                  # Data persistence (SQL + In-Memory adapters)
   │  ├─ SqlAnimalsRepository.cs     # ADO.NET implementation for animals
   │  └─ SqlEnclosureRepository.cs   # ADO.NET implementation for enclosures
   ├─ Logging/                       # Pluggable logging (XmlLogger, JsonLogger)
   ├─ Configuration/                 # Dependency injection setup
   ├─ Utils/                         # Helper services (AnimalFactory, SoundService, RelayCommand)
   └─ Assets/                        # Images, sounds, and i18n files
      ├─ Bat/, Bird/, Cat/, Dog/, Eagle/, Fox/, Lion/, Monkey/,
      │  Parrot/, Penguin/, Raccoon/, Turtle/  # Animal-specific assets
      └─ i18n/                       # JSON localization files
```
---

## **Quick Start**

```bash
# Clone repository
git clone https://github.com/<your-user>/AnimalZoo.git
cd AnimalZoo

# Start database
make docker-up && make docker-init

# Build and run
make build && make run
```

For detailed setup instructions, Docker management, and database operations, see:
- **[QUICK_START.md](./QUICK_START.md)** - Complete setup guide with and without Make
- **[MAKE.md](./MAKE.md)** - All available Make commands
- **[DATABASE_SETUP.md](./DATABASE_SETUP.md)** - Database initialization and schema details
- - **[IMPLEMENTATION_DB_SUMMARY.md](./IMPLEMENTATION_DB_SUMMARY.md)** - Architecture details

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