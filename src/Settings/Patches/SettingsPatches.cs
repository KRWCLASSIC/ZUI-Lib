using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZUI.Settings;

namespace ZUI.Settings.Patches
{
    [HarmonyPatch(typeof(InitUI), nameof(InitUI.ApplyChanges_Btn))]
    internal static class ApplySettingsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUISettings.ApplyPending();
        }
    }

    [HarmonyPatch(typeof(InitUI), "RevertAllChanges")]
    internal static class RevertSettingsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUISettings.RevertPending();
        }
    }

    [HarmonyPatch(typeof(InitUI), nameof(InitUI.OpenSettings_Btn))]
    internal static class OpenSettingsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUISettings.OnSettingsOpened();
        }
    }

    [HarmonyPatch(typeof(ButtonFunctions), nameof(ButtonFunctions.OpenSettings_Btn))]
    internal static class ButtonFunctionsOpenSettingsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUISettings.OnSettingsOpened();
        }
    }
}
