# Zombie Apocalypse: Survival

A high-quality realistic 3D third-person zombie survival game built with **Unity 6** and **C#**.

---

## Project Structure

```text
ZombieApocalypseSurvival/
├── .gitignore
├── README.md
├── Packages/
│   └── manifest.json                # Pre-configured Unity 6 dependencies (URP, Input System, TMP, AI Navigation)
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   └── ProjectVersion.txt           # Target Unity 6 Version (6000.0.0f1)
└── Assets/
    ├── Art/                         # 3D Mesh Models, Textures, Shaders, VFX
    │   ├── Characters/
    │   │   ├── Player/
    │   │   └── Zombies/
    │   ├── Weapons/
    │   ├── Environment/
    │   ├── Props/
    │   └── VFX/
    ├── Animations/                  # Animation Controllers, Clips, Blend Trees
    │   ├── Player/
    │   ├── Zombies/
    │   └── Weapons/
    ├── Audio/                       # Music, Sound Effects, Spatial Audio
    │   ├── Music/
    │   ├── Weapons/
    │   ├── Zombies/
    │   ├── Environment/
    │   └── UI/
    ├── Materials/                   # PBR and URP Materials
    ├── Prefabs/                     # Reusable GameObject Assemblies
    │   ├── Player/
    │   ├── Zombies/
    │   ├── Weapons/
    │   ├── Environment/
    │   └── UI/
    ├── Scenes/                      # Core Game Environments
    │   ├── MainMenu/
    │   │   └── MainMenu.unity       # Entry point scene for main menu
    │   ├── Gameplay/
    │   │   └── Gameplay.unity       # Main urban city survival environment
    │   └── Test/
    │       └── TestArena.unity      # Isolated prototyping & combat testing sandbox
    ├── Scripts/                     # Clean C# System Architecture
    │   ├── Player/                  # Player locomotion, health, stamina, camera, animations
    │   ├── Weapons/                 # Weapon stats, controller, shooting, recoil, ammo
    │   ├── Zombies/                 # Zombie types data & health
    │   ├── AI/                      # NavMesh AI state machine (Patrol, Search, Chase, Attack)
    │   ├── Missions/                # Objective pipeline & progress tracking
    │   ├── Inventory/               # Inventory slots & item storage
    │   ├── UI/                      # Dynamic HUD, Main Menu, Pause Menu
    │   ├── Audio/                   # Sound effects & background music
    │   └── Systems/                 # GameManager singleton & SaveSystem JSON data
    ├── UI/                          # Textures, Sprites, Fonts
    ├── Resources/                   # Dynamic runtime loading assets
    └── Settings/                    # URP Graphic Render Pipeline Assets & Settings
```

---

## Core Systems Architecture

### 1. Core & Save System (`ZombieApocalypse.Core`)
* [`GameManager.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Systems/GameManager.cs): Central game state controller (`MainMenu`, `Playing`, `Paused`, `GameOver`, `MissionComplete`).
* [`SaveSystem.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Systems/SaveSystem.cs): Handles JSON serialization of player health, position, inventory, and mission progress to `Application.persistentDataPath`.

### 2. Player System (`ZombieApocalypse.Player`)
* [`PlayerController.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Player/PlayerController.cs): CharacterController locomotion, grounded physics, sprint, crouch, and jump states.
* [`PlayerCamera.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Player/PlayerCamera.cs): Third-person camera orbit and target tracking.
* [`PlayerHealth.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Player/PlayerHealth.cs): Health pool management and event notifications (`OnHealthChanged`, `OnPlayerDied`).
* [`PlayerStamina.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Player/PlayerStamina.cs): Stamina consumption during sprint with automatic delayed regeneration.
* [`PlayerAnimation.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Player/PlayerAnimation.cs): Animator component driver binding locomotion speed, aiming, firing, and reload parameters.

### 3. Weapon System (`ZombieApocalypse.Weapons`)
* [`WeaponData.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Weapons/WeaponData.cs): ScriptableObject defining weapon stats (Pistol, Shotgun, Assault Rifle damage, fire rate, mag size, recoil).
* [`WeaponController.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Weapons/WeaponController.cs): Equipping weapons, rate-of-fire timing, magazine ammo tracking, and reload handling.

### 4. Zombie & AI System (`ZombieApocalypse.Zombies` & `ZombieApocalypse.AI`)
* [`ZombieData.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Zombies/ZombieData.cs): ScriptableObject defining stats for Walker, Runner, Tank, and Boss zombie variants.
* [`ZombieHealth.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Zombies/ZombieHealth.cs): Zombie health, hit feedback, and elimination callbacks.
* [`ZombieAI.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/AI/ZombieAI.cs): NavMeshAgent AI finite state machine (`Idle`, `Patrol`, `Search`, `Chase`, `Attack`).

### 5. Mission System (`ZombieApocalypse.Missions`)
* [`MissionData.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Missions/MissionData.cs): ScriptableObject for mission objectives and target requirements.
* [`MissionManager.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Missions/MissionManager.cs): Tracks active mission progress and triggers mission completion events.

### 6. Inventory System (`ZombieApocalypse.Inventory`)
* [`ItemData.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Inventory/ItemData.cs): ScriptableObject defining items (Weapons, Ammo, Medkits, Resources).
* [`InventoryManager.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Inventory/InventoryManager.cs): Slot storage, item stacking, and inventory updates.

### 7. UI System (`ZombieApocalypse.UI`)
* [`HUDController.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/UI/HUDController.cs): Realtime HUD display for Health, Stamina, Ammo, Crosshair, and Objectives.
* [`MainMenuUI.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/UI/MainMenuUI.cs): Main menu screen button handlers.
* [`PauseMenuUI.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/UI/PauseMenuUI.cs): Pause menu overlay toggle and scene reloading.

### 8. Audio System (`ZombieApocalypse.Audio`)
* [`AudioManager.cs`](file:///D:/UnityProjects/ZombieApocalypseSurvival/Assets/Scripts/Audio/AudioManager.cs): Global manager for playing spatial 3D SFX and background music tracks.

---

## Phase 2 Roadmap

In **Phase 2**, we will build:
1. **Player Locomotion & Camera**:
   - Unity New Input System actions mapping (WASD, Mouse Look, Shift Sprint, Ctrl Crouch, Space Jump).
   - CharacterController physics integration with smooth third-person camera rotation and mouse orbit.
2. **Player Visual Setup in TestArena**:
   - Setting up a temporary capsule / mannequin player object with ground detection and third-person camera target.
