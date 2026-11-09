using UnityEngine;

namespace ZUI.Settings
{
    public interface ISettingItem
    {
        string Id { get; }
        string Label { get; set; }
        GameObject SpawnedObject { get; }
        bool HasPendingChanges { get; }

        GameObject Instantiate(Transform parent, SettingsTemplates templates);
        void Apply();
        void Revert();
        void RefreshUI();
        void Destroy();
    }
}
