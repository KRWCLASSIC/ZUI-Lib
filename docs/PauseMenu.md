# Pause Menu

Namespace: `ZUI.PauseMenu`

API for the in-game pause menu: extra buttons, nested drawers (submenus), a custom right panel, buttons on player cards and the players header.

Source: `src/PauseMenu`.

## Concepts

- The pause menu has two modes, `PauseMenuMode.Vanilla` (stock buttons) and `PauseMenuMode.Tabbed` (mod drawers).
- Buttons registered with `ZUIPauseMenu.RegisterButton` are placed in `ZUIPauseMenu.RootDrawer` (title `MORE`, id `zui_root_pause_more`, paginated).
- A drawer is a page of buttons (`PauseMenuDrawer`). Drawers can be nested. Pressing ESC or the back button pops one drawer.
- A drawer is either paginated (5 buttons per page, Back on slot 6, page row on slot 7) or compact (up to 6 buttons, Back on slot 7). In a compact drawer only the first 6 buttons are shown, a warning is logged for more.
- Every drawer has a lazily created right panel. While that drawer is open, the vanilla connected players list is hidden and replaced by the panel. The vanilla list returns when you go back.
- Ids are case-insensitive. Adding an item with an existing id replaces the old one.

```csharp
using ZUI.PauseMenu;

var drawer = ZUIPauseMenu.RegisterSubmenu("my_mod", "My Mod", paginated: false, breadcrumbHex: "#FFD54F");
drawer.AddButton("my_mod_hello", "Hello", () => Plugin.Log.LogInfo("hello"));
```

## ZUIPauseMenu

`public static class ZUIPauseMenu`

### State

| Member | Type | Description |
|---|---|---|
| `IsOpen` | `bool` | `InGameUI.IsGamePaused`. |
| `Registrations` | `IReadOnlyList<PauseMenuButtonRegistration>` | Buttons registered on the root drawer. |
| `PlayerActions` | `IReadOnlyList<ZUIPlayerAction>` | Registered player card actions. |
| `RootDrawer` | `PauseMenuDrawer` | The `MORE` drawer. |
| `CurrentDrawer` | `PauseMenuDrawer` | Drawer currently shown, `RootDrawer` when the stack is empty, `null` if the tab controller is not installed yet. |
| `PlayerIconResolver` | `Func<PlayerController, Sprite>` | Get/set. Optional callback invoked per connected player row to override their avatar icon (e.g. for custom skin previews or character portraits). |

### Open and close

```csharp
public static void Open();
public static void Close();
```

Open or close the pause menu through `InGameUI.SetPauseStatus`. Both do nothing if `InGameUI` is not available.

### Buttons

```csharp
public static PauseMenuButtonRegistration RegisterButton(string id, string label, Action onClick, int priority = 0);
public static void RegisterButton(PauseMenuButtonRegistration registration);
public static bool UnregisterButton(string id);
public static List<PauseMenuButtonRegistration> GetSortedRegistrations();
public static void RefreshUI();
```

- `RegisterButton(id, label, onClick, priority)` throws `ArgumentNullException` if `id` or `label` is null/empty or `onClick` is null.
- `GetSortedRegistrations` returns visible registrations ordered by `Priority`, then by `Label`.
- `UnregisterButton` returns `true` if something was removed.
- `RefreshUI` rebuilds the tabbed view if it is currently shown. Registration methods call it for you.

### Submenus

```csharp
public static PauseMenuDrawer RegisterSubmenu(
    string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null);
```

Creates a `PauseMenuDrawer` with `Parent = RootDrawer` and registers a button for it on the root drawer. Pressing the button opens the drawer.

### Navigation

```csharp
public static void SetMode(PauseMenuMode mode);
public static void PushDrawer(PauseMenuDrawer drawer);
public static void PopDrawer();
```

- All three do nothing until the tab controller is installed (see Lifecycle).
- `SetMode(Vanilla)` clears the drawer stack.
- `PushDrawer` switches to `Tabbed` if needed and shows the drawer.
- `PopDrawer` goes back one drawer, or back to `Vanilla` if the stack is empty.

To open a nested drawer directly, reset to vanilla and push each level (this is what the example does for its skin changer):

```csharp
ZUIPauseMenu.SetMode(PauseMenuMode.Vanilla);
ZUIPauseMenu.PushDrawer(exampleDrawer);
ZUIPauseMenu.PushDrawer(skinChangerDrawer);
```

### Players header

The title and counter of the connected players panel.

```csharp
public static void SetPlayersHeader(string title = null, string counter = null, string titleHex = null, string counterHex = null);
public static void ResetPlayersHeader();
public static void ApplyPlayersHeader();
```

- `null` for `title` or `counter` keeps the vanilla value.
- `titleHex` / `counterHex` color the text, for example `"#81C784"`.
- `ResetPlayersHeader` restores the vanilla `CONNECTED PLAYERS` title and the `(n/4)` counter.
- `ApplyPlayersHeader` re-applies the current values. It runs automatically when the menu opens.

```csharp
ZUIPauseMenu.SetPlayersHeader("SURVIVORS", "[CO-OP]", titleHex: "#81C784", counterHex: "#4FC3F7");
ZUIPauseMenu.ResetPlayersHeader();
```

### Player card actions

```csharp
public static ZUIPlayerAction AddPlayerActionButton(
    string id,
    Sprite icon,
    string tooltip,
    Action<PlayerController> onClick,
    string hoverHex = null,
    string normalHex = null,
    bool alwaysVisible = false,
    Func<PlayerController, bool> visibilityPredicate = null);

public static bool RemovePlayerActionButton(string id);
public static event Action OnPlayerCardsRefreshed;
public static void RefreshPlayerCards();
```

| Parameter | Description |
|---|---|
| `id` | Unique id. Required. |
| `icon` | Button sprite (for example from `ZUIImage`). |
| `tooltip` | Tooltip or label text. |
| `onClick` | Called with the player of the clicked card. Required. |
| `hoverHex` | Hover tint, for example `"#4FC3F7"`. |
| `normalHex` | Base tint. Default is white. |
| `alwaysVisible` | `true` shows the button all the time, `false` only on hover of the card. |
| `visibilityPredicate` | Called per player. Return `false` to hide the button for that player. |

- Buttons appear next to the vanilla Kick button.
- `RefreshPlayerCards` rebuilds the buttons on all `EscapePlayerUI` cards and then raises `OnPlayerCardsRefreshed`. Add/remove calls and opening the menu call it already.

```csharp
ZUIPauseMenu.AddPlayerActionButton(
    id: "my_tp",
    icon: ZUIImage.LoadEmbedded("teleport.png"),
    tooltip: "Teleport to Player",
    onClick: target => { /* ... */ },
    hoverHex: "#4FC3F7",
    visibilityPredicate: p => p == null || !p.IsMine);
```

### Lifecycle

These are called by the library, you normally do not need them.

```csharp
public static void Initialize(MonoBehaviour host);
public static IEnumerator InstallWhenReadyRoutine();
public static bool TryInstall();
public static void OnPauseStatusChanged(bool state);
public static void Shutdown();
```

- `Initialize` hooks scene loading and starts installing the tab controller on `host`. Calling it twice has no effect.
- `TryInstall` returns `false` in `SplashScreen` / `MainMenu` or when the pause menu buttons are not found yet. `InstallWhenReadyRoutine` retries it for up to 120 frames.
- `Shutdown` unhooks scene loading and clears `Registrations`. It does not clear player actions.

## PauseMenuMode

```csharp
public enum PauseMenuMode { Vanilla, Tabbed }
```

## PauseMenuButtonRegistration

`public class PauseMenuButtonRegistration`

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Get/set. |
| `Label` | `string` | Get/set. |
| `OnClick` | `Action` | Get/set. |
| `Priority` | `int` | Get/set. Default `0`. Lower values come first. |
| `Interactable` | `bool` | Get/set. Default `true`. |
| `VisibilityCondition` | `Func<bool>` | Get/set. `null` means always visible. |
| `IsVisible` | `bool` | `true` if there is no condition or it returns `true`. |

Constructor: `PauseMenuButtonRegistration(string id, string label, Action onClick, int priority = 0)`. Throws `ArgumentNullException` for a null `id`, `label` or `onClick`.

```csharp
var reg = ZUIPauseMenu.RegisterButton("my_btn", "My Button", () => { }, priority: 10);
reg.VisibilityCondition = () => PlayerController.PlayerClient.Value != null;
```

## PauseMenuDrawer

`public class PauseMenuDrawer`

Constructor: `PauseMenuDrawer(string id, string title, bool paginated = true, string breadcrumbHex = null)`. Throws `ArgumentNullException` for a null `id` or `title`. Usually you get drawers from `RegisterSubmenu` or `AddSubmenu`.

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Read-only. |
| `Title` | `string` | Get/set. |
| `Paginated` | `bool` | Get/set. Default `true`. |
| `BreadcrumbHex` | `string` | Get/set. Color of the breadcrumb bar. |
| `Parent` | `PauseMenuDrawer` | Get only (internal set). Parent drawer. |
| `Buttons` | `IReadOnlyList<PauseMenuButtonRegistration>` | Buttons in insertion order. |
| `RightPanel` | `PauseMenuRightPanel` | Created on first access. |
| `HasRightPanel` | `bool` | `true` if a right panel exists and has content. |

```csharp
public PauseMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0);
public void AddButton(PauseMenuButtonRegistration registration);
public PauseMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null);
public bool RemoveButton(string id);
public List<PauseMenuButtonRegistration> GetSortedButtons();
```

- `AddButton` in a drawer keeps insertion order unless priorities differ (stable sort by `Priority`).
- `AddSubmenu` creates a child drawer (with this drawer as `Parent`) and a button that opens it. It works for drawers of a `ZUIScreen` as well.
- `GetSortedButtons` returns the visible buttons sorted by `Priority`.

## ZUIPlayerAction

`public class ZUIPlayerAction`

Returned by `AddPlayerActionButton`. The properties can be changed after creation.

| Member | Type |
|---|---|
| `Id` | `string` (read-only) |
| `Icon` | `Sprite` |
| `Tooltip` | `string` |
| `OnClick` | `Action<PlayerController>` |
| `HoverColor` | `Color?` |
| `NormalColor` | `Color?` |
| `AlwaysVisible` | `bool` (default `false`) |
| `VisibilityPredicate` | `Func<PlayerController, bool>` |

Constructor:

```csharp
public ZUIPlayerAction(string id, Sprite icon, string tooltip, Action<PlayerController> onClick,
    Color? hoverColor = null, Color? normalColor = null, bool alwaysVisible = false,
    Func<PlayerController, bool> visibilityPredicate = null);
```

## ZUIPlayerCardExtension

`public class ZUIPlayerCardExtension : MonoBehaviour`

Component the library attaches to each `EscapePlayerUI` card to draw the registered player actions. Normally you only use `ZUIPauseMenu.RefreshPlayerCards()`.

| Member | Description |
|---|---|
| `const int MAX_VISIBLE_SLOTS = 3` | Maximum number of visible action slots. |
| `PlayerController TargetPlayer { get; }` | Player of the card. |
| `static ZUIPlayerCardExtension Attach(EscapePlayerUI playerUI, PlayerController player)` | Adds the component to a card. |
| `void Initialize(PlayerController player)` | Sets up the component for a player. |
| `void RebuildButtons()` | Rebuilds the action buttons. |
| `void OnHoverChanged(bool isHovered)` | Hover state callback. |

## Right panel

Every `PauseMenuDrawer` has a `RightPanel`. It is also used by `ZUIScreen` (see [Screens.md](Screens.md)). Width is 989 px. Elements are cards or buttons, or you mount your own GameObject.

All `Set*` / `Add*` methods update the panel in place if it is currently displayed.

### PauseMenuRightPanel

`public class PauseMenuRightPanel`

Constructor: `PauseMenuRightPanel(PauseMenuDrawer drawer)`. The default header title is the drawer title in upper case (`"CUSTOM PANEL"` if the title is null) with an empty counter.

| Member | Type | Description |
|---|---|---|
| `Drawer` | `PauseMenuDrawer` | Owner. |
| `HeaderTitle` | `string` | Get only. |
| `HeaderCounter` | `string` | Get only. |
| `HeaderTitleHex` | `string` | Get only. |
| `HeaderCounterHex` | `string` | Get only. |
| `DefaultItemHeight` | `float?` | Get only. Set by `SetItemHeight`. |
| `CustomContentRoot` | `GameObject` | Get only. Set by `SetCustomContent`. |
| `Elements` | `IReadOnlyList<PauseMenuRightElement>` | Cards and buttons in order. |
| `HasContent` | `bool` | `true` if there is a header title/counter, any element or custom content. |

```csharp
public PauseMenuRightPanel SetHeader(string title, string counter = "", string titleHex = null, string counterHex = null);
public PauseMenuRightPanel SetTitle(string title, string counter = "", string titleHex = null, string counterHex = null); // alias of SetHeader
public PauseMenuRightPanel SetItemHeight(float height);

public PauseMenuRightCard AddCard(string id, string title, string subtitle = null, Sprite icon = null,
    Action onClick = null, float? height = null, string titleHex = null, string subtitleHex = null);
public PauseMenuRightCard AddEntry(/* same parameters as AddCard */); // alias of AddCard

public PauseMenuRightButton AddButton(string id, string label, Action onClick,
    float? height = null, string textHex = null, string backgroundHex = null);

public bool RemoveElement(string id);
public PauseMenuRightPanel ClearElements();
public PauseMenuRightPanel SetCustomContent(GameObject customRoot);
```

- `SetItemHeight` stores `max(24, height)`. It is the default for elements added afterwards without an explicit `height`.
- Height resolution when adding: `height ?? DefaultItemHeight ?? 72` for cards, `... ?? 56` for buttons.
- `AddCard` throws `ArgumentNullException` for an empty `id`. `AddButton` also for an empty `label` or null `onClick`.
- `SetCustomContent(null)` clears the custom content.

```csharp
var panel = drawer.RightPanel;
panel.SetHeader("SKIN CHANGER", "(4 SKINS)", titleHex: "#FFD54F", counterHex: "#4FC3F7");
panel.SetItemHeight(60f);
var card = panel.AddCard("skin_red", "Red", "Click to equip", onClick: () => { }, titleHex: "#EF5350");
panel.AddButton("skin_reset", "Reset", () => { }, height: 44f, textHex: "#B0BEC5");
```

### PauseMenuRightElement

`public abstract class PauseMenuRightElement`

Base class of `PauseMenuRightCard` and `PauseMenuRightButton`.

| Member | Type |
|---|---|
| `Id` | `string` (read-only) |
| `Height` | `float` (get/set) |

### PauseMenuRightCard

`public class PauseMenuRightCard : PauseMenuRightElement`

Card styled like a player bar with a 3-zone layout:
* **Leading Zone**: Left icon and optional left action buttons (`AddLeftButton`).
* **Center Zone**: Title and subtitle with automatic line-height calibration and ellipsis truncation.
* **Trailing Zone**: Right-side metadata text (`SetRightText`) and action buttons (`AddRightButton`).
* **Selection & Tint**: Active item selection (`SetSelected`) and custom background coloring (`SetBackgroundColor`).
* **Escape Hatch**: Mount custom UI hierarchies directly into the row (`AttachCustomContent`).

`public const float DefaultCardHeight = 72f;`

Constructor: `PauseMenuRightCard(PauseMenuRightPanel owner, string id, string title, string subtitle = null, Sprite icon = null, Action onClick = null, float height = DefaultCardHeight, string titleHex = null, string subtitleHex = null)`. Use `PauseMenuRightPanel.AddCard` instead of calling it.

| Member | Type | Description |
|---|---|---|
| `Title` | `string` | Primary card title. |
| `Subtitle` | `string` | Secondary subtitle text. |
| `TitleHex` | `string` | Title color hex code. |
| `SubtitleHex` | `string` | Subtitle color hex code. |
| `RightText` | `string` | Metadata text on the right side (e.g. artist attribution). |
| `RightTextHex` | `string` | RightText color hex code. |
| `Icon` | `Sprite` | Left-side thumbnail/icon. |
| `OnClick` | `Action` | Full-row click callback. |
| `IsSelected` | `bool` | Whether the card is visually highlighted as active/equipped. |
| `SelectedColor` | `Color?` | Custom tint applied when `IsSelected` is true (default dark slate). |
| `BackgroundColor` | `Color?` | Explicit card background color override. |
| `CustomContentRoot` | `GameObject` | Custom UI hierarchy mounted into the card row. |
| `CustomContentOnly` | `bool` | If true, hides standard card elements to give full row ownership to `CustomContentRoot`. |
| `LeftButtons` | `IReadOnlyList<PauseMenuRightCardAction>` | Action buttons placed in the Leading zone. |
| `RightButtons` | `IReadOnlyList<PauseMenuRightCardAction>` | Action buttons placed in the Trailing zone. |

```csharp
public PauseMenuRightCard SetHeight(float height);   // max(24, height)
public PauseMenuRightCard SetText(string title, string subtitle = null);
public PauseMenuRightCard SetIcon(Sprite icon);
public PauseMenuRightCard SetRightText(string text, string hexColor = null);
public PauseMenuRightCard SetSelected(bool selected, Color? customColor = null);
public PauseMenuRightCard SetSelected(bool selected, string hexColor);
public PauseMenuRightCard SetBackgroundColor(Color? color);
public PauseMenuRightCard SetBackgroundColor(string hexColor);

public PauseMenuRightCard AddLeftButton(string id, Sprite icon, string tooltip, Action onClick,
    string hoverHex = "#4FC3F7", string normalHex = null, bool alwaysVisible = true);
public PauseMenuRightCard SetLeftButtonColors(string id, string normalHex, string hoverHex = null);
public PauseMenuRightCard SetLeftButtonIcon(string id, Sprite icon);

public PauseMenuRightCard AddRightButton(string id, Sprite icon, string tooltip, Action onClick,
    string hoverHex = "#4FC3F7", string normalHex = null, bool alwaysVisible = true);
public PauseMenuRightCard SetRightButtonColors(string id, string normalHex, string hoverHex = null);
public PauseMenuRightCard SetRightButtonIcon(string id, Sprite icon);

public PauseMenuRightCard AttachCustomContent(GameObject customRoot, bool replaceStandardContent = false);
```

- `SetText`, `SetIcon`, `SetHeight`, `SetRightText`, `SetSelected`, `SetBackgroundColor`, `SetLeftButtonColors`, `SetLeftButtonIcon`, `SetRightButtonColors`, and `SetRightButtonIcon` update the card without rebuilding the panel.
- `AddLeftButton` and `AddRightButton` add an action button to the respective side. Passing `alwaysVisible: false` hides the button until the card is hovered.
- `SetSelected(true)` highlights the card with a subtle dark accent tint (or custom color) without needing `[EQUIPPED]` bracket tags in the subtitle.
- `AttachCustomContent` mounts a developer-supplied GameObject into the row as an escape hatch for custom widgets or custom layouts.

```csharp
var card = panel.AddCard("skin_red", "Red", "Click to equip", icon: skinIcon, onClick: () => Equip("Red"));
card.SetRightText("Artist: Erne", "#AAAAAA");
card.AddRightButton("skin_info", infoIcon, "Skin Info", () => ShowSkinDetails("Red"));

// When equipped:
card.SetSelected(true);
card.SetText("Red", "Equipped");
```

### PauseMenuRightCardAction

`public class PauseMenuRightCardAction`

Icon button on the right side of a card. Created by `AddRightButton`.

| Member | Type |
|---|---|
| `Id` | `string` (read-only) |
| `Icon` | `Sprite` |
| `Tooltip` | `string` |
| `OnClick` | `Action` |
| `HoverColor` | `Color?` |
| `NormalColor` | `Color?` |
| `AlwaysVisible` | `bool` |

Constructor: `PauseMenuRightCardAction(string id, Sprite icon, string tooltip, Action onClick, Color? hoverColor = null, Color? normalColor = null, bool alwaysVisible = true)`.

### PauseMenuRightButton

`public class PauseMenuRightButton : PauseMenuRightElement`

Button styled like the native pause menu buttons.

`public const float DefaultButtonHeight = 56f;`

Constructor: `PauseMenuRightButton(PauseMenuRightPanel owner, string id, string label, Action onClick, float height = DefaultButtonHeight, string textHex = null, string backgroundHex = null)`. Use `PauseMenuRightPanel.AddButton` instead.

| Member | Type |
|---|---|
| `Label` | `string` |
| `OnClick` | `Action` |
| `TextHex` | `string` |
| `BackgroundHex` | `string` |
| `Interactable` | `bool` (default `true`) |

```csharp
public PauseMenuRightButton SetHeight(float height);        // max(24, height)
public PauseMenuRightButton SetLabel(string label);
public PauseMenuRightButton SetInteractable(bool interactable);
```

### PauseMenuRightPanelController

`public static void SyncWithCurrentDrawer()` and `public static void RestoreVanillaPlayersList()` are public on `PauseMenuRightPanelController`. The library calls them when the drawer changes, you normally do not.
