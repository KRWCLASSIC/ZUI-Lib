using HarmonyLib;
using ZUI.Panels;
using ZUI.Screens;

namespace ZUI.PauseMenu.Patches
{
    [HarmonyPatch(typeof(InGameUI), "Start")]
    internal static class InGameUIStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ZUIPauseMenu.TryInstall();
        }
    }

    [HarmonyPatch(typeof(InGameUI), "SetPauseStatus")]
    internal static class InGameUISetPauseStatusPatch
    {
        [HarmonyPostfix]
        private static void Postfix(bool state)
        {
            ZUIPauseMenu.OnPauseStatusChanged(state);
        }
    }

    [HarmonyPatch(typeof(InGameUI), "CalculateEscape")]
    internal static class InGameUICalculateEscapePatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // 1. If a ZUI modal panel is currently open or was closed by ESC on this frame,
            // consume ESC to close the panel and prevent popping drawers or closing pause menu
            if (ZUIPanelController.CurrentOpenPanel != null || ZUIPanelController.LastEscConsumedFrame == UnityEngine.Time.frameCount)
            {
                if (ZUIPanelController.CurrentOpenPanel != null)
                {
                    ZUIPanelController.LastEscConsumedFrame = UnityEngine.Time.frameCount;
                    ZUIPanelController.ClosePanel(ZUIPanelController.CurrentOpenPanel);
                }
                return false;
            }

            // 2. If a popup is active in the game
            if (InitUI.PopupManager != null && InitUI.PopupManager.GetPopupCount > 0)
            {
                var activePopup = InitUI.PopupManager.GetPopup(0);
                if (activePopup != null)
                {
                    var behavior = activePopup.GetComponent<Popups.ZUIPopupBehavior>();
                    if (behavior != null)
                    {
                        if (behavior.Options.DismissOnEscape)
                        {
                            Popups.ZUIPopup.LastEscConsumedFrame = UnityEngine.Time.frameCount;
                            var choice = behavior.Options.EscapeChoice.GetValueOrDefault(Popups.PopupChoice.Secondary);
                            if (choice == Popups.PopupChoice.Primary)
                            {
                                activePopup.ClickYes_Btn();
                            }
                            else
                            {
                                activePopup.ClickNo_Btn();
                            }
                        }
                        else
                        {
                            // DismissOnEscape is false (default): consume ESC and keep popup open
                            Popups.ZUIPopup.LastEscConsumedFrame = UnityEngine.Time.frameCount;
                        }
                        return false;
                    }
                }

                // Native vanilla popup (not ZUI-managed): let vanilla handle, but record frame
                Popups.ZUIPopup.LastEscConsumedFrame = UnityEngine.Time.frameCount;
                return true;
            }

            // 3. If a popup was closed on this frame by ESC, do not pop drawers or close pause menu
            if (Popups.ZUIPopup.LastEscConsumedFrame == UnityEngine.Time.frameCount)
            {
                return false;
            }

            if (StaticInstance<InitUI>.Instance != null && StaticInstance<InitUI>.Instance.IsSettingsOpen)
            {
                return true;
            }

            // 4. If a standalone ZUIScreen is currently open or was closed by ESC on this frame,
            // consume ESC to pop nested screen drawers or close the screen without toggling vanilla pause menu
            if (ZUIScreenController.CurrentOpenScreen != null || ZUIScreenController.LastEscConsumedFrame == UnityEngine.Time.frameCount)
            {
                if (ZUIScreenController.CurrentOpenScreen != null)
                {
                    ZUIScreenController.Instance?.HandleEscape();
                }
                return false;
            }

            // 5. If Pause Menu is in Tabbed / Drawer mode, step back / pop drawer
            if (PauseMenuTabController.Instance != null &&
                PauseMenuTabController.Instance.IsPauseMenuOpen &&
                PauseMenuTabController.Instance.CurrentMode == PauseMenuMode.Tabbed)
            {
                PauseMenuTabController.Instance.HandleEscape();
                StaticInstance<InitUI>.Instance?.PlayClickSFX();
                return false; // Prevent vanilla from closing the pause menu
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InGameUI), "CalculateEnter")]
    internal static class InGameUICalculateEnterPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (InitUI.PopupManager != null && InitUI.PopupManager.GetPopupCount > 0)
            {
                return true;
            }

            // Prevent Enter from opening the in-game chat field while a ZUIScreen or ZUIPanel is open
            if (ZUIScreenController.CurrentOpenScreen != null || ZUIPanelController.CurrentOpenPanel != null)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(EscapePlayerUI), nameof(EscapePlayerUI.Initialize))]
    internal static class EscapePlayerUIInitializePatch
    {
        [HarmonyPostfix]
        private static void Postfix(EscapePlayerUI __instance, PlayerController player, bool __result)
        {
            if (!__result || __instance == null || player == null) return;
            ZUIPlayerCardExtension.Attach(__instance, player);
        }
    }

    [HarmonyPatch(typeof(EscapePlayerUI), nameof(EscapePlayerUI.OnPointerEnter))]
    internal static class EscapePlayerUIOnPointerEnterPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EscapePlayerUI __instance)
        {
            if (__instance == null) return;
            var ext = __instance.GetComponent<ZUIPlayerCardExtension>();
            if (ext != null)
            {
                ext.OnHoverChanged(true);
            }
        }
    }

    [HarmonyPatch(typeof(EscapePlayerUI), nameof(EscapePlayerUI.OnPointerExit))]
    internal static class EscapePlayerUIOnPointerExitPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EscapePlayerUI __instance)
        {
            if (__instance == null) return;
            var ext = __instance.GetComponent<ZUIPlayerCardExtension>();
            if (ext != null)
            {
                ext.OnHoverChanged(false);
            }
        }
    }

    [HarmonyPatch(typeof(InGameUI), nameof(InGameUI.RecalculateCursorLock))]
    internal static class InGameUIRecalculateCursorLockPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (ZUIScreenController.CurrentOpenScreen != null)
            {
                ZUIScreenController.ApplyCursorAndInputLock();
            }
        }
    }
}
