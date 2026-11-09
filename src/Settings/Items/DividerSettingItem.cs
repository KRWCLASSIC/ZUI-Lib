using UnityEngine;

namespace ZUI.Settings
{
    public class DividerSettingItem : ISettingItem
    {
        public string Id { get; }
        public string Label { get; set; }
        public GameObject SpawnedObject { get; private set; }
        public bool HasPendingChanges => false;

        public DividerSettingItem(string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? "ZUI_Divider_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) : id;
            Label = "";
        }

        public GameObject Instantiate(Transform parent, SettingsTemplates templates)
        {
            if (templates.LineTemplate == null) return null;

            GameObject obj = Object.Instantiate(templates.LineTemplate.gameObject, parent, false);
            obj.name = Id;
            SpawnedObject = obj;
            return obj;
        }

        public void Apply() { }
        public void Revert() { }
        public void RefreshUI() { }

        public void Destroy()
        {
            if (SpawnedObject != null)
            {
                Object.Destroy(SpawnedObject);
                SpawnedObject = null;
            }
        }
    }
}
