using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ZUI.Notifications
{
    /// <summary>
    /// Public API for triggering native animated in-game info banners (top-center clueboard/inventory notification banner).
    /// </summary>
    public static class ZUIInfoText
    {
        private static FieldInfo _infoObjectField;
        private static FieldInfo _infoTextField;
        private static FieldInfo _infoClipField;
        private static bool _reflectionCached = false;

        private static Coroutine _activeCoroutine;
        private static MonoBehaviour _coroutineHost;

        /// <summary>
        /// Whether an info banner is currently being displayed.
        /// </summary>
        public static bool IsShowing { get; private set; }

        public static void Initialize(MonoBehaviour host)
        {
            _coroutineHost = host;
        }

        private static bool EnsureReflection()
        {
            if (_reflectionCached) return true;

            try
            {
                _infoObjectField = typeof(InGameUI).GetField("infoObject", BindingFlags.Instance | BindingFlags.NonPublic);
                _infoTextField = typeof(InGameUI).GetField("infoText", BindingFlags.Instance | BindingFlags.NonPublic);
                _infoClipField = typeof(InGameUI).GetField("infoClip", BindingFlags.Instance | BindingFlags.NonPublic);
                _reflectionCached = true;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ZUIInfoText] Failed to reflect InGameUI info banner fields: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Displays a native in-game banner with audio feedback that auto-dismisses after the specified duration.
        /// </summary>
        /// <param name="message">The text to display on the banner.</param>
        /// <param name="duration">How long the banner remains visible in seconds (default: 3.0s).</param>
        /// <param name="playSFX">Whether to play the native info ding sound (default: true).</param>
        /// <param name="drawOnTop">If true, overrides canvas sorting so the banner renders in front of the Pause Menu and all other screens.</param>
        public static void Show(string message, float duration = 3.0f, bool playSFX = true, bool drawOnTop = false)
        {
            if (string.IsNullOrEmpty(message)) return;

            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI == null)
            {
                Plugin.Log.LogWarning("[ZUIInfoText] Cannot display banner: InGameUI instance is not active (not in-game).");
                return;
            }

            if (!EnsureReflection()) return;

            var infoObject = _infoObjectField?.GetValue(inGameUI) as GameObject;
            var infoText = _infoTextField?.GetValue(inGameUI) as TMP_Text;
            var infoClip = _infoClipField?.GetValue(inGameUI) as AudioClip;

            if (infoObject == null || infoText == null)
            {
                Plugin.Log.LogWarning("[ZUIInfoText] InGameUI infoObject or infoText is null.");
                return;
            }

            // Interrupt any previous banner coroutine
            Dismiss();

            var host = _coroutineHost != null ? _coroutineHost : inGameUI;
            _activeCoroutine = host.StartCoroutine(BannerRoutine(inGameUI, infoObject, infoText, infoClip, message, duration, playSFX, drawOnTop));
        }

        /// <summary>
        /// Displays a native banner using a localized string key with optional arguments.
        /// </summary>
        public static void ShowLocalized(string localizationKey, float duration = 3.0f, bool playSFX = true, bool drawOnTop = false, params string[] args)
        {
            if (string.IsNullOrEmpty(localizationKey)) return;

            string localizedText;
            try
            {
                localizedText = new LocalizedString(localizationKey, args ?? Array.Empty<string>()).SimpleLocalize();
            }
            catch
            {
                localizedText = localizationKey;
            }

            Show(localizedText, duration, playSFX, drawOnTop);
        }

        /// <summary>
        /// Immediately dismisses and hides any active info banner.
        /// </summary>
        public static void Dismiss()
        {
            if (_activeCoroutine != null)
            {
                var host = _coroutineHost != null ? _coroutineHost : StaticInstance<InGameUI>.Instance as MonoBehaviour;
                if (host != null)
                {
                    host.StopCoroutine(_activeCoroutine);
                }
                _activeCoroutine = null;
            }

            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI != null && EnsureReflection())
            {
                if (_infoObjectField?.GetValue(inGameUI) is GameObject infoObject && infoObject != null)
                {
                    RestoreDefaultSorting(infoObject);
                    infoObject.SetActive(false);
                }
                if (_infoTextField?.GetValue(inGameUI) is TMP_Text infoText && infoText != null)
                {
                    infoText.text = string.Empty;
                }
            }

            IsShowing = false;
        }

        private static IEnumerator BannerRoutine(
            InGameUI inGameUI,
            GameObject infoObject,
            TMP_Text infoText,
            AudioClip infoClip,
            string message,
            float duration,
            bool playSFX,
            bool drawOnTop)
        {
            IsShowing = true;

            if (playSFX && infoClip != null)
            {
                InitUI.PlayUISFX(infoClip);
            }

            infoObject.SetActive(true);
            ApplySorting(infoObject, drawOnTop);
            FontManager.ApplyGameFontToCurrent(infoText);
            infoText.text = message;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));

            infoText.text = string.Empty;
            RestoreDefaultSorting(infoObject);
            infoObject.SetActive(false);

            _activeCoroutine = null;
            IsShowing = false;
        }

        private static void ApplySorting(GameObject infoObject, bool drawOnTop)
        {
            if (infoObject == null) return;

            var overrideCanvas = infoObject.GetComponent<Canvas>();
            if (drawOnTop)
            {
                if (overrideCanvas == null)
                {
                    overrideCanvas = infoObject.AddComponent<Canvas>();
                }
                overrideCanvas.overrideSorting = true;
                overrideCanvas.sortingOrder = 30000;
            }
            else if (overrideCanvas != null)
            {
                overrideCanvas.overrideSorting = false;
            }
        }

        private static void RestoreDefaultSorting(GameObject infoObject)
        {
            if (infoObject == null) return;
            var overrideCanvas = infoObject.GetComponent<Canvas>();
            if (overrideCanvas != null)
            {
                overrideCanvas.overrideSorting = false;
            }
        }
    }
}
