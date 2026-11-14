# ZUI-Lib

UI library for Zort mods.

## Features

- Custom submenus and buttons in the Main Menu and Pause Menu
- Standalone split screens without opening the vanilla pause menu
- Custom right-panel cards with 3-zone layouts (leading icons, metadata, selection, action buttons)
- Action buttons on player cards with volume slider auto-collapse
- Mod settings modals styled like native settings
- Custom popups, subtitles, and top banner notifications

## Setup

Requires [.NET SDK](https://dotnet.microsoft.com/download) and [UMK](https://www.nuget.org/packages/krwclassic.umk).

Install UMK:
```cmd
dotnet tool install -g krwclassic.umk
```

Setup project on a fresh clone:
```cmd
git clone https://github.com/KRWCLASSIC/ZUI-Lib.git
cd ZUI-Lib
umk setup
```

## Running

- `umk run` - builds clean library and launches Zort with the mod
- `umk run-example` - builds with showcase examples and launches Zort
- `umk build` - compiles clean library and copies DLL to BepInEx plugins
- `umk build-example` - compiles with showcase examples and copies DLL to plugins
- `umk pack` - builds clean release and packages Thunderstore zip into `dist/`
- `umk modlessrun` - launches clean vanilla Zort
- `umk compile` - builds the project
- `umk clean` - cleans build files
- `umk list` - shows all available scripts and variables

## Documentation

API reference for each subsystem:
- [Core](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Core.md) - Plugin info, module discovery, `ZUIAPI`, `ZUIImage`, `ZUIDefaultIcons`, `ZUICrashHandler`
- [Main Menu](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/MainMenu.md) - Custom buttons, submenus, pagination, and breadcrumbs
- [Pause Menu](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/PauseMenu.md) - Pause drawers, player card actions, right panels and cards
- [Screens](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Screens.md) - Standalone custom split screens (`ZUIScreen`)
- [Panels](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Panels.md) - Modal settings dialogs (`ZUIPanel`)
- [Settings](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Settings.md) - Native InGameUI settings injection (`ZUISettings`)
- [Popups](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Popups.md) - Dialogs, alerts, confirmations, and ESC handling (`ZUIPopup`)
- [Notifications](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Notifications.md) - Top banner alerts (`ZUIInfoText`)
- [Dialogue](https://github.com/KRWCLASSIC/ZUI-Lib/blob/main/docs/Dialogue.md) - Subtitles (`ZUISubtitle`) and sequences (`ZUIDialogueSequence`)

