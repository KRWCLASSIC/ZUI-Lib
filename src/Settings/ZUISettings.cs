using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZUI.Core;
using ZUI.Panels;

namespace ZUI.Settings
{
    public static class ZUISettings
    {
        private const string ContentPath = "settingsPanel/Panel/Scroll View/Viewport/Content";
        private static readonly SettingContainer _container = new SettingContainer();
        private static MonoBehaviour _coroutineHost;
        private static bool _initialized = false;

        public static SettingContainer Container => _container;
        public static IReadOnlyList<ISettingItem> Items => _container.Items;

        public static void Initialize(MonoBehaviour host)
        {
            if (_initialized) return;
            _initialized = true;
            _coroutineHost = host;

            _container.OnChangesMade += MarkChangesMade;

            SceneManager.sceneLoaded += OnSceneLoaded;
            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(InstallWhenReadyRoutine());
            }
        }

        #region Registration API

        public static ToggleSettingItem AddToggle(
            ConfigEntry<bool> configEntry,
            string label = null,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
        {
            var item = _container.AddToggle(configEntry, label, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static ToggleSettingItem AddToggle(
            string id,
            string label,
            bool defaultValue,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
        {
            var item = _container.AddToggle(id, label, defaultValue, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static SliderSettingItem AddSlider(
            ConfigEntry<float> configEntry,
            float minValue,
            float maxValue,
            bool wholeNumbers = false,
            string label = null,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
        {
            var item = _container.AddSlider(configEntry, minValue, maxValue, wholeNumbers, label, valueFormatter, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static SliderSettingItem AddSlider(
            ConfigEntry<int> configEntry,
            int minValue,
            int maxValue,
            string label = null,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
        {
            var item = _container.AddSlider(configEntry, minValue, maxValue, label, valueFormatter, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static SliderSettingItem AddSlider(
            string id,
            string label,
            float minValue,
            float maxValue,
            float defaultValue,
            bool wholeNumbers = false,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
        {
            var item = _container.AddSlider(id, label, minValue, maxValue, defaultValue, wholeNumbers, valueFormatter, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static DropdownSettingItem AddDropdown(
            string id,
            string label,
            IList<string> options,
            int defaultIndex = 0,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = _container.AddDropdown(id, label, options, defaultIndex, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static DropdownSettingItem AddDropdown(
            ConfigEntry<string> configEntry,
            IList<string> options,
            string label = null,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = _container.AddDropdown(configEntry, options, label, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static DropdownSettingItem AddDropdown(
            ConfigEntry<int> configEntry,
            IList<string> options,
            string label = null,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = _container.AddDropdown(configEntry, options, label, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        public static DropdownSettingItem AddDropdown<TEnum>(
            ConfigEntry<TEnum> configEntry,
            string label = null,
            Action<TEnum> onApply = null,
            Action<TEnum> onChanged = null) where TEnum : struct, Enum
        {
            var item = _container.AddDropdown(configEntry, label, onApply, onChanged);
            InstallItemIfOpen(item);
            return item;
        }

        #region Headers, Categories and Text

        public static HeaderSettingItem AddHeader(string title)
        {
            var item = _container.AddHeader(title);
            InstallItemIfOpen(item);
            return item;
        }

        public static HeaderSettingItem AddHeader(string title, Color color)
        {
            var item = _container.AddHeader(title, color);
            InstallItemIfOpen(item);
            return item;
        }

        public static HeaderSettingItem AddHeader(string title, string hexColor)
        {
            var item = _container.AddHeader(title, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static HeaderSettingItem AddHeader(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
        {
            var item = _container.AddHeader(id, title, color, font, fontSize, fontStyle, height);
            InstallItemIfOpen(item);
            return item;
        }

        public static HeaderSettingItem AddHeader(string id, string title, string hexColor)
        {
            var item = _container.AddHeader(id, title, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddCategory(string title)
        {
            var item = _container.AddCategory(title);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddCategory(string title, Color color)
        {
            var item = _container.AddCategory(title, color);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddCategory(string title, string hexColor)
        {
            var item = _container.AddCategory(title, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddCategory(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
        {
            var item = _container.AddCategory(id, title, color, font, fontSize, fontStyle, height);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddCategory(string id, string title, string hexColor)
        {
            var item = _container.AddCategory(id, title, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddText(string text)
        {
            var item = _container.AddText(text);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddText(string text, Color color)
        {
            var item = _container.AddText(text, color);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddText(string text, string hexColor)
        {
            var item = _container.AddText(text, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddText(
            string id,
            string text,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            TextAlignmentOptions? alignment = null,
            float? height = null)
        {
            var item = _container.AddText(id, text, color, font, fontSize, fontStyle, alignment, height);
            InstallItemIfOpen(item);
            return item;
        }

        public static TextSettingItem AddText(string id, string text, string hexColor)
        {
            var item = _container.AddText(id, text, hexColor);
            InstallItemIfOpen(item);
            return item;
        }

        public static SpaceSettingItem AddSpace(float height = 15f)
        {
            var item = _container.AddSpace(height);
            InstallItemIfOpen(item);
            return item;
        }

        public static SpaceSettingItem AddSpacer(float height = 15f) => AddSpace(height);

        public static DividerSettingItem AddDivider(string id = null)
        {
            var item = _container.AddDivider(id);
            InstallItemIfOpen(item);
            return item;
        }

        #endregion

        #region Buttons

        public static ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick)
        {
            var item = _container.AddButton(id, label, buttonText, onClick);
            InstallItemIfOpen(item);
            return item;
        }

        public static ButtonSettingItem AddButton(
            string id,
            string label,
            string buttonText,
            Action onClick,
            float? width = null,
            float? height = null,
            ButtonAlignment alignment = ButtonAlignment.Right,
            string buttonHex = null,
            string textHex = null,
            float? fontSize = null,
            bool squishTextToFit = true)
        {
            var item = _container.AddButton(id, label, buttonText, onClick, width, height, alignment, buttonHex, textHex, fontSize, squishTextToFit);
            InstallItemIfOpen(item);
            return item;
        }

        public static ButtonSettingItem AddButton(ButtonSettingItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _container.AddItem(item);
            InstallItemIfOpen(item);
            return item;
        }

        public static void AddItem(ISettingItem item)
        {
            if (item == null) return;
            if (!_container.Contains(item))
            {
                _container.AddItem(item);
            }
            InstallItemIfOpen(item);
        }

        private static void InstallItemIfOpen(ISettingItem item)
        {
            Transform content = FindContentTransform();
            if (content != null)
            {
                Transform canvas = FindCanvas()?.transform;
                var templates = SettingsTemplates.Discover(content, canvas);
                if (content.Find(item.Id) == null)
                {
                    item.Instantiate(content, templates);
                }
            }
        }

        #endregion

        #endregion

        #region Installation and Lifecycle

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(InstallWhenReadyRoutine());
            }
        }

        public static void OnSettingsOpened()
        {
            TryInstallElements();
            RefreshUI();
            SetApplyButtonInteractable(false);
        }

        public static IEnumerator InstallWhenReadyRoutine()
        {
            for (int i = 0; i < 120; i++)
            {
                if (TryInstallElements())
                    yield break;
                yield return null;
            }
        }

        public static bool TryInstallElements()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "SplashScreen" || string.IsNullOrEmpty(activeScene.name))
                return false;

            GameObject canvas = FindCanvas();
            if (canvas == null) return false;

            Transform content = canvas.transform.Find(ContentPath);
            if (content == null) return false;

            var templates = SettingsTemplates.Discover(content, canvas.transform);

            foreach (var item in _container.Items)
            {
                if (content.Find(item.Id) == null)
                {
                    item.Instantiate(content, templates);
                }
            }

            return true;
        }

        public static void MarkChangesMade()
        {
            if (ZUIPanel.CurrentOpenPanel != null)
            {
                ZUIPanel.CurrentOpenPanel.NotifyChangesMade();
                return;
            }

            SetApplyButtonInteractable(true);

            // Inform InitUI of pending changes so vanilla discard popup triggers if user cancels
            if (StaticInstance<InitUI>.Instance != null)
            {
                try
                {
                    var field = typeof(InitUI).GetField("changesMade", BindingFlags.Instance | BindingFlags.NonPublic);
                    field?.SetValue(StaticInstance<InitUI>.Instance, true);
                }
                catch { }
            }
        }

        public static void SetApplyButtonInteractable(bool interactable)
        {
            // 1. Through InitUI instance
            if (StaticInstance<InitUI>.Instance != null)
            {
                try
                {
                    var btnField = typeof(InitUI).GetField("applyButton", BindingFlags.Instance | BindingFlags.NonPublic);
                    var btn = btnField?.GetValue(StaticInstance<InitUI>.Instance) as Button;
                    if (btn != null)
                    {
                        btn.interactable = interactable;
                        return;
                    }
                }
                catch { }
            }

            // 2. Direct search in canvas
            GameObject canvas = FindCanvas();
            if (canvas != null)
            {
                foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
                {
                    if (button.gameObject.name == "ApplyButton")
                    {
                        button.interactable = interactable;
                        return;
                    }
                }
            }
        }

        public static void ApplyPending()
        {
            _container.ApplyPending();
            SetApplyButtonInteractable(false);
        }

        public static void RevertPending()
        {
            _container.RevertPending();
            SetApplyButtonInteractable(false);
        }

        public static void RefreshUI()
        {
            _container.RefreshUI();
        }

        public static Transform FindContentTransform()
        {
            GameObject canvas = FindCanvas();
            return canvas == null ? null : canvas.transform.Find(ContentPath);
        }

        public static GameObject FindCanvas()
        {
            GameObject canvas = GameObject.Find("InGameUICanvas");
            if (canvas != null) return canvas;

            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == "InGameUICanvas" && candidate.scene.IsValid())
                    return candidate;
            }
            return null;
        }

        public static void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            _container.DestroyItems();
            _coroutineHost = null;
            _initialized = false;
        }

        #endregion
    }
}
