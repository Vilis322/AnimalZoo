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
   │  ├─ Animal.cs                     
   │  ├─ AnimalMood.cs                 
   │  ├─ Cat.cs
   │  ├─ Dog.cs
   │  └─ Bird.cs                       
   ├─ Interfaces/
   │  ├─ Flyable.cs                    
   │  └─ ICrazyAction.cs               
   ├─ Utils/
   │  ├─ RelayCommand.cs
   │  ├─ AnimalFactory.cs             
   │  └─ AssetService.cs               
   └─ Assets/
      ├─ Cat/
      │  ├─ hungry.png
      │  ├─ happy.png
      │  ├─ sleeping.png
      │  └─ gaming.png
      ├─ Dog/
      │  ├─ hungry.png
      │  ├─ happy.png
      │  ├─ sleeping.png
      │  └─ gaming.png
      └─ Bird/
         ├─ hungry.png
         ├─ happy.png
         ├─ sleeping.png
         ├─ gaming.png
         └─ flying.png                 
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

## **Issue** (university task)
- Create three new animal types that inherit from Animal and optionally implement Flyable or ICrazyAction.
- Each class must:
  - Implement its own MakeSound() method.
  - Optionally override Describe().
  - Have at least one unique “crazy” action.
  - Provide corresponding images in Assets/<AnimalName>/. (optional)