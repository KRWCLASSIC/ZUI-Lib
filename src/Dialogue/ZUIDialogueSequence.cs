using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZUI.Dialogue
{
    /// <summary>
    /// Orchestrates multi-line conversation sequences between characters, with automatic timing,
    /// voice line playback, speaker colors, and completion callbacks.
    /// </summary>
    public class ZUIDialogueSequence
    {
        private readonly List<DialogueLine> _lines = new List<DialogueLine>();
        private Coroutine _playbackRoutine;
        private MonoBehaviour _coroutineHost;
        private GameObject _activeSubtitleObject;

        public static ZUIDialogueSequence ActiveSequence { get; private set; }

        public IReadOnlyList<DialogueLine> Lines => _lines.AsReadOnly();
        public bool IsPlaying => _playbackRoutine != null;
        public int CurrentLineIndex { get; private set; } = -1;

        public Action OnCompleted { get; set; }
        public Action<int, DialogueLine> OnLineStarted { get; set; }
        public Action<int, DialogueLine> OnLineFinished { get; set; }

        public ZUIDialogueSequence(MonoBehaviour host = null)
        {
            _coroutineHost = host != null ? host : Plugin.Instance;
        }

        public static ZUIDialogueSequence Create(MonoBehaviour host = null)
        {
            return new ZUIDialogueSequence(host);
        }

        #region Builder API

        /// <summary>
        /// Adds a dialogue line to the sequence with custom speaker name, message, color, and optional voice line.
        /// </summary>
        public ZUIDialogueSequence AddLine(
            string speaker,
            string message,
            string speakerHex = "#FFD54F",
            float duration = 0f,
            float delayBefore = 0.2f,
            AudioClip audio = null,
            string audioName = null)
        {
            _lines.Add(new DialogueLine(speaker, message, speakerHex, duration, delayBefore, audio, audioName));
            return this;
        }

        /// <summary>
        /// Adds a dialogue line to the sequence using a Unity Color object.
        /// </summary>
        public ZUIDialogueSequence AddLine(
            string speaker,
            string message,
            Color speakerColor,
            float duration = 0f,
            float delayBefore = 0.2f,
            AudioClip audio = null,
            string audioName = null)
        {
            _lines.Add(new DialogueLine(speaker, message, speakerColor, duration, delayBefore, audio, audioName));
            return this;
        }

        /// <summary>
        /// Adds a narrator / ambient speech line without a speaker header.
        /// </summary>
        public ZUIDialogueSequence AddLine(
            string message,
            float duration = 0f,
            float delayBefore = 0.2f,
            AudioClip audio = null,
            string audioName = null)
        {
            _lines.Add(new DialogueLine(null, message, Color.white, duration, delayBefore, audio, audioName));
            return this;
        }

        /// <summary>
        /// Adds an explicit silence / pause between lines.
        /// </summary>
        public ZUIDialogueSequence AddPause(float pauseSeconds)
        {
            if (_lines.Count > 0)
            {
                // Attach delay to the next line or dummy pause
                _lines.Add(new DialogueLine(null, string.Empty, Color.white, duration: 0.01f, delayBefore: pauseSeconds));
            }
            return this;
        }

        /// <summary>
        /// Adds a preconfigured DialogueLine instance.
        /// </summary>
        public ZUIDialogueSequence Add(DialogueLine line)
        {
            if (line != null) _lines.Add(line);
            return this;
        }

        /// <summary>
        /// Sets the callback invoked when the entire conversation sequence finishes.
        /// </summary>
        public ZUIDialogueSequence WithOnComplete(Action callback)
        {
            OnCompleted = callback;
            return this;
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Begins playback of the dialogue sequence. If a sequence is already playing on this instance, it restarts.
        /// </summary>
        public void Play()
        {
            if (ActiveSequence != null && ActiveSequence != this && ActiveSequence.IsPlaying)
            {
                ActiveSequence.Stop();
            }

            Stop();

            if (_lines.Count == 0) return;

            var host = _coroutineHost != null ? _coroutineHost : Plugin.Instance;
            if (host == null)
            {
                Plugin.Log.LogError("[ZUIDialogueSequence] Cannot start playback: no active MonoBehaviour host available.");
                return;
            }

            ActiveSequence = this;
            _playbackRoutine = host.StartCoroutine(PlaybackRoutine());
        }

        /// <summary>
        /// Immediately stops playback, destroying any active subtitle on screen.
        /// </summary>
        public void Stop()
        {
            if (_playbackRoutine != null)
            {
                var host = _coroutineHost != null ? _coroutineHost : Plugin.Instance;
                if (host != null)
                {
                    host.StopCoroutine(_playbackRoutine);
                }
                _playbackRoutine = null;
            }

            if (_activeSubtitleObject != null)
            {
                UnityEngine.Object.Destroy(_activeSubtitleObject);
                _activeSubtitleObject = null;
            }

            if (ActiveSequence == this)
            {
                ActiveSequence = null;
            }

            CurrentLineIndex = -1;
        }

        private IEnumerator PlaybackRoutine()
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                CurrentLineIndex = i;
                DialogueLine line = _lines[i];

                // 1. Pre-line delay
                if (line.DelayBefore > 0f)
                {
                    yield return new WaitForSeconds(line.DelayBefore);
                }

                // If empty pause line, skip display
                if (string.IsNullOrEmpty(line.Message))
                {
                    continue;
                }

                // 2. Resolve Audio
                AudioClip clipToPlay = line.Audio;
                if (clipToPlay == null && !string.IsNullOrEmpty(line.AudioName))
                {
                    clipToPlay = FindAudioClip(line.AudioName);
                }

                // 3. Calculate display duration
                float displayDuration = line.Duration;
                if (displayDuration <= 0f)
                {
                    if (clipToPlay != null && clipToPlay.length > 0f)
                    {
                        displayDuration = clipToPlay.length + 0.35f;
                    }
                    else
                    {
                        int wordCount = line.Message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        displayDuration = Mathf.Clamp(wordCount * 0.35f + 1.2f, 3.0f, 7.5f);
                    }
                }
                displayDuration = Mathf.Max(2.5f, displayDuration);

                // 4. Trigger line callbacks
                line.OnLineStart?.Invoke();
                OnLineStarted?.Invoke(i, line);

                // 5. Play voice audio if present
                if (clipToPlay != null)
                {
                    try
                    {
                        InitUI.PlayUISFX(clipToPlay, Mathf.Clamp01(line.AudioVolume));
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"[ZUIDialogueSequence] Could not play audio clip '{clipToPlay.name}': {ex.Message}");
                    }
                }

                // 6. Spawn subtitle banner (clear previous line if still hanging)
                if (_activeSubtitleObject != null)
                {
                    UnityEngine.Object.Destroy(_activeSubtitleObject);
                    _activeSubtitleObject = null;
                }
                _activeSubtitleObject = ZUISubtitle.Show(line.Speaker, line.Message, line.SpeakerColor, displayDuration);

                // 7. Wait for display duration (the subtitle will naturally finish its scaled fade-out and destroy itself)
                yield return new WaitForSeconds(displayDuration);
                _activeSubtitleObject = null;

                OnLineFinished?.Invoke(i, line);
            }

            _playbackRoutine = null;
            CurrentLineIndex = -1;
            if (ActiveSequence == this) ActiveSequence = null;

            OnCompleted?.Invoke();
        }

        /// <summary>
        /// Searches currently loaded Unity audio assets for a clip matching the specified name.
        /// </summary>
        public static AudioClip FindAudioClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;

            try
            {
                var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null && clips[i].name.Equals(clipName, StringComparison.OrdinalIgnoreCase))
                    {
                        return clips[i];
                    }
                }
            }
            catch { }

            return null;
        }

        #endregion
    }
}
