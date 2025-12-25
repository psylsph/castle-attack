# Siege by Trebuchet - Unity Project

A physics-driven mobile game where the player assaults fortified castles using a trebuchet.

## Project Structure

```
castle-attack/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # Core game systems
│   │   │   ├── GameManager.cs
│   │   │   ├── SaveManager.cs
│   │   │   └── EventManager.cs
│   │   ├── Physics/            # Physics and destruction
│   │   │   ├── TrajectoryPredictor.cs
│   │   │   └── DestructionManager.cs
│   │   ├── Trebuchet/          # Trebuchet controls
│   │   │   ├── TrebuchetController.cs
│   │   │   └── TrebuchetParameters.cs
│   │   ├── Castle/             # Castle structures
│   │   │   ├── StructureComponent.cs
│   │   │   └── CastleBuilder.cs
│   │   ├── Ammunition/          # Projectiles
│   │   │   └── Projectile.cs
│   │   ├── Level/              # Level management
│   │   │   └── LevelManager.cs
│   │   ├── UI/                 # User interface
│   │   │   ├── UIManager.cs
│   │   │   ├── TouchControls.cs
│   │   │   ├── CameraController.cs
│   │   │   └── AmmunitionSelector.cs
│   │   └── VisualEffects/      # Particle effects
│   │       └── DestructionEffects.cs
│   ├── ScriptableObjects/    # Data assets
│   │   ├── AmmunitionTypes/
│   │   │   └── AmmunitionData.cs
│   │   ├── MaterialProperties/
│   │   │   └── MaterialData.cs
│   │   └── LevelData/
│   │       └── LevelData.cs
│   ├── Prefabs/              # GameObject prefabs
│   ├── Materials/             # Visual materials
│   ├── Audio/                 # Sound effects and music
│   ├── Sprites/               # Visual assets
│   ├── Scenes/                # Unity scenes
│   └── Resources/             # Runtime loaded assets
├── plans/
│   └── architecture-plan.md   # Technical architecture documentation
└── README.md
```

## Core Systems

### GameManager
Central game controller managing:
- Game state (MainMenu, Playing, Paused, Victory, Defeat)
- Level flow and progression
- Shot tracking and star rating
- Event coordination

### SaveManager
Handles player data persistence:
- Level completion and stars
- Unlocked ammunition and upgrades
- Cosmetics and currency
- Settings (audio, accessibility)
- Auto-save functionality

### EventManager
Decoupled event system:
- String-based event registration
- Support for events with/without arguments
- Centralized event constants

## Physics System

### TrajectoryPredictor
Calculates projectile paths for:
- Ghost arc visualization
- Impact point prediction
- Required velocity/angle calculations
- Simplified physics for accessibility mode

### DestructionManager
Manages structural destruction:
- Damage calculation based on projectile and material
- Chain reaction evaluation
- Environmental effect application (fire, plague)
- Visual effect coordination

## Trebuchet System

### TrebuchetController
Controls trebuchet behavior:
- Touch input handling (drag, swipe, tap)
- Parameter adjustment (power, angle)
- Ghost arc rendering
- Fire animation

### TrebuchetParameters
Data class for trebuchet settings:
- Arm pullback strength
- Release angle
- Counterweight mass
- Sling length
- Physics calculations

## Castle System

### StructureComponent
Represents castle building blocks:
- Health and damage system
- Material properties
- Joint connections
- Weak point marking
- Destruction events

### CastleBuilder
Constructs castles from blueprints:
- Structure spawning
- Joint creation
- Material application
- Structure mapping

## Ammunition System

### AmmunitionData (ScriptableObject)
Defines ammunition types:
- Stone: Standard damage
- Fire Pot: Fire damage over time
- Plague Barrel: Weakening effect
- Chain Shot: Effective against wood
- Royal Boulder: Massive single-shot damage

### Projectile
Projectile behavior:
- Physics-based movement
- Collision handling
- Secondary effects
- Trail particles

## Level System

### LevelData (ScriptableObject)
Level configuration:
- Castle blueprints
- Environment variables (wind, elevation)
- Ammunition allocation
- Goal types and thresholds

### LevelManager
Level lifecycle management:
- Castle building
- Goal tracking
- Obstacle spawning
- Environment application

## UI System

### UIManager
Main UI coordinator:
- HUD updates (shots, stars, parameters)
- Menu management (pause, victory, defeat)
- Settings controls
- Button event handling

### TouchControls
Mobile input handling:
- Drag gestures for power
- Swipe gestures for angle
- Tap for fire
- Pinch for zoom

### CameraController
Camera behavior:
- Zoom and pan
- Projectile following
- Screen shake
- Bounds clamping

### AmmunitionSelector
Ammunition UI:
- Slot-based selection
- Quantity tracking
- Auto-selection
- Visual feedback

## Visual Effects

### DestructionEffects
Particle system management:
- Material-specific debris
- Dust clouds
- Fire and plague effects
- Object pooling

## Getting Started

### Prerequisites
- Unity 2022 LTS or later
- iOS and Android build support
- 2D Physics package

### Setup
1. Open project in Unity
2. Create ScriptableObject assets:
   - Materials (Assets/Create/Siege/Material)
   - Ammunition (Assets/Create/Siege/Ammunition)
   - Levels (Assets/Create/Siege/Level Data)
3. Build castle prefabs with StructureComponent
4. Create projectile prefabs with Projectile component
5. Set up UI in Canvas
6. Configure GameManager references

### Controls
| Action | Input |
|---------|--------|
| Set Power | Drag arm backward |
| Set Angle | Swipe up/down |
| Fire | Tap anywhere |
| Zoom | Pinch |
| Pan | One-finger drag |

## Architecture Documentation

See [`plans/architecture-plan.md`](plans/architecture-plan.md) for:
- Complete technical architecture
- Technology stack recommendations
- Implementation roadmap
- System diagrams
- Data models

## Implementation Status

- [x] Core system scripts
- [x] Physics system
- [x] Material properties
- [x] Trebuchet system
- [x] Ammunition system
- [x] Level management
- [x] UI system

## Next Steps

1. Create Unity scenes (Boot, MainMenu, Game, etc.)
2. Build prefabs for trebuchet, castle components, and projectiles
3. Create ScriptableObject assets for materials, ammunition, and levels
4. Set up UI Canvas with buttons and sliders
5. Implement audio system
6. Create particle effects
7. Build and test on target devices

## License

This project is created for the "Siege by Trebuchet" game specification.
