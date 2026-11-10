using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using ZUI.Core;

namespace ZUI.Settings
{
    public class DropdownSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public List<string> Options { get; } = new List<string>();
        public GameObject SpawnedObject { get; private set; }

        public int CurrentIndex { get; private set; }
        public int PendingIndex { get; set; }

        public bool HasPendingChanges => CurrentIndex != PendingIndex;

        public Action<int> OnApply { get; set; }
        public Action<int> OnChanged { get; set; }

        private TMP_Dropdown _dropdown;
        private TMP_Text _labelText;

        public DropdownSettingItem(
            string id,
            string label,
            IList<string> options,
            int defaultIndex = 0,
            Action<int> onApply = null,
            Action<int> onChanged = null)
        {
            Id = id;
            Label = label;
            if (options != null) Options.AddRange(options);
            CurrentIndex = Mathf.Clamp(defaultIndex, 0, Math.Max(0, Options.Count - 1));
            PendingIndex = CurrentIndex;
            OnApply = onApply;
            OnChanged = onChanged;
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.DropdownTemplate == null)
            {
                throw new InvalidOperationException("[ZUI] Dropdown template could not be found in Settings panel.");
            }

            GameObject obj = UnityEngine.Object.Instantiate(templates.DropdownTemplate.gameObject, parent, false);
            obj.name = Id;
            SpawnedObject = obj;

            var dropdownSetting = obj.GetComponent<DropdownSetting>();
            if (dropdownSetting == null)
            {
                throw new InvalidOperationException($"[ZUI] Dropdown template '{templates.DropdownTemplate?.name}' is missing the DropdownSetting component.");
            }

            var nameField = typeof(GameSetting).GetField("settingNameText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(GameSetting), "settingNameText");
            _labelText = (TMP_Text)nameField.GetValue(dropdownSetting)
                ?? throw new InvalidOperationException($"[ZUI] Dropdown template '{dropdownSetting.name}' has null settingNameText.");

            var dropdownField = typeof(DropdownSetting).GetField("dropdown", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(nameof(DropdownSetting), "dropdown");
            _dropdown = (TMP_Dropdown)dropdownField.GetValue(dropdownSetting)
                ?? throw new InvalidOperationException($"[ZUI] Dropdown template '{dropdownSetting.name}' has null dropdown.");

            // Remove GameSetting script to prevent vanilla interference and NREs
            UnityEngine.Object.DestroyImmediate(dropdownSetting);

            // Populate options
            _dropdown.ClearOptions();
            _dropdown.AddOptions(Options);
            _dropdown.onValueChanged.RemoveAllListeners();
            _dropdown.SetValueWithoutNotify(PendingIndex);

            UpdateCaption();

            _dropdown.onValueChanged.AddListener(new UnityAction<int>(idx =>
            {
                PendingIndex = idx;
                UpdateCaption();
                OnChanged?.Invoke(idx);
                ZUISettings.MarkChangesMade();
            }));

            if (_labelText != null)
            {
                _labelText.text = Label;
                UIUtils.ApplyFontSafe(_labelText);
            }

            if (_dropdown.captionText != null)
                UIUtils.ApplyFontSafe(_dropdown.captionText);
            if (_dropdown.itemText != null)
                UIUtils.ApplyFontSafe(_dropdown.itemText);

            return obj;
        }

        private void UpdateCaption()
        {
            if (_dropdown != null && _dropdown.captionText != null)
            {
                if (_dropdown.options.Count > 0 && PendingIndex >= 0 && PendingIndex < _dropdown.options.Count)
                {
                    _dropdown.captionText.text = _dropdown.options[PendingIndex].text;
                }
                else
                {
                    _dropdown.captionText.text = "NONE";
                }
            }
        }

        public void Apply()
        {
            if (HasPendingChanges)
            {
                CurrentIndex = PendingIndex;
                OnApply?.Invoke(CurrentIndex);
            }
        }

        public void Revert()
        {
            PendingIndex = CurrentIndex;
            if (_dropdown != null)
            {
                _dropdown.SetValueWithoutNotify(CurrentIndex);
            }
            UpdateCaption();
        }

        public void RefreshUI()
        {
            if (_dropdown != null)
            {
                _dropdown.SetValueWithoutNotify(PendingIndex);
            }
            UpdateCaption();
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
            _dropdown = null;
            _labelText = null;
        }
    }
}
