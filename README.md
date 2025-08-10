# PinBall Heroes

PinBall Heroes is a Unity-based hybrid game combining pinball mechanics with card-based upgrades and strategic gameplay. Players launch pinballs, trigger pins for bonuses, and use cards to upgrade their stats and abilities. The game is designed for mobile platforms.

## Game Overview

### Core Gameplay Modes

- **Idle Combat:**
  - Your heroes automatically battle waves of enemies, earning currency and rewards over time.
  - Progress continues even when not actively playing, allowing for incremental upgrades and resource collection.
  - Strategic upgrades and card choices impact your team's effectiveness in combat.

- **Pinball Mode:**
  - Enter pinball mode to launch pinballs onto a dynamic field filled with pins, obstacles, and bonuses.
  - Use flippers and precise aiming to maximize your score, collect currency, and trigger special pin effects.
  - Pinball mode is a skill-based minigame that can yield powerful rewards and progress boosts.

- **Roguelike Cards:**
  - Collect, upgrade, and choose from a variety of cards that offer unique abilities, stat boosts, and modifiers.
  - Each run or session presents new card choices, encouraging different strategies and builds.
  - Card selection is permanent for the run, adding replayability and depth to each playthrough.
- **Genre:** Pinball / Card Strategy Hybrid
- **Engine:** Unity
- **Key Features:**
  - Pinball gameplay with physics-based flippers and obstacles
  - Card system for upgrades and special abilities
  - Dynamic pin field with randomized pin placement
  - Mobile-friendly controls and UI
  - Currency and stat progression

## Core Systems

### 1. Pinball System
- **PinballManager.cs:** Handles pinball mode, spawning, and flipper UI.
- **Flipper.cs:** Controls flipper movement using HingeJoint2D and UI buttons.
- **CannonManager.cs:** Manages aiming, shooting, and muzzle logic for launching pinballs.
- **PinFieldGenerator.cs:** Generates the pin field with adjustable spawn area, row skipping, and pin count limiting.

### 2. Pin Logic
- **RectanglePin.cs:** Multiplier pin with cooldown to prevent rapid retriggering. Triggers score multipliers when hit.
- **CirclePin.cs:** Spawns additional pinballs when hit, with a cooldown to prevent spamming.

### 3. Card and Upgrade System
- **CardWindow UI:** Animated window for selecting and upgrading cards.
- **Stat Upgrades:** Cards can upgrade player stats, pinball abilities, and flipper power.
- **Currency:** Dropped by pinballs, used to purchase upgrades.

### 4. UI and Controls
- **Mobile Controls:** On-screen buttons for flippers and shooting.
- **Desktop Controls:** Keyboard and mouse support for aiming and flippers.
- **DOTween Animations:** Smooth UI transitions and feedback.

### 5. Miscellaneous
- **Debug Logging:** For development and troubleshooting.
- **Gizmos:** Visualize and adjust pin field spawn area in the Unity Editor.
- **Code Organization:** Regions and comments for maintainability.

## Getting Started
1. Open the project in Unity (recommended version: 2022.3+).
2. Open the `PinBallHeroes` scene from the `Assets/Scenes/` folder.
3. Press Play to start the game.
4. Use the on-screen or keyboard controls to launch pinballs and control flippers.
5. Collect currency, trigger pins, and use cards to upgrade your abilities.

## Customization
- **Pin Field:** Adjust spawn area and pin settings in `PinFieldGenerator`.
- **Cards:** Add or modify card assets and logic in the Card system.
- **UI:** Customize UI elements and animations using Unity UI and DOTween.

## Code Structure
- `Assets/_Game/_Scripts/Pinball/` — Pinball, pin, and flipper logic
- `Assets/_Game/_Scripts/Cannon/` — Cannon aiming and shooting
- `Assets/_Game/_Scripts/UI/` — UI windows, card selection, and animations
- `Assets/_Game/_Scripts/` — Core managers and utilities

## Credits
- Developed by VioleGracex
- Uses DOTween, TextMeshPro, and Unity's Input System

## License
This project is for educational and non-commercial use. For other uses, please contact the developer.
