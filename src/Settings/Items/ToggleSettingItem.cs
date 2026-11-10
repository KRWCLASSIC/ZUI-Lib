using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZUI.Settings
{
    public class ToggleSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public GameObject SpawnedObject { get; private set; }

        public bool CurrentValue { get; private set; }
        public bool PendingValue { get; set; }

        public bool HasPendingChanges => CurrentValue != PendingValue;

        public Action<bool> OnApply { get; set; }
        public Action<bool> OnChanged { get; set; }

        private Toggle _toggle;
        private TMP_Text _labelText;

        public ToggleSettingItem(
            string id,
            string label,
            bool defaultValue,
            Action<bool> onApply = null,
            Action<bool> onChanged = null)
        {
            Id = id;
            Label = label;
            CurrentValue = defaultValue;
            PendingValue = defaultValue;
            OnApply = onApply;
            OnChanged = onChanged;
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.ToggleTemplate == null) return null;

            GameObject obj = UnityEngine.Object.Instantiate(templates.ToggleTemplate.gameObject, parent, false);
            obj.name = Id;
            SpawnedObject = obj;

            var toggleSetting = obj.GetComponent<ToggleSetting>();
            if (toggleSetting == null)
            {
                throw new InvalidOperationException($"[ZUI] Toggle template '{templates.ToggleTemplate?.name}' is missing the ToggleSetting component.");
            }

            var nameField = typeof(GameSetting).GetField("settingNameText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(GameSetting), "settingNameText");
            _labelText = (TMP_Text)nameField.GetValue(toggleSetting)
                ?? throw new InvalidOperationException($"[ZUI] Toggle template '{toggleSetting.name}' has null settingNameText.");

            var toggleField = typeof(ToggleSetting).GetField("toggle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(ToggleSetting), "toggle");
            _toggle = (Toggle)toggleField.GetValue(toggleSetting)
                ?? throw new InvalidOperationException($"[ZUI] Toggle template '{toggleSetting.name}' has null toggle.");

            // Remove GameSetting script to prevent vanilla interference and NREs
            UnityEngine.Object.DestroyImmediate(toggleSetting);

            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveAllListeners();
                _toggle.SetIsOnWithoutNotify(PendingValue);
                _toggle.onValueChanged.AddListener(new UnityAction<bool>(val =>
                {
                    PendingValue = val;
                    OnChanged?.Invoke(val);
                    ZUISettings.MarkChangesMade();
                }));
            }

            if (_labelText != null)
            {
                _labelText.text = Label;
                ZUI.Core.UIUtils.ApplyFontSafe(_labelText);
            }

            return obj;
        }

        public void Apply()
        {
            if (CurrentValue != PendingValue)
            {
                CurrentValue = PendingValue;
                OnApply?.Invoke(CurrentValue);
            }
        }

        public void Revert()
        {
            PendingValue = CurrentValue;
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(CurrentValue);
            }
        }

        public void RefreshUI()
        {
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(PendingValue);
            }
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
            _toggle = null;
            _labelText = null;
        }
    }
}
