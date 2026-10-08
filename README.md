# KOFXIII Hitbox Viewer / Frame Data

A Windows training tool for **THE KING OF FIGHTERS XIII GLOBAL MATCH**. It shows hitboxes (where attacks connect) and hurtboxes (where characters can be hit), helping players understand spacing, reach, and why an attack hits or misses.

The experimental frame meter displays Startup, Active, Recovery, OnHit, and OnGuard measurements. Some moves and characters remain unverified; unavailable readings show `--`. This is not a complete full-roster frame-data database.

## Installation

Download the autoload ZIP from Releases, close KOF and any older viewer, and copy **dinput8.dll** plus the **KOF13HITBOX** folder beside the game's `game.exe`. Do not overwrite another mod's dinput8.dll. Launch KOF normally in windowed or borderless mode and enter offline Training.

- **F8:** attach and toggle the overlay.
- **F9:** open settings.
- **X:** hide the settings panel; F8 keeps working.
- **Tray menu → Exit viewer:** detach and exit for the session.

The viewer starts silently with the game; no manual EXE launch is needed. It closes automatically when KOF exits.

## Status

Early release. Only the supported Global Match executable can attach. Exclusive fullscreen and input recording/playback are not supported. Initial autoload/F8 operation was confirmed in-game. The latest X-to-hide fix passed automated lifecycle tests and awaits an in-game acceptance check.

Inspired by [odabugs/kof13-hitboxes](https://github.com/odabugs/kof13-hitboxes), with Dear ImGui for the interface. This unofficial project is not affiliated with SNK. No game files or personal recordings are included.
