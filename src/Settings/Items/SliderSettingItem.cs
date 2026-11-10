using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZUI.Settings
{
    public class SliderSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public GameObject SpawnedObject { get; private set; }

        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public bool WholeNumbers { get; set; }
        public Func<float, string> ValueFormatter { get; set; }

        public float CurrentValue { get; private set; }
        public float PendingValue { get; set; }

        public bool HasPendingChanges => Math.Abs(CurrentValue - PendingValue) > 0.0001f;

        public Action<float> OnApply { get; set; }
        public Action<float> OnChanged { get; set; }

        private Slider _slider;
        private TMP_Text _labelText;
        private TMP_Text _valueText;

        public SliderSettingItem(
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
            Id = id;
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
            WholeNumbers = wholeNumbers;
            CurrentValue = defaultValue;
            PendingValue = defaultValue;
            ValueFormatter = valueFormatter ?? (v => wholeNumbers ? v.ToString("0") : v.ToString("0.00"));
            OnApply = onApply;
            OnChanged = onChanged;
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.SliderTemplate == null) return null;

            GameObject obj = UnityEngine.Object.Instantiate(templates.SliderTemplate.gameObject, parent, false);
            obj.name = Id;
            SpawnedObject = obj;

            var sliderSetting = obj.GetComponent<SliderSetting>();
            if (sliderSetting == null)
            {
                throw new InvalidOperationException($"[ZUI] Slider template '{templates.SliderTemplate?.name}' is missing the SliderSetting component.");
            }

            var nameField = typeof(GameSetting).GetField("settingNameText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(GameSetting), "settingNameText");
            _labelText = (TMP_Text)nameField.GetValue(sliderSetting)
                ?? throw new InvalidOperationException($"[ZUI] Slider template '{sliderSetting.name}' has null settingNameText.");

            var valField = typeof(SliderSetting).GetField("valueText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SliderSetting), "valueText");
            _valueText = (TMP_Text)valField.GetValue(sliderSetting)
                ?? throw new InvalidOperationException($"[ZUI] Slider template '{sliderSetting.name}' has null valueText.");

            var sliderField = typeof(SliderSetting).GetField("slider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(SliderSetting), "slider");
            _slider = (Slider)sliderField.GetValue(sliderSetting)
                ?? throw new InvalidOperationException($"[ZUI] Slider template '{sliderSetting.name}' has null slider.");

            // Remove GameSetting script to prevent vanilla interference and NREs
            UnityEngine.Object.DestroyImmediate(sliderSetting);

            if (_slider != null)
            {
                _slider.minValue = MinValue;
                _slider.maxValue = MaxValue;
                _slider.wholeNumbers = WholeNumbers;
                _slider.onValueChanged.RemoveAllListeners();
                _slider.SetValueWithoutNotify(PendingValue);
                _slider.onValueChanged.AddListener(new UnityAction<float>(val =>
                {
                    PendingValue = val;
                    UpdateValueLabel(val);
                    OnChanged?.Invoke(val);
                    ZUISettings.MarkChangesMade();
                }));
            }

            if (_labelText != null)
            {
                _labelText.text = Label;
                ZUI.Core.UIUtils.ApplyFontSafe(_labelText);
            }

            UpdateValueLabel(PendingValue);

            return obj;
        }

        private void UpdateValueLabel(float value)
        {
            if (_valueText != null)
            {
                _valueText.text = ValueFormatter != null ? ValueFormatter(value) : value.ToString("0.00");
                ZUI.Core.UIUtils.ApplyFontSafe(_valueText);
            }
        }

        public void Apply()
        {
            if (HasPendingChanges)
            {
                CurrentValue = PendingValue;
                OnApply?.Invoke(CurrentValue);
            }
        }

        public void Revert()
        {
            PendingValue = CurrentValue;
            if (_slider != null)
            {
                _slider.SetValueWithoutNotify(CurrentValue);
            }
            UpdateValueLabel(CurrentValue);
        }

        public void RefreshUI()
        {
            if (_slider != null)
            {
                _slider.SetValueWithoutNotify(PendingValue);
            }
            UpdateValueLabel(PendingValue);
            if (_labelText != null)
            {
                _labelText.text = Label;
            }
        }

        public void Destroy()
        {
            if (SpawnedObject != null)
            {
                UnityEngine.Object.Destroy(SpawnedObject);
                SpawnedObject = null;
            }
            _slider = null;
            _labelText = null;
            _valueText = null;
        }
    }
}
