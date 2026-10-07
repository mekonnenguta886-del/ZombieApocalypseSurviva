# 🧟 Zombie Apocalypse: Survival (Release Candidate)

> **Unity 6 | 3D Third-Person Survival Action | Feature-Complete Release Candidate**

[![Unity 6000.0.0f1](https://img.shields.io/badge/Unity-6000.0.0f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-orange.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![Build Status](https://img.shields.io/badge/Build-Windows%2064--bit-success.svg)](https://github.com/mekonnenguta886-del/ZombieApocalypseSurviva)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📖 Overview

**Zombie Apocalypse: Survival** is a feature-complete, AAA-architected third-person zombie survival game built with **Unity 6 (6000.0.0f1)** and **C#**. 

Set in an abandoned post-apocalyptic city, players must explore ruined buildings, manage survival vitals (Health, Hunger, Thirst, Stamina), craft equipment, complete supply missions, upgrade weapons at the Safe House, and survive dynamic weather hazards and ferocious zombie horde attacks.

---

## 🌟 Key Features & Systems

### 🕹️ 1. Advanced Player Controller & Camera
* **CharacterController Locomotion**: Smooth walking, sprinting, crouching, jumping, and grounded physics.
* **Over-the-Shoulder Camera**: Third-person orbit camera with aim-down-sight (ADS) zoom, shoulder offset, vertical recoil impulse recovery, and spherecast obstruction collision prevention.
* **Input Customization**: Full mouse sensitivity scaling and Invert-Y axis toggling.

### 🔫 2. Weaponry & Combat System
* **3 Weapon Categories**:
  * 🔫 **Pistol**: High mobility sidearm with quick reload.
  * 🔴 **Shotgun**: High close-range burst damage with pellet spread.
  * 💥 **Assault Rifle**: Automatic fire, medium-to-long range accuracy.
* **Combat Mechanics**: Fire rate timing, magazine reload management, recoil kick, hitmarkers, critical headshot multipliers, and 3D noise event propagation that alerts nearby zombie groups.
* **Workbench Weapon Upgrades**: Spend scrap parts to boost weapon damage, magazine capacity, and fire rate.

### 🧟 3. Zombie AI & Horde Mechanics
* **4 Zombie Variants**:
  * 🚶 **Walker**: Standard slow zombie, high spawn count.
  * 🏃 **Runner**: Fast aggressive zombie that rushes the player.
  * 🛡️ **Tank**: Heavy armored zombie with high health pool and staggering attacks.
  * 👑 **Boss Zombie**: Massive threat with unique telegraphs and full Boss HUD health bar overlay.
* **NavMesh AI State Machine**: `Patrol`, `Search`, `Chase`, and `Attack` states.
* **Group Alert System**: Shooting or making loud noise triggers area-of-effect noise events that rouse idle zombies.

### ⛺ 4. Safe House Base & Survival Mechanics
* **Safe House Refuge (`0, 0, 0`)**: Safe zone trigger suppressing zombie aggression and environmental hazard damage.
* **Base Stash Chest**: Store excess loot securely.
* **Rest Cot**: Sleep to advance time and restore health.
* **Upgrade Terminal**: Upgrade base defenses and utilities.
* **Survival Vitals**: Monitor Health, Stamina, Hunger, and Thirst with low survival warnings and visual indicators.

### 🌧️ 5. Dynamic Day/Night & Weather Hazards
* **24-Hour Time Clock**: Daylight hours (`06:00`–`20:00`) provide safe visibility; Nightfall (`20:00`–`06:00`) boosts zombie speed (+35%) and spawn caps (+50%).
* **Dynamic Weather Patterns**:
  * ☀️ **Clear Weather**: Standard conditions.
  * 🌧️ **Heavy Rain**: Increased thirst drain rate.
  * 🌫️ **Dense Fog**: Reduced visibility & accelerated hunger drain.
  * ☣️ **Toxic Storm**: Airborne chemical hazard dealing periodic health damage.

### 🎯 6. Missions & Dynamic World Events
* **Mission Pipeline**: Complete multi-stage missions (e.g., *Medical Supply Run*) with world objective markers and UI checklists.
* **World Events**: Supply Drops, Survivor Distress Calls, and Zombie Horde Waves with countdown timers and reward containers.

### 💾 7. Atomic Save/Load Persistence
* **Atomic JSON Storage**: 3-stage safe write pattern (`savegame.json.tmp` $\rightarrow$ `savegame.json.bak` $\rightarrow$ `savegame.json`) preventing corruption.
* **Comprehensive Serialization**: Saves player transform, health, hunger, thirst, inventory, equipped weapons, ammo, mission objectives, door states, loot box states, XP perks, weather ID, and safe house storage.
* **Main Menu "CONTINUE"**: Detects existing save files on launch and restores complete state seamlessly.

### ⚙️ 8. Main Menu & Settings System
* **Main Menu Options**: **NEW GAME**, **CONTINUE**, **SETTINGS**, and **QUIT**.
* **Settings Modal**:
  * **Graphics**: Performance Presets (LOW, MEDIUM, HIGH, ULTRA), Quality Level, VSync Toggle, Fullscreen.
  * **Audio**: Master Volume, Music Volume, and SFX Volume sliders.
  * **Gameplay**: Mouse Sensitivity slider and Invert-Y toggle.

### 🔊 9. Audio Engine & Performance Pooling
* **Spatial 3D SFX**: 16-channel non-allocating AudioSource channel pool with logarithmic distance attenuation.
* **Procedural PCM Sound Fallback**: Programmatically synthesizes weapon fire, reload, empty click, footstep, and zombie audio feedback so sound works 100% reliably out of the box.
* **Object Pooling**: [`SimpleObjectPool`](file:///d:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Systems/SimpleObjectPool.cs) for zombies and combat projectiles to maintain 60+ FPS stability.

---

## 🕹️ Controls Guide

| Action | Control Key |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` |
| **Look Around** | Mouse |
| **Aim Down Sight (ADS)** | `Right Click` (Hold) |
| **Fire Weapon / Attack** | `Left Click` |
| **Reload** | `R` |
| **Sprint** | `Left Shift` (Hold) |
| **Crouch** | `C` |
| **Jump** | `Spacebar` |
| **Equip Slot 1 / 2 / 3** | `1`, `2`, `3` |
| **Interact** | `E` (Doors, Loot Crates, Workbench, Stash, Bed, Terminal) |
| **Inventory UI** | `I` |
| **Quick Save / Quick Load** | `F5` / `F9` |
| **Pause / Settings** | `Escape` |

---

## 📁 Repository Directory Structure

```text
ZombieApocalypseSurvival/
├── Assets/
│   ├── Editor/                      # Production Build Automation Scripts
│   │   └── BuildScript.cs
│   ├── Scenes/                      # Game Environments
│   │   ├── MainMenu/MainMenu.unity # Entry point Main Menu scene
│   │   ├── Gameplay/Gameplay.unity # Core city survival environment
│   │   └── Test/TestArena.unity    # Prototyping & combat testing arena
│   ├── Scripts/                     # Modular C# Architecture
│   │   ├── AI/                      # Zombie FSM & Noise propagation (ZombieAI, NoiseManager)
│   │   ├── Audio/                   # Spatial Audio & PCM Synthesis (AudioManager)
│   │   ├── Crafting/                # Recipe data & CraftingSystem
│   │   ├── Environment/             # Hazard zones & EnvironmentalConditionManager
│   │   ├── Inventory/               # ItemData, LootContainer, InventorySystem
│   │   ├── Missions/                # MissionData & MissionManager
│   │   ├── Player/                  # Locomotion, Camera, Health, Stamina, Survival
│   │   ├── Progression/             # XP & Perks System (PlayerProgressionSystem)
│   │   ├── SafeHouse/               # SafeHouseManager, BaseStash, BedRest, UpgradeTerminal
│   │   ├── Save/                    # SaveManager & atomic SaveFileUtility
│   │   ├── Systems/                 # GameManager, DifficultyManager, SettingsManager
│   │   ├── UI/                      # HUDController, MainMenuUI, PauseMenuUI, SettingsUIController
│   │   ├── Weapons/                 # WeaponController, WeaponUpgradeSystem
│   │   ├── World/                   # WorldTimeManager, WeatherManager, DoorController, WorldSetup
│   │   ├── WorldEvents/             # WorldEventManager & SupplyDropContainer
│   │   └── Zombies/                 # ZombieSpawner, ZombieHealth, ZombieHitbox
│   └── Settings/                    # Universal Render Pipeline (URP) Config
└── ProjectSettings/                 # Unity 6 Project & Tag Configurations
```

---

## 🛠️ Building & Running

### Requirements
* **Unity Version**: Unity 6 (`6000.0.0f1`) or higher
* **Target Platform**: Windows 64-bit (Standalone)
* **Render Pipeline**: Universal Render Pipeline (URP)

### Running in Unity Editor
1. Clone the repository:
   ```bash
   git clone https://github.com/mekonnenguta886-del/ZombieApocalypseSurviva.git
   ```
2. Open the project folder in **Unity Hub** (version 6000.0.0f1).
3. Open [`Assets/Scenes/MainMenu/MainMenu.unity`](file:///d:/UnityProjects/ZombieApocalypseSurvival/Assets/Scenes/MainMenu/MainMenu.unity).
4. Press **Play**.

### Standalone Windows 64-bit Release Build
To generate a production release build:
1. Open Unity Editor.
2. Click top menu: **Build $\rightarrow$ Build Windows 64-Bit Release**.
3. The standalone `.exe` package will compile to:
   ```text
   D:\UnityProjects\ZombieApocalypseSurvival_Builds\Windows64\ZombieApocalypseSurvival.exe
   ```

---

## 📜 Development History (Phases 1–20)

| Phase | Description |
| :--- | :--- |
| **Phase 1–2** | Foundation, Player Controller, 3D Camera & Animations |
| **Phase 3–4** | Zombie AI, Combat Foundation & Weapon Systems |
| **Phase 5–6** | Inventory, Survival Vitals, World Locations & Missions |
| **Phase 7–13**| Atomic Save/Load, Zombie Variants/Boss, Crafting, Upgrades & XP Perks |
| **Phase 14–16**| World Events, Hazards, Dynamic Day/Night Cycle & Weather |
| **Phase 17–19**| Safe House Base, Final Gameplay Integration, Audio & UI/UX Polish |
| **Phase 20** | **Final QA, Visual Upgrade, Settings System & Windows Production Build** |

---

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.
