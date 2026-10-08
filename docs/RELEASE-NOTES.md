# KOF13HITBOX v0.1.2 — Autoload + panel-close fix

Download **KOF13HITBOXv0.1-Autoload.zip** below. Close KOF, extract it, and copy `dinput8.dll` plus the `KOF13HITBOX` folder beside `game.exe`. Do not replace a loader belonging to another mod.

Launch KOF normally in windowed/borderless mode, enter offline Training, and press **F8**. **F9** opens settings. Closing the panel with **X now hides it** and keeps F8 working. Use the tray menu's **Exit viewer** to shut down the viewer.

For existing autoload users, replace `KOF13HITBOX/KOF13HITBOX.exe` while KOF and the viewer are closed.

This is an early release. Supported-build validation is enforced; full-roster frame-data accuracy is not claimed. Some readings remain unavailable. Initial autoload/F8 behavior was confirmed in-game; the latest panel-close fix passed automated lifecycle tests and awaits its in-game acceptance check.

Native forwarding, startup/exit, and UI regression tests passed. See the README for the supported game hash, frame-count conventions, limitations, and uninstall instructions.
