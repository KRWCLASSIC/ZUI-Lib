using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.Settings
{
    /// <summary>
    /// An empty layout spacer that provides clean, consistent vertical space between settings sections.
    /// </summary>
    public class SpaceSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; } = "";
        public GameObject SpawnedObject { get; private set; }
        public bool HasPendingChanges => false;
        public float Height { get; set; }

        public SpaceSettingItem(string id = null, float height = 15f)
        {
            Id = string.IsNullOrEmpty(id) ? "ZUI_Space_" + Guid.NewGuid().ToString("N").Substring(0, 8) : id;
            Height = height;
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            var obj = new GameObject(Id, typeof(RectTransform), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            SpawnedObject = obj;

            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Height);

            var le = obj.GetComponent<LayoutElement>();
            le.preferredHeight = Height;
            le.minHeight = Height;

            return obj;
        }

        public void Apply() { }
        public void Revert() { }
        public void RefreshUI() { }

        public void Destroy()
        {
            if (SpawnedObject != null)
            {
                UnityEngine.Object.Destroy(SpawnedObject);
                SpawnedObject = null;
            }
        }
    }
}
