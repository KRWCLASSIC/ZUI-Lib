# Panels

Namespace: `ZUI.Panels`

`ZUIPanel` is a full-screen settings-style panel. It is a clone of the game's `InGameUICanvas/settingsPanel`, so it uses the vanilla look, with an Apply button and a Close/Back button. Panels are registered once by id and opened by id. The clone is built on the first `Open`.

```csharp
var panel = ZUIPanel.Register("my_mod_panel", "My Mod Settings");
panel.ShowRevertButton = true;
panel.AddHeader("Display");
panel.AddToggle("bloom", "Bloom", true);
panel.AddSlider("fov", "Field of View", 60f, 120f, 90f, wholeNumbers: true, valueFormatter: v => $"{v:F0}°");
panel.AddDropdown("quality", "Quality", new[] { "Low", "Medium", "High" }, defaultIndex: 1);
panel.AddDivider();

ZUIPanel.Open("my_mod_panel");
```

## Static members

| Member | Description |
|---|---|
| `IReadOnlyDictionary<string, ZUIPanel> Registry` | All registered panels. Keys are case-insensitive. |
| `ZUIPanel CurrentOpenPanel` | The open panel, or `null`. |
| `void Initialize(MonoBehaviour host)` | Creates the `ZUIPanelController` on the host's GameObject, or on a new GameObject if `host` is `null`. Does nothing if it already exists. |
| `ZUIPanel Register(string id, string title)` | Creates a panel. If the id already exists, the existing panel is returned and its `Title` is updated. Throws `ArgumentNullException` if `id` is null or empty. |
| `ZUIPanel Get(string id)` | Returns the panel, or `null` if the id is empty or unknown. |
| `bool Unregister(string id)` | Closes the panel if it is open, destroys its clone and removes it from the registry. Returns `false` if the id is unknown. |
| `void Open(string id)` | Opens the panel. Logs an error if the id is not registered. |
| `void Open(ZUIPanel panel)` | Opens the panel. Throws `ArgumentNullException` if `panel` is null. |
| `void CloseCurrent()` | Closes the open panel, if any. |
| `void CloseActive()` | Same as `CloseCurrent()`. |
| `void CloseAll()` | Same as `CloseCurrent()`. |

## Instance members

Constructor: `ZUIPanel(string id, string title)`. Use `Register` instead of calling it directly, because only `Register` adds the panel to the registry.

| Member | Description |
|---|---|
| `string Id { get; }` | Panel id. |
| `string Title { get; set; }` | Title text. It is shown in upper case. |
| `SettingContainer Container { get; }` | Holds the items. |
| `IReadOnlyList<ISettingItem> Items` | The items in order. |
| `bool HasPendingChanges` | `true` if any item has unapplied changes. |
| `bool ShowRevertButton { get; set; }` | Adds a REVERT button next to APPLY. Default `false`. Set it before the first `Open`, because the clone is built then. |
| `Action OnOpened { get; set; }` | Called after the panel is shown. |
| `Action OnClosed { get; set; }` | Called after the panel is hidden. |
| `Action OnApplied { get; set; }` | Called at the end of `Apply()`. |
| `GameObject ClonedObject` | The cloned panel object. It is `null` until opened and is reset on every scene load. |
| `Button ApplyButton`, `Button CloseButton`, `Button RevertButton` | Cloned buttons. They are read-only from outside and may be `null`. `RevertButton` is only set when `ShowRevertButton` is on. |
| `TMP_Text TitleText` | The cloned title text. Read-only from outside. |
| `void Open()` | Opens this panel. If another panel is open, it is hidden first. |
| `void Close()` | Closes this panel (see Closing). |
| `void Apply()` | Applies all pending items, disables Apply/Revert and calls `OnApplied`. |
| `void Revert()` | Reverts all pending items and disables Apply/Revert. |
| `void RefreshUI()` | Refreshes all item visuals from their current values. |
| `void NotifyChangesMade()` | Marks the panel as changed, which enables Apply/Revert. |
| `void AddItem(ISettingItem item)` | Adds a custom item. If the panel is already built, the item is spawned immediately. |

## Closing and ESC

- ESC closes the open panel. If a `ZUIPopup` is showing, the popup gets ESC first and the panel stays open.
- The Close/Back button does the same as `Close()`.
- If the panel has pending changes, a "DISCARD CHANGES" confirmation popup is shown. Choosing "Discard" reverts the changes and hides the panel, and "Cancel" keeps it open.
- `OnClosed` is called when the panel is hidden.
- On scene load, `CurrentOpenPanel` is reset to `null` and every clone is dropped. The clone is rebuilt on the next `Open`.
- `ZUIPanelController.LastEscConsumedFrame` holds the frame in which the panel consumed ESC. Other ESC handlers can compare it to `Time.frameCount`.

## Adding items

All `Add*` methods return the created item, so you can keep it and change it later. Each one adds the item to the panel. Item types live in `ZUI.Settings`.

### Toggle

```csharp
ToggleSettingItem AddToggle(ConfigEntry<bool> configEntry, string label = null, Action<bool> onApply = null, Action<bool> onChanged = null)
ToggleSettingItem AddToggle(string id, string label, bool defaultValue, Action<bool> onApply = null, Action<bool> onChanged = null)
```

- With a `ConfigEntry`, the value is read from and written to the entry on apply.
- `onChanged` fires when the user changes the value. `onApply` fires when the change is applied.
- `ToggleSettingItem` members: `Id`, `Label`, `CurrentValue`, `PendingValue`, `ConfigEntry`, `OnApply`, `OnChanged`, `HasPendingChanges`, `SpawnedObject`.

### Slider

```csharp
SliderSettingItem AddSlider(ConfigEntry<float> configEntry, float minValue, float maxValue, bool wholeNumbers = false, string label = null, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
SliderSettingItem AddSlider(ConfigEntry<int> configEntry, int minValue, int maxValue, string label = null, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
SliderSettingItem AddSlider(string id, string label, float minValue, float maxValue, float defaultValue, bool wholeNumbers = false, Func<float, string> valueFormatter = null, Action<float> onApply = null, Action<float> onChanged = null)
```

- `valueFormatter` turns the slider value into the displayed text.
- `SliderSettingItem` members: `Id`, `Label`, `MinValue`, `MaxValue`, `WholeNumbers`, `ValueFormatter`, `CurrentValue`, `PendingValue`, `ConfigEntry`, `IntConfigEntry`, `OnApply`, `OnChanged`, `HasPendingChanges`, `SpawnedObject`.

### Dropdown

```csharp
DropdownSettingItem AddDropdown(string id, string label, IList<string> options, int defaultIndex = 0, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown(ConfigEntry<string> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown(ConfigEntry<int> configEntry, IList<string> options, string label = null, Action<int> onApply = null, Action<int> onChanged = null)
DropdownSettingItem AddDropdown<TEnum>(ConfigEntry<TEnum> configEntry, string label = null, Action<TEnum> onApply = null, Action<TEnum> onChanged = null) where TEnum : struct, Enum
```

- Callbacks receive the selected index. The enum overload receives the `TEnum` value instead.
- `DropdownSettingItem` members: `Id`, `Label`, `Options`, `CurrentIndex`, `PendingIndex`, `StringConfigEntry`, `IntConfigEntry`, `OnApply`, `OnChanged`, `HasPendingChanges`, `SpawnedObject`.

### Header

```csharp
HeaderSettingItem AddHeader(string title)
HeaderSettingItem AddHeader(string title, Color color)
HeaderSettingItem AddHeader(string title, string hexColor)
HeaderSettingItem AddHeader(string id, string title, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, float? height = null)
HeaderSettingItem AddHeader(string id, string title, string hexColor)
```

- `HeaderSettingItem` derives from `TextSettingItem`. `HeaderSettingItem.DefaultHeaderColor` is the default color.

### Category

```csharp
TextSettingItem AddCategory(string title)
TextSettingItem AddCategory(string title, Color color)
TextSettingItem AddCategory(string title, string hexColor)
TextSettingItem AddCategory(string id, string title, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, float? height = null)
TextSettingItem AddCategory(string id, string title, string hexColor)
```

### Text

```csharp
TextSettingItem AddText(string text)
TextSettingItem AddText(string text, Color color)
TextSettingItem AddText(string text, string hexColor)
TextSettingItem AddText(string id, string text, Color? color = null, TMP_FontAsset font = null, float? fontSize = null, FontStyles? fontStyle = null, TextAlignmentOptions? alignment = null, float? height = null)
TextSettingItem AddText(string id, string text, string hexColor)
```

- `TextSettingItem` members: `Id`, `Label` (the text), `Color`, `Font`, `FontSize`, `FontStyle`, `Alignment`, `Height`, `HasPendingChanges`, `SpawnedObject`.
- Fluent setters: `SetColor(Color)`, `SetColor(string hex)`, `WithColor(Color)`, `WithColor(string hex)`, `WithFontSize(float)`, `WithHeight(float)`.

### Space and divider

```csharp
SpaceSettingItem AddSpace(float height = 15f)
SpaceSettingItem AddSpacer(float height = 15f)   // same as AddSpace
DividerSettingItem AddDivider(string id = null)
```

### Button

```csharp
ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick)
ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick, float? width = null, float? height = null, ButtonAlignment alignment = ButtonAlignment.Right, string buttonHex = null, string textHex = null, float? fontSize = null, bool squishTextToFit = true)
ButtonSettingItem AddButton(ButtonSettingItem item)
```

- The first overload uses the `ButtonSettingItem` defaults. The second one always sets `alignment`, which defaults to `Right`. `ButtonSettingItem` itself defaults to `Center`.
- `ButtonAlignment` values: `Center`, `Right`, `Left`, `Fill`.
- `ButtonSettingItem` members: `Id`, `Label`, `ButtonText`, `OnClick`, `Width`, `Height`, `Alignment`, `FullWidth`, `ButtonColor`, `TextColor`, `HoverColor`, `PressedColor`, `LabelColor`, `FontSize`, `FontSizeMin`, `FontSizeMax`, `SquishTextToFit`, the matching `*Hex` string properties (`ButtonColorHex`, `TextColorHex`, `HoverColorHex`, `PressedColorHex`, `LabelColorHex`), `SpawnedObject`.
- Fluent setters: `WithSize(float, float)`, `WithWidth`, `WithHeight`, `WithAlignment(ButtonAlignment)`, `WithFullWidth(bool fullWidth = true, float? height = null)`, `WithColors(Color, Color)`, `WithColors(string, string)`, `WithButtonColor(Color|string)`, `WithTextColor(Color|string)`, `WithFontSize(float, bool squish = true)`, `WithSquish(bool squish = true)`.

## ZUIPanelController

`ZUIPanelController` is the `MonoBehaviour` behind `ZUIPanel`. It builds the clones, wires the buttons and handles ESC and scene changes. You normally use `ZUIPanel` instead.

| Member | Description |
|---|---|
| `static ZUIPanelController Instance` | The active controller, or `null`. |
| `static ZUIPanel CurrentOpenPanel` | The open panel. |
| `static int LastEscConsumedFrame` | See Closing and ESC. |
| `static void EnsureInitialized(MonoBehaviour host)` | What `ZUIPanel.Initialize` calls. |
| `static void OpenPanel(ZUIPanel panel)` | What `ZUIPanel.Open` calls. It logs an error and returns if `InGameUICanvas` or its `settingsPanel` is not found. |
| `static void ClosePanel(ZUIPanel panel)` | What `ZUIPanel.Close` calls. It does nothing if `panel` is null. |

Panels need the in-game canvas, so they can only be opened in a scene where `InGameUICanvas` exists.
