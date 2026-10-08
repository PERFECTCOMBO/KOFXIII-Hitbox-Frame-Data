# KOFXIII Hitbox Viewer / Frame Data

A Windows 11 hitbox, hurtbox, and experimental frame-data viewer for **THE KING OF FIGHTERS XIII GLOBAL MATCH**.

The autoload edition starts with the game. Press **F8** in offline Training to attach and show the overlay. You do not need to launch a separate application manually.

**Early release:** selected moves and characters have been tested. This is not a complete or verified full-roster frame-data database. Unsupported readings display `--`.

## Download and install

Download **KOF13HITBOXv0.1-Autoload.zip** from [Releases](https://github.com/PERFECTCOMBO/KOFXIII-Hitbox-Frame-Data/releases). GitHub's automatically generated source archives are for developers, not installation.

1. Close KOF and any older hitbox viewer normally.
2. Extract the release ZIP.
3. Copy **dinput8.dll** and the **KOF13HITBOX** folder into the Global Match folder containing `game.exe`. In Steam, use **Manage → Browse local files** to find it.
4. Launch KOF normally. Use **windowed or borderless** mode and enter **offline Training**.
5. Press **F8** with the game focused.

```text
THE KING OF FIGHTERS XIII GLOBAL MATCH/
├── game.exe                  # Your existing game
├── dinput8.dll               # From the release ZIP
└── KOF13HITBOX/              # Copy the entire folder
    ├── KOF13HITBOX.exe
    ├── Kof13ImGui.dll
    ├── reference-data.xml
    ├── special-moves.xml
    └── licenses/
```

If your game folder already has `dinput8.dll` from another mod, **do not overwrite it**. This version does not support chaining multiple DirectInput loaders. Do not install these files into Windows system folders.

## Controls

| Control | Result |
| --- | --- |
| F8 | First press attaches and shows the overlay; later presses hide/show it |
| F9 | Opens the settings/viewer panel while the game is focused |
| Panel X / Alt+F4 | Hides the panel; the viewer and F8 remain available |
| Return to game | Hides the panel |
| Tray menu → Exit viewer | Detaches and exits the viewer for this game session |
| Close KOF | Automatically exits its background viewer |

After selecting **Exit viewer**, restart KOF to start it again.

## What is included

- Hitbox and hurtbox overlays using captured native game data.
- Compact frame meter, with Startup, Active, Recovery, OnHit, and OnGuard fields.
- Dear ImGui settings, history inspection, and export tools.
- A game-folder autoload DLL and background viewer.
- Reference tables and assisted test queues imported from `KOFXIII BOT Framedata.xlsx`. Their applicability to Global Match is not fully verified; reference values are not a substitute for captured results.

## Limitations

- Use offline Training; the tool does not automatically verify that the game is offline.
- Only the supported `game.exe` build below can attach. Updated or different builds are rejected.
- Exclusive fullscreen is not supported by the external overlay.
- Startup currently counts native ticks **before** the first observed attack region. This differs from the inclusive startup convention used by some frame-data tables.
- OnHit and OnGuard are experimental observed return gaps. Recovery is available for validated transitions; unsupported transitions remain `--`.
- No full-roster accuracy claim. Some jumping attacks, projectiles, transitions, and special cases remain incomplete.
- Input recording and training-dummy playback are not included.

Supported game executable SHA-256:

```text
E7718F5C553DE852135DDEF5833A7BB6EAFD05508D1206AC6F3CE7767B474835
```

## How autoload works

The 32-bit `dinput8.dll` forwards DirectInput calls to Windows and starts the 64-bit viewer silently. The viewer validates the game executable and session identity, then waits for F8 before installing its capture hook. Rendering remains in the background viewer rather than inside the game's renderer. There is no startup task, service, or registry installation.

Settings and diagnostics for autoload sessions are stored in:

```text
%LOCALAPPDATA%\KOF13HITBOX\v0.1
```

## Troubleshooting and removal

If F8 does nothing, restart KOF after installation, close older viewers, check the directory layout, and focus the game. The tray menu provides an alternative if another program reserves F8. Inspect `debug.log` in the location above for attachment errors.

To uninstall, close KOF and the viewer, then remove **only this package's** `dinput8.dll` and `KOF13HITBOX` folder. To disable it temporarily, create an empty `KOF13HITBOX/disabled.txt` file and restart KOF.

## Build and tests

Requirements: 64-bit Windows, Visual Studio 2022 C++ Build Tools with x86/x64 compilers and Windows SDK, and the .NET Framework 4.x compiler.

```powershell
.\build.ps1
.\test.ps1
.\KOF13HITBOX.exe --self-test
```

`build.ps1` builds the x86 loader, x64 native ImGui renderer, and x64 managed viewer. `test.ps1` exercises native DirectInput forwarding and autoload lifetime behavior with test processes; it does not drive or patch the game. The viewer's self-test checks the existing frame model and ImGui rendering/UI behavior. These tests briefly open test windows.

Initial autoload and F8 operation were confirmed in-game by the maintainer. The latest X-to-hide update passed automated lifecycle tests; its in-game acceptance check is pending. See [validation reports](docs/verification).

## Credits and licensing

Inspired by [odabugs/kof13-hitboxes](https://github.com/odabugs/kof13-hitboxes). Dear ImGui is included under its upstream license. Third-party notices are preserved in [licenses](licenses) and alongside the vendored ImGui source.

No project-wide open-source license has been selected yet. The third-party licenses apply to their respective components. This project is unofficial and is not affiliated with SNK. No game executable, game assets, or personal recordings are included.
