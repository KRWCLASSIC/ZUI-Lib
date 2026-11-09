# Screens

Namespace: `ZUI.Screens`

`ZUIScreen` is a standalone full-screen split interface: a navigation column on the left (buttons, nested drawers) and a 989 px content column on the right (cards, buttons or your own GameObject). It is built from a clone of the vanilla `pausePanel` and does not use the vanilla pause state.

It reuses `PauseMenuDrawer` for the left side and `PauseMenuRightPanel` for the right side, see [PauseMenu.md](PauseMenu.md).

Source: `src/Screens`.

```csharp
using ZUI.Screens;

var screen = ZUIScreen.Register("my_terminal", "MAP TERMINAL", paginated: false, titleHex: "#4FC3F7");
screen.SetRightHeader("SECTOR OVERVIEW", "[ONLINE]", titleHex: "#4FC3F7", counterHex: "#81C784");

screen.AddCard("sec_alpha", "Sector Alpha", "Power: 100%", onClick: () => { });
screen.AddRightButton("ping", "Ping Beacons", () => { }, height: 52f, textHex: "#81C784");
screen.AddButton("left_alpha", "Sector Alpha", () => { });

var diag = screen.AddSubmenu("diag", "Diagnostics", paginated: false, breadcrumbHex: "#81C784");
diag.RightPanel.SetHeader("SYSTEM DIAGNOSTICS", "[3 LOGS]");
diag.AddButton("scan", "Run Full Scan", () => { });

screen.Open();
```

## Behavior

- Only one screen is open at a time. Opening a screen closes the previously open one, closes an open `ZUIPanel`, and closes the vanilla pause menu if it is open.
- Opening a screen sets `InGameUI.IsGamePaused` to true, unlocks and shows the cursor, so player input is frozen. Closing it recalculates the cursor lock through `InGameUI.RecalculateCursorLock()`.
- ESC pops one nested drawer. At the root drawer ESC closes the screen. If a `ZUIPanel` or a `ZUIPopup` handled ESC in the same frame, the screen ignores it.
- The bottom return button shows `CloseButtonLabel` (default `"CLOSE"`) at the root drawer.
- Loading a scene resets `CurrentOpenScreen` to `null` and destroys the screen views. They are rebuilt on the next `Open`.
- Opening fails with an error in the log if `InGameUICanvas` or the vanilla `pausePanel` is not found.
- Opening a screen that is already open refreshes it.

## ZUIScreen

`public class ZUIScreen`

### Static members

| Member | Type | Description |
|---|---|---|
| `Registry` | `IReadOnlyDictionary<string, ZUIScreen>` | Registered screens, id comparison is case-insensitive. |
| `CurrentOpenScreen` | `ZUIScreen` | The open screen or `null`. |
| `IsAnyScreenOpen` | `bool` | `true` if a screen is open. |

```csharp
public static void Initialize(MonoBehaviour host);
public static ZUIScreen Register(string id, string title, bool paginated = true, string titleHex = null);
public static ZUIScreen Get(string id);
public static bool Unregister(string id);
public static void Open(string id);
public static void Toggle(string id);
public static void CloseActive();
public static void Shutdown();
```

- `Initialize(host)` sets up the screen controller (a component on `host`'s GameObject, or a new `ZUIScreenController` object, kept across scenes). The library calls it, you normally do not.
- `Register` returns the existing screen if the id is already registered, after updating its title, `titleHex` and paginated flag. Throws `ArgumentNullException` for an empty id.
- `Get` returns `null` for an unknown or empty id.
- `Unregister` closes the screen if open, destroys its view and removes it. Returns `true` if it was registered.
- `Open(id)` / `Toggle(id)` log a warning and do nothing if the id is not registered.
- `CloseActive` closes the open screen, if any.
- `Shutdown` closes the active screen, destroys all views and clears the registry.

### Constructor

```csharp
public ZUIScreen(string id, string title, bool paginated = true, string titleHex = null);
```

Prefer `Register`, which also adds the screen to `Registry`. The right panel header defaults to the upper-cased title.

### Instance properties

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Read-only. |
| `Title` | `string` | Get/set. Left column header text. |
| `TitleHex` | `string` | Get/set. Header color. |
| `ShowLeftHeader` | `bool` | Get/set. Show the header above the left buttons. Default `true`. |
| `CloseButtonLabel` | `string` | Get/set. Default `"CLOSE"`. |
| `RootDrawer` | `PauseMenuDrawer` | Root of the left navigation. |
| `RightPanel` | `PauseMenuRightPanel` | Same as `RootDrawer.RightPanel`. Shown at the root and in drawers without their own content. |
| `OnOpened` | `Action` | Get/set. Invoked after the screen is shown. |
| `OnClosed` | `Action` | Get/set. Invoked after the screen is hidden. |
| `IsOpen` | `bool` | `true` if this screen is the open one. |

`OnOpened` and `OnClosed` are plain `Action` properties, not events. Assigning replaces the previous handler, use `+=` to add one.

```csharp
screen.OnOpened += () => Plugin.Log.LogInfo("opened");
screen.OnClosed += () => Plugin.Log.LogInfo("closed");
```

### Instance methods

```csharp
public void Open();
public void Close();
public void Toggle();
public void PushDrawer(PauseMenuDrawer drawer);
public void PopDrawer();
public ZUIScreen SetTitle(string title, string titleHex = null);
```

- `Close` does nothing if this screen is not the open one.
- `PushDrawer` sets the drawer's owning screen and shows it. It does nothing if the screen is not open. `PopDrawer` goes back one drawer.
- `SetTitle` keeps the current `TitleHex` when `titleHex` is `null`.

### Left column

These forward to `RootDrawer`.

```csharp
public PauseMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0);
public PauseMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null);
public bool RemoveButton(string id);
```

A drawer returned by `AddSubmenu` has its own `RightPanel`. Add buttons to it with `drawer.AddButton(...)`.

### Right column

These forward to `RightPanel`.

```csharp
public ZUIScreen SetRightHeader(string title, string counter = "", string titleHex = null, string counterHex = null);
public ZUIScreen SetContent(GameObject customRoot);

public PauseMenuRightCard AddCard(string id, string title, string subtitle = null, Sprite icon = null,
    Action onClick = null, float? height = null, string titleHex = null, string subtitleHex = null);
public PauseMenuRightCard AddEntry(/* same parameters as AddCard */); // alias of AddCard

public PauseMenuRightButton AddRightButton(string id, string label, Action onClick,
    float? height = null, string textHex = null, string backgroundHex = null);
```

- `SetContent` mounts a GameObject below the header (`PauseMenuRightPanel.SetCustomContent`).
- Cards and right buttons are the same types as in the pause menu right panel (`PauseMenuRightCard`, `PauseMenuRightButton`), including `SetText`, `SetIcon`, `AddRightButton`, `SetRightButtonColors`, `SetHeight`. See [PauseMenu.md](PauseMenu.md).
- Use `screen.RightPanel` for the rest of the panel API (`SetItemHeight`, `RemoveElement`, `ClearElements`).

## ZUIScreenController

`public class ZUIScreenController : MonoBehaviour`

Runtime manager behind `ZUIScreen`. Prefer the `ZUIScreen` API.

| Member | Description |
|---|---|
| `static ZUIScreenController Instance { get; }` | Active controller. |
| `static ZUIScreen CurrentOpenScreen { get; }` | Open screen or `null`. |
| `static int LastEscConsumedFrame { get; }` | Frame in which a screen last consumed ESC. |
| `static void EnsureInitialized(MonoBehaviour host)` | Creates the controller if missing. |
| `bool HandleEscape()` | ESC handling described above. Returns `true` if a screen is open. |
| `static void OpenScreen(ZUIScreen screen)` | Same as `screen.Open()`. Throws `ArgumentNullException` for `null`. |
| `static void CloseScreen(ZUIScreen screen)` | Hides the screen, recalculates the cursor lock and invokes `OnClosed`. Ignores `null`. |
