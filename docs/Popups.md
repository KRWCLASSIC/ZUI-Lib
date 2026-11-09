# Popups

Modal popups built on the game's native `PopupManager`. Namespace `ZUI.Popups`. All methods return the game's `Popup` object (or take it as an argument).

Popups need the game UI to be loaded. `ZUIPopup.Show` throws `InvalidOperationException` if `InitUI.PopupManager` does not exist yet, so call it from user actions or after the game UI is up, not from early `Awake`.

## Styles

`PopupStyle` enum:

| Value | Description |
|---|---|
| `Confirmation` | Two buttons (Yes/No). Default style |
| `Alert` | One button (OK) |
| `AcceptDecline` | Two buttons, green Accept and red Decline by default, white button text |
| `Loading` | Rotating spinner, buttons hidden |

Default native template per style (used when `PopupOptions.NativePopupType` is not set):

| Style | `AllPopups` value |
|---|---|
| `Confirmation` | `SureYouWantToDiscardChanges` |
| `Alert` | `UnknownError` |
| `AcceptDecline` | `InviteGame` |
| `Loading` | `Loading` |

## Quick examples

```csharp
using ZUI.Popups;

// Info
ZUIPopup.ShowInfo("Saved", "Settings were saved.");

// Confirmation
ZUIPopup.ShowConfirmation(
    title: "Quit Session?",
    message: "Unsaved changes will be lost.",
    yesText: "Quit",
    noText: "Cancel",
    onYes: () => Application.Quit(),
    dismissOnEscape: true,
    escapeChoice: PopupChoice.Secondary);

// Loading with manual progress
var loading = ZUIPopup.ShowLoading("Syncing", "Step 1/3");
ZUIPopup.UpdateMessage(loading, "Step 2/3");
ZUIPopup.Close(loading);

// Loading that closes itself
ZUIPopup.ShowLoading(
    title: "Please wait",
    waitUntil: new WaitUntil(() => isDone),
    onFinished: () => Debug.Log("done"));

// Full control
ZUIPopup.Show(new PopupOptions
{
    Title = "Custom",
    Message = "Hello",
    Style = PopupStyle.Alert,
    YesText = "Got it",
    TitleColorHex = "#64B5F6",
    DismissOnEscape = true
});
```

## ZUIPopup

Static class.

### Properties

| Member | Description |
|---|---|
| `static int ActivePopupCount` | Number of active popups, `0` if the popup manager is not available |
| `static int LastEscConsumedFrame` | `Time.frameCount` of the last frame in which ESC was consumed by a popup. Read-only for mods (internal setter), `-1` initially. ZUI uses it so the same ESC press does not also go back in a drawer or close the pause menu |

### Show

```csharp
public static Popup Show(PopupOptions options)
```

Creates the popup from the options. Throws `ArgumentNullException` if `options` is null, `InvalidOperationException` if the popup manager is missing or the native popup cannot be created. The new popup is moved on top of other panels and drawers, and a `ZUIPopupBehavior` is attached to it.

Defaults applied when a label is empty: primary button `"Yes"` (`Confirmation`), `"OK"` (`Alert`), `"Accept"` (`AcceptDecline`); secondary button `"No"` or `"Decline"`.

### Updating and closing

```csharp
public static void UpdateTitle(Popup popup, string title)
public static void UpdateMessage(Popup popup, string message)
public static void Close(Popup popup)
public static void CloseAll()
```

| Method | Description |
|---|---|
| `UpdateTitle` | Changes the title of an open popup and rebuilds its layout. Ignores a null popup |
| `UpdateMessage` | Changes the message text. Useful for progress on loading popups. Ignores a null popup |
| `Close` | Disposes the popup. Ignores a null popup |
| `CloseAll` | Disposes every active popup |

### ShowConfirmation

Two overloads: one with `Color?` parameters and one with hex strings. Both return `Popup`.

```csharp
public static Popup ShowConfirmation(
    string title,
    string message,
    string yesText = "Yes",
    string noText = "No",
    Action onYes = null,
    Action onNo = null,
    Color? titleColor = null,
    Color? messageColor = null,
    Color? yesTextColor = null,
    Color? noTextColor = null,
    Color? yesButtonColor = null,
    Color? noButtonColor = null,
    bool showCloseButton = true,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false,
    PopupChoice? escapeChoice = null)

public static Popup ShowConfirmation(
    string title,
    string message,
    string yesText = "Yes",
    string noText = "No",
    Action onYes = null,
    Action onNo = null,
    string titleHex = null,
    string messageHex = null,
    string yesTextHex = null,
    string noTextHex = null,
    string yesButtonHex = null,
    string noButtonHex = null,
    bool showCloseButton = true,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false,
    PopupChoice? escapeChoice = null)
```

### ShowAlert

```csharp
public static Popup ShowAlert(
    string title,
    string message,
    string buttonText = "OK",
    Action onDismiss = null,
    Color? titleColor = null,
    Color? messageColor = null,
    Color? buttonTextColor = null,
    Color? buttonColor = null,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false)

public static Popup ShowAlert(
    string title,
    string message,
    string buttonText = "OK",
    Action onDismiss = null,
    string titleHex = null,
    string messageHex = null,
    string buttonTextHex = null,
    string buttonHex = null,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false)
```

The close button is always hidden. `onDismiss` is the primary (`OnYes`) action.

### ShowInfo

```csharp
public static Popup ShowInfo(
    string title,
    string message,
    string buttonText = "OK",
    Action onDismiss = null,
    string titleHex = null,
    string messageHex = null,
    string buttonTextHex = null,
    string buttonHex = null,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false)
```

Same as the hex overload of `ShowAlert`. Only the hex version exists.

### ShowAcceptDecline

```csharp
public static Popup ShowAcceptDecline(
    string title,
    string message,
    string acceptText = "Accept",
    string declineText = "Decline",
    Action onAccept = null,
    Action onDecline = null,
    Color? titleColor = null,
    Color? messageColor = null,
    Color? acceptTextColor = null,
    Color? declineTextColor = null,
    Color? acceptButtonColor = null,
    Color? declineButtonColor = null,
    bool showCloseButton = true,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false,
    PopupChoice? escapeChoice = null)

public static Popup ShowAcceptDecline(
    string title,
    string message,
    string acceptText = "Accept",
    string declineText = "Decline",
    Action onAccept = null,
    Action onDecline = null,
    string titleHex = null,
    string messageHex = null,
    string acceptTextHex = null,
    string declineTextHex = null,
    string acceptButtonHex = null,
    string declineButtonHex = null,
    bool showCloseButton = true,
    float? buttonWidth = null,
    float? buttonHeight = null,
    bool squishTextToFit = true,
    bool dismissOnEscape = false,
    PopupChoice? escapeChoice = null)
```

`onAccept` maps to `OnYes`, `onDecline` to `OnNo`.

### ShowLoading

```csharp
public static Popup ShowLoading(
    string title,
    string message = null,
    WaitUntil waitUntil = null,
    Action onFinished = null,
    Color? titleColor = null,
    Color? messageColor = null,
    bool showCloseButton = false,
    Action onCancel = null)

public static Popup ShowLoading(
    string title,
    string message = null,
    WaitUntil waitUntil = null,
    Action onFinished = null,
    string titleHex = null,
    string messageHex = null,
    bool showCloseButton = false,
    Action onCancel = null)
```

| Parameter | Description |
|---|---|
| `waitUntil` | `UnityEngine.WaitUntil`. When it becomes true, `onFinished` is called and the popup closes. If `null`, the popup stays until you call `Close` |
| `onFinished` | Called when `waitUntil` completes |
| `showCloseButton` | Shows the corner X. Clicking it invokes `onCancel` |
| `onCancel` | Mapped to `OnNo` |

The overloads do not expose `dismissOnEscape`; use `Show` with `PopupOptions` for that.

### Parameters shared by the convenience methods

| Parameter | Description |
|---|---|
| `title`, `message` | Texts. Null or empty keeps the native template text |
| `yesText` / `acceptText` / `buttonText` | Primary button label |
| `noText` / `declineText` | Secondary button label |
| `onYes` / `onAccept` / `onDismiss` | Primary button callback |
| `onNo` / `onDecline` | Secondary or close button callback |
| `...Color` / `...Hex` | Colors for title, message, button text and button background. Hex strings go through `UIUtils.ParseColor`; an unparsable string becomes "not set" |
| `showCloseButton` | Corner X button (default `true` for two-button styles) |
| `buttonWidth`, `buttonHeight` | Button size override in pixels |
| `squishTextToFit` | Single-line button labels, auto-sized to fit |
| `dismissOnEscape` | Allow ESC to close the popup |
| `escapeChoice` | Which button ESC triggers on two-button popups |

## PopupOptions

Class used by `ZUIPopup.Show`. All members are settable properties.

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string` | `null` | Title. Null keeps the native title |
| `Message` | `string` | `null` | Message body. Null keeps the native text |
| `Style` | `PopupStyle` | `Confirmation` | Popup style |
| `NativePopupType` | `AllPopups?` | `null` | Native popup used as template. Null selects by style (see above) |
| `YesText` | `string` | `null` | Primary button label |
| `NoText` | `string` | `null` | Secondary button label |
| `OnYes` | `Action` | `null` | Primary button callback |
| `OnNo` | `Action` | `null` | Secondary or close button callback |
| `WaitUntil` | `WaitUntil` | `null` | Loading style: condition to wait for |
| `OnFinished` | `Action` | `null` | Loading style: called when `WaitUntil` completes |
| `TitleColor` | `Color?` | `null` | Title color |
| `MessageColor` | `Color?` | `null` | Message color |
| `YesTextColor` | `Color?` | `null` | Primary button text color |
| `NoTextColor` | `Color?` | `null` | Secondary button text color |
| `YesButtonColor` | `Color?` | `null` | Primary button background tint |
| `NoButtonColor` | `Color?` | `null` | Secondary button background tint |
| `ShowCloseButton` | `bool?` | `null` | Corner X. Null means `true` for `Confirmation` and `AcceptDecline`, `false` for `Alert` and `Loading` |
| `DisposeOnButtonClick` | `bool` | `true` | Close the popup when a button is clicked |
| `DismissOnEscape` | `bool` | `false` | ESC dismisses the popup |
| `EscapeChoice` | `PopupChoice?` | `null` | Button triggered by ESC on two-button popups |
| `SquishTextToFit` | `bool` | `true` | Single-line, auto-sized button labels |
| `ButtonFontSizeMin` | `float?` | `null` | Minimum font size when squishing (10 if null) |
| `ButtonFontSizeMax` | `float?` | `null` | Maximum font size when squishing (33 if null) |
| `ButtonWidth` | `float?` | `null` | Button width in pixels |
| `ButtonHeight` | `float?` | `null` | Button height in pixels |

For `AcceptDecline`, unset colors default to: Accept button green, Decline button red, both button texts white.

Write-only hex helpers. Each one parses the string with `UIUtils.ParseColor` and sets the matching `Color?` property:

| Helper | Sets |
|---|---|
| `TitleColorHex` | `TitleColor` |
| `MessageColorHex` | `MessageColor` |
| `YesTextColorHex` | `YesTextColor` |
| `NoTextColorHex` | `NoTextColor` |
| `YesButtonColorHex` | `YesButtonColor` |
| `NoButtonColorHex` | `NoButtonColor` |

## PopupChoice

```csharp
public enum PopupChoice
{
    Primary,    // Yes / OK / Accept / Confirm
    Secondary   // No / Cancel / Decline
}
```

Used by `EscapeChoice` to pick which action ESC triggers.

## ESC handling

ZUI hooks the game's ESC handling (the in-game `CalculateEscape` and the main menu tab controller). While a ZUI popup is on top:

- `DismissOnEscape = false` (default): ESC is consumed and the popup stays open.
- `DismissOnEscape = true`: ESC clicks one of the buttons. `EscapeChoice` picks which: `Primary` clicks the Yes button, `Secondary` clicks the No button. The matching callback runs (`OnYes` / `OnNo`) and the popup closes as for a normal click.
- If `EscapeChoice` is null, `Secondary` is used. On two-button popups a warning is written to the log when `DismissOnEscape` is true and `EscapeChoice` is not set.
- On one-button popups (`Alert`, `Info`), vanilla's `ClickNo_Btn` automatically forwards to `ClickYes_Btn()` when no secondary button exists, so `dismissOnEscape: true` cleanly dismisses the alert without extra configuration.
- In every case, `ZUIPopup.LastEscConsumedFrame` is set so the same key press does not also close a drawer, panel, screen or the pause menu.
- Native popups that were not created by ZUI keep vanilla behavior. ZUI only records the frame.

## ZUIPopupBehavior

```csharp
public class ZUIPopupBehavior : MonoBehaviour
{
    public PopupOptions Options { get; set; }
}
```

Component added to every popup created through `ZUIPopup.Show`. It stores the `PopupOptions` used, and the ESC handling reads `Options.DismissOnEscape` and `Options.EscapeChoice` from it. It is also how ZUI tells its own popups from native ones.

```csharp
var popup = ZUIPopup.ShowInfo("Title", "Text");
var behavior = popup.GetComponent<ZUIPopupBehavior>();
bool escCloses = behavior.Options.DismissOnEscape;
```
