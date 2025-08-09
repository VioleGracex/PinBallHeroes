TurnManager Game Flow
====================

This file describes the high-level flow and responsibilities of the TurnManager script.

Game Loop Phases:
-----------------
1. **Combat Mode**
    - Player and enemies take turns until one side is defeated.
    - If all enemies are defeated, proceed to currency collection.
2. **Currency Collection**
    - All currency is magnetically collected to the cannon using CannonManager.
3. **Pinball Mode**
    - (Not yet implemented) Player plays a pinball minigame.
    - After pinball, all currency is calculated.
4. **Card Select Mode**
    - Player is presented with rogue-like card upgrades.
    - After selection, upgrades are applied to the player.
5. **Return to Combat**
    - The loop continues with new waves and upgrades.

Key Methods:
------------
- `StartCombatMode()`: Entry point for the main game loop..
- `StartPinballMode()`: Placeholder for pinball gameplay.
- `StartCardMode()`: Placeholder for card selection and upgrades. 

This structure makes it easy to extend or modify each phase independently.
