using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ZUI.Dialogue
{
    /// <summary>
    /// Public API for triggering native stylized bottom dialogue subtitles with custom speakers, colors, and animations.
    /// </summary>
    public static class ZUISubtitle
    {
        private static FieldInfo _subtitlesParentField;
        private static FieldInfo _subtitlePrefabField;
        private static bool _reflectionCached = false;

        private static bool EnsureReflection()
        {
            if (_reflectionCached) return true;

            try
            {
                _subtitlesParentField = typeof(InGameUI).GetField("subtitlesParent", BindingFlags.Instance | BindingFlags.NonPublic);
                _subtitlePrefabField = typeof(InGameUI).GetField("subtitlePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                _reflectionCached = true;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ZUISubtitle] Failed to reflect InGameUI subtitle fields: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Displays an animated bottom dialogue subtitle with custom speaker name, text, and color.
        /// </summary>
        /// <param name="speaker">The speaker name displayed before the text (e.g. "Jimmy"). If null or empty, only the message is displayed.</param>
        /// <param name="message">The dialogue body text.</param>
        /// <param name="speakerColor">The color used for the speaker name tag.</param>
        /// <param name="duration">How long the subtitle remains on screen in seconds (default: 5.0s, or auto if 0 with audio).</param>
        /// <param name="audioClip">Optional voice line audio clip to play.</param>
        /// <param name="audioVolume">Volume for the voice line (0.0 to 1.0).</param>
        /// <returns>The spawned subtitle GameObject, or null if failed.</returns>
        public static GameObject Show(string speaker, string message, Color speakerColor, float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f)
        {
            if (string.IsNullOrEmpty(message)) return null;

            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI == null)
            {
                Plugin.Log.LogWarning("[ZUISubtitle] Cannot display subtitle: InGameUI instance is not active (not in-game).");
                return null;
            }

            if (!EnsureReflection()) return null;

            var subtitlesParent = _subtitlesParentField?.GetValue(inGameUI) as Transform;
            var subtitlePrefab = _subtitlePrefabField?.GetValue(inGameUI) as GameObject;

            if (subtitlesParent == null || subtitlePrefab == null)
            {
                Plugin.Log.LogWarning("[ZUISubtitle] InGameUI subtitlesParent or subtitlePrefab is null.");
                return null;
            }

            // Play voice audio if provided
            if (audioClip != null)
            {
                try
                {
                    InitUI.PlayUISFX(audioClip, Mathf.Clamp01(audioVolume));
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[ZUISubtitle] Could not play audio clip: {ex.Message}");
                }

                if (duration <= 0f && audioClip.length > 0f)
                {
                    duration = audioClip.length + 0.35f;
                }
            }

            if (duration <= 0f)
            {
                duration = 5.0f;
            }

            // Guarantee a readable minimum floor
            duration = Mathf.Max(2.5f, duration);

            GameObject subtitleObj = null;
            try
            {
                subtitleObj = UnityEngine.Object.Instantiate(subtitlePrefab, subtitlesParent);
                var textComponent = subtitleObj.GetComponentInChildren<TMP_Text>();

                if (textComponent != null)
                {
                    FontManager.ApplyGameFontToCurrent(textComponent);

                    if (!string.IsNullOrEmpty(speaker))
                    {
                        string hexColor = ColorUtility.ToHtmlStringRGB(speakerColor);
                        textComponent.text = $"<color=#{hexColor}>{speaker}</color>: {message}";
                    }
                    else
                    {
                        textComponent.text = message;
                    }

                    if (textComponent.transform.parent is RectTransform parentRect)
                    {
                        parentRect.sizeDelta = new Vector2(textComponent.preferredWidth, parentRect.sizeDelta.y);
                    }
                }

                // Scale Animator speed so the native 5.0s animation curve matches the exact requested duration
                var animator = subtitleObj.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.speed = 5.0f / duration;
                    animator.Update(0f);
                }

                // Attach lifecycle manager to strictly prevent the 5s animation from looping
                var lifecycle = subtitleObj.AddComponent<ZUISubtitleLifecycle>();
                lifecycle.Lifetime = duration;
                lifecycle.CanvasGroup = subtitleObj.GetComponentInChildren<CanvasGroup>();

                UnityEngine.Object.Destroy(subtitleObj, duration + 0.05f);
                return subtitleObj;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ZUISubtitle] Error instantiating subtitle: {ex}");
                if (subtitleObj != null)
                {
                    UnityEngine.Object.Destroy(subtitleObj);
                }
                return null;
            }
        }

        /// <summary>
        /// Displays an animated bottom dialogue subtitle with custom speaker color specified as a hex string (e.g. "#4FC3F7").
        /// </summary>
        public static GameObject Show(string speaker, string message, string speakerHex = "#FFD54F", float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f)
        {
            Color color = new Color(1f, 0.835f, 0.31f); // Default #FFD54F
            if (!string.IsNullOrEmpty(speakerHex))
            {
                string hex = speakerHex.StartsWith("#") ? speakerHex : "#" + speakerHex;
                if (!ColorUtility.TryParseHtmlString(hex, out color))
                {
                    color = new Color(1f, 0.835f, 0.31f);
                }
            }

            return Show(speaker, message, color, duration, audioClip, audioVolume);
        }

        /// <summary>
        /// Displays an animated bottom dialogue subtitle without a speaker prefix (narrator / environmental speech).
        /// </summary>
        public static GameObject Show(string message, float duration = 5.0f, AudioClip audioClip = null, float audioVolume = 1.0f)
        {
            return Show(null, message, Color.white, duration, audioClip, audioVolume);
        }

        /// <summary>
        /// Displays an animated dialogue subtitle using localized keys for speaker and message.
        /// </summary>
        public static GameObject ShowLocalized(string speakerKey, string messageKey, Color speakerColor, float duration = 5.0f, params string[] messageArgs)
        {
            string speaker = string.Empty;
            if (!string.IsNullOrEmpty(speakerKey))
            {
                try { speaker = new LocalizedString(speakerKey).SimpleLocalize(); }
                catch { speaker = speakerKey; }
            }

            string message = messageKey;
            if (!string.IsNullOrEmpty(messageKey))
            {
                try { message = new LocalizedString(messageKey, messageArgs ?? Array.Empty<string>()).SimpleLocalize(); }
                catch { message = messageKey; }
            }

            return Show(speaker, message, speakerColor, duration);
        }

        /// <summary>
        /// Clears all currently visible dialogue subtitles immediately.
        /// </summary>
        public static void ClearAll()
        {
            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI == null || !EnsureReflection()) return;

            if (_subtitlesParentField?.GetValue(inGameUI) is Transform subtitlesParent && subtitlesParent != null)
            {
                for (int i = subtitlesParent.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(subtitlesParent.GetChild(i).gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Enforces the exact lifetime of a subtitle object and guarantees no animation looping occurs.
    /// </summary>
    public class ZUISubtitleLifecycle : MonoBehaviour
    {
        public float Lifetime = 5f;
        public CanvasGroup CanvasGroup;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= Lifetime)
            {
                if (CanvasGroup != null) CanvasGroup.alpha = 0f;
                Destroy(gameObject);
            }
        }
    }
}
