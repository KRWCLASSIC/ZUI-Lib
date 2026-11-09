using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using ZUI.Core;

namespace ZUI.Settings
{
    /// <summary>
    /// Reusable container for setting items (toggles, sliders, dropdowns, buttons, headers, etc.).
    /// Shared between official game settings (ZUISettings) and custom modal panels (ZUIPanel).
    /// </summary>
    public class SettingContainer
    {
        private readonly List<ISettingItem> _items = new List<ISettingItem>();

        public IReadOnlyList<ISettingItem> Items => _items.AsReadOnly();
        public bool HasPendingChanges => _items.Any(i => i.HasPendingChanges);
        public bool Contains(ISettingItem item) => item != null && _items.Contains(item);
        public bool Contains(string id) => !string.IsNullOrEmpty(id) && _items.Any(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        public event Action<ISettingItem> OnItemAdded;
        public event Action OnChangesMade;

        public void AddItem(ISettingItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
            OnItemAdded?.Invoke(item);
        }

        public void NotifyChangesMade()
        {
            OnChangesMade?.Invoke();
        }

        public void ApplyPending()
        {
            foreach (var item in _items)
            {
                item.Apply();
            }
        }

        public void RevertPending()
        {
            foreach (var item in _items)
            {
                item.Revert();
            }
        }

        public void RefreshUI()
        {
            foreach (var item in _items)
            {
                item.RefreshUI();
            }
        }

        public void DestroyItems()
        {
            foreach (var item in _items)
            {
                item.Destroy();
            }
            _items.Clear();
        }

        #region Toggle Widgets

        public ToggleSettingItem AddToggle(
            ConfigEntry<bool> configEntry,
            string label = null,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
        {
            var item = new ToggleSettingItem(configEntry, label, onApply, val =>
            {
                onChanged?.Invoke(val);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        public ToggleSettingItem AddToggle(
            string id,
            string label,
            bool defaultValue,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
        {
            var item = new ToggleSettingItem(id, label, defaultValue, onApply, val =>
            {
                onChanged?.Invoke(val);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        #endregion

        #region Slider Widgets

        public SliderSettingItem AddSlider(
            ConfigEntry<float> configEntry,
            float minValue,
            float maxValue,
            bool wholeNumbers = false,
            string label = null,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
        {
            var item = new SliderSettingItem(configEntry, minValue, maxValue, wholeNumbers, label, valueFormatter, onApply, val =>
            {
                onChanged?.Invoke(val);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        public SliderSettingItem AddSlider(
            ConfigEntry<int> configEntry,
            int minValue,
            int maxValue,
            string label = null,
            Func<float, string> valueFormatter = null,
            Action<float> onApply = null,
            Action<float> onChanged = null)
        {
            var item = new SliderSettingItem(configEntry, minValue, maxValue, label, valueFormatter, onApply, val =>
            {
                onChanged?.Invoke(val);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

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
        {
            var item = new SliderSettingItem(id, label, minValue, maxValue, defaultValue, wholeNumbers, valueFormatter, onApply, val =>
            {
                onChanged?.Invoke(val);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        #endregion

        #region Dropdown Widgets

        public DropdownSettingItem AddDropdown(
            string id,
            string label,
            IList<string> options,
            int defaultIndex = 0,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = new DropdownSettingItem(id, label, options, defaultIndex, onApply, idx =>
            {
                onChanged?.Invoke(idx);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        public DropdownSettingItem AddDropdown(
            ConfigEntry<string> configEntry,
            IList<string> options,
            string label = null,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = new DropdownSettingItem(configEntry, options, label, onApply, idx =>
            {
                onChanged?.Invoke(idx);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        public DropdownSettingItem AddDropdown(
            ConfigEntry<int> configEntry,
            IList<string> options,
            string label = null,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            var item = new DropdownSettingItem(configEntry, options, label, onApply, idx =>
            {
                onChanged?.Invoke(idx);
                NotifyChangesMade();
            });
            AddItem(item);
            return item;
        }

        public DropdownSettingItem AddDropdown<TEnum>(
            ConfigEntry<TEnum> configEntry,
            string label = null,
            Action<TEnum> onApply = null,
            Action<TEnum> onChanged = null) where TEnum : struct, Enum
        {
            string[] names = Enum.GetNames(typeof(TEnum));
            int currentIndex = Array.IndexOf(names, configEntry.Value.ToString());
            if (currentIndex < 0) currentIndex = 0;

            string id = "ZUI_Dropdown_" + configEntry.Definition.Section + "_" + configEntry.Definition.Key;
            string displayLabel = string.IsNullOrEmpty(label) ? configEntry.Definition.Key : label;

            var item = new DropdownSettingItem(
                id,
                displayLabel,
                names,
                currentIndex,
                onApply: idx =>
                {
                    if (Enum.TryParse<TEnum>(names[idx], out var parsed))
                    {
                        configEntry.Value = parsed;
                        configEntry.ConfigFile?.Save();
                        onApply?.Invoke(parsed);
                    }
                },
                onChanged: idx =>
                {
                    if (Enum.TryParse<TEnum>(names[idx], out var parsed))
                    {
                        onChanged?.Invoke(parsed);
                        NotifyChangesMade();
                    }
                }
            );

            AddItem(item);
            return item;
        }

        #endregion

        #region Headers, Categories and Text

        public HeaderSettingItem AddHeader(string title)
        {
            string id = "ZUI_Header_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddHeader(id, title, color: (Color?)null);
        }

        public HeaderSettingItem AddHeader(string title, Color color)
        {
            string id = "ZUI_Header_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddHeader(id, title, color: color);
        }

        public HeaderSettingItem AddHeader(string title, string hexColor)
        {
            string id = "ZUI_Header_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddHeader(id, title, color: UIUtils.ParseColor(hexColor));
        }

        public HeaderSettingItem AddHeader(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
        {
            var item = new HeaderSettingItem(id, title, color, font, fontSize, fontStyle, height);
            AddItem(item);
            return item;
        }

        public HeaderSettingItem AddHeader(string id, string title, string hexColor)
        {
            return AddHeader(id, title, color: UIUtils.ParseColor(hexColor));
        }

        public TextSettingItem AddCategory(string title)
        {
            string id = "ZUI_Category_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddCategory(id, title, color: (Color?)null);
        }

        public TextSettingItem AddCategory(string title, Color color)
        {
            string id = "ZUI_Category_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddCategory(id, title, color: color);
        }

        public TextSettingItem AddCategory(string title, string hexColor)
        {
            string id = "ZUI_Category_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddCategory(id, title, color: UIUtils.ParseColor(hexColor));
        }

        public TextSettingItem AddCategory(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
        {
            var item = new HeaderSettingItem(id, title, color, font, fontSize, fontStyle, height);
            AddItem(item);
            return item;
        }

        public TextSettingItem AddCategory(string id, string title, string hexColor)
        {
            return AddCategory(id, title, color: UIUtils.ParseColor(hexColor));
        }

        public TextSettingItem AddText(string text)
        {
            string id = "ZUI_Text_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddText(id, text, color: (Color?)null);
        }

        public TextSettingItem AddText(string text, Color color)
        {
            string id = "ZUI_Text_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddText(id, text, color: color);
        }

        public TextSettingItem AddText(string text, string hexColor)
        {
            string id = "ZUI_Text_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return AddText(id, text, color: UIUtils.ParseColor(hexColor));
        }

        public TextSettingItem AddText(
            string id,
            string text,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            TextAlignmentOptions? alignment = null,
            float? height = null)
        {
            var item = new TextSettingItem(id, text, color, font, fontSize, fontStyle, alignment, height);
            AddItem(item);
            return item;
        }

        public TextSettingItem AddText(string id, string text, string hexColor)
        {
            return AddText(id, text, color: UIUtils.ParseColor(hexColor));
        }

        public SpaceSettingItem AddSpace(float height = 15f)
        {
            var item = new SpaceSettingItem(height: height);
            AddItem(item);
            return item;
        }

        public SpaceSettingItem AddSpacer(float height = 15f) => AddSpace(height);

        public DividerSettingItem AddDivider(string id = null)
        {
            var item = new DividerSettingItem(id);
            AddItem(item);
            return item;
        }

        #endregion

        #region Buttons

        public ButtonSettingItem AddButton(string id, string label, string buttonText, Action onClick)
        {
            var item = new ButtonSettingItem(id, label, buttonText, onClick);
            AddItem(item);
            return item;
        }

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
        {
            var item = new ButtonSettingItem(id, label, buttonText, onClick)
            {
                Width = width,
                Height = height,
                Alignment = alignment,
                FontSize = fontSize,
                SquishTextToFit = squishTextToFit
            };
            if (!string.IsNullOrEmpty(buttonHex)) item.ButtonColorHex = buttonHex;
            if (!string.IsNullOrEmpty(textHex)) item.TextColorHex = textHex;

            AddItem(item);
            return item;
        }

        public ButtonSettingItem AddButton(ButtonSettingItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            AddItem(item);
            return item;
        }

        #endregion
    }
}
