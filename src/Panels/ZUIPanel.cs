using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Settings;

namespace ZUI.Panels
{
    /// <summary>
    /// Represents a standalone full-screen official mod settings/modal panel (Type 2).
    /// Cloned directly from InGameUICanvas/settingsPanel, sharing 100% official styling,
    /// shaders, controls, typography, and widgets.
    /// </summary>
    public class ZUIPanel
    {
        private static readonly Dictionary<string, ZUIPanel> _registry = new Dictionary<string, ZUIPanel>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, ZUIPanel> Registry => _registry;
        public static ZUIPanel CurrentOpenPanel => ZUIPanelController.CurrentOpenPanel;

        public string Id { get; }
        public string Title { get; set; }
        public SettingContainer Container { get; } = new SettingContainer();

        public IReadOnlyList<ISettingItem> Items => Container.Items;
        public bool HasPendingChanges => Container.HasPendingChanges;

        /// <summary>
        /// If true, spawns a dedicated "REVERT" button next to "APPLY". Defaults to false (vanilla style).
        /// </summary>
        public bool ShowRevertButton { get; set; } = false;

        public Action OnOpened { get; set; }
        public Action OnClosed { get; set; }
        public Action OnApplied { get; set; }

        public GameObject ClonedObject { get; internal set; }
        public Button ApplyButton { get; internal set; }
        public Button CloseButton { get; internal set; }
        public Button RevertButton { get; internal set; }
        public TMP_Text TitleText { get; internal set; }

        public ZUIPanel(string id, string title)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? string.Empty;

            Container.OnChangesMade += OnContainerChangesMade;
        }

        private void OnContainerChangesMade()
        {
            if (ApplyButton != null)
            {
                ApplyButton.interactable = true;
            }
            if (RevertButton != null)
            {
                RevertButton.interactable = true;
            }
        }

        public void NotifyChangesMade()
        {
            Container.NotifyChangesMade();
        }

        #region Static Registry & Lifecycle

        public static void Initialize(MonoBehaviour host)
        {
            ZUIPanelController.EnsureInitialized(host);
        }

        public static ZUIPanel Register(string id, string title)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            if (_registry.TryGetValue(id, out var existing))
            {
                existing.Title = title;
                return existing;
            }

            var panel = new ZUIPanel(id, title);
            _registry[id] = panel;
            return panel;
        }

        public static ZUIPanel Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var panel);
            return panel;
        }

        public static bool Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_registry.TryGetValue(id, out var panel))
            {
                if (CurrentOpenPanel == panel)
                {
                    panel.Close();
                }
                if (panel.ClonedObject != null)
                {
                    UnityEngine.Object.Destroy(panel.ClonedObject);
                    panel.ClonedObject = null;
                }
                _registry.Remove(id);
                return true;
            }
            return false;
        }

        public static void Open(string id)
        {
            var panel = Get(id);
            if (panel == null)
            {
                Plugin.Log.LogError($"[ZUIPanel] Cannot open panel '{id}': not registered.");
                return;
            }
            panel.Open();
        }

        public static void Open(ZUIPanel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            panel.Open();
        }

        public static void CloseCurrent()
        {
            CurrentOpenPanel?.Close();
        }

        public static void CloseActive()
        {
            CloseCurrent();
        }

        public static void CloseAll()
        {
            CloseCurrent();
        }

        #endregion

        #region Instance Control

        public void Open()
        {
            ZUIPanelController.OpenPanel(this);
        }

        public void Close()
        {
            ZUIPanelController.ClosePanel(this);
        }

        public void Apply()
        {
            Container.ApplyPending();
            if (ApplyButton != null)
            {
                ApplyButton.interactable = false;
                ResetButtonVisuals(ApplyButton);
            }
            if (RevertButton != null)
            {
                RevertButton.interactable = false;
                ResetButtonVisuals(RevertButton);
            }
            OnApplied?.Invoke();
        }

        public void Revert()
        {
            Container.RevertPending();
            if (ApplyButton != null)
            {
                ApplyButton.interactable = false;
                ResetButtonVisuals(ApplyButton);
            }
            if (RevertButton != null)
            {
                RevertButton.interactable = false;
                ResetButtonVisuals(RevertButton);
            }
        }

        internal static void ResetButtonVisuals(Button button)
        {
            if (button == null) return;

            var anim = button.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                anim.CrossFadeInFixedTime("Exit", 0.05f, 0);
            }

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == button.gameObject)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void RefreshUI()
        {
            Container.RefreshUI();
        }

        public void AddItem(ISettingItem item)
        {
            Container.AddItem(item);
        }

        #endregion

        #region Fluent Widget Builder Extensions

        public ToggleSettingItem AddToggle(
            string id,
            string label,
            bool defaultValue,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
            => Container.AddToggle(id, label, defaultValue, onApply, onChanged);

        public SliderSettingItem AddSlider(
            string id,
            string label,
            float minValue,
            float maxValue,
            float defaultValue,
            bool wholeNumbers = false,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
            => Container.AddSlider(id, label, minValue, maxValue, defaultValue, wholeNumbers, valueFormatter, onApply, onChanged);

        public DropdownSettingItem AddDropdown(
            string id,
            string label,
            IList<string> options,
            int defaultIndex = 0,
            Action<int> onApply = null,
            Action<int> onChanged = null)
            => Container.AddDropdown(id, label, options, defaultIndex, onApply, onChanged);

        public DropdownSettingItem AddDropdown<TEnum>(
            string id,
            string label,
            TEnum defaultValue = default,
            Action<TEnum> onApply = null,
            Action<TEnum> onChanged = null) where TEnum : struct, Enum
            => Container.AddDropdown(id, label, defaultValue, onApply, onChanged);

        public HeaderSettingItem AddHeader(string title) => Container.AddHeader(title);
        public HeaderSettingItem AddHeader(string title, Color color) => Container.AddHeader(title, color);
        public HeaderSettingItem AddHeader(string title, string hexColor) => Container.AddHeader(title, hexColor);
        public HeaderSettingItem AddHeader(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
            => Container.AddHeader(id, title, color, font, fontSize, fontStyle, height);
        public HeaderSettingItem AddHeader(string id, string title, string hexColor) => Container.AddHeader(id, title, hexColor);

        public TextSettingItem AddCategory(string title) => Container.AddCategory(title);
        public TextSettingItem AddCategory(string title, Color color) => Container.AddCategory(title, color);
        public TextSettingItem AddCategory(string title, string hexColor) => Container.AddCategory(title, hexColor);
        public TextSettingItem AddCategory(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
            => Container.AddCategory(id, title, color, font, fontSize, fontStyle, height);
        public TextSettingItem AddCategory(string id, string title, string hexColor) => Container.AddCategory(id, title, hexColor);

        public TextSettingItem AddText(string text) => Container.AddText(text);
        public TextSettingItem AddText(string text, Color color) => Container.AddText(text, color);
        public TextSettingItem AddText(string text, string hexColor) => Container.AddText(text, hexColor);
        public TextSettingItem AddText(
            string id,
            string text,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            TextAlignmentOptions? alignment = null,
            float? height = null)
            => Container.AddText(id, text, color, font, fontSize, fontStyle, alignment, height);
        public TextSettingItem AddText(string id, string text, string hexColor) => Container.AddText(id, text, hexColor);

        public SpaceSettingItem AddSpace(float height = 15f) => Container.AddSpace(height);
        public SpaceSettingItem AddSpacer(float height = 15f) => Container.AddSpacer(height);
        public DividerSettingItem AddDivider(string id = null) => Container.AddDivider(id);

        public ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick)
            => Container.AddButton(id, label, buttonText, onClick);

        public ButtonSettingItem AddButton(
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
            => Container.AddButton(id, label, buttonText, onClick, width, height, alignment, buttonHex, textHex, fontSize, squishTextToFit);

        public ButtonSettingItem AddButton(ButtonSettingItem item)
            => Container.AddButton(item);

        #endregion
    }
}
