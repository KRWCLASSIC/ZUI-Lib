# Settings

Namespace: `ZUI.Settings`

ZUI injects custom items into the vanilla Zort settings screen. Items are registered through the static `ZUISettings` class and are created in the settings list when the screen is opened (or immediately if it is already open).

`SettingContainer` and the item classes are shared with custom panels, see [Panels.md](Panels.md).

## Apply / revert model

- Toggles, sliders and dropdowns keep a `CurrentValue` (applied) and a `PendingValue` (what the user sees). Changing the control only changes the pending value and calls `OnChanged`.
- Any change calls `ZUISettings.MarkChangesMade()`, which enables the vanilla Apply button and tells the game there are unsaved changes (so the discard popup is shown when leaving).
- On Apply, `ZUISettings.ApplyPending()` runs: every item whose value changed copies pending to current, writes the bound `ConfigEntry` and saves its config file, then calls `OnApply`.
- On Revert / discard, `ZUISettings.RevertPending()` sets pending back to current and resets the control.
- When the settings screen is opened, items are installed, refreshed and the Apply button is disabled.
- Buttons, text, headers, spaces and dividers never have pending changes.

These calls are driven by Harmony patches on `InitUI.ApplyChanges_Btn`, `InitUI.RevertAllChanges`, `InitUI.OpenSettings_Btn` and `ButtonFunctions.OpenSettings_Btn`.

## Items without a ConfigEntry vs with one

- With an `id`: standalone item, no persistence. Use `onApply` to store the value yourself.
- With a `ConfigEntry`: the id is derived (`ZUI_Toggle_<Section>_<Key>`, `ZUI_Slider_...`, `ZUI_Dropdown_...`), the initial value is read from the entry, and the entry is saved on apply. If `label` is null or empty, the entry key is used as label.

Item ids must be unique. An item is only instantiated if no child with the same name already exists in the settings content.

## ZUISettings

Static class. Every `Add...` method creates the item, adds it to `Container` and instantiates it right away if the settings screen exists.

### Members

```csharp
SettingContainer Container { get; }
IReadOnlyList<ISettingItem> Items { get; }

void Initialize(MonoBehaviour host)
void Shutdown()
bool TryInstallElements()
IEnumerator InstallWhenReadyRoutine()
void OnSettingsOpened()
void MarkChangesMade()
void SetApplyButtonInteractable(bool interactable)
void ApplyPending()
void RevertPending()
void RefreshUI()
Transform FindContentTransform()
GameObject FindCanvas()
void AddItem(ISettingItem item)
```

- `Initialize(host)`: hooks scene loading and starts the install routine on `host`. Only the first call has an effect.
- `TryInstallElements`: instantiates all registered items that are not yet in the settings list. Returns `false` if the settings canvas is not available yet. `InstallWhenReadyRoutine` retries for up to 120 frames.
- `MarkChangesMade`: if a `ZUIPanel` is currently open, notifies the panel instead of the vanilla screen.
- `AddItem`: adds a custom `ISettingItem` (ignored if `null`, not added twice) and instantiates it if possible.
- `FindContentTransform` / `FindCanvas`: return the settings list content / `InGameUICanvas`, or `null`.
- `Shutdown`: unhooks scene loading and destroys and clears all items.

### Toggle

```csharp
ToggleSettingItem AddToggle(ConfigEntry<bool> configEntry, string label = null, Action<bool> onApply = null, Action<bool> onChanged = null)
ToggleSettingItem AddToggle(string id, string label, bool defaultValue, Action<bool> onApply = null, Action<bool> onChanged = null)
```

### Slider

```csharp
SliderSettingItem AddSlider(ConfigEntry<float> configEntry, float minValue, float maxValue, bool wholeNumbers = false, string label = null,
    Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)

SliderSettingItem AddSlider(ConfigEntry<int> configEntry, int minValue, int maxValue, string label = null,
    Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)

SliderSettingItem AddSlider(string id, string label, float minValue, float maxValue, float defaultValue, bool wholeNumbers = false,
    Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
```

- The `int` overload always uses whole numbers and rounds the value when writing the entry.
- Without `valueFormatter`, the value is shown as `0` (whole numbers) or `0.00`.

### Dropdown

```csharp
DropdownSettingItem AddDropdown(string id, string label, IList<string> options, int defaultIndex = 0, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown(ConfigEntry<string> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown(ConfigEntry<int> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown<TEnum>(ConfigEntry<TEnum> configEntry, string label = null, Action<TEnum> onApply = null, Action<TEnum> onChanged = null) where TEnum : struct, Enum
```

- `ConfigEntry<string>` stores the selected option text, `ConfigEntry<int>` stores the index.
- The enum overload uses the enum names as options and the callbacks receive the enum value.
- The item creates its dropdown from the game's template and throws `InvalidOperationException` on instantiate if no dropdown template exists in the settings screen.

### Header, category, text

```csharp
HeaderSettingItem AddHeader(string title)
HeaderSettingItem AddHeader(string title, Color color)
HeaderSettingItem AddHeader(string title, string hexColor)
HeaderSettingItem AddHeader(string id, string title, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, float? height = null)
HeaderSettingItem AddHeader(string id, string title, string hexColor)

TextSettingItem AddCategory(string title)
TextSettingItem AddCategory(string title, Color color)
TextSettingItem AddCategory(string title, string hexColor)
TextSettingItem AddCategory(string id, string title, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, float? height = null)
TextSettingItem AddCategory(string id, string title, string hexColor)

TextSettingItem AddText(string text)
TextSettingItem AddText(string text, Color color)
TextSettingItem AddText(string text, string hexColor)
TextSettingItem AddText(string id, string text, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, TextAlignmentOptions? alignment = null, float? height = null)
TextSettingItem AddText(string id, string text, string hexColor)
```

- Overloads without an id generate one (`ZUI_Header_xxxxxxxx`, `ZUI_Category_xxxxxxxx`, `ZUI_Text_xxxxxxxx`).
- `AddCategory` currently creates a `HeaderSettingItem` (same look as a header) and returns it as `TextSettingItem`.
- A header is centered, 52 px high, default color `HeaderSettingItem.DefaultHeaderColor` (#F2D973). Text defaults to 38 px height.

### Space and divider

```csharp
SpaceSettingItem AddSpace(float height = 15f)
SpaceSettingItem AddSpacer(float height = 15f)   // alias of AddSpace
DividerSettingItem AddDivider(string id = null)
```

The divider clones the vanilla `Line` element. If no line template is found it is not created.

### Button

```csharp
ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick)

ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick,
    float? width = null, float? height = null, ButtonAlignment alignment = ButtonAlignment.Right,
    string buttonHex = null, string textHex = null, float? fontSize = null, bool squishTextToFit = true)

ButtonSettingItem AddButton(ButtonSettingItem item)   // throws ArgumentNullException for null
```

The extended overload defaults to `ButtonAlignment.Right`, while a `ButtonSettingItem` created with its constructor defaults to `ButtonAlignment.Center`.

## SettingContainer

Holds a list of items. `ZUISettings.Container` is one instance, and each panel has its own. It has the same `Add...` methods as `ZUISettings` (same overloads and parameters, including `AddToggle`, `AddSlider`, `AddDropdown`, `AddHeader`, `AddCategory`, `AddText`, `AddSpace`, `AddSpacer`, `AddDivider`, `AddButton`) but only adds to the container and does not instantiate anything. Its `onChanged` wrappers call `NotifyChangesMade()`.

| Member | Description |
|---|---|
| `IReadOnlyList<ISettingItem> Items` | All items in order. |
| `bool HasPendingChanges` | `true` if any item has pending changes. |
| `bool Contains(ISettingItem item)` | Item is in the container. |
| `bool Contains(string id)` | An item with this id exists (case-insensitive). |
| `event Action<ISettingItem> OnItemAdded` | Raised after an item is added. |
| `event Action OnChangesMade` | Raised by `NotifyChangesMade()`. `ZUISettings` subscribes `MarkChangesMade` to it on `Initialize`. |
| `void AddItem(ISettingItem item)` | Adds any item. Throws `ArgumentNullException` for null. |
| `void NotifyChangesMade()` | Raises `OnChangesMade`. |
| `void ApplyPending()` | Calls `Apply()` on every item. |
| `void RevertPending()` | Calls `Revert()` on every item. |
| `void RefreshUI()` | Calls `RefreshUI()` on every item. |
| `void DestroyItems()` | Destroys the spawned objects and clears the list. |

## ISettingItem

```csharp
public interface ISettingItem
{
    string Id { get; }
    string Label { get; set; }
    GameObject SpawnedObject { get; }
    bool HasPendingChanges { get; }

    GameObject Instantiate(Transform parent, SettingsTemplates templates);
    void Apply();
    void Revert();
    void RefreshUI();
    void Destroy();
}
```

`SettingsTemplates` has the discovered vanilla elements (`LineTemplate`, `ToggleTemplate`, `SliderTemplate`, `DropdownTemplate`, `ButtonTemplate`, all `Transform`) and `static SettingsTemplates Discover(Transform content, Transform canvasRoot)`. `Instantiate` implementations clone one of these templates. A custom item can be registered with `ZUISettings.AddItem`.

## Item types

All items implement `ISettingItem` (`Id`, `Label`, `SpawnedObject`, `HasPendingChanges`, `Apply`, `Revert`, `RefreshUI`, `Destroy`). Only type-specific members are listed.

### ToggleSettingItem

```csharp
ToggleSettingItem(string id, string label, bool defaultValue, Action<bool> onApply = null, Action<bool> onChanged = null)
ToggleSettingItem(ConfigEntry<bool> configEntry, string label = null, Action<bool> onApply = null, Action<bool> onChanged = null)
```

| Member | Description |
|---|---|
| `bool CurrentValue { get; }` | Applied value. |
| `bool PendingValue { get; set; }` | Value shown in the UI. |
| `ConfigEntry<bool> ConfigEntry { get; }` | Bound entry or `null`. |
| `Action<bool> OnApply, OnChanged` | Callbacks. |

### SliderSettingItem

```csharp
SliderSettingItem(string id, string label, float minValue, float maxValue, float defaultValue, bool wholeNumbers = false, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
SliderSettingItem(ConfigEntry<float> configEntry, float minValue, float maxValue, bool wholeNumbers = false, string label = null, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
SliderSettingItem(ConfigEntry<int> configEntry, int minValue, int maxValue, string label = null, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
```

| Member | Description |
|---|---|
| `float MinValue, MaxValue { get; set; }` | Range. |
| `bool WholeNumbers { get; set; }` | Snap to integers. |
| `Func<float, string> ValueFormatter { get; set; }` | Formats the value label. |
| `float CurrentValue { get; }` | Applied value. |
| `float PendingValue { get; set; }` | Value shown in the UI. |
| `ConfigEntry<float> ConfigEntry { get; }` | Bound float entry or `null`. |
| `ConfigEntry<int> IntConfigEntry { get; }` | Bound int entry or `null`. |
| `Action<float> OnApply, OnChanged` | Callbacks. |

`HasPendingChanges` compares current and pending with a tolerance of 0.0001.

### DropdownSettingItem

```csharp
DropdownSettingItem(string id, string label, IList<string> options, int defaultIndex = 0, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem(ConfigEntry<string> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem(ConfigEntry<int> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
```

| Member | Description |
|---|---|
| `List<string> Options { get; }` | Option texts. |
| `int CurrentIndex { get; }` | Applied index. |
| `int PendingIndex { get; set; }` | Index shown in the UI. |
| `ConfigEntry<string> StringConfigEntry { get; }` | Bound string entry or `null`. |
| `ConfigEntry<int> IntConfigEntry { get; }` | Bound int entry or `null`. |
| `Action<int> OnApply, OnChanged` | Callbacks, receive the index. |

### TextSettingItem

```csharp
TextSettingItem(string id, string text, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, TextAlignmentOptions? alignment = null, float? height = null)
```

Text-only row (rich text supported). `Label` holds the text.

| Member | Description |
|---|---|
| `Color? Color { get; set; }` | Text color. |
| `TMP_FontAsset Font { get; set; }` | Font asset. |
| `float? FontSize { get; set; }` | Font size. |
| `FontStyles FontStyle { get; set; }` | Default `FontStyles.Normal`. |
| `TextAlignmentOptions? Alignment { get; set; }` | Text alignment. |
| `float Height { get; set; }` | Row height, default 38. |
| `TextSettingItem SetColor(Color color)` / `SetColor(string hexColor)` | Fluent, returns `this`. |
| `TextSettingItem WithColor(Color color)` / `WithColor(string hexColor)` | Same as `SetColor`. |
| `TextSettingItem WithFontSize(float size)` | Fluent. |
| `TextSettingItem WithHeight(float height)` | Fluent. |

### HeaderSettingItem

Derives from `TextSettingItem`. Centered, height 52 by default, color `DefaultHeaderColor`.

```csharp
public static readonly Color DefaultHeaderColor;   // (0.95, 0.85, 0.45)

HeaderSettingItem(string id, string title, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, float? height = null)
HeaderSettingItem(string id, string title, string hexColor)
```

### SpaceSettingItem

```csharp
SpaceSettingItem(string id = null, float height = 15f)
float Height { get; set; }
```

### DividerSettingItem

```csharp
DividerSettingItem(string id = null)
```

A null or empty id generates `ZUI_Divider_xxxxxxxx`.

### ButtonSettingItem

```csharp
ButtonSettingItem(string id, string label, string buttonText, Action onClick)
```

| Member | Description |
|---|---|
| `string ButtonText { get; set; }` | Text on the button. |
| `Action OnClick { get; set; }` | Click handler. |
| `float? Width { get; set; }` | Button width in px. Default 260 (full row for `Fill`). |
| `float? Height { get; set; }` | Button height in px. Default 44. |
| `ButtonAlignment Alignment { get; set; }` | Default `Center`. |
| `bool FullWidth { get; set; }` | Shortcut for `Alignment == Fill`. |
| `Color? ButtonColor, TextColor, HoverColor, PressedColor, LabelColor` | Optional colors. |
| `string ButtonColorHex, TextColorHex, HoverColorHex, PressedColorHex, LabelColorHex` | Write-only hex setters for the colors above. |
| `float? FontSize { get; set; }` | Base font size (default 20). |
| `float? FontSizeMin { get; set; }` | Min size when squishing (default 10). |
| `float? FontSizeMax { get; set; }` | Max size when squishing (default 22). |
| `bool SquishTextToFit { get; set; }` | Default `true`. Single line, auto-sized down to `FontSizeMin`. |

Fluent builder methods (all return `this`):

```csharp
// Sizing & Alignment
button.WithSize(float width, float height);
button.WithWidth(float width);
button.WithHeight(float height);
button.WithAlignment(ButtonAlignment alignment);
button.WithFullWidth(bool fullWidth = true, float? height = null);

// Colors
button.WithColors(Color buttonColor, Color textColor);
button.WithColors(string buttonHex, string textHex);
button.WithButtonColor(Color color);
button.WithButtonColor(string hex);
button.WithTextColor(Color color);
button.WithTextColor(string hex);

// Typography & Auto-squish
button.WithFontSize(float fontSize, bool squish = true);
button.WithSquish(bool squish = true);
```

### ButtonAlignment

```csharp
public enum ButtonAlignment
{
    Center,  // label and button grouped and centered
    Right,   // grouped, aligned right
    Left,    // grouped, aligned left
    Fill     // button stretches across the whole row
}
```

## Example

```csharp
using ZUI.Settings;

var cfg = Config.Bind("General", "Enabled", true, "Enable the mod");
var volume = Config.Bind("General", "Volume", 50, "Volume");

ZUISettings.AddDivider();
ZUISettings.AddHeader("My Mod", "#F2D973");
ZUISettings.AddToggle(cfg, "Enabled");                                   // saved to .cfg on Apply
ZUISettings.AddSlider(volume, 0, 100, "Volume", v => $"{v:0}%");

ZUISettings.AddDropdown("my_mode", "Mode", new[] { "Easy", "Normal", "Hard" }, defaultIndex: 1,
    onApply: i => Plugin.Log.LogInfo($"Mode: {i}"));

ZUISettings.AddSpace(14f);
ZUISettings.AddText("Some description", "#CCCCCC");

ZUISettings.AddButton("my_reset", "Reset", "Reset now", () => { },
    width: 200f, alignment: ButtonAlignment.Center, buttonHex: "#AA3333");

// Fluent configuration of a button item
var item = new ButtonSettingItem("my_btn", "Action", "Run", () => { })
    .WithFullWidth(true, 48f)
    .WithColors("#335599", "#FFFFFF");
ZUISettings.AddButton(item);
```
