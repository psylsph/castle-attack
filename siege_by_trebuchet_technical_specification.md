# Siege by Trebuchet

## Overview
Siege by Trebuchet is a web-based, server-backed physics puzzle game where players destroy castles using a trebuchet. The game runs entirely in the browser using HTML5 technologies and communicates with a backend server for persistence, progression, and optional multiplayer or leaderboard features.

This document is designed to be **agent-friendly**: explicit technology choices, clear system boundaries, and implementation-ready detail.

---

## Table of Contents
- [Target Platforms](#target-platforms)
- [Core Technology Stack](#core-technology-stack)
- [High-Level Architecture](#high-level-architecture)
- [Game Loop](#game-loop)
- [Phaser Scene Structure](#phaser-scene-structure)
- [Physics Implementation](#physics-implementation-matterjs)
- [Trebuchet Mechanics](#trebuchet-mechanics)
- [Ammunition System](#ammunition-system)
- [Level Definition Format](#level-definition-format)
- [UI/UX Design](#uiux-design)
- [Server Responsibilities](#server-responsibilities)
- [API Specification](#api-specification)
- [Database Schema](#database-schema)
- [Networking](#networking)
- [Performance Requirements](#performance-requirements)
- [Security Considerations](#security-considerations)
- [Testing Strategy](#testing-strategy)
- [Error Handling](#error-handling)
- [Localization](#localization)
- [Analytics & Telemetry](#analytics--telemetry)
- [Development Workflow](#development-workflow)
- [Build & Deployment](#build--deployment)
- [Monetization](#monetization-web-friendly)
- [Accessibility](#accessibility)
- [Minimum Viable Product (MVP)](#minimum-viable-product-mvp)
- [Next Expansions](#next-expansions)
- [Success Criteria](#success-criteria)
- [Design Pillar](#design-pillar)

---

## Target Platforms
- Desktop browsers (Chrome, Firefox, Safari, Edge)
- Mobile browsers (iOS Safari, Android Chrome)
- Deployable via static hosting (Netlify, Vercel, Cloudflare Pages)

---

## Core Technology Stack

### Client (Game Runtime)
- **Framework:** Phaser 3.80+
- **Physics Engine:** Matter.js 0.19+ (via Phaser Matter integration)
- **Language:** TypeScript 5.0+
- **Rendering:** WebGL with Canvas fallback
- **Build Tooling:** Vite 5.0+
- **UI Framework:** React 18+ (optional, for complex UI)

### Server (Game Backend)
- **Runtime:** Node.js 20+ (LTS)
- **Framework:** Express 4.18+ or NestJS 10+
- **Real-Time Transport:** Socket.IO 4.6+ or native ws 8+
- **Persistence:** PostgreSQL 15+ (progression, scores) + Redis 7+ (sessions, rooms)
- **ORM:** Prisma 5+ or TypeORM 0.3+
- **Auth (optional):** JWT or OAuth via third-party provider (Auth0, Firebase Auth)
- **Validation:** Zod or Joi for request validation

### Shared
- **Data Schemas:** JSON with TypeScript interfaces
- **Physics Validation (optional):** Headless Matter.js on server for anti-cheat

### DevOps
- **Containerization:** Docker
- **CI/CD:** GitHub Actions or GitLab CI
- **Hosting:** Vercel/Netlify (client), Railway/Render/AWS (server)
- **Monitoring:** Sentry (errors), Datadog/New Relic (performance)

---

## High-Level Architecture

```
Browser
 └─ Phaser 3 Game Client
     ├─ Matter.js (physics simulation)
     ├─ UI Layer (React/Vanilla)
     └─ Network Layer
         ⇅ HTTPS / WSS
Server
 └─ Node.js API Server
     ├─ REST API Layer
     ├─ WebSocket Layer (Socket.IO)
     ├─ Business Logic Layer
     │   ├─ Game State Management
     │   ├─ Progression System
     │   └─ Leaderboard Service
     ├─ Validation Layer
     └─ Persistence Layer
         ├─ PostgreSQL (relational data)
         └─ Redis (caching, sessions)
```

The client is authoritative for moment-to-moment physics, while the server validates outcomes and stores results.

---

## Game Loop
1. Load level configuration from server or static JSON
2. Player inspects castle (camera pan/zoom)
3. Player configures trebuchet (adjust parameters)
4. Player fires projectile
5. Matter.js simulates trajectory, collisions, and destruction
6. Win/loss conditions evaluated client-side
7. Result submitted to server
8. Server validates and persists outcome
9. Player receives progression updates and unlocks

---

## Phaser Scene Structure

### BootScene
- Asset preloading (sprites, audio, fonts)
- Device capability checks (WebGL support, touch detection)
- Initialize audio context
- Load player progression from local storage

### MenuScene
- Level selection grid
- Progression overview (stars, unlocks)
- Settings menu
- Player profile display

### GameScene
- Matter.js world initialization
- Trebuchet entity (arm, counterweight, sling)
- Castle entities (blocks, structures)
- Terrain generation
- UI overlay (controls, score, ammo)
- Camera management

### PauseScene
- Pause menu overlay
- Settings access
- Quit to menu option

### ResultScene
- Score breakdown (accuracy, damage, time)
- Star rating (1-3 stars)
- Retry / continue buttons
- Unlock notifications
- Share result option

---

## Physics Implementation (Matter.js)

### World Configuration
- **Gravity:** Configurable per level (default: { x: 0, y: 1 })
- **Timestep:** Fixed at 1000/60 for deterministic behavior
- **Engine:** Matter.Engine.create({ gravity, timing: { timeScale: 1 } })
- **Runner:** Matter.Runner.create({ delta: 1000/60 })

### Collision Categories
```typescript
const CollisionCategories = {
  DEFAULT: 0x0001,
  PROJECTILE: 0x0002,
  CASTLE: 0x0004,
  TERRAIN: 0x0008,
  TREBUCHET: 0x0010,
  SENSOR: 0x0020,
};

const CollisionMasks = {
  PROJECTILE: CollisionCategories.CASTLE | CollisionCategories.TERRAIN,
  CASTLE: CollisionCategories.PROJECTILE | CollisionCategories.CASTLE,
  TERRAIN: CollisionCategories.PROJECTILE | CollisionCategories.CASTLE,
};
```

### Bodies

#### Trebuchet Arm
- **Type:** Rectangle body
- **Dimensions:** 300x20 pixels
- **Density:** 0.001
- **Constraint:** Revolute joint at pivot point
- **Stiffness:** 0.9
- **Damping:** 0.1

#### Counterweight
- **Type:** Circle body
- **Radius:** 30-50 pixels (adjustable)
- **Density:** 0.01 (10x wood density)
- **Mass:** 100-500 kg (progression unlock)

#### Projectile Bodies
- **Type:** Circle (stone), Rectangle (chain shot), Custom (special)
- **Radius:** 15-25 pixels
- **Density:** 0.005-0.02 (varies by ammo type)
- **Restitution:** 0.2-0.6 (bounciness)

#### Castle Blocks
- **Type:** Compound bodies for complex shapes
- **Dimensions:** 50-150 pixels
- **Health:** 100-1000 HP (varies by material)
- **Constraints:** Weld joints for structural integrity

### Materials

| Material | Density | Restitution | Durability | Color | Notes |
|----------|---------|-------------|------------|-------|-------|
| Wood | 0.001 | 0.3 | 100 HP | #8B4513 | Low density, breaks easily |
| Stone | 0.005 | 0.1 | 300 HP | #808080 | High density, brittle |
| Reinforced Stone | 0.006 | 0.1 | 600 HP | #696969 | High durability |
| Iron | 0.01 | 0.2 | 1000 HP | #434343 | Very durable, minimal deformation |
| Glass | 0.002 | 0.5 | 50 HP | #ADD8E6 | Brittle, shatters on impact |

### Destruction Mechanics
- **Health System:** Each block has HP reduced by impact damage
- **Damage Formula:** `damage = projectileMass * velocity^2 * damageMultiplier / blockDurability`
- **Debris Generation:** When block HP ≤ 0, spawn 3-5 smaller debris particles
- **Structural Collapse:** When supporting blocks are destroyed, remaining blocks fall due to gravity
- **Sleep Threshold:** Bodies with velocity < 0.1 for > 1 second are put to sleep for performance

### Performance Optimization
- **Object Pooling:** Reuse projectile and debris bodies
- **Sleep Thresholds:** Enable body sleeping to reduce calculations
- **Spatial Partitioning:** Matter.js grid-based collision detection
- **Body Culling:** Remove bodies that fall below world bounds
- **LOD System:** Reduce physics precision for distant objects

---

## Trebuchet Mechanics

### Adjustable Parameters

#### Arm Pullback Distance
- **Range:** 0-180 degrees from vertical
- **Default:** 45 degrees
- **Effect:** Higher pullback = more potential energy = longer range
- **UI Control:** Slider or drag-to-rotate interaction

#### Release Angle
- **Range:** -30 to +30 degrees from horizontal
- **Default:** 0 degrees (horizontal)
- **Effect:** Adjusts trajectory arc
- **UI Control:** Fine-tuning slider

#### Counterweight Mass
- **Range:** 100-500 kg
- **Default:** 200 kg
- **Unlock:** Progression-based (heavier weights unlock at level 5, 10, 15)
- **Effect:** More mass = more force = longer range
- **UI Control:** Selector with unlocked options

#### Sling Length
- **Range:** 50-150 pixels
- **Default:** 100 pixels
- **Unlock:** Advanced feature (unlocks at level 8)
- **Effect:** Longer sling = higher release velocity but less control
- **UI Control:** Slider (advanced mode only)

### Trajectory Prediction
- **Algorithm:** Ballistic approximation using projectile motion equations
- **Formula:** `y = x * tan(θ) - (g * x²) / (2 * v₀² * cos²(θ))`
- **Visualization:** Dotted arc line showing predicted path
- **Accuracy:** 85-95% (approximation, not full physics simulation)
- **Update Rate:** Real-time as parameters change

### Physics Assist Mode (Accessibility)
- **Auto-aim:** Suggests optimal angle and power
- **Trajectory Guide:** Shows full predicted path with collision points
- **Reduced Complexity:** Limits adjustable parameters
- **Visual Aids:** Highlight weak points in castle structure

### Progression Unlock System
- **Level 1-4:** Basic trebuchet (arm pullback, release angle)
- **Level 5-7:** Counterweight mass (200-300 kg)
- **Level 8-12:** Sling length, heavier counterweight (400 kg)
- **Level 13+:** All parameters, max counterweight (500 kg)

---

## Ammunition System

### Ammunition Types

#### Stone (Default)
- **Mass:** 10 kg
- **Damage Multiplier:** 1.0x
- **Radius:** 20 pixels
- **Restitution:** 0.3
- **Special Effects:** None
- **Unlock:** Available from start

#### Fire Pot
- **Mass:** 8 kg
- **Damage Multiplier:** 0.8x
- **Radius:** 25 pixels
- **Restitution:** 0.2
- **Special Effects:** Damage over time (10 DPS for 5 seconds)
- **Unlock:** Level 5
- **Visual:** Orange/red glow, particle trail

#### Plague Barrel
- **Mass:** 12 kg
- **Damage Multiplier:** 0.6x
- **Radius:** 30 pixels
- **Restitution:** 0.1
- **Special Effects:** Area weakening (50% reduced durability for 10 seconds)
- **Unlock:** Level 10
- **Visual:** Green cloud on impact

#### Chain Shot
- **Mass:** 15 kg (2x 7.5kg balls connected)
- **Damage Multiplier:** 1.2x
- **Radius:** 18 pixels (each ball)
- **Restitution:** 0.4
- **Special Effects:** Wider impact area, spinning motion
- **Unlock:** Level 15
- **Visual:** Two connected spheres, spinning

#### Explosive Barrel
- **Mass:** 10 kg
- **Damage Multiplier:** 2.0x (explosion)
- **Radius:** 35 pixels
- **Restitution:** 0.1
- **Special Effects:** Explosion on impact (100px radius, 500 damage)
- **Unlock:** Level 20
- **Visual:** Red barrel, explosion effect

### Ammo Economy
- **Cost System:** Each shot costs "ammo points" (not real money)
- **Ammo Points:** Earned by completing levels (3 stars = 10 points, 2 stars = 7 points, 1 star = 5 points)
- **Ammo Costs:** Stone (free), Fire Pot (2 points), Plague Barrel (3 points), Chain Shot (4 points), Explosive Barrel (5 points)
- **Daily Bonus:** +5 ammo points per day

### Special Effects Implementation

#### Fire Pot (Damage Over Time)
```typescript
interface FirePotEffect {
  targetBody: Matter.Body;
  damagePerSecond: number;
  duration: number;
  elapsedTime: number;
}
// Update loop: apply damage every second, remove after duration
```

#### Plague Barrel (Area Weakening)
```typescript
interface PlagueEffect {
  affectedBodies: Matter.Body[];
  originalDurability: Map<string, number>;
  duration: number;
  elapsedTime: number;
}
// Reduce durability by 50% for all bodies within 100px radius
```

#### Chain Shot (Connected Projectiles)
```typescript
interface ChainShot {
  ball1: Matter.Body;
  ball2: Matter.Body;
  constraint: Matter.Constraint;
}
// Two bodies connected by a stiff constraint, creates spinning motion
```

---

## Level Definition Format

### Level JSON Schema

```typescript
interface Level {
  id: string;
  name: string;
  description: string;
  version: string;
  difficulty: 1 | 2 | 3 | 4 | 5;
  requiredStars: number; // Stars needed to unlock
  maxAmmo: number; // Maximum shots allowed
  timeLimit?: number; // Optional time limit in seconds
  environment: {
    gravity: { x: number; y: number };
    wind: { x: number; y: number }; // Wind force vector
    background: string; // Asset key
    terrain: TerrainBlock[];
  };
  trebuchet: {
    position: { x: number; y: number };
    defaultCounterweightMass: number;
    unlockedFeatures: string[]; // ['sling', 'heavyCounterweight']
  };
  castle: {
    blocks: CastleBlock[];
    constraints: CastleConstraint[];
    targetZones: TargetZone[]; // Areas to destroy
  };
  winConditions: WinCondition[];
  loseConditions: LoseCondition[];
  availableAmmo: string[]; // Ammo types allowed
  parScore?: number; // Target score for 3 stars
}

interface TerrainBlock {
  type: 'rectangle' | 'polygon';
  position: { x: number; y: number };
  vertices?: { x: number; y: number }[];
  width?: number;
  height?: number;
  material: 'wood' | 'stone' | 'reinforcedStone' | 'iron';
}

interface CastleBlock {
  id: string;
  type: 'rectangle' | 'circle' | 'polygon';
  position: { x: number; y: number };
  dimensions: { width: number; height: number } | { radius: number };
  rotation: number;
  material: 'wood' | 'stone' | 'reinforcedStone' | 'iron' | 'glass';
  health: number;
  isTarget?: boolean; // Part of win condition
}

interface CastleConstraint {
  bodyA: string; // Block ID
  bodyB: string; // Block ID
  type: 'weld' | 'hinge' | 'slider';
  stiffness?: number;
  damping?: number;
}

interface TargetZone {
  id: string;
  position: { x: number; y: number };
  radius: number;
  requiredDestruction: number; // Percentage 0-100
}

interface WinCondition {
  type: 'destroyPercentage' | 'destroySpecific' | 'targetZone';
  threshold: number; // Percentage or count
  targetIds?: string[]; // For destroySpecific
  targetZoneId?: string; // For targetZone
}

interface LoseCondition {
  type: 'ammoDepleted' | 'timeExpired' | 'castleStable';
  threshold?: number;
}
```

### Level Example 1: Simple Tower

```json
{
  "id": "level_001",
  "name": "First Strike",
  "description": "Knock down this simple tower to begin your journey.",
  "version": "1.0.0",
  "difficulty": 1,
  "requiredStars": 0,
  "maxAmmo": 3,
  "environment": {
    "gravity": { "x": 0, "y": 1 },
    "wind": { "x": 0, "y": 0 },
    "background": "sky_day",
    "terrain": [
      {
        "type": "rectangle",
        "position": { "x": 400, "y": 550 },
        "width": 800,
        "height": 100,
        "material": "stone"
      }
    ]
  },
  "trebuchet": {
    "position": { "x": 100, "y": 450 },
    "defaultCounterweightMass": 200,
    "unlockedFeatures": []
  },
  "castle": {
    "blocks": [
      {
        "id": "base_1",
        "type": "rectangle",
        "position": { "x": 600, "y": 500 },
        "dimensions": { "width": 100, "height": 50 },
        "rotation": 0,
        "material": "stone",
        "health": 300
      },
      {
        "id": "base_2",
        "type": "rectangle",
        "position": { "x": 700, "y": 500 },
        "dimensions": { "width": 100, "height": 50 },
        "rotation": 0,
        "material": "stone",
        "health": 300
      },
      {
        "id": "middle",
        "type": "rectangle",
        "position": { "x": 650, "y": 425 },
        "dimensions": { "width": 150, "height": 50 },
        "rotation": 0,
        "material": "wood",
        "health": 100
      },
      {
        "id": "top",
        "type": "rectangle",
        "position": { "x": 650, "y": 350 },
        "dimensions": { "width": 100, "height": 100 },
        "rotation": 0,
        "material": "wood",
        "health": 100,
        "isTarget": true
      }
    ],
    "constraints": [
      {
        "bodyA": "base_1",
        "bodyB": "middle",
        "type": "weld"
      },
      {
        "bodyA": "base_2",
        "bodyB": "middle",
        "type": "weld"
      },
      {
        "bodyA": "middle",
        "bodyB": "top",
        "type": "weld"
      }
    ],
    "targetZones": []
  },
  "winConditions": [
    {
      "type": "destroyPercentage",
      "threshold": 80
    }
  ],
  "loseConditions": [
    {
      "type": "ammoDepleted"
    }
  ],
  "availableAmmo": ["stone"],
  "parScore": 5000
}
```

### Level Example 2: Fortified Castle

```json
{
  "id": "level_010",
  "name": "Stone Fortress",
  "description": "This castle is built to last. Find its weak points.",
  "version": "1.0.0",
  "difficulty": 3,
  "requiredStars": 15,
  "maxAmmo": 5,
  "environment": {
    "gravity": { "x": 0, "y": 1 },
    "wind": { "x": 0.1, "y": 0 },
    "background": "sky_sunset",
    "terrain": [
      {
        "type": "rectangle",
        "position": { "x": 400, "y": 550 },
        "width": 800,
        "height": 100,
        "material": "stone"
      },
      {
        "type": "rectangle",
        "position": { "x": 200, "y": 400 },
        "width": 50,
        "height": 150,
        "material": "stone"
      }
    ]
  },
  "trebuchet": {
    "position": { "x": 100, "y": 450 },
    "defaultCounterweightMass": 300,
    "unlockedFeatures": ["heavyCounterweight"]
  },
  "castle": {
    "blocks": [
      {
        "id": "wall_left",
        "type": "rectangle",
        "position": { "x": 550, "y": 450 },
        "dimensions": { "width": 30, "height": 100 },
        "rotation": 0,
        "material": "reinforcedStone",
        "health": 600
      },
      {
        "id": "wall_right",
        "type": "rectangle",
        "position": { "x": 750, "y": 450 },
        "dimensions": { "width": 30, "height": 100 },
        "rotation": 0,
        "material": "reinforcedStone",
        "health": 600
      },
      {
        "id": "roof",
        "type": "polygon",
        "position": { "x": 650, "y": 350 },
        "vertices": [
          { "x": -100, "y": 50 },
          { "x": 100, "y": 50 },
          { "x": 0, "y": -50 }
        ],
        "rotation": 0,
        "material": "wood",
        "health": 150
      },
      {
        "id": "tower",
        "type": "rectangle",
        "position": { "x": 650, "y": 400 },
        "dimensions": { "width": 80, "height": 100 },
        "rotation": 0,
        "material": "stone",
        "health": 300,
        "isTarget": true
      }
    ],
    "constraints": [],
    "targetZones": [
      {
        "id": "main_target",
        "position": { "x": 650, "y": 400 },
        "radius": 60,
        "requiredDestruction": 100
      }
    ]
  },
  "winConditions": [
    {
      "type": "targetZone",
      "targetZoneId": "main_target",
      "threshold": 100
    }
  ],
  "loseConditions": [
    {
      "type": "ammoDepleted"
    }
  ],
  "availableAmmo": ["stone", "firePot"],
  "parScore": 8000
}
```

### Level Design Principles

#### Castle Structure Graph
- **Nodes:** Individual blocks with health and material properties
- **Edges:** Constraints (weld, hinge) connecting blocks
- **Structural Integrity:** Remove supporting nodes → collapse of dependent nodes
- **Weak Points:** Identify critical blocks whose destruction causes maximum collapse

#### Difficulty Scaling
- **Level 1-4:** Simple towers, single material, no wind
- **Level 5-9:** Complex structures, multiple materials, light wind
- **Level 10-19:** Fortified castles, reinforced materials, moderate wind
- **Level 20+:** Multi-tier fortresses, iron components, strong wind, time limits

#### Procedural Generation (Future)
- **Castle Templates:** Predefined structural patterns
- **Randomization:** Vary block positions, materials, and constraints
- **Validation:** Ensure levels are solvable (pathfinding through physics)
- **Difficulty Adjustment:** Auto-balance based on player completion rate

---

## UI/UX Design

### Screen Layouts

#### Menu Scene
```
┌─────────────────────────────────────┐
│  SIEGE BY TREBUCHET      [Settings] │
├─────────────────────────────────────┤
│                                     │
│  [Player Avatar]  Level 15          │
│  Stars: 42/60    Ammo Points: 25   │
│                                     │
├─────────────────────────────────────┤
│  Level Selection                    │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐  │
│  │  1  │ │  2  │ │  3  │ │  4  │  │
│  │ ★★★ │ │ ★★★ │ │ ★★☆ │ │ ☆☆☆ │  │
│  └─────┘ └─────┘ └─────┘ └─────┘  │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐  │
│  │  5  │ │  6  │ │  7  │ │  8  │  │
│  │ ☆☆☆ │ │ 🔒  │ │ 🔒  │ │ 🔒  │  │
│  └─────┘ └─────┘ └─────┘ └─────┘  │
│                                     │
│  [← Prev]        [Next →]          │
└─────────────────────────────────────┘
```

#### Game Scene
```
┌─────────────────────────────────────┐
│  Level 15: Stone Fortress  [⏸] [✕] │
├─────────────────────────────────────┤
│                                     │
│  [Trebuchet]                        │
│      │                              │
│      ▼                              │
│  ┌─────────────────────────────┐   │
│  │                             │   │
│  │     [Castle Structure]      │   │
│  │                             │   │
│  └─────────────────────────────┘   │
│                                     │
├─────────────────────────────────────┤
│  Score: 0  Ammo: 3/5  Time: 0:00   │
│  [Stone] [Fire Pot] [Chain Shot]   │
│                                     │
│  Power: ████████░░ 80%              │
│  Angle: ━━━━━━━━━━ 0°              │
│  [FIRE!]                            │
└─────────────────────────────────────┘
```

#### Result Scene
```
┌─────────────────────────────────────┐
│         LEVEL COMPLETE!             │
├─────────────────────────────────────┤
│                                     │
│            ★★★                      │
│                                     │
│  Score: 8,500                       │
│  Accuracy: 85%                     │
│  Damage: 92%                        │
│  Time: 45s                          │
│                                     │
│  ┌─────────────────────────────┐   │
│  │  🎉 New Ammo Unlocked!      │   │
│  │  Chain Shot                 │   │
│  └─────────────────────────────┘   │
│                                     │
│  [Retry]  [Next Level]  [Share]    │
└─────────────────────────────────────┘
```

### Input Controls

#### Desktop (Mouse)
- **Pan:** Click and drag on background
- **Zoom:** Mouse wheel
- **Adjust Power:** Drag power slider or use scroll wheel on trebuchet
- **Adjust Angle:** Drag angle slider or use arrow keys
- **Select Ammo:** Click ammo icons
- **Fire:** Click FIRE button or press Spacebar
- **Pause:** Press Escape or click pause button

#### Mobile (Touch)
- **Pan:** One-finger drag on background
- **Zoom:** Two-finger pinch
- **Adjust Power:** Drag power slider
- **Adjust Angle:** Drag angle slider
- **Select Ammo:** Tap ammo icons
- **Fire:** Tap FIRE button
- **Pause:** Tap pause button

### Feedback Systems

#### Visual Feedback
- **Impact:** Flash effect at collision point
- **Damage:** Red tint on damaged blocks
- **Destruction:** Particle explosion when blocks break
- **Trajectory:** Dotted line showing predicted path
- **Power Indicator:** Color-coded bar (green → yellow → red)
- **Star Progress:** Animated star filling on completion

#### Audio Feedback
- **Trebuchet Release:** Creaking sound
- **Impact:** Thud/crash sound based on material
- **Destruction:** Crumbling sound
- **Win:** Victory fanfare
- **Loss:** Defeat sound

#### Progression Display
- **Level Selection:** Star count per level, lock icons for locked levels
- **Unlocks:** Popup notification when new features/ammo unlock
- **Ammo Points:** Counter in menu scene
- **Leaderboard:** Optional display in menu scene

### UI Scale & Accessibility
- **Scale Options:** 75%, 100%, 125%, 150%
- **Colorblind Mode:** Material indicators with patterns (stripes, dots)
- **High Contrast:** Enhanced border and text contrast
- **Large Text:** Increased font size option

---

## Server Responsibilities

### Core Functions
- Store player progression and scores
- Validate level completion claims
- Serve level definitions
- Manage leaderboards
- Optional multiplayer room coordination

### Validation Strategy
- **Result Verification:** Validate submitted results without full physics simulation
- **Checksums:** Verify level data integrity
- **Anti-Cheat:** Detect impossible scores, suspicious patterns
- **Rate Limiting:** Prevent result spamming

### Session Management
- **JWT Tokens:** For authenticated users
- **Redis Sessions:** Store active game sessions
- **Room Management:** For multiplayer/spectating

The server does **not** stream physics frames; it operates on events and results.

---

## API Specification

### REST API Endpoints

#### Authentication (Optional)

##### POST /api/auth/register
Register a new player account.

**Request:**
```json
{
  "email": "player@example.com",
  "password": "securePassword123",
  "username": "PlayerName"
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "playerId": "uuid",
    "email": "player@example.com",
    "username": "PlayerName",
    "token": "jwt_token_here"
  }
}
```

##### POST /api/auth/login
Authenticate a player.

**Request:**
```json
{
  "email": "player@example.com",
  "password": "securePassword123"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "playerId": "uuid",
    "username": "PlayerName",
    "token": "jwt_token_here"
  }
}
```

#### Levels

##### GET /api/levels
Get all available levels (or filtered by criteria).

**Query Parameters:**
- `difficulty` (optional): Filter by difficulty (1-5)
- `unlocked` (optional): Only return unlocked levels (requires auth)

**Response (200):**
```json
{
  "success": true,
  "data": {
    "levels": [
      {
        "id": "level_001",
        "name": "First Strike",
        "description": "Knock down this simple tower...",
        "difficulty": 1,
        "requiredStars": 0,
        "maxAmmo": 3,
        "thumbnail": "https://cdn.example.com/levels/001/thumb.png"
      }
    ],
    "total": 50,
    "page": 1,
    "pageSize": 20
  }
}
```

##### GET /api/levels/:id
Get a specific level definition.

**Response (200):**
```json
{
  "success": true,
  "data": {
    "level": {
      "id": "level_001",
      "name": "First Strike",
      "description": "Knock down this simple tower...",
      "version": "1.0.0",
      "difficulty": 1,
      "requiredStars": 0,
      "maxAmmo": 3,
      "environment": { /* ... */ },
      "trebuchet": { /* ... */ },
      "castle": { /* ... */ },
      "winConditions": [ /* ... */ ],
      "loseConditions": [ /* ... */ ],
      "availableAmmo": ["stone"],
      "parScore": 5000
    }
  }
}
```

**Response (404):**
```json
{
  "success": false,
  "error": {
    "code": "LEVEL_NOT_FOUND",
    "message": "Level with ID 'level_001' not found"
  }
}
```

#### Progression

##### GET /api/progression
Get player progression data.

**Headers:**
- `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "playerId": "uuid",
    "totalStars": 42,
    "completedLevels": 15,
    "currentLevel": 16,
    "ammoPoints": 25,
    "unlockedAmmo": ["stone", "firePot"],
    "unlockedFeatures": ["heavyCounterweight"],
    "levelProgress": {
      "level_001": { "stars": 3, "highScore": 5200, "attempts": 5 },
      "level_002": { "stars": 2, "highScore": 4800, "attempts": 8 }
    }
  }
}
```

##### POST /api/progression/level/:id/complete
Submit level completion result.

**Headers:**
- `Authorization: Bearer <token>`

**Request:**
```json
{
  "score": 8500,
  "stars": 3,
  "ammoUsed": 3,
  "timeTaken": 45,
  "shots": [
    {
      "ammoType": "stone",
      "power": 80,
      "angle": 15,
      "impactDamage": 350
    }
  ],
  "destroyedBlocks": 12,
  "totalBlocks": 15,
  "checksum": "sha256_hash_of_game_state"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "stars": 3,
    "newHighScore": true,
    "unlocks": {
      "ammo": ["chainShot"],
      "features": ["sling"]
    },
    "ammoPointsEarned": 10,
    "totalAmmoPoints": 35
  }
}
```

**Response (400):**
```json
{
  "success": false,
  "error": {
    "code": "INVALID_RESULT",
    "message": "Submitted result does not match expected values"
  }
}
```

#### Leaderboards

##### GET /api/leaderboards
Get global leaderboards.

**Query Parameters:**
- `level` (optional): Filter by level ID
- `limit` (optional): Number of entries (default: 10, max: 100)
- `offset` (optional): Pagination offset (default: 0)

**Response (200):**
```json
{
  "success": true,
  "data": {
    "leaderboard": [
      {
        "rank": 1,
        "username": "SiegeMaster",
        "score": 12500,
        "levelId": "level_010",
        "timestamp": "2024-01-15T10:30:00Z"
      },
      {
        "rank": 2,
        "username": "CastleCrusher",
        "score": 11800,
        "levelId": "level_010",
        "timestamp": "2024-01-14T15:22:00Z"
      }
    ],
    "total": 1523,
    "playerRank": 45
  }
}
```

#### Player Profile

##### GET /api/profile
Get player profile.

**Headers:**
- `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "playerId": "uuid",
    "username": "PlayerName",
    "email": "player@example.com",
    "createdAt": "2024-01-01T00:00:00Z",
    "totalStars": 42,
    "completedLevels": 15,
    "totalScore": 125000,
    "rank": 45
  }
}
```

##### PUT /api/profile
Update player profile.

**Headers:**
- `Authorization: Bearer <token>`

**Request:**
```json
{
  "username": "NewUsername",
  "avatar": "avatar_05"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "playerId": "uuid",
    "username": "NewUsername",
    "avatar": "avatar_05"
  }
}
```

### WebSocket Events

#### Connection

##### Client → Server: connect
```json
{
  "event": "connect",
  "data": {
    "token": "jwt_token_here",
    "clientVersion": "1.0.0"
  }
}
```

##### Server → Client: connected
```json
{
  "event": "connected",
  "data": {
    "sessionId": "session_uuid",
    "serverTime": 1705300200000
  }
}
```

#### Game Events

##### Client → Server: game_start
```json
{
  "event": "game_start",
  "data": {
    "levelId": "level_010",
    "timestamp": 1705300200000
  }
}
```

##### Client → Server: game_end
```json
{
  "event": "game_end",
  "data": {
    "levelId": "level_010",
    "result": {
      "score": 8500,
      "stars": 3,
      "completed": true
    },
    "timestamp": 1705300245000
  }
}
```

##### Server → Client: live_leaderboard_update
```json
{
  "event": "live_leaderboard_update",
  "data": {
    "levelId": "level_010",
    "updates": [
      {
        "rank": 1,
        "username": "SiegeMaster",
        "score": 12500
      }
    ]
  }
}
```

#### Multiplayer Events (Future)

##### Client → Server: join_room
```json
{
  "event": "join_room",
  "data": {
    "roomId": "room_uuid",
    "mode": "competitive"
  }
}
```

##### Server → Client: room_joined
```json
{
  "event": "room_joined",
  "data": {
    "roomId": "room_uuid",
    "players": [
      { "id": "uuid1", "username": "Player1" },
      { "id": "uuid2", "username": "Player2" }
    ]
  }
}
```

##### Server → Client: player_shot
```json
{
  "event": "player_shot",
  "data": {
    "playerId": "uuid1",
    "ammoType": "stone",
    "power": 80,
    "angle": 15
  }
}
```

#### Error Events

##### Server → Client: error
```json
{
  "event": "error",
  "data": {
    "code": "AUTH_FAILED",
    "message": "Invalid or expired token",
    "timestamp": 1705300200000
  }
}
```

### Error Response Format

All error responses follow this structure:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {}, // Optional additional context
    "timestamp": 1705300200000
  }
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTH_FAILED` | 401 | Invalid or expired authentication token |
| `PERMISSION_DENIED` | 403 | Insufficient permissions |
| `RESOURCE_NOT_FOUND` | 404 | Requested resource does not exist |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Server-side error |
| `LEVEL_NOT_FOUND` | 404 | Level does not exist |
| `INVALID_RESULT` | 400 | Submitted result is invalid or suspicious |
| `DUPLICATE_ENTRY` | 409 | Resource already exists |

---

## Database Schema

### PostgreSQL Schema

#### Players Table
```sql
CREATE TABLE players (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email VARCHAR(255) UNIQUE NOT NULL,
  username VARCHAR(50) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  avatar VARCHAR(50) DEFAULT 'avatar_01',
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  last_login TIMESTAMP WITH TIME ZONE,
  is_active BOOLEAN DEFAULT TRUE,
  CONSTRAINT username_length CHECK (CHAR_LENGTH(username) >= 3 AND CHAR_LENGTH(username) <= 50),
  CONSTRAINT email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

CREATE INDEX idx_players_email ON players(email);
CREATE INDEX idx_players_username ON players(username);
```

#### Player Progression Table
```sql
CREATE TABLE player_progression (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  total_stars INTEGER DEFAULT 0,
  completed_levels INTEGER DEFAULT 0,
  current_level INTEGER DEFAULT 1,
  ammo_points INTEGER DEFAULT 10,
  total_score BIGINT DEFAULT 0,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  CONSTRAINT non_negative_stars CHECK (total_stars >= 0),
  CONSTRAINT non_negative_ammo CHECK (ammo_points >= 0)
);

CREATE UNIQUE INDEX idx_progression_player ON player_progression(player_id);
```

#### Level Progress Table
```sql
CREATE TABLE level_progress (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  level_id VARCHAR(50) NOT NULL,
  stars INTEGER DEFAULT 0,
  high_score INTEGER DEFAULT 0,
  attempts INTEGER DEFAULT 0,
  best_time INTEGER, -- in seconds
  completed_at TIMESTAMP WITH TIME ZONE,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  CONSTRAINT valid_stars CHECK (stars >= 0 AND stars <= 3),
  CONSTRAINT non_negative_score CHECK (high_score >= 0),
  CONSTRAINT non_negative_attempts CHECK (attempts >= 0),
  UNIQUE(player_id, level_id)
);

CREATE INDEX idx_level_progress_player ON level_progress(player_id);
CREATE INDEX idx_level_progress_level ON level_progress(level_id);
CREATE INDEX idx_level_progress_stars ON level_progress(stars);
```

#### Unlocks Table
```sql
CREATE TABLE unlocks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  unlock_type VARCHAR(50) NOT NULL, -- 'ammo', 'feature', 'cosmetic'
  unlock_id VARCHAR(50) NOT NULL,
  unlocked_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(player_id, unlock_type, unlock_id)
);

CREATE INDEX idx_unlocks_player ON unlocks(player_id);
CREATE INDEX idx_unlocks_type ON unlocks(unlock_type);
```

#### Leaderboard Table
```sql
CREATE TABLE leaderboard (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  level_id VARCHAR(50) NOT NULL,
  score INTEGER NOT NULL,
  stars INTEGER NOT NULL,
  achieved_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  CONSTRAINT valid_leaderboard_stars CHECK (stars >= 1 AND stars <= 3),
  CONSTRAINT non_negative_leaderboard_score CHECK (score >= 0)
);

CREATE INDEX idx_leaderboard_level ON leaderboard(level_id);
CREATE INDEX idx_leaderboard_score ON leaderboard(level_id, score DESC);
CREATE INDEX idx_leaderboard_player ON leaderboard(player_id);
```

#### Game Sessions Table
```sql
CREATE TABLE game_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID REFERENCES players(id) ON DELETE SET NULL,
  level_id VARCHAR(50) NOT NULL,
  started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  ended_at TIMESTAMP WITH TIME ZONE,
  completed BOOLEAN DEFAULT FALSE,
  score INTEGER,
  stars INTEGER,
  checksum VARCHAR(64),
  ip_address INET,
  user_agent TEXT
);

CREATE INDEX idx_sessions_player ON game_sessions(player_id);
CREATE INDEX idx_sessions_level ON game_sessions(level_id);
CREATE INDEX idx_sessions_date ON game_sessions(started_at);
```

#### Audit Log Table
```sql
CREATE TABLE audit_log (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  player_id UUID REFERENCES players(id) ON DELETE SET NULL,
  action VARCHAR(50) NOT NULL,
  resource_type VARCHAR(50),
  resource_id VARCHAR(50),
  details JSONB,
  ip_address INET,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_audit_player ON audit_log(player_id);
CREATE INDEX idx_audit_action ON audit_log(action);
CREATE INDEX idx_audit_date ON audit_log(created_at);
```

### Redis Data Structures

#### Session Storage
```
Key: session:{sessionId}
Type: Hash
Fields:
  - playerId: uuid
  - levelId: string
  - startTime: timestamp
  - lastActivity: timestamp
TTL: 3600 seconds (1 hour)
```

#### Leaderboard Cache
```
Key: leaderboard:{levelId}
Type: Sorted Set (ZSET)
Score: player score
Member: playerId
TTL: 300 seconds (5 minutes)
```

#### Player Cache
```
Key: player:{playerId}
Type: Hash
Fields:
  - username: string
  - totalStars: integer
  - ammoPoints: integer
  - unlockedAmmo: JSON array
  - unlockedFeatures: JSON array
TTL: 600 seconds (10 minutes)
```

#### Rate Limiting
```
Key: ratelimit:{playerId}:{endpoint}
Type: String (counter)
Value: request count
TTL: 60 seconds (window)
```

#### Room Management (Multiplayer)
```
Key: room:{roomId}
Type: Hash
Fields:
  - hostId: uuid
  - mode: string
  - maxPlayers: integer
  - currentPlayers: integer
  - status: string ('waiting', 'active', 'completed')
  - createdAt: timestamp
TTL: 3600 seconds (1 hour)

Key: room:{roomId}:players
Type: Set
Members: playerIds
```

### Entity Relationships

```mermaid
erDiagram
    Players ||--|| PlayerProgression : has
    Players ||--o{ LevelProgress : has
    Players ||--o{ Unlocks : has
    Players ||--o{ Leaderboard : has
    Players ||--o{ GameSessions : has
    Players ||--o{ AuditLog : has
    
    Players {
        uuid id PK
        string email UK
        string username UK
        string password_hash
        string avatar
        timestamp created_at
        timestamp updated_at
        timestamp last_login
        boolean is_active
    }
    
    PlayerProgression {
        uuid id PK
        uuid player_id FK
        integer total_stars
        integer completed_levels
        integer current_level
        integer ammo_points
        bigint total_score
    }
    
    LevelProgress {
        uuid id PK
        uuid player_id FK
        string level_id
        integer stars
        integer high_score
        integer attempts
        integer best_time
        timestamp completed_at
    }
    
    Unlocks {
        uuid id PK
        uuid player_id FK
        string unlock_type
        string unlock_id
        timestamp unlocked_at
    }
    
    Leaderboard {
        uuid id PK
        uuid player_id FK
        string level_id
        integer score
        integer stars
        timestamp achieved_at
    }
    
    GameSessions {
        uuid id PK
        uuid player_id FK
        string level_id
        timestamp started_at
        timestamp ended_at
        boolean completed
        integer score
        integer stars
        string checksum
    }
    
    AuditLog {
        uuid id PK
        uuid player_id FK
        string action
        string resource_type
        string resource_id
        jsonb details
        timestamp created_at
    }
```

---

## Networking

### REST API

#### Connection Handling
- **Protocol:** HTTPS only (production)
- **Timeout:** 30 seconds for most requests
- **Retry Strategy:** Exponential backoff (1s, 2s, 4s, 8s)
- **Keep-Alive:** Enabled with 60-second timeout

#### Data Compression
- **Compression:** gzip for request/response bodies > 1KB
- **Content-Encoding:** Accept-Encoding: gzip, deflate, br

### WebSocket

#### Connection Handling
- **Protocol:** WSS (Secure WebSocket) in production
- **Heartbeat:** Ping every 30 seconds, pong response required
- **Reconnection Strategy:**
  - Immediate reconnect on disconnect
  - Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s (max)
  - Max 10 reconnection attempts
- **Timeout:** 60 seconds of inactivity before disconnect

#### Message Queue
- **Buffer:** Client-side queue for messages during disconnect
- **Max Buffer Size:** 100 messages
- **Flush:** Send all buffered messages on reconnect

### Offline Support
- **Level Data:** Cache level definitions locally (IndexedDB)
- **Progression:** Sync progression when connection restored
- **Graceful Degradation:** Allow gameplay without server, sync later

### Anti-Cheat Networking
- **Timestamp Validation:** Verify client timestamps are within ±5 seconds
- **Checksum Validation:** SHA-256 hash of game state on completion
- **Rate Limiting:** Max 1 result submission per 10 seconds per level
- **Anomaly Detection:** Flag scores > 3 standard deviations from mean

---

## Performance Requirements

### Client-Side Performance

#### Frame Rate
- **Target:** 60 FPS on mid-range mobile devices
- **Minimum Acceptable:** 30 FPS on low-end devices
- **Measurement:** Average FPS over 60-second window

#### Memory Usage
- **Target:** < 200 MB on desktop
- **Maximum Acceptable:** < 300 MB on mobile
- **Measurement:** Peak memory during gameplay

#### Load Times
- **Initial Load:** < 3 seconds on 4G connection
- **Level Load:** < 1 second
- **Asset Loading:** Progressive loading with placeholders

#### Physics Performance
- **Active Bodies:** < 200 bodies at any time
- **Collision Checks:** < 1000 checks per frame
- **Sleep Threshold:** Bodies with velocity < 0.1 px/s for > 1 second

### Server-Side Performance

#### Response Times
- **API Latency (p50):** < 100ms
- **API Latency (p95):** < 300ms
- **API Latency (p99):** < 500ms

#### Concurrent Users
- **Target:** 10,000 concurrent players
- **Maximum:** 50,000 concurrent players (with scaling)

#### Database Performance
- **Query Latency (p50):** < 20ms
- **Query Latency (p95):** < 100ms
- **Connection Pool:** 20 connections per instance

#### WebSocket Performance
- **Message Latency:** < 50ms
- **Throughput:** 1000 messages/second per instance
- **Concurrent Connections:** 5000 per instance

### Network Requirements

#### Bandwidth
- **Initial Load:** < 5 MB (compressed)
- **Level Data:** < 100 KB per level
- **WebSocket Data:** < 1 KB/second per active session

#### Latency Tolerance
- **Gameplay:** No real-time requirements (turn-based)
- **Leaderboard Updates:** < 5 seconds acceptable
- **Progression Sync:** < 1 second preferred

### Performance Monitoring

#### Client Metrics
- FPS counter (display in debug mode)
- Memory usage graph
- Network latency indicator
- Physics body count

#### Server Metrics
- Request/response times
- Error rates
- Database query times
- WebSocket connection count
- CPU/memory usage

#### Alerts
- **Critical:** p99 latency > 2 seconds
- **Warning:** p95 latency > 500ms
- **Critical:** Error rate > 5%
- **Warning:** Memory usage > 80%

---

## Security Considerations

### Authentication & Authorization

#### JWT Tokens
- **Algorithm:** RS256 (asymmetric)
- **Expiration:** Access tokens: 1 hour, Refresh tokens: 30 days
- **Storage:** HttpOnly, Secure, SameSite cookies
- **Rotation:** Refresh token rotation on each use

#### Password Security
- **Hashing:** bcrypt with 12 rounds
- **Minimum Length:** 8 characters
- **Complexity:** At least one uppercase, one lowercase, one number
- **Rate Limiting:** 5 attempts per 15 minutes per IP

### Input Validation

#### Request Validation
- **Schema Validation:** Zod/Joi for all request bodies
- **Type Checking:** Strict TypeScript on server
- **Sanitization:** Strip HTML/JS from all string inputs
- **Length Limits:** Max 1000 characters for most fields

#### SQL Injection Prevention
- **Parameterized Queries:** Always use prepared statements
- **ORM:** Use Prisma/TypeORM for safe queries
- **Least Privilege:** Database user with minimal permissions

### Rate Limiting

#### API Rate Limits
- **Unauthenticated:** 100 requests/minute per IP
- **Authenticated:** 1000 requests/minute per user
- **Result Submission:** 1 submission/10 seconds per level
- **Leaderboard Fetch:** 10 requests/minute per user

#### WebSocket Rate Limits
- **Message Rate:** 10 messages/second per connection
- **Connection Rate:** 5 connections/minute per IP
- **Room Creation:** 1 room/minute per user

### Anti-Cheat Measures

#### Result Validation
- **Checksum Verification:** SHA-256 hash of game state
- **Score Bounds:** Reject scores > 3 standard deviations from mean
- **Time Validation:** Reject completion times < minimum possible
- **Pattern Detection:** Flag suspicious patterns (perfect scores, instant completions)

#### Server-Side Validation
- **Level Verification:** Ensure level data matches server version
- **Progression Validation:** Verify unlock prerequisites
- **Session Validation:** Check for duplicate session IDs

### Data Protection

#### Encryption
- **In Transit:** TLS 1.3 for all connections
- **At Rest:** AES-256 for sensitive data (optional)
- **Password Hashes:** bcrypt (never store plaintext)

#### Privacy
- **Data Minimization:** Only collect necessary data
- **Anonymization:** Remove IP addresses after 30 days
- **GDPR Compliance:** Provide data export/deletion endpoints

### Security Headers

#### HTTP Headers
```
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Referrer-Policy: strict-origin-when-cross-origin
```

### Vulnerability Management

#### Dependencies
- **Automated Scanning:** npm audit, Snyk
- **Regular Updates:** Monthly dependency updates
- **Vulnerability Patching:** Within 7 days for critical vulnerabilities

#### Code Review
- **Security Review:** All code changes reviewed for security issues
- **Penetration Testing:** Quarterly external penetration tests
- **Bug Bounty:** Program for responsible disclosure

---

## Testing Strategy

### Unit Testing

#### Client-Side
- **Framework:** Jest + React Testing Library (if using React)
- **Coverage Target:** 80% code coverage
- **Test Areas:**
  - Physics calculations
  - Trajectory prediction
  - Damage formulas
  - UI components
  - State management

#### Server-Side
- **Framework:** Jest + Supertest
- **Coverage Target:** 85% code coverage
- **Test Areas:**
  - API endpoints
  - Business logic
  - Data validation
  - Database operations
  - Authentication/authorization

### Integration Testing

#### API Integration
- **Framework:** Supertest
- **Test Scenarios:**
  - Full user flow (register → login → play → submit)
  - Level fetching and validation
  - Progression updates
  - Leaderboard queries

#### Database Integration
- **Framework:** Testcontainers (PostgreSQL, Redis)
- **Test Scenarios:**
  - CRUD operations
  - Transaction handling
  - Constraint validation
  - Query performance

### End-to-End Testing

#### E2E Framework
- **Tool:** Playwright or Cypress
- **Test Scenarios:**
  - Complete game flow from menu to result
  - Multiplayer interactions (when implemented)
  - Cross-browser testing (Chrome, Firefox, Safari)
  - Mobile responsiveness

### Performance Testing

#### Load Testing
- **Tool:** k6 or Artillery
- **Test Scenarios:**
  - 1000 concurrent users
  - 10,000 concurrent users (stress test)
  - API response times under load
  - WebSocket connection handling

#### Benchmarking
- **Client:** FPS, memory usage, load times
- **Server:** Request latency, query times, CPU/memory usage

### Security Testing

#### Vulnerability Scanning
- **Tools:** OWASP ZAP, npm audit, Snyk
- **Frequency:** Weekly automated scans
- **Coverage:** Dependencies, API endpoints, authentication flows

#### Penetration Testing
- **Frequency:** Quarterly
- **Scope:** Full application stack
- **Reporting:** Detailed findings with remediation steps

### Physics Testing

#### Determinism Testing
- **Test:** Same seed → same outcome
- **Method:** Run simulation 100 times with same inputs
- **Pass Criteria:** 100% identical results

#### Edge Cases
- **Extreme Values:** Max/min parameters
- **Boundary Conditions:** World edges, collision boundaries
- **Performance:** Maximum body count, complex structures

### Continuous Integration

#### CI Pipeline
1. **Linting:** ESLint, Prettier
2. **Unit Tests:** Jest (fail if < 80% coverage)
3. **Integration Tests:** Supertest
4. **Build:** Vite/Webpack
5. **Security Scan:** npm audit
6. **Deploy:** Staging environment

#### CD Pipeline
1. **Merge to main**
2. **Run full test suite**
3. **Deploy to staging**
4. **Run E2E tests**
5. **Manual QA approval**
6. **Deploy to production**

---

## Error Handling

### Client-Side Error Handling

#### Network Errors
- **Retry Strategy:** Exponential backoff (1s, 2s, 4s, 8s)
- **User Notification:** Toast message with retry option
- **Offline Mode:** Cache data locally, sync when online
- **Fallback:** Use cached level data if server unavailable

#### Physics Errors
- **Simulation Failure:** Reset level, notify user
- **Body Overflow:** Remove bodies outside world bounds
- **Constraint Failure:** Remove broken constraints, continue simulation
- **Performance Degradation:** Reduce physics precision, notify user

#### UI Errors
- **Component Errors:** Error boundaries to catch React errors
- **Asset Loading:** Show placeholder, retry in background
- **Input Errors:** Validate before submission, show inline errors

### Server-Side Error Handling

#### API Errors
- **Validation Errors:** Return 400 with field-level errors
- **Authentication Errors:** Return 401 with clear message
- **Authorization Errors:** Return 403 with required permissions
- **Not Found:** Return 404 with resource identifier
- **Internal Errors:** Return 500 with generic message, log details

#### Database Errors
- **Connection Errors:** Retry with exponential backoff
- **Query Errors:** Log query, return 500, alert team
- **Constraint Violations:** Return 400 with specific constraint
- **Timeout Errors:** Return 504, log slow query

#### WebSocket Errors
- **Connection Errors:** Notify client, allow reconnect
- **Message Errors:** Log invalid message, ignore
- **Room Errors:** Remove from room, notify participants

### Error Logging

#### Client Logging
- **Tool:** Sentry or similar
- **Data:** Error stack trace, user ID, level ID, game state
- **Privacy:** Sanitize sensitive data before sending
- **Sampling:** Log 100% of errors, 10% of warnings

#### Server Logging
- **Tool:** Winston or Pino
- **Levels:** error, warn, info, debug
- **Format:** JSON structured logs
- **Destinations:** File (local), Cloud (S3/CloudWatch)
- **Retention:** 30 days for error logs, 7 days for info logs

### Error Recovery

#### Automatic Recovery
- **Network:** Auto-reconnect WebSocket, retry failed requests
- **Database:** Connection pooling, automatic reconnection
- **Physics:** Reset simulation on critical errors

#### User Recovery
- **Retry Button:** Allow user to retry failed actions
- **Continue Option:** Save progress, allow resume
- **Support Link:** Direct users to help documentation

### Error Codes

| Code | HTTP | Description | Recovery |
|------|-----|-------------|----------|
| `NETWORK_ERROR` | N/A | Failed to connect to server | Retry, check internet |
| `PHYSICS_ERROR` | N/A | Physics simulation failed | Restart level |
| `VALIDATION_ERROR` | 400 | Invalid input data | Fix and resubmit |
| `AUTH_FAILED` | 401 | Authentication failed | Re-login |
| `PERMISSION_DENIED` | 403 | Insufficient permissions | Contact support |
| `RESOURCE_NOT_FOUND` | 404 | Resource does not exist | Check resource ID |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests | Wait and retry |
| `SERVER_ERROR` | 500 | Internal server error | Retry later |
| `DATABASE_ERROR` | 500 | Database operation failed | Retry later |
| `LEVEL_CORRUPTED` | N/A | Level data invalid | Report to support |

---

## Localization

### Supported Languages
- **Primary:** English (en)
- **Planned:** Spanish (es), French (fr), German (de), Japanese (ja), Chinese (zh)

### i18n Architecture

#### Translation Files
```
/locales
  /en
    common.json
    game.json
    menu.json
    errors.json
  /es
    common.json
    game.json
    menu.json
    errors.json
```

#### Translation Format
```json
{
  "level_complete": "Level Complete!",
  "score": "Score",
  "stars": "Stars",
  "retry": "Retry",
  "next_level": "Next Level"
}
```

### Translation Management

#### Tooling
- **Library:** i18next for client-side
- **Format:** ICU message format for pluralization/gender
- **Validation:** Automated checks for missing translations
- **Crowdsourcing:** Optional community translation platform

#### Dynamic Loading
- **Initial Load:** Only load user's preferred language
- **Lazy Loading:** Load additional languages on demand
- **Fallback:** English as fallback for missing translations

### Text Assets

#### Font Support
- **Primary Font:** Noto Sans (supports all languages)
- **Fallback:** System fonts for CJK languages
- **Loading:** Subset fonts to reduce size

#### UI Text
- **Hardcoded Text:** None - all text externalized
- **Variable Substitution:** Use i18next interpolation
- **Pluralization:** Handle singular/plural forms
- **RTL Support:** Right-to-left layout for Arabic/Hebrew (future)

### Content Localization

#### Level Names/Descriptions
- **Translatable:** Level names, descriptions, objectives
- **Cultural Adaptation:** Adjust references for cultural appropriateness
- **Character Limits:** Consider text length for different languages

#### Audio
- **Voiceovers:** Optional localized voice lines (future)
- **Sound Effects:** Universal (no localization needed)

---

## Analytics & Telemetry

### Data Collection

#### Player Metrics
- **Session Start/End:** Track play sessions
- **Level Attempts:** Count attempts per level
- **Completion Rates:** Track level completion percentages
- **Time Spent:** Average time per level
- **Progression:** Unlock events, milestone achievements

#### Gameplay Metrics
- **Ammo Usage:** Which ammo types are used most
- **Parameter Usage:** Average power/angle settings
- **Failure Reasons:** Why players fail (ammo, time, etc.)
- **Win Conditions:** How players achieve victory

#### Performance Metrics
- **FPS Distribution:** Frame rate across devices
- **Load Times:** Initial load, level load times
- **Error Rates:** Client and server error frequency
- **Network Latency:** API response times

#### Business Metrics
- **Retention:** Daily/Weekly/Monthly active users
- **Monetization:** Purchase events, revenue
- **Acquisition:** Source of new players
- **Engagement:** Session length, return rate

### Privacy & Compliance

#### Data Minimization
- **Collect Only Necessary Data:** No personal identifiers without consent
- **Anonymization:** Hash player IDs, remove IP addresses
- **Aggregation:** Report aggregated data, not individual records

#### Consent Management
- **Opt-In:** Require consent for analytics
- **Opt-Out:** Allow users to disable analytics
- **Clear Disclosure:** Explain what data is collected and why

#### GDPR Compliance
- **Data Export:** Provide user data on request
- **Data Deletion:** Delete user data on request
- **Right to be Forgotten:** Remove all user data

### Analytics Tools

#### Client-Side
- **Tool:** Google Analytics 4 or Plausible (privacy-focused)
- **Events:** Custom events for game actions
- **Properties:** Device info, browser, OS

#### Server-Side
- **Tool:** Custom analytics pipeline or Mixpanel
- **Events:** API calls, database queries, errors
- **Aggregation:** Real-time dashboards

### Data Retention

#### Retention Policy
- **Raw Events:** 90 days
- **Aggregated Data:** 2 years
- **User Profiles:** Until account deletion
- **Error Logs:** 30 days

#### Data Storage
- **Hot Storage:** PostgreSQL for recent data
- **Cold Storage:** S3 for archival
- **Analytics Database:** ClickHouse or BigQuery for queries

### Reporting

#### Dashboards
- **Real-Time:** Active users, current levels, error rates
- **Daily:** New signups, level completions, revenue
- **Weekly:** Retention rates, engagement metrics
- **Monthly:** Growth trends, feature usage

#### Alerts
- **Critical:** Error rate > 5%, downtime > 5 minutes
- **Warning:** Retention drop > 10%, new bugs reported

---

## Development Workflow

### Version Control

#### Git Workflow
- **Branching Strategy:** Git Flow
  - `main`: Production-ready code
  - `develop`: Integration branch
  - `feature/*`: Feature branches
  - `bugfix/*`: Bug fix branches
  - `hotfix/*`: Critical production fixes

#### Commit Conventions
- **Format:** Conventional Commits
  - `feat:` New feature
  - `fix:` Bug fix
  - `docs:` Documentation
  - `style:` Code style (formatting)
  - `refactor:` Code refactoring
  - `test:` Test additions/changes
  - `chore:` Maintenance tasks

#### Pull Request Process
1. Create feature branch from `develop`
2. Make changes with descriptive commits
3. Open PR with template description
4. Require at least 1 approval
5. Pass all CI checks
6. Squash and merge to `develop`

### Code Style

#### TypeScript/JavaScript
- **Linter:** ESLint with Airbnb config
- **Formatter:** Prettier
- **Rules:** Enforce consistent style
- **Pre-commit Hook:** Run linting on staged files

#### Naming Conventions
- **Files:** kebab-case (`game-scene.ts`)
- **Classes:** PascalCase (`GameScene`)
- **Functions/Variables:** camelCase (`getLevelData`)
- **Constants:** UPPER_SNAKE_CASE (`MAX_AMMO`)
- **Interfaces:** PascalCase with `I` prefix (`ILevelData`)

### Code Review

#### Review Checklist
- [ ] Code follows style guidelines
- [ ] Tests are included and passing
- [ ] Documentation is updated
- [ ] No security vulnerabilities
- [ ] Performance impact considered
- [ ] Error handling is proper
- [ ] Accessibility is maintained

#### Review Process
- **Self-Review:** Author reviews own changes
- **Peer Review:** At least 1 team member reviews
- **Lead Review:** Complex changes require lead approval
- **Time Limit:** Respond within 24 hours

### CI/CD Pipeline

#### Continuous Integration
1. **Trigger:** Push to any branch or PR
2. **Linting:** ESLint, Prettier check
3. **Unit Tests:** Jest with coverage
4. **Integration Tests:** Supertest
5. **Build:** Vite/Webpack build
6. **Security Scan:** npm audit, Snyk
7. **Docker Build:** Create Docker image
8. **Deploy to Staging:** Automatic on `develop` merge

#### Continuous Deployment
1. **Trigger:** Merge to `main`
2. **Run Full Test Suite:** Unit + Integration + E2E
3. **Security Scan:** Full vulnerability scan
4. **Performance Tests:** Load testing
5. **Manual Approval:** Required for production
6. **Deploy to Production:** Automatic after approval
7. **Post-Deploy Checks:** Smoke tests, monitoring

### Environment Configuration

#### Environments
- **Development:** Local development with hot reload
- **Staging:** Pre-production testing environment
- **Production:** Live production environment

#### Configuration Management
- **Environment Variables:** `.env` files (never committed)
- **Secrets:** AWS Secrets Manager or similar
- **Config Files:** Separate configs per environment
- **Feature Flags:** Toggle features without deployment

#### Local Development
```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Run tests
npm test

# Run linter
npm run lint

# Build for production
npm run build
```

### Documentation

#### Code Documentation
- **Comments:** JSDoc for public APIs
- **README:** Project setup and usage
- **API Docs:** OpenAPI/Swagger specification
- **Architecture Docs:** System design decisions

#### Knowledge Sharing
- **Weekly Standups:** Progress updates
- **Design Reviews:** Discuss major features
- **Retrospectives:** Learn from completed work
- **Wiki:** Document processes and decisions

---

## Build & Deployment

### Client Build Configuration

#### Vite Configuration
```typescript
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
  plugins: [react(), viteStaticCopy()],
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: true,
    minify: 'terser',
    rollupOptions: {
      output: {
        manualChunks: {
          'phaser': ['phaser'],
          'vendor': ['react', 'react-dom'],
        },
      },
    },
    chunkSizeWarningLimit: 1000,
  },
  optimizeDeps: {
    include: ['phaser', 'react', 'react-dom'],
  },
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
});
```

#### Asset Pipeline
- **Images:** Optimize with imagemin (WebP, AVIF)
- **Audio:** Compress with ffmpeg (MP3, OGG)
- **Fonts:** Subset with glyphhanger
- **Sprites:** Combine with texture packer

#### Build Optimization
- **Code Splitting:** Separate vendor and app code
- **Tree Shaking:** Remove unused code
- **Minification:** Terser for JavaScript, CSSNano for CSS
- **Compression:** Gzip and Brotli pre-compression

### Server Build Configuration

#### Docker Configuration
```dockerfile
# Dockerfile
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build

FROM node:20-alpine
WORKDIR /app
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/node_modules ./node_modules
COPY package*.json ./
EXPOSE 5000
CMD ["node", "dist/server.js"]
```

#### Docker Compose (Development)
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: siege_game
      POSTGRES_USER: game_user
      POSTGRES_PASSWORD: secure_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  server:
    build: .
    ports:
      - "5000:5000"
    environment:
      DATABASE_URL: postgresql://game_user:secure_password@postgres:5432/siege_game
      REDIS_URL: redis://redis:6379
    depends_on:
      - postgres
      - redis

volumes:
  postgres_data:
  redis_data:
```

### Deployment Architecture

#### Client Deployment
- **Platform:** Vercel, Netlify, or Cloudflare Pages
- **CDN:** Global CDN for asset delivery
- **Caching:** Aggressive caching for static assets
- **Rollback:** Instant rollback capability

#### Server Deployment
- **Platform:** Railway, Render, or AWS ECS
- **Scaling:** Horizontal scaling with load balancer
- **Database:** Managed PostgreSQL (RDS, Supabase)
- **Redis:** Managed Redis (ElastiCache, Upstash)

#### Infrastructure Diagram
```mermaid
graph TD
    A[Users] --> B[CDN]
    B --> C[Static Assets]
    A --> D[Load Balancer]
    D --> E[Server Instance 1]
    D --> F[Server Instance 2]
    D --> G[Server Instance N]
    E --> H[PostgreSQL Primary]
    F --> H
    G --> H
    H --> I[PostgreSQL Replica]
    E --> J[Redis Cluster]
    F --> J
    G --> J
    E --> K[Message Queue]
    F --> K
    G --> K
```

### Database Migrations

#### Migration Tool
- **Tool:** Prisma Migrate or custom migration scripts
- **Versioning:** Semantic versioning for schema changes
- **Rollback:** Automatic rollback capability

#### Migration Process
1. Create migration file
2. Write migration SQL
3. Test on staging database
4. Apply to production
5. Verify data integrity
6. Commit migration to version control

### Feature Flags

#### Flag Management
- **Tool:** LaunchDarkly, Unleash, or custom solution
- **Flags:** Toggle features without deployment
- **Targeting:** Roll out to specific users/segments

#### Flag Types
- **Boolean:** Simple on/off flags
- **Percentage:** Gradual rollout (e.g., 10%, 50%, 100%)
- **User-Based:** Enable for specific users
- **Environment-Based:** Different flags per environment

### Monitoring & Observability

#### Application Monitoring
- **Tool:** Sentry for error tracking
- **Metrics:** Custom metrics for business logic
- **Alerts:** PagerDuty or similar for critical issues

#### Infrastructure Monitoring
- **Tool:** Datadog, New Relic, or Prometheus/Grafana
- **Metrics:** CPU, memory, disk, network
- **Logs:** Centralized logging with ELK stack

#### Uptime Monitoring
- **Tool:** UptimeRobot or Pingdom
- **Endpoints:** API health checks, WebSocket connectivity
- **Alerts:** SMS/email for downtime

---

## Monetization (Web-Friendly)

### Monetization Strategy

#### Cosmetic Purchases
- **Trebuchet Skins:** Visual themes (medieval, steampunk, futuristic)
- **Projectile Effects:** Custom particle effects
- **Avatar Items:** Profile customization
- **Background Themes:** Unlockable backgrounds

#### Expansion Level Packs
- **New Levels:** 10-20 levels per pack
- **New Materials:** Special materials (crystal, obsidian)
- **New Ammo Types:** Exclusive ammunition
- **New Environments:** Unique terrain and weather

### Pricing Model

#### Cosmetic Items
- **Skins:** $0.99 - $2.99
- **Effects:** $0.49 - $1.99
- **Avatars:** $0.99 - $4.99
- **Bundles:** $4.99 - $9.99

#### Level Packs
- **Small Pack (10 levels):** $2.99
- **Medium Pack (20 levels):** $4.99
- **Large Pack (50 levels):** $9.99
- **Season Pass:** $19.99 (all packs for 6 months)

### Anti-Pay-to-Win

#### Principles
- **No Gameplay Advantages:** Cosmetics only
- **No Power-Ups:** No purchasable advantages
- **Fair Competition:** Leaderboards unaffected by purchases
- **Progression:** All gameplay content unlockable through play

#### Implementation
- **Separate Currency:** Cosmetic currency vs. gameplay currency
- **No Gates:** Never lock levels behind paywalls
- **Balance:** Ensure purchased content doesn't affect difficulty

### Ad Strategy

#### No Mid-Attempt Ads
- **No Interruptions:** Never show ads during gameplay
- **No Forced Ads:** All ads are opt-in
- **Respectful:** Ads only between levels or in menu

#### Optional Ad Rewards
- **Watch for Ammo:** +5 ammo points for watching ad
- **Watch for Hint:** Trajectory preview for watching ad
- **Daily Bonus:** Extra ammo points for daily ad

### Payment Processing

#### Payment Providers
- **Stripe:** Credit card processing
- **PayPal:** Alternative payment method
- **Apple Pay/Google Pay:** Mobile payments
- **Steam:** If ported to Steam

#### Security
- **PCI Compliance:** Follow PCI DSS standards
- **Tokenization:** Never store payment details
- **Fraud Detection:** Monitor for suspicious transactions

---

## Accessibility

### One-Hand Play

#### Controls
- **Single-Hand Mode:** All controls accessible with one hand
- **Gesture-Based:** Swipe for camera, tap for actions
- **Voice Control:** Optional voice commands (future)

#### Layout
- **Reachable Zones:** All controls within thumb reach
- **Large Touch Targets:** Minimum 44x44 pixels
- **Floating Controls:** Repositionable UI elements

### Adjustable UI Scale

#### Scale Options
- **75%:** Compact view for large screens
- **100%:** Default scale
- **125%:** Larger text and buttons
- **150%:** Maximum scale for accessibility

#### Implementation
- **CSS Variables:** Use `rem` units with base font size
- **Viewport Units:** Responsive sizing with `vw`, `vh`
- **User Preference:** Remember scale setting per user

### Physics Assist Mode

#### Features
- **Auto-Aim:** Suggests optimal angle and power
- **Trajectory Guide:** Full predicted path with collision points
- **Reduced Complexity:** Fewer adjustable parameters
- **Slower Physics:** Slower simulation for easier tracking

#### Visual Aids
- **Weak Points:** Highlight structural weaknesses
- **Impact Prediction:** Show where projectile will hit
- **Damage Indicators:** Preview damage potential

### Colorblind Support

#### Material Indicators
- **Patterns:** Add patterns to materials (stripes, dots, crosshatch)
- **Labels:** Text labels on blocks
- **High Contrast:** Enhanced border visibility
- **Colorblind Modes:** Protanopia, Deuteranopia, Tritanopia presets

#### UI Elements
- **Icons:** Use icons alongside colors
- **Text Labels:** Clear labels for all indicators
- **Status Colors:** Use patterns + colors for status

### Keyboard Navigation

#### Keyboard Shortcuts
- **Arrow Keys:** Camera movement
- **W/S:** Adjust power
- **A/D:** Adjust angle
- **1-5:** Select ammo type
- **Space:** Fire
- **Escape:** Pause menu
- **Tab:** Cycle through UI elements

#### Focus Management
- **Visible Focus:** Clear focus indicators
- **Logical Order:** Tab order matches visual layout
- **Skip Links:** Jump to main content

### Screen Reader Support

#### ARIA Labels
- **Descriptive Labels:** All UI elements have ARIA labels
- **Live Regions:** Dynamic updates announced
- **Roles:** Proper semantic roles for elements

#### Audio Descriptions
- **Game State:** Announce score, ammo, time
- **Level Info:** Read level name and description
- **Results:** Announce completion status and stars

### WCAG Compliance

#### Target Level
- **WCAG 2.1 AA:** Target compliance level
- **Testing:** Regular accessibility audits
- **User Testing:** Include users with disabilities

#### Key Requirements
- **Perceivable:** Information is presented in ways users can perceive
- **Operable:** Interface is operable by all users
- **Understandable:** Information and operation are understandable
- **Robust:** Content is robust enough for various assistive technologies

---

## Minimum Viable Product (MVP)

### MVP Scope

The MVP focuses on core gameplay mechanics with a simplified feature set to validate the game concept and gather user feedback.

### MVP Features

#### Core Gameplay
- **10 Levels:** Hand-crafted levels with increasing difficulty
- **Basic Trebuchet:** Arm pullback and release angle only
- **Stone Ammo:** Single ammunition type
- **Physics Simulation:** Full Matter.js simulation with destruction
- **Win/Loss Conditions:** Destroy percentage threshold

#### UI/UX
- **Menu Scene:** Level selection grid
- **Game Scene:** Basic game controls (power, angle, fire)
- **Result Scene:** Score display, star rating, retry button
- **Progression:** Local storage only (no server)

#### Technical
- **Client Only:** No backend server required
- **Static Level Data:** Levels embedded in client code
- **Local Progression:** Save progress to localStorage
- **Responsive Design:** Desktop and mobile support

### MVP Exclusions

#### Not in MVP
- ❌ Server backend
- ❌ User accounts/authentication
- ❌ Online leaderboards
- ❌ Multiple ammo types
- ❌ Advanced trebuchet features (counterweight, sling)
- ❌ Multiplayer
- ❌ Monetization
- ❌ Analytics
- ❌ Localization

### MVP Success Criteria

#### Technical Metrics
- **Performance:** 60 FPS on mid-range mobile devices
- **Load Time:** < 3 seconds initial load
- **Crash Rate:** < 1% of sessions
- **Browser Support:** Chrome, Firefox, Safari, Edge (latest versions)

#### User Engagement
- **Completion Rate:** 70% of players complete all 10 levels
- **Session Length:** Average 5-10 minutes per session
- **Retention:** 40% of players return within 7 days
- **Rating:** 4.0+ star rating on app stores

### MVP Development Timeline

#### Phase 1: Foundation (Week 1-2)
- [ ] Set up project structure (Vite, TypeScript, Phaser)
- [ ] Implement basic Phaser scenes (Boot, Menu, Game, Result)
- [ ] Set up Matter.js physics world
- [ ] Create basic UI components

#### Phase 2: Core Mechanics (Week 3-4)
- [ ] Implement trebuchet with arm and counterweight
- [ ] Create projectile firing mechanism
- [ ] Implement collision detection and damage
- [ ] Add destruction mechanics and debris

#### Phase 3: Level Design (Week 5-6)
- [ ] Design and implement 10 levels
- [ ] Create terrain generation
- [ ] Add castle structures with materials
- [ ] Implement win/loss conditions

#### Phase 4: Polish (Week 7-8)
- [ ] Add visual effects (particles, animations)
- [ ] Implement sound effects and music
- [ ] Add local progression system
- [ ] Performance optimization

#### Phase 5: Testing & Launch (Week 9-10)
- [ ] Cross-browser testing
- [ ] Mobile testing
- [ ] Bug fixes and refinement
- [ ] Deploy to production

### MVP Technology Stack

#### Client
- **Phaser 3.80+**
- **Matter.js 0.19+**
- **TypeScript 5.0+**
- **Vite 5.0+**

#### Build & Deploy
- **Vite** for building
- **Netlify/Vercel** for hosting
- **GitHub Actions** for CI/CD

#### Assets
- **Placeholder graphics:** Simple shapes and colors
- **Free sound effects:** From open-source libraries
- **Basic fonts:** Google Fonts (Roboto)

### MVP Post-Launch

#### Data Collection
- **Analytics:** Basic usage tracking (session length, completion rates)
- **Feedback:** In-game feedback form
- **Bug Reports:** Automated error tracking

#### Decision Points
- **Engagement:** If retention > 30%, proceed to full development
- **Performance:** If performance issues, optimize before expansion
- **Feedback:** If positive feedback, plan next features

---

## Next Expansions

### Phase 1: Server Integration (Post-MVP)

#### Features
- **Backend Server:** Node.js/Express with PostgreSQL and Redis
- **User Accounts:** Registration, login, profile management
- **Online Progression:** Server-side progression storage
- **Leaderboards:** Global and per-level leaderboards
- **API Documentation:** OpenAPI/Swagger specification

#### Technical
- **REST API:** Full API implementation
- **WebSocket:** Real-time events
- **Authentication:** JWT-based auth
- **Database:** PostgreSQL schema and migrations

#### Success Criteria
- **API Latency:** p95 < 300ms
- **Uptime:** 99.9% availability
- **User Registration:** 1000+ users in first month

### Phase 2: Advanced Gameplay

#### Features
- **20 New Levels:** More complex castles and challenges
- **Multiple Ammo Types:** Fire pot, plague barrel, chain shot
- **Advanced Trebuchet:** Counterweight mass, sling length
- **Environmental Effects:** Wind, gravity variations
- **Time Limits:** Optional time challenge mode

#### Technical
- **Level Editor:** In-game level creation tool
- **Procedural Generation:** Algorithmic level generation
- **Physics Optimization:** Performance improvements
- **Save/Load System:** Replay system

#### Success Criteria
- **Level Completion Rate:** 60% for new levels
- **Ammo Usage:** Balanced usage across ammo types
- **User Feedback:** 4.0+ rating for new features

### Phase 3: Social Features

#### Features
- **Friends System:** Add friends, compare progress
- **Challenge Mode:** Send challenges to friends
- **Spectating:** Watch friends play in real-time
- **Sharing:** Share results on social media
- **Clans/Guilds:** Form groups, compete together

#### Technical
- **Real-Time Multiplayer:** WebSocket-based multiplayer
- **Room Management:** Matchmaking system
- **Social Graph:** Friend relationship tracking
- **Push Notifications:** Mobile push notifications

#### Success Criteria
- **Social Engagement:** 30% of users add friends
- **Challenge Participation:** 20% of users send/receive challenges
- **Retention:** 50% weekly retention for social users

### Phase 4: Monetization

#### Features
- **Cosmetic Shop:** Purchase skins, effects, avatars
- **Level Packs:** Premium level expansions
- **Battle Pass:** Seasonal progression with rewards
- **Daily Rewards:** Login bonuses, daily challenges
- **Ad Integration:** Optional ad rewards

#### Technical
- **Payment Processing:** Stripe/PayPal integration
- **In-App Purchases:** Web-based purchase flow
- **Receipt Validation:** Server-side purchase verification
- **Analytics:** Revenue tracking and reporting

#### Success Criteria
- **Conversion Rate:** 2-5% of users make purchases
- **ARPU:** $2-5 per active user
- **Revenue:** $10,000+ in first month

### Phase 5: Content Expansion

#### Features
- **50 New Levels:** Massive content expansion
- **New Materials:** Crystal, obsidian, explosive blocks
- **New Ammo Types:** Explosive barrel, freezing shot
- **New Environments:** Snow, desert, underwater themes
- **Boss Levels:** Special challenge levels with unique mechanics

#### Technical
- **Asset Pipeline:** Improved asset loading and optimization
- **Level Compression:** Efficient level data storage
- **Dynamic Loading:** On-demand asset loading
- **Content Management System:** Admin panel for level management

#### Success Criteria
- **Content Consumption:** 70% of new levels completed
- **User Engagement:** 20% increase in session length
- **Retention:** 60% monthly retention

### Phase 6: Platform Expansion

#### Features
- **Mobile Apps:** Native iOS and Android apps
- **Steam Release:** PC version with Steam integration
- **Console Port:** Nintendo Switch, PlayStation, Xbox
- **Cross-Platform Play:** Play across all platforms
- **Cloud Saves:** Sync progress across devices

#### Technical
- **Native Wrappers:** React Native or Cordova for mobile
- **Steamworks:** Steam achievements, leaderboards, cloud saves
- **Console SDKs:** Platform-specific SDKs and certifications
- **Unified Backend:** Single backend for all platforms

#### Success Criteria
- **Platform Distribution:** 30% mobile, 30% PC, 40% console
- **Cross-Platform Play:** 50% of users play on multiple platforms
- **Total Users:** 100,000+ across all platforms

### Phase 7: Advanced Features

#### Features
- **AI Opponents:** Play against computer-controlled trebuchets
- **Tournament Mode:** Competitive events with prizes
- **Custom Games:** User-created levels and game modes
- **Mod Support:** Allow community-created content
- **VR Support:** Virtual reality gameplay (optional)

#### Technical
- **AI System:** Pathfinding and strategy AI
- **Tournament System:** Bracket management, live streaming
- **Mod API:** Public API for mod development
- **VR Integration:** Unity/Unreal VR support

#### Success Criteria
- **AI Difficulty:** Balanced difficulty curve
- **Tournament Participation:** 10% of users participate
- **Mod Community:** 100+ community-created mods
- **VR Adoption:** 5% of users try VR mode

### Expansion Roadmap Summary

| Phase | Timeline | Key Features | Success Metrics |
|-------|----------|--------------|------------------|
| MVP | 10 weeks | Core gameplay, 10 levels | 70% completion, 4.0+ rating |
| Phase 1 | 8 weeks | Server, accounts, leaderboards | 1000+ users, 99.9% uptime |
| Phase 2 | 10 weeks | 20 levels, multiple ammo, advanced trebuchet | 60% completion, 4.0+ rating |
| Phase 3 | 12 weeks | Friends, challenges, spectating | 30% social engagement |
| Phase 4 | 8 weeks | Cosmetics, level packs, monetization | 2-5% conversion, $10K+ revenue |
| Phase 5 | 12 weeks | 50 levels, new materials, new environments | 70% content consumption |
| Phase 6 | 16 weeks | Mobile, Steam, console, cross-platform | 100K+ users across platforms |
| Phase 7 | 12 weeks | AI, tournaments, mods, VR | 10% tournament participation |

---

## Success Criteria

### Technical Success

#### Performance
- Stable physics at 60 FPS on mid-range mobile devices
- Deterministic outcomes per seed
- Load time < 3 seconds on 4G connection
- 99.9% server uptime

#### Quality
- Crash rate < 1% of sessions
- Bug-free core gameplay
- Cross-browser compatibility
- Mobile responsiveness

### Gameplay Success

#### Engagement
- High replayability via emergent destruction
- Satisfying physics feedback
- Clear progression system
- Balanced difficulty curve

#### Retention
- 40% of players return within 7 days
- 30% of players complete all levels
- 20% of players engage with social features
- 10% of players make purchases

### Business Success

#### Growth
- 100,000+ registered users
- 10,000+ daily active users
- Positive reviews (4.0+ rating)
- Community growth (forums, Discord)

#### Revenue
- $100,000+ annual revenue
- 2-5% conversion rate
- $2-5 ARPU (Average Revenue Per User)
- Sustainable monetization without pay-to-win

---

## Design Pillar

This is a game about **thinking like an engineer**. The joy comes not from speed, but from watching a well-planned strike turn a fortress into a physics lesson.

Every element of the game should reinforce this core value:
- **Physics First:** Realistic, satisfying destruction
- **Strategic Depth:** Multiple approaches to each level
- **Learning Through Play:** Players discover physics principles naturally
- **Satisfying Feedback:** Clear visual and audio feedback for every action
- **Progressive Complexity:** Introduce mechanics gradually, build mastery over time

The game should make players feel smart when they succeed, and curious to try again when they fail.
