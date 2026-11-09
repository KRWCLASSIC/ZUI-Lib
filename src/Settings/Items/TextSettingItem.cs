using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.Settings
{
    /// <summary>
    /// A text-only settings item used to display category headers, descriptions, notes, or section titles.
    /// Uses strict reflection against the game's ToggleSetting template with all interactive elements removed.
    /// Supports rich text, customizable color (Color or hex), font asset, font size, font style, alignment, and row height.
    /// </summary>
    public class TextSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public GameObject SpawnedObject { get; private set; }
        public bool HasPendingChanges => false;

        public Color? Color { get; set; }
        public TMP_FontAsset Font { get; set; }
        public float? FontSize { get; set; }
        public FontStyles FontStyle { get; set; } = FontStyles.Normal;
        public TextAlignmentOptions? Alignment { get; set; }
        public float Height { get; set; }

        private TMP_Text _labelText;

        public TextSettingItem(
            string id,
            string text,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            TextAlignmentOptions? alignment = null,
            float? height = null)
        {
            Id = id;
            Label = text;
            Color = color;
            Font = font;
            FontSize = fontSize;
            FontStyle = fontStyle ?? FontStyles.Normal;
            Alignment = alignment;
            Height = height ?? 38f;
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.ToggleTemplate == null)
            {
                throw new InvalidOperationException($"[ZUI] Cannot instantiate {nameof(TextSettingItem)} '{Id}': ToggleTemplate is null.");
            }

            GameObject obj = UnityEngine.Object.Instantiate(templates.ToggleTemplate.gameObject, parent, false);
            obj.name = Id;
            SpawnedObject = obj;

            var toggleSetting = obj.GetComponent<ToggleSetting>();
            if (toggleSetting == null)
            {
                throw new InvalidOperationException($"[ZUI] Toggle template '{templates.ToggleTemplate.name}' is missing the ToggleSetting component.");
            }

            var nameField = typeof(GameSetting).GetField("settingNameText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(GameSetting), "settingNameText");
            _labelText = (TMP_Text)nameField.GetValue(toggleSetting)
                ?? throw new InvalidOperationException($"[ZUI] Toggle template '{toggleSetting.name}' has null settingNameText.");

            var toggleField = typeof(ToggleSetting).GetField("toggle", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(ToggleSetting), "toggle");
            var toggle = (Toggle)toggleField.GetValue(toggleSetting)
                ?? throw new InvalidOperationException($"[ZUI] Toggle template '{toggleSetting.name}' has null toggle.");

            // Hide the toggle checkbox graphics specifically, NEVER deactivate obj or parent of _labelText!
            if (toggle.gameObject != obj && !_labelText.transform.IsChildOf(toggle.transform))
            {
                toggle.gameObject.SetActive(false);
            }
            else
            {
                if (toggle.graphic != null && toggle.graphic != _labelText)
                {
                    toggle.graphic.gameObject.SetActive(false);
                }
                if (toggle.targetGraphic != null && toggle.targetGraphic != _labelText)
                {
                    toggle.targetGraphic.gameObject.SetActive(false);
                }
            }

            // Remove GameSetting and Toggle component scripts
            UnityEngine.Object.DestroyImmediate(toggleSetting);
            UnityEngine.Object.DestroyImmediate(toggle);

            // Remove any LocalizedTextUI components to prevent game localization from overwriting custom label
            var locs = obj.GetComponentsInChildren<LocalizedTextUI>(true);
            for (int i = 0; i < locs.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(locs[i]);
            }

            // Ensure the root row and the text label are completely active and visible
            obj.SetActive(true);
            _labelText.gameObject.SetActive(true);
            _labelText.enabled = true;
            _labelText.richText = true;

            // Apply text styling
            _labelText.text = Label;

            if (Color.HasValue)
            {
                _labelText.color = Color.Value;
            }
            else
            {
                // Ensure text is fully opaque white
                var c = _labelText.color;
                if (c.a <= 0.01f) c.a = 1f;
                _labelText.color = c;
            }

            // Apply custom font if provided; otherwise safely apply native game font
            if (Font != null)
            {
                _labelText.font = Font;
            }
            else
            {
                UIUtils.ApplyFontSafe(_labelText);
            }

            if (FontSize.HasValue) _labelText.fontSize = FontSize.Value;
            _labelText.fontStyle = FontStyle;
            if (Alignment.HasValue) _labelText.alignment = Alignment.Value;
            _labelText.overflowMode = TextOverflowModes.Overflow;

            // Maintain natural centering: do not set asymmetrical margins
            _labelText.margin = Vector4.zero;

            // Set uniform row height via RectTransform and LayoutElement
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, Height);
            }

            var layoutElement = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = Height;
            layoutElement.minHeight = Height;

            return obj;
        }

        #region Fluent Configuration

        public TextSettingItem SetColor(Color color)
        {
            Color = color;
            if (_labelText != null) _labelText.color = color;
            return this;
        }

        public TextSettingItem SetColor(string hexColor)
        {
            var parsed = UIUtils.ParseColor(hexColor);
            if (parsed.HasValue) SetColor(parsed.Value);
            return this;
        }

        public TextSettingItem WithColor(Color color) => SetColor(color);
        public TextSettingItem WithColor(string hexColor) => SetColor(hexColor);

        public TextSettingItem WithFontSize(float size)
        {
            FontSize = size;
            if (_labelText != null) _labelText.fontSize = size;
            return this;
        }

        public TextSettingItem WithHeight(float height)
        {
            Height = height;
            if (SpawnedObject != null)
            {
                var rect = SpawnedObject.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(rect.sizeDelta.x, Height);
                var le = SpawnedObject.GetComponent<LayoutElement>();
                if (le != null) { le.preferredHeight = Height; le.minHeight = Height; }
            }
            return this;
        }

        #endregion

        public void Apply() { }
        public void Revert() { }

        public void RefreshUI()
        {
            if (_labelText != null)
            {
                _labelText.text = Label;
                if (Color.HasValue) _labelText.color = Color.Value;
                if (Font != null) _labelText.font = Font;
                if (FontSize.HasValue) _labelText.fontSize = FontSize.Value;
                _labelText.fontStyle = FontStyle;
                if (Alignment.HasValue) _labelText.alignment = Alignment.Value;
            }
        }

        public void Destroy()
        {
            if (SpawnedObject != null)
            {
                UnityEngine.Object.Destroy(SpawnedObject);
                SpawnedObject = null;
            }
            _labelText = null;
        }
    }
}
