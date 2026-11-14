# Core

Base classes of ZUI-Lib: plugin entry point, module discovery, global helpers, image loading, and the crash handler.

## Referencing ZUI-Lib from your mod

Add `ZUI-Lib.dll` as a reference in your csproj and declare a BepInEx dependency so ZUI-Lib loads first.

```xml
<ItemGroup>
  <Reference Include="ZUI-Lib">
    <HintPath>libs\ZUI-Lib.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

```csharp
using BepInEx;

[BepInPlugin("com.example.mymod", "MyMod", "1.0.0")]
[BepInDependency("mods.krwclassic.zuilib", "0.1.0")]
public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        ZUI.Popups.ZUIPopup.ShowInfo("Hello", "Loaded with ZUI-Lib");
    }
}
```

The second argument of `BepInDependency` is the minimum version. If it is set, the crash handler shows it as the expected version when a missing-API crash is detected.

All public types live in these namespaces: `ZUI`, `ZUI.Core`, `ZUI.Popups` (and others documented elsewhere).

## Plugin and PluginInfo

`PluginInfo` (namespace `ZUI`):

| Constant | Value |
|---|---|
| `PLUGIN_GUID` | `"mods.krwclassic.zuilib"` |
| `PLUGIN_NAME` | `"ZUI-Lib"` |
| `PLUGIN_VERSION` | `"0.2.0"` |

`Plugin` (namespace `ZUI`, extends `BaseUnityPlugin`):

| Member | Type | Description |
|---|---|---|
| `Log` | `static ManualLogSource` | ZUI-Lib's BepInEx logger |
| `Instance` | `static Plugin` | The plugin instance. Useful as a coroutine host: `Plugin.Instance.StartCoroutine(...)` |
| `EnableExampleModules` | `static ConfigEntry<bool>` | Config entry, see below |

Config (`BepInEx/config/mods.krwclassic.zuilib.cfg`):

```ini
[General]
## Whether to load the built-in ZUI.Example showcase modules on startup.
EnableExampleModules = true
```

Default is `true`. Set it to `false` to skip every module whose namespace starts with `ZUI.Example`.

On startup the plugin initializes the crash handler, Harmony patches, settings, panels, main menu, pause menu, screens and info text subsystems, registers its own "ZUI-Lib" drawers, then loads modules.

## IZUIModule

```csharp
namespace ZUI.Core
{
    public interface IZUIModule
    {
        string Name { get; }
        void Initialize();
    }
}
```

At startup ZUI-Lib scans its own assembly for non-abstract classes implementing `IZUIModule`, creates each with `Activator.CreateInstance` (a public parameterless constructor is required) and calls `Initialize()`. Exceptions are logged and passed to `ZUICrashHandler.HandleException`; a failing module does not stop the others.

Discovery only covers the ZUI-Lib assembly. A mod in a separate DLL does not need `IZUIModule`: register your UI from your own plugin `Awake` instead.

```csharp
public class MyModule : IZUIModule
{
    public string Name => "MyModule";

    public void Initialize()
    {
        ZUISettings.AddDivider(id: "my_divider");
    }
}
```

## ZUIAPI

Static class in namespace `ZUI`. Shortcuts for the open ZUI layers.

| Member | Description |
|---|---|
| `static bool IsPauseMenuOpen` | The in-game pause menu is open |
| `static bool IsPanelOpen` | A `ZUIPanel` modal is open |
| `static bool IsScreenOpen` | A `ZUIScreen` split screen is open |
| `static void CloseAll()` | Closes the active panel, the active screen and the pause menu |
| `static void ClosePauseMenu()` | Closes the pause menu |
| `static void CloseActivePanel()` | Closes the active panel, if any |
| `static void CloseActiveScreen()` | Closes the active screen, if any |
| `static Sprite DefaultCircleIcon` | Built-in circular ring indicator icon for equip and radio buttons |

```csharp
if (ZUIAPI.IsPanelOpen)
    ZUIAPI.CloseActivePanel();
```

## ZUIDefaultIcons

Static class in `ZUI.Core`. Provides built-in procedural vector icons ready for use without packing extra image assets into your mod.

| Member | Description |
|---|---|
| `static Sprite CircleIndicator` | Cached white circular ring + center dot icon (44x44). Ideal for equip toggles and radio buttons tintable via Button ColorBlock. |
| `static Sprite CreateCircleIndicator(int size = 44, Color? ringColor = null)` | Generates or retrieves a cached circular indicator with custom size and base color. |
| `static void ClearCache()` | Clears cached procedural default icon sprites. |

```csharp
// Fast 1-liner using the cached default white circle
Sprite icon = ZUIDefaultIcons.CircleIndicator;

// Add to card button
card.AddRightButton("equip_skin", icon, "Equip", () => Equip());
```

## ZUIImage

Static class in `ZUI.Core`. Loads PNG/JPG data into Unity `Sprite` objects. Sprites are created as RGBA32 textures, bilinear filter, clamp wrap, pivot at the center.

### Load

```csharp
public static Sprite Load(string filePath, bool useCache = true, float pixelsPerUnit = 100f)
```

| Parameter | Description |
|---|---|
| `filePath` | Absolute path, or a path relative to the BepInEx plugins folder, the BepInEx root, or the working directory (checked in that order) |
| `useCache` | Reuse a previously loaded sprite for the same resolved path |
| `pixelsPerUnit` | Sprite pixels per unit |

Returns `null` if `filePath` is empty or loading fails (a warning or error is logged). If the file does not exist, an embedded resource with that name is tried, but only in the ZUI-Lib assembly. For your own embedded resources use `LoadEmbedded`.

The sprite name is the file name without extension.

### LoadEmbedded

```csharp
public static Sprite LoadEmbedded(string resourceName, Assembly assembly = null, bool useCache = true, float pixelsPerUnit = 100f)
```

| Parameter | Description |
|---|---|
| `resourceName` | Resource name. Matched against the manifest names in this order: exact (case-insensitive), then a name ending with it |
| `assembly` | Assembly that holds the resource. `null` means the calling assembly |
| `useCache` | Cache by assembly and resource name |
| `pixelsPerUnit` | Sprite pixels per unit |

If the resource is not found in `assembly`, ZUI-Lib's own assembly is searched as well. Returns `null` if nothing is found or the image cannot be decoded. No warning is logged for a missing resource.

Because suffix matching is used, `"teleport.png"` finds `MyMod.assets.teleport.png`. If two resources end with the same name, the first one in the manifest wins, so keep file names unique.

To embed images in your mod, add them to your csproj:

```xml
<ItemGroup>
  <EmbeddedResource Include="assets\*.png" />
</ItemGroup>
```

ZUI-Lib itself does the same for its example sprites (`<EmbeddedResource Include="src\Example\sprites\*.png" />`). Usage:

```csharp
Sprite icon = ZUIImage.LoadEmbedded("teleport.png");
Sprite other = ZUIImage.LoadEmbedded("icon.png", typeof(MyMod).Assembly);
```

### LoadFromBytes

```csharp
public static Sprite LoadFromBytes(byte[] bytes, float pixelsPerUnit = 100f)
```

Decodes PNG/JPG bytes into a sprite. Returns `null` for null or empty input, or if decoding fails. Not cached.

### ClearCache

```csharp
public static void ClearCache()
```

Clears the sprite cache. It only drops the references, it does not destroy the sprites or textures.

## UIUtils

Static class in `ZUI.Core`.

```csharp
public static void ApplyFontSafe(params TMP_Text[] texts)
public static Color ParseColor(string colorStr, Color fallback)
public static Color? ParseColor(string colorStr)
```

| Method | Description |
|---|---|
| `ApplyFontSafe` | Applies the current game font to the given texts through `FontManager`. Does nothing for `null`, and swallows the exception if the game's `FontManager` is not ready yet |
| `ParseColor(string, Color)` | Parses a color and returns `fallback` if it cannot be parsed |
| `ParseColor(string)` | Same, returns `null` if it cannot be parsed |

`ParseColor` accepts anything `ColorUtility.TryParseHtmlString` accepts (`"#FFCC00"`, `"yellow"`) and also hex without `#` (`"FF5555"`). Empty or null input counts as failure.

## ZUICrashHandler

Static class in `ZUI.Core`. Catches version mismatches between a mod and ZUI-Lib (for example a mod built against a newer ZUI-Lib calling a method that the installed version does not have) and shows a popup instead of failing silently.

It handles these exception types:

- `MissingMethodException`
- `MissingFieldException`
- `MissingMemberException`
- `TypeLoadException`

Two sources feed it:

- `HandleException` called by ZUI-Lib where it runs callbacks or loads modules.
- A listener on `Application.logMessageReceived`. It reacts to logged exceptions of the types above whose message or stack trace contains "ZUI" (case-insensitive). Duplicate reports of the same member in the same frame are ignored.

If the game's popup manager is not available yet (early startup), reports are queued and shown after the next scene load.

The popup (`ZUIPopup.ShowConfirmation`) is titled `ZUI-LIB CRASH` and lists:

- Mod: name and version of the culprit mod
- Error: the exception type
- Tried to call missing API: the member or type name taken from the exception message
- Expected ZUI-Lib: only shown if known
- Installed ZUI-Lib: the installed version

Buttons are "Close Game" (calls `Application.Quit()`) and "Ignore". ESC triggers "Ignore". The same information is written to the BepInEx log as an error.

The culprit mod is found from the exception stack trace (first assembly that is not ZUI-Lib, the system or Unity), then from the `callerHint` assembly, then by matching BepInEx plugin assembly or namespace names in the stack trace. The expected version comes from the mod's `[BepInDependency("mods.krwclassic.zuilib", "x.y.z")]` minimum version, or otherwise from the version of the `ZUI-Lib` assembly reference compiled into the mod.

### Members

```csharp
public static void Initialize(MonoBehaviour host)
public static void Shutdown()
public static bool HandleException(Exception ex, string context = null, Assembly callerHint = null)
```

| Method | Description |
|---|---|
| `Initialize` | Hooks the log and scene-load events. Called by `Plugin.Awake`, repeated calls are ignored. You do not need to call it |
| `Shutdown` | Unhooks the events. Called by `Plugin.OnDestroy` |
| `HandleException` | Inspects `ex` (unwrapping `TargetInvocationException`). Returns `true` if it was a compatibility exception and a popup was queued or shown, `false` otherwise or if `ex` is `null`. `context` is only used for the log message. `callerHint` is the assembly to blame if the stack trace gives none |

```csharp
try
{
    CallSomethingThatMayBreak();
}
catch (Exception ex)
{
    if (!ZUICrashHandler.HandleException(ex, "MyMod init", typeof(MyMod).Assembly))
        throw;
}
```
