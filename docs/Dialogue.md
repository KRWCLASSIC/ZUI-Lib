# Dialogue

Namespace: `ZUI.Dialogue`

- `ZUISubtitle` shows a single subtitle at the bottom of the screen, using the game's own subtitle prefab and animation.
- `ZUIDialogueSequence` plays a list of `DialogueLine`s one after another.
- `DialogueLine` is one line of a sequence.

Everything here only works in-game. If the in-game UI is not active, `ZUISubtitle.Show` logs a warning and returns `null`.

## ZUISubtitle

Static class.

```csharp
// Show subtitle with speaker name and custom color
public static GameObject Show(string speaker, string message, Color speakerColor, float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f);
public static GameObject Show(string speaker, string message, string speakerHex = "#FFD54F", float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f);

// Show narrator text (without speaker name)
public static GameObject Show(string message, float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f);

// Show localized subtitle from game localization keys
public static GameObject ShowLocalized(string speakerKey, string messageKey, Color speakerColor, float duration = 5.0f, params string[] messageArgs);

// Destroy all subtitles currently visible
public static void ClearAll();
```

| Parameter | Description |
|---|---|
| `speaker` | Name shown before the message, colored with `speakerColor`. If null or empty, only the message is shown. |
| `message` | Subtitle text. A null or empty message returns `null`. |
| `speakerColor`, `speakerHex` | Speaker name color. The hex string may start with `#`. An empty or invalid hex falls back to `#FFD54F`. |
| `duration` | Seconds on screen. A value of 0 or less means: the clip length plus 0.35 s if there is an audio clip, otherwise 5 s. The final value is at least 2.5 s. |
| `audioClip` | Optional voice clip, played through the game's UI sound. |
| `audioVolume` | Clip volume, clamped to 0..1. |

- `Show` returns the spawned subtitle `GameObject`, or `null` on failure. The object destroys itself when `duration` ends.
- The `Show(string message, ...)` overload is for narrator text without a speaker.
- `ShowLocalized` looks up `speakerKey` and `messageKey` with the game's localization (`messageArgs` go to the message). If a lookup throws, the key itself is used. It takes no audio parameters.
- `ClearAll()` destroys every subtitle currently on screen.

```csharp
ZUISubtitle.Show("Jimmy", "Way is clear! Move move move!", "#81C784", 4.5f);
ZUISubtitle.Show("The lights flicker.", 3f);
```

`ZUISubtitleLifecycle` is a helper `MonoBehaviour` added to each subtitle to end it on time. It has the public fields `Lifetime` and `CanvasGroup`. You do not need to use it.

## DialogueLine

| Member | Default | Description |
|---|---|---|
| `string Speaker` | `null` | Speaker name. If null or empty, the line is shown without a speaker. |
| `string Message` | `null` | Text. An empty message is treated as a pause and not displayed. |
| `Color SpeakerColor` | `#FFD54F` | Speaker name color. |
| `float Duration` | `0` | Seconds on screen. A value of 0 or less means automatic (see Timing). |
| `float DelayBefore` | `0.2` | Seconds to wait before the line starts. |
| `AudioClip Audio` | `null` | Voice clip. |
| `string AudioName` | `null` | Name of a clip to look up with `ZUIDialogueSequence.FindAudioClip` if `Audio` is null. |
| `float AudioVolume` | `1.0` | Voice volume, clamped to 0..1. |
| `Action OnLineStart` | `null` | Called when this line starts displaying. |

Constructors:

```csharp
DialogueLine()
DialogueLine(string speaker, string message, Color speakerColor, float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
DialogueLine(string speaker, string message, string speakerHex = "#FFD54F", float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
```

## ZUIDialogueSequence

Constructor and factory:

```csharp
ZUIDialogueSequence(MonoBehaviour host = null)
static ZUIDialogueSequence Create(MonoBehaviour host = null)
```

`host` runs the playback coroutine. If it is null, `Plugin.Instance` is used.

### Builder methods

All return the sequence, so calls can be chained.

```csharp
ZUIDialogueSequence AddLine(string speaker, string message, string speakerHex = "#FFD54F", float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
ZUIDialogueSequence AddLine(string speaker, string message, Color speakerColor, float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
ZUIDialogueSequence AddLine(string message, float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
ZUIDialogueSequence AddPause(float pauseSeconds)
ZUIDialogueSequence Add(DialogueLine line)
ZUIDialogueSequence WithOnComplete(Action callback)
```

- The `AddLine(string message, ...)` overload adds a line without a speaker.
- `AddPause` adds an empty line that only waits. It does nothing if the sequence has no lines yet.
- `Add` ignores a null line.
- `WithOnComplete` sets `OnCompleted`.

### Properties and events

| Member | Description |
|---|---|
| `static ZUIDialogueSequence ActiveSequence` | The sequence currently playing, or `null`. |
| `IReadOnlyList<DialogueLine> Lines` | The lines added so far. |
| `bool IsPlaying` | `true` while playing. |
| `int CurrentLineIndex` | Index of the current line, or -1 when not playing. |
| `Action OnCompleted { get; set; }` | Called after the last line finishes. It is not called when `Stop()` is used. |
| `Action<int, DialogueLine> OnLineStarted { get; set; }` | Called when a line starts, with its index. |
| `Action<int, DialogueLine> OnLineFinished { get; set; }` | Called when a line's display time ends. |

### Playback

| Method | Description |
|---|---|
| `void Play()` | Starts playback. Any other sequence that is playing is stopped, and this sequence restarts from the beginning if it was already playing. Does nothing if there are no lines. |
| `void Stop()` | Stops playback and destroys the subtitle currently on screen. |
| `static AudioClip FindAudioClip(string clipName)` | Searches all loaded `AudioClip` assets for a clip with this name (case-insensitive). Returns `null` if not found. |

### Timing

For each line, in order:

1. Wait `DelayBefore`.
2. Skip the line if `Message` is empty. The delay still applies.
3. Pick the audio: `Audio` if set, otherwise `FindAudioClip(AudioName)`.
4. Pick the display duration. If `Duration` is 0 or less, it is the clip length plus 0.35 s when there is a clip. With no clip it is `words * 0.35 + 1.2` seconds, clamped to 3.0..7.5. The result is at least 2.5 s.
5. Call `OnLineStart` and `OnLineStarted`, play the audio at `AudioVolume`, then show the subtitle.
6. Wait for the duration, then call `OnLineFinished`.

After the last line, `OnCompleted` is called.

### Example

```csharp
var seq = ZUIDialogueSequence.Create()
    .AddLine("Telerick", "Hold on, I need to check something.", "#4FC3F7")
    .AddPause(1.0f)
    .AddLine("Jimmy", "Hurry up.", "#81C784", duration: 3f, audioName: "jimmy_hurry")
    .AddLine("Static fills the room.")
    .WithOnComplete(() => ZUIInfoText.Show("Objective: Restore Power", 3.5f));

seq.OnLineStarted = (i, line) => Debug.Log($"Line {i}: {line.Message}");
seq.Play();
```
