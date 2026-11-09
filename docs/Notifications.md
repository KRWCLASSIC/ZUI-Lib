# Notifications

Namespace: `ZUI.Notifications`

`ZUIInfoText` shows the game's own top-center info banner, with the game's font and its info sound. It only works in-game. If the in-game UI is not active, a warning is logged and nothing is shown.

```csharp
using ZUI.Notifications;

ZUIInfoText.Show("Checkpoint Reached!", 2.5f);
ZUIInfoText.Show("Terminal focused", 2.0f, drawOnTop: true);
```

## Members

| Member | Description |
|---|---|
| `bool IsShowing { get; }` | `true` while a banner is displayed. |
| `void Initialize(MonoBehaviour host)` | Optional. Sets the `MonoBehaviour` that runs the banner coroutine. If not set, the in-game UI object runs it. |
| `void Show(string message, float duration = 3.0f, bool playSFX = true, bool drawOnTop = false)` | Shows a banner. A null or empty `message` is ignored. |
| `void ShowLocalized(string localizationKey, float duration = 3.0f, bool playSFX = true, bool drawOnTop = false, params string[] args)` | Looks up the key with the game's localization (with `args`), then calls `Show`. If the lookup throws, the key text itself is shown. |
| `void Dismiss()` | Hides the current banner immediately. |

## Parameters

| Parameter | Description |
|---|---|
| `message` | Banner text. |
| `duration` | Seconds on screen, measured in real time. Values below 0.1 are raised to 0.1. |
| `playSFX` | Plays the game's info sound. |
| `drawOnTop` | Raises the banner's canvas sorting order to 30000, so it renders in front of the pause menu and other screens. |
| `localizationKey` | Key of a game localization string. |
| `args` | Arguments for the localized string. |

## Behavior

- Only one banner is shown at a time. Calling `Show` while a banner is visible replaces it.
- `Dismiss()` also resets the sorting override.
