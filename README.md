# Deathmatch Score System

**Author:** Priyanshu Yadav  
**Platform:** Android  
**Unity Version:** Unity 6

---
## 🎥 Gameplay Demo

Watch the gameplay demonstration here:

[![Gameplay Demo](https://img.youtube.com/vi/JrElSp3xnFI/maxresdefault.jpg)](https://youtu.be/JrElSp3xnFI)

## Overview

A deathmatch-style match simulation focused on **architecture and clean system design**.

- 10 players spawn automatically at match start
- Every 1–2 seconds, a kill is simulated between two random players
- Killer receives **+1 score**, victim **respawns after 3 seconds**

**Match ends when:**
- A player reaches **10 kills**, or
- **3 minutes** have elapsed

**UI includes:**
- Live leaderboard
- Match timer
- Winner screen

---

## Architecture

> **Design rule:** Gameplay logic should not depend on `MonoBehaviour`.

Core systems are plain C# classes. Unity components act only as entry points or scene bridges — keeping logic testable, predictable, and easy to extend.

```
Unity Layer (MonoBehaviours)
    MatchBootstrapper
    KillSimulator
    UI Components

Core Systems (Pure C#)
    MatchController
    TimerSystem
    ScoreSystem
    PlayerRegistry
    PlayerData

Event Layer
    GameEvents (event bus)
```

`MatchBootstrapper` is the single initialization point — it creates systems, connects dependencies, and forwards `deltaTime` to the simulation. Everything else communicates through events.

---

## Project Structure

```
Scripts/
├── Core/
│   └── GameEvents.cs
├── Match/
│   ├── MatchBootstrapper.cs
│   ├── MatchController.cs
│   └── MatchConfig.cs
├── Player/
│   ├── PlayerData.cs
│   └── PlayerRegistry.cs
├── Systems/
│   ├── TimerSystem.cs
│   ├── ScoreSystem.cs
│   └── KillSimulator.cs
└── UI/
    ├── UIManager.cs
    ├── LeaderboardUI.cs
    ├── LeaderboardRowUI.cs
    ├── TimerUI.cs
    ├── KillFeedUI.cs
    ├── ScorePopupUI.cs
    └── WinnerUI.cs
```

---

## Core Systems

| System | Responsibility |
|---|---|
| `MatchController` | Orchestrates match rules, win conditions, kill processing |
| `PlayerRegistry` | Centralizes player state, random alive player selection, sorted snapshots |
| `ScoreSystem` | Registers kills, updates scores, fires score change events |
| `TimerSystem` | Countdown timer driven by a single `Tick(deltaTime)` from the bootstrapper |

---

## Event Communication

Systems are decoupled via a lightweight event bus:

```
GameEvents.OnKill
GameEvents.OnScoreChanged
GameEvents.OnMatchEnded
```

**Kill flow example:**
```
MatchController  →  fires OnKill
ScoreSystem      →  updates score
KillFeedUI       →  displays kill message
ScorePopupUI     →  shows +1 popup
LeaderboardUI    →  marks UI dirty
```

Each system reacts independently — no direct cross-references.

---

## Configurable Match Rules

Match parameters live in a `ScriptableObject` (`MatchConfig`), editable without touching code:

- Player count
- Kill limit
- Match duration
- Respawn delay
- Kill simulation interval

---

## Mobile Optimization (Android)

### Timer Updates
- Updates once per second (not every frame)
- Text built using a **preallocated char buffer** — no repeated string allocations

### Leaderboard Updates
- Score change → mark dirty → rebuild **once in `LateUpdate`**
- Multiple kills in the same frame = one UI rebuild

### Sorting
- Uses a **reusable comparer** instead of lambda expressions — avoids closure allocations

### Player Lists
- Temporary lists are **reused**, not recreated, during random player selection

### Acceptable Allocations
- Kill feed strings: one per event at ~1–2/sec — negligible cost
- Score popups and feed rows use **object pooling**

---

## Scaling

### Multiplayer
The event-driven architecture makes network integration straightforward:

```
// Local
KillSimulator → fires OnKill

// Networked (drop-in replacement)
Network message → fires OnKill
```

All downstream systems remain unchanged.

### 50+ Players
Core systems handle large player counts without changes. UI would need:
- Virtualized list rendering
- Row pooling
- Scrollable leaderboard

