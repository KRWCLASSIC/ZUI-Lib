# Main Menu

Namespace: `ZUI.MainMenu`

ZUI adds a `MORE` button to the Zort main menu. It switches the button stack from vanilla mode to tabbed mode, where mod buttons are listed on pages. Buttons can open drawers (submenus), which can be nested to any depth.

Layout of a drawer:

- Paginated drawer: 3 mod buttons per page, Back in slot 3, page row (previous / indicator / next) in slot 4.
- Compact drawer (`paginated: false`): up to 4 mod buttons in slots 0-3, Back in slot 4. Buttons beyond the 4th are not shown (a warning is logged).
- A breadcrumb bar below the stack shows the drawer path. Long paths are shortened with `...`.

## ZUIMainMenu

Static class, the entry point of the API.

### Members

| Member | Description |
|---|---|
| `IReadOnlyList<MainMenuButtonRegistration> Registrations` | Buttons registered directly on the root drawer. |
| `MainMenuDrawer RootDrawer` | The root drawer (id `zui_root_more`, title `MORE`, paginated). |
| `MainMenuDrawer CurrentDrawer` | Drawer currently shown, or `null` if the menu is not installed yet. |

### Registering Buttons & Submenus

```csharp
// Register a button on the root MORE drawer
public static MainMenuButtonRegistration RegisterButton(string id, string label, Action onClick, int priority = 0);

// Register a prebuilt button registration
public static void RegisterButton(MainMenuButtonRegistration registration);

// Register a button that opens a child submenu drawer
public static MainMenuDrawer RegisterSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null);

// Unregister a button by id (case-insensitive)
public static bool UnregisterButton(string id);
```

- `RegisterButton`: adds a button to the root drawer. An existing registration with the same ID is replaced.
- `RegisterSubmenu`: creates and returns a child drawer, adding an opener button on the root drawer. `breadcrumbHex` sets the breadcrumb color.
- `UnregisterButton`: returns `true` if removed, `false` if not found.

### Navigation & Mode

```csharp
// Switch between vanilla buttons and tabbed mod drawer mode
public static void SetMode(MainMenuMode mode);

// Push a child drawer onto the active navigation stack
public static void PushDrawer(MainMenuDrawer drawer);

// Step back one drawer level (or return to vanilla menu if at root)
public static void PopDrawer();
```

- `SetMode`: transitions the main menu button container between vanilla buttons and the mod tab stack.
- `PushDrawer`: opens the specified drawer, switching to tabbed mode if currently in vanilla mode.
- `PopDrawer`: navigates back to the parent drawer or exits to the vanilla menu.

### State & Refresh

```csharp
// Returns visible root buttons sorted by Priority, then Label
public static List<MainMenuButtonRegistration> GetSortedRegistrations();

// Rebuilds the tabbed view layout if currently visible
public static void RefreshUI();
```

### Lifecycle (Internal)

```csharp
public static void Initialize(MonoBehaviour host);
public static bool TryInstall();
public static void Shutdown();
```

Managed automatically by ZUI-Lib on startup and scene transitions.

### Priority

- Root buttons are sorted by `Priority` ascending (lower comes first), ties by label.
- Drawer buttons are sorted by `Priority` ascending, ties keep insertion order.

## MainMenuMode

```csharp
public enum MainMenuMode
{
    Vanilla,
    Tabbed
}
```

## MainMenuButtonRegistration

A single button entry.

```csharp
public MainMenuButtonRegistration(string id, string label, Action onClick, int priority = 0)
```

Null `id`, `label` or `onClick` throws `ArgumentNullException`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Id` | `string` | ctor | Unique id within its drawer, case-insensitive. |
| `Label` | `string` | ctor | Button text. |
| `OnClick` | `Action` | ctor | Click handler. |
| `Priority` | `int` | `0` | Sort order, lower first. |
| `Interactable` | `bool` | `true` | Whether the button can be clicked. |
| `VisibilityCondition` | `Func<bool>` | `null` | If set, the button is only shown while it returns `true`. |
| `IsVisible` | `bool` (read-only) | | `true` if `VisibilityCondition` is `null` or returns `true`. |

`VisibilityCondition` is evaluated when the view is rebuilt, so call `ZUIMainMenu.RefreshUI()` after the condition changes.

## MainMenuDrawer

A submenu page.

```csharp
public MainMenuDrawer(string id, string title, bool paginated = true, string breadcrumbHex = null)
```

Null `id` or `title` throws `ArgumentNullException`.

| Member | Description |
|---|---|
| `string Id { get; }` | Drawer id. |
| `string Title { get; set; }` | Title, used in the breadcrumb. |
| `bool Paginated { get; set; }` | `true` for the paginated layout, `false` for compact. |
| `string BreadcrumbHex { get; set; }` | Breadcrumb color, e.g. `"#FFD54F"`. |
| `MainMenuDrawer Parent { get; }` | Parent drawer (set when created through `RegisterSubmenu` / `AddSubmenu`). |
| `IReadOnlyList<MainMenuButtonRegistration> Buttons { get; }` | All buttons in insertion order. |

### Methods

```csharp
// Add a button to this drawer
public MainMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0);
public void AddButton(MainMenuButtonRegistration registration);

// Add a nested child submenu drawer (nesting depth is unlimited)
public MainMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null);

// Remove a button by id
public bool RemoveButton(string id);

// Get visible buttons sorted by Priority (stable sort preserving insertion order)
public List<MainMenuButtonRegistration> GetSortedButtons();
```

## Example

```csharp
using ZUI.MainMenu;
using ZUI.Popups;

var drawer = ZUIMainMenu.RegisterSubmenu(
    id: "my_mod_drawer",
    label: "My Mod",
    paginated: true,
    breadcrumbHex: "#FFD54F");

drawer.AddButton("my_mod_hello", "Hello", () =>
{
    ZUIPopup.ShowInfo("Hello", "Clicked from My Mod.", "OK");
});

// Compact nested drawer (max 4 buttons)
var sub = drawer.AddSubmenu("my_mod_sub", "More", paginated: false, breadcrumbHex: "#4DD0E1");
sub.AddButton("my_mod_sub_btn", "Do thing", () => { });

// Single button directly in the root MORE list
var reg = ZUIMainMenu.RegisterButton("my_mod_root", "My Action", () => { }, priority: 10);
reg.VisibilityCondition = () => SomeFlag;
reg.Interactable = true;

// Navigation from code
ZUIMainMenu.PushDrawer(drawer);
ZUIMainMenu.PopDrawer();
ZUIMainMenu.SetMode(MainMenuMode.Vanilla);
```
