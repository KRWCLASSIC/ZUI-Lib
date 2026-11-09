using HarmonyLib;

namespace ZUI.MainMenu.Patches
{
    [HarmonyPatch(typeof(MainMenuManager), "Awake")]
    internal static class MainMenuManagerAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUIMainMenu.TryInstall();
        }
    }
}
