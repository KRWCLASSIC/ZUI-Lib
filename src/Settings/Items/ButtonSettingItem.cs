using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.Settings
{
    /// <summary>
    /// Configurable interactive button item inside a ZUI settings category.
    /// Supports customizable dimensions (width/height), layout alignment (Right, Left, Center, Fill),
    /// custom button/text/hover colors, font sizing, and optional text squish auto-fitting.
    /// </summary>
    public class ButtonSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public string ButtonText { get; set; }
        public Action OnClick { get; set; }

        #region Customization Properties

        /// <summary>
        /// Explicit button width in pixels. If null, defaults to 260px (or full width when Alignment is Fill).
        /// </summary>
        public float? Width { get; set; }

        /// <summary>
        /// Explicit button height in pixels. If null, defaults to 44px.
        /// </summary>
        public float? Height { get; set; }

        /// <summary>
        /// Button layout alignment inside the setting row.
        /// Default is <see cref="ButtonAlignment.Center"/> (centered together like native checkboxes).
        /// </summary>
        public ButtonAlignment Alignment { get; set; } = ButtonAlignment.Center;

        /// <summary>
        /// When true, stretches the button across the entire setting row.
        /// Equivalent to setting <see cref="Alignment"/> to <see cref="ButtonAlignment.Fill"/>.
        /// </summary>
        public bool FullWidth
        {
            get => Alignment == ButtonAlignment.Fill;
            set => Alignment = value ? ButtonAlignment.Fill : ButtonAlignment.Center;
        }

        /// <summary>
        /// Optional custom background tint color for the button.
        /// </summary>
        public Color? ButtonColor { get; set; }

        /// <summary>
        /// Optional custom text color for the button label.
        /// </summary>
        public Color? TextColor { get; set; }

        /// <summary>
        /// Optional custom hover color for interactive feedback.
        /// </summary>
        public Color? HoverColor { get; set; }

        /// <summary>
        /// Optional custom pressed color for click feedback.
        /// </summary>
        public Color? PressedColor { get; set; }

        /// <summary>
        /// Optional custom color for the setting row label on the left.
        /// </summary>
        public Color? LabelColor { get; set; }

        /// <summary>
        /// Base/maximum font size for the button label (default: 20pt).
        /// </summary>
        public float? FontSize { get; set; }

        /// <summary>
        /// Minimum font size when <see cref="SquishTextToFit"/> is enabled (default: 10pt).
        /// </summary>
        public float? FontSizeMin { get; set; }

        /// <summary>
        /// Maximum font size when <see cref="SquishTextToFit"/> is enabled (default: 22pt).
        /// </summary>
        public float? FontSizeMax { get; set; }

        /// <summary>
        /// When true (default), forces button text to fit on a single line by disabling word wrapping
        /// and auto-sizing text down to <see cref="FontSizeMin"/>.
        /// </summary>
        public bool SquishTextToFit { get; set; } = true;

        #region Hex Helpers

        public string ButtonColorHex
        {
            set => ButtonColor = UIUtils.ParseColor(value);
        }

        public string TextColorHex
        {
            set => TextColor = UIUtils.ParseColor(value);
        }

        public string HoverColorHex
        {
            set => HoverColor = UIUtils.ParseColor(value);
        }

        public string PressedColorHex
        {
            set => PressedColor = UIUtils.ParseColor(value);
        }

        public string LabelColorHex
        {
            set => LabelColor = UIUtils.ParseColor(value);
        }

        #endregion

        #endregion

        public GameObject SpawnedObject { get; private set; }
        public bool HasPendingChanges => false;

        private TMP_Text _labelText;
        private TMP_Text _buttonTextComp;
        private Button _button;

        public ButtonSettingItem(string id, string label, string buttonText, Action onClick)
        {
            Id = id;
            Label = label;
            ButtonText = buttonText;
            OnClick = onClick;
        }

        #region Fluent Builder API

        public ButtonSettingItem WithSize(float width, float height)
        {
            Width = width;
            Height = height;
            return this;
        }

        public ButtonSettingItem WithWidth(float width)
        {
            Width = width;
            return this;
        }

        public ButtonSettingItem WithHeight(float height)
        {
            Height = height;
            return this;
        }

        public ButtonSettingItem WithAlignment(ButtonAlignment alignment)
        {
            Alignment = alignment;
            return this;
        }

        public ButtonSettingItem WithFullWidth(bool fullWidth = true, float? height = null)
        {
            FullWidth = fullWidth;
            if (height.HasValue) Height = height.Value;
            return this;
        }

        public ButtonSettingItem WithColors(Color buttonColor, Color textColor)
        {
            ButtonColor = buttonColor;
            TextColor = textColor;
            return this;
        }

        public ButtonSettingItem WithColors(string buttonHex, string textHex)
        {
            ButtonColorHex = buttonHex;
            TextColorHex = textHex;
            return this;
        }

        public ButtonSettingItem WithButtonColor(Color color)
        {
            ButtonColor = color;
            return this;
        }

        public ButtonSettingItem WithButtonColor(string hex)
        {
            ButtonColorHex = hex;
            return this;
        }

        public ButtonSettingItem WithTextColor(Color color)
        {
            TextColor = color;
            return this;
        }

        public ButtonSettingItem WithTextColor(string hex)
        {
            TextColorHex = hex;
            return this;
        }

        public ButtonSettingItem WithFontSize(float fontSize, bool squish = true)
        {
            FontSize = fontSize;
            SquishTextToFit = squish;
            return this;
        }

        public ButtonSettingItem WithSquish(bool squish = true)
        {
            SquishTextToFit = squish;
            return this;
        }

        #endregion

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.ToggleTemplate == null)
            {
                throw new InvalidOperationException($"[ZUI] Cannot instantiate {nameof(ButtonSettingItem)} '{Id}': ToggleTemplate is null.");
            }

            // 1. Clone toggle setting row for the base layout
            GameObject obj = UnityEngine.Object.Instantiate(templates.ToggleTemplate.gameObject, parent, false);
            obj.name = Id;
            obj.SetActive(true);
            SpawnedObject = obj;

            // Remove native ToggleSetting and root Toggle components
            var toggleSetting = obj.GetComponent<ToggleSetting>();
            if (toggleSetting != null)
            {
                var nameField = typeof(GameSetting).GetField("settingNameText", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(nameof(GameSetting), "settingNameText");
                _labelText = (TMP_Text)nameField.GetValue(toggleSetting);
                UnityEngine.Object.DestroyImmediate(toggleSetting);
            }

            var rootToggle = obj.GetComponent<Toggle>();
            if (rootToggle != null)
            {
                UnityEngine.Object.DestroyImmediate(rootToggle);
            }

            // 2. Remove native Check box from the row
            Transform checkTransform = obj.transform.Find("Check");
            if (checkTransform != null)
            {
                UnityEngine.Object.DestroyImmediate(checkTransform.gameObject);
            }

            // 2. Remove ContentSizeFitter so row spans the full container width
            var csf = obj.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                UnityEngine.Object.DestroyImmediate(csf);
            }

            // Configure row RectTransform to span the full width of the Content container
            float effWidth = Width ?? (Alignment == ButtonAlignment.Fill ? 0f : 260f);
            float effHeight = Height ?? 44f;
            float rowHeight = Math.Max(48f, effHeight + 6f);

            var objRt = obj.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 1f);
            objRt.anchorMax = new Vector2(1f, 1f);
            objRt.pivot = new Vector2(0.5f, 1f);
            objRt.offsetMin = new Vector2(0f, objRt.offsetMin.y);
            objRt.offsetMax = new Vector2(0f, objRt.offsetMax.y);
            objRt.sizeDelta = new Vector2(0f, rowHeight);

            var rowLe = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            rowLe.preferredHeight = rowHeight;
            rowLe.minHeight = rowHeight;
            rowLe.preferredWidth = -1f;
            rowLe.flexibleWidth = 1f;

            // 3. Configure Setting Label
            var textContainer = obj.transform.Find("Text");
            if (_labelText == null && textContainer != null)
            {
                _labelText = textContainer.Find("OptionText")?.GetComponent<TMP_Text>();
            }

            bool hasLabel = !string.IsNullOrEmpty(Label);
            if (_labelText != null)
            {
                _labelText.text = Label ?? string.Empty;
                if (LabelColor.HasValue)
                {
                    _labelText.color = LabelColor.Value;
                }
                _labelText.alignment = TextAlignmentOptions.MidlineRight;
                _labelText.textWrappingMode = TextWrappingModes.NoWrap;
                _labelText.overflowMode = TextOverflowModes.Ellipsis;
                _labelText.enableAutoSizing = true;
                _labelText.fontSizeMin = 14f;
                _labelText.fontSizeMax = 28f;
                _labelText.fontSize = 28f;
                UIUtils.ApplyFontSafe(_labelText);
                _labelText.ForceMeshUpdate();

                var labelRt = _labelText.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
            }

            if (textContainer != null)
            {
                foreach (var h in textContainer.GetComponents<HorizontalLayoutGroup>()) UnityEngine.Object.DestroyImmediate(h);
                foreach (var c in textContainer.GetComponents<ContentSizeFitter>()) UnityEngine.Object.DestroyImmediate(c);

                if (!hasLabel || Alignment == ButtonAlignment.Fill)
                {
                    textContainer.gameObject.SetActive(false);
                }
                else
                {
                    textContainer.gameObject.SetActive(true);
                    float textPrefWidth = _labelText != null ? _labelText.preferredWidth : 200f;
                    float labelWidth = Mathf.Clamp(textPrefWidth + 8f, 40f, 650f);

                    var textRt = textContainer.GetComponent<RectTransform>();
                    textRt.sizeDelta = new Vector2(labelWidth, rowHeight);

                    var textLe = textContainer.GetComponent<LayoutElement>() ?? textContainer.gameObject.AddComponent<LayoutElement>();
                    textLe.preferredWidth = labelWidth;
                    textLe.minWidth = labelWidth;
                    textLe.preferredHeight = rowHeight;
                    textLe.flexibleWidth = 0f;

                    textContainer.SetSiblingIndex(0);
                }
            }

            // 4. Create the button control
            GameObject btnObj;
            if (templates.ButtonTemplate != null)
            {
                btnObj = UnityEngine.Object.Instantiate(templates.ButtonTemplate.gameObject, obj.transform, false);
            }
            else
            {
                btnObj = new GameObject(Id + "_Button");
                btnObj.transform.SetParent(obj.transform, false);
                btnObj.AddComponent<Image>();
                btnObj.AddComponent<Button>();
            }

            btnObj.name = Id + "_Btn";
            btnObj.SetActive(true);

            // Strip out ButtonAnimation & Animator to eliminate all console NullReferenceExceptions
            foreach (var ba in btnObj.GetComponentsInChildren<ButtonAnimation>(true))
            {
                UnityEngine.Object.DestroyImmediate(ba);
            }
            foreach (var anim in btnObj.GetComponentsInChildren<Animator>(true))
            {
                UnityEngine.Object.DestroyImmediate(anim);
            }

            // Strip out LocalizedTextUI so it never forces "Apply"
            foreach (var loc in btnObj.GetComponentsInChildren<LocalizedTextUI>(true))
            {
                UnityEngine.Object.DestroyImmediate(loc);
            }

            var btnRt = btnObj.GetComponent<RectTransform>() ?? btnObj.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(effWidth, effHeight);

            var btnLe = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
            if (Alignment == ButtonAlignment.Fill)
            {
                btnLe.preferredWidth = -1f;
                btnLe.flexibleWidth = 1f;
            }
            else
            {
                btnLe.preferredWidth = effWidth;
                btnLe.minWidth = effWidth;
                btnLe.flexibleWidth = 0f;
            }
            btnLe.preferredHeight = effHeight;
            btnLe.minHeight = effHeight;

            btnObj.transform.SetSiblingIndex(1);

            // 5. Configure row HorizontalLayoutGroup (centered together like checkboxes)
            var hlg = obj.GetComponent<HorizontalLayoutGroup>() ?? obj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childControlHeight = false;
            hlg.childForceExpandHeight = false;

            switch (Alignment)
            {
                case ButtonAlignment.Fill:
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.padding = new RectOffset(20, 20, 0, 0);
                    hlg.childControlWidth = true;
                    hlg.childForceExpandWidth = true;
                    break;

                case ButtonAlignment.Left:
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.padding = new RectOffset(40, 40, 0, 0);
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    break;

                case ButtonAlignment.Right:
                    hlg.childAlignment = TextAnchor.MiddleRight;
                    hlg.padding = new RectOffset(40, 40, 0, 0);
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    break;

                case ButtonAlignment.Center:
                default:
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.padding = new RectOffset(0, 0, 0, 0);
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    break;
            }

            // 6. Wire button click and interactive hover tint
            _button = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(new UnityAction(() =>
            {
                try
                {
                    StaticInstance<InitUI>.Instance.PlayClickSFX();
                }
                catch { }
                OnClick?.Invoke();
            }));
            _button.interactable = true;
            _button.transition = Selectable.Transition.ColorTint;

            var btnImage = btnObj.GetComponent<Image>() ?? btnObj.GetComponentInChildren<Image>();
            if (btnImage != null)
            {
                _button.targetGraphic = btnImage;
            }

            if (ButtonColor.HasValue)
            {
                Color normal = ButtonColor.Value;
                Color hover = HoverColor ?? Color.Lerp(normal, Color.white, 0.25f);
                Color pressed = PressedColor ?? Color.Lerp(normal, Color.black, 0.25f);

                var graphics = btnObj.GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics)
                {
                    if (g is TMP_Text) continue;
                    g.color = normal;
                }

                ColorBlock cb = _button.colors;
                cb.colorMultiplier = 1f;
                cb.normalColor = normal;
                cb.highlightedColor = hover;
                cb.pressedColor = pressed;
                cb.selectedColor = normal;
                _button.colors = cb;
            }

            // 7. Configure button text & squish auto-fit
            _buttonTextComp = btnObj.GetComponentInChildren<TMP_Text>(true);
            if (_buttonTextComp == null)
            {
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                _buttonTextComp = textObj.AddComponent<TextMeshProUGUI>();
            }

            var btnTextRt = _buttonTextComp.rectTransform;
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = new Vector2(12f, 2f);
            btnTextRt.offsetMax = new Vector2(-12f, -2f);

            _buttonTextComp.text = ButtonText ?? "Click";
            _buttonTextComp.alignment = TextAlignmentOptions.Center;
            UIUtils.ApplyFontSafe(_buttonTextComp);
            Color finalTextColor = TextColor ?? Color.white;
            _buttonTextComp.color = finalTextColor;
            _buttonTextComp.faceColor = finalTextColor;

            float maxFont = FontSize ?? FontSizeMax ?? 20f;
            float minFont = FontSizeMin ?? 10f;

            if (SquishTextToFit)
            {
                _buttonTextComp.textWrappingMode = TextWrappingModes.NoWrap;
                _buttonTextComp.overflowMode = TextOverflowModes.Ellipsis;
                _buttonTextComp.enableAutoSizing = true;
                _buttonTextComp.fontSizeMin = minFont;
                _buttonTextComp.fontSizeMax = maxFont;
                _buttonTextComp.fontSize = maxFont;
            }
            else
            {
                _buttonTextComp.textWrappingMode = TextWrappingModes.Normal;
                _buttonTextComp.overflowMode = TextOverflowModes.Overflow;
                _buttonTextComp.enableAutoSizing = false;
                _buttonTextComp.fontSize = maxFont;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)obj.transform);

            return obj;
        }

        public void Apply() { }
        public void Revert() { }

        public void RefreshUI()
        {
            if (_labelText != null)
            {
                _labelText.text = Label ?? string.Empty;
                _labelText.ForceMeshUpdate();
                if (SpawnedObject != null)
                {
                    var textContainer = SpawnedObject.transform.Find("Text");
                    if (textContainer != null)
                    {
                        float labelWidth = Mathf.Clamp(_labelText.preferredWidth + 8f, 40f, 650f);
                        var textRt = textContainer.GetComponent<RectTransform>();
                        if (textRt != null) textRt.sizeDelta = new Vector2(labelWidth, textRt.sizeDelta.y);
                        var textLe = textContainer.GetComponent<LayoutElement>();
                        if (textLe != null)
                        {
                            textLe.preferredWidth = labelWidth;
                            textLe.minWidth = labelWidth;
                        }
                    }
                }
            }
            if (_buttonTextComp != null)
            {
                _buttonTextComp.text = ButtonText ?? "Click";
                Color finalTextColor = TextColor ?? Color.white;
                _buttonTextComp.color = finalTextColor;
                _buttonTextComp.faceColor = finalTextColor;
            }
            if (SpawnedObject != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)SpawnedObject.transform);
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
            _buttonTextComp = null;
            _button = null;
        }
    }
}
