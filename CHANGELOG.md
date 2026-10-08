# Changelog

## v0.1.2 — Autoload panel-close fix

- Closing the settings panel with X or Alt+F4 hides it while preserving capture, F8, and F9.
- Explicit tray-menu exit still detaches and closes the viewer.
- Game shutdown still closes its background viewer automatically.
- Lifecycle tests cover repeated close/reopen, retained polling/provider, overlay toggles, and explicit exit.

## v0.1.1 — Autoload edition

- Added a 32-bit DirectInput proxy that launches the viewer with the game.
- Added hidden startup, F8 attachment/toggle, F9 settings, and a tray menu.
- Added exact game-session validation and automatic exit when the game closes.
- Moved autoload settings and diagnostics to LocalAppData.

## v0.1 — Shareable viewer

- Packaged the R14 hitbox and experimental frame-data viewer.
- Added installation-independent detection of the supported game build.
- Preserved third-party notices and omitted game files and personal recordings.
