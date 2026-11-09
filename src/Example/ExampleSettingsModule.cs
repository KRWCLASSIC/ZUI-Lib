using System;
using System.Collections;
using UnityEngine;
using ZUI.Core;
using ZUI.Settings;

namespace ZUI.Example
{
    /// <summary>
    /// Example showcase module for ZUI (ZortUILib).
    /// Demonstrates all supported UI controls (toggles, sliders, dropdowns, and text/categories)
    /// and showcases automatic BepInEx ConfigFile two-way binding and saving.
    /// </summary>
    public class ExampleSettingsModule : IZUIModule
    {
        public string Name => "ExampleSettingsModule";

        public void Initialize()
        {
            Plugin.Log.LogInfo("[ExampleSettingsModule] Registering showcase settings to ZUI...");

            // 1. Showcase section header & divider
            ZUISettings.AddDivider(id: "zui_divider_top");

            ZUISettings.AddHeader(
                id: "zui_header",
                title: "ZUI Examples",
                color: Color.yellow
            );

            ZUISettings.AddText(
                id: "zui_desc",
                text: "Showcase of ZUI controls and BepInEx config binding.",
                hexColor: "#CCCCCC"
            );

            // 2. Category: Toggles
            ZUISettings.AddSpace(height: 14f);

            ZUISettings.AddCategory(
                id: "zui_cat_toggles",
                title: "Category: Toggles",
                hexColor: "#55FF99"
            );

            var cfgToggle = Plugin.Instance.Config.Bind(
                section: "Examples",
                key: "ExampleSavedToggle",
                defaultValue: true,
                description: "Showcases BepInEx config persistence for toggles."
            );

            // Bound Toggle (automatically saved to BepInEx .cfg)
            ZUISettings.AddToggle(
                configEntry: cfgToggle,
                label: "Example Toggle (Saved to .cfg)"
            );

            // Standalone Toggle (callback only)
            ZUISettings.AddToggle(
                id: "zui_example_toggle_raw",
                label: "Example Toggle (Callback only)",
                defaultValue: false,
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Raw toggle applied: {val}")
            );

            // 3. Category: Sliders
            ZUISettings.AddSpace(height: 14f);

            ZUISettings.AddCategory(
                id: "zui_cat_sliders",
                title: "Category: Sliders",
                hexColor: "#FFAA33"
            );

            var cfgInteger = Plugin.Instance.Config.Bind(
                section: "Examples",
                key: "ExampleSavedInteger",
                defaultValue: 42,
                description: "Showcases BepInEx config persistence for integers."
            );

            // Bound Integer Slider (0 - 100)
            ZUISettings.AddSlider(
                configEntry: cfgInteger,
                minValue: 0,
                maxValue: 100,
                label: "Example Integer",
                valueFormatter: v => $"{v:0}"
            );

            // Decimal Slider (floating point precision)
            ZUISettings.AddSlider(
                id: "zui_example_decimal",
                label: "Example Decimal",
                minValue: 0.00f,
                maxValue: 2.50f,
                defaultValue: 1.25f,
                wholeNumbers: false,
                valueFormatter: v => $"{v:0.00}",
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Decimal applied: {val:0.00}")
            );

            // Degrees Slider (angles / rotation / FOV)
            ZUISettings.AddSlider(
                id: "zui_example_degrees",
                label: "Example Degrees",
                minValue: 0f,
                maxValue: 360f,
                defaultValue: 90f,
                wholeNumbers: true,
                valueFormatter: v => $"{v:0}°",
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Degrees applied: {val:0}°")
            );

            // Percentage Slider (0% - 100%)
            ZUISettings.AddSlider(
                id: "zui_example_percent",
                label: "Example Percentage",
                minValue: 0f,
                maxValue: 100f,
                defaultValue: 75f,
                wholeNumbers: true,
                valueFormatter: v => $"{v:0}%",
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Percentage applied: {val:0}%")
            );

            // 4. Category: Dropdowns
            ZUISettings.AddSpace(height: 20f);

            ZUISettings.AddCategory(
                id: "zui_cat_dropdowns",
                title: "Category: Dropdowns",
                hexColor: "#55CCFF"
            );
            ZUISettings.AddSpace(height: 10f);

            var cfgDropdown = Plugin.Instance.Config.Bind(
                section: "Examples",
                key: "ExampleSavedOption",
                defaultValue: "Option 2",
                description: "Showcases BepInEx config persistence for dropdown selections."
            );

            // Bound Dropdown (automatically saved to BepInEx .cfg)
            ZUISettings.AddDropdown(
                configEntry: cfgDropdown,
                options: new[] { "Option 1", "Option 2", "Option 3" },
                label: "Example Dropdown"
            );

            // 5. Category: Buttons, Popups & Dialogs
            ZUISettings.AddSpace(height: 26f);

            ZUISettings.AddCategory(
                id: "zui_cat_popups",
                title: "Category: Buttons, Popups & Dialogs",
                hexColor: "#E040FB"
            );
            ZUISettings.AddSpace(height: 16f);

            // Style 1: Confirmation Dialog (Quit Game recreation)
            ZUISettings.AddButton(
                id: "zui_btn_popup_confirm",
                label: "Custom Confirmation Dialog (Style 1)",
                buttonText: "Show Confirm",
                onClick: () =>
                {
                    Popups.ZUIPopup.ShowConfirmation(
                        title: "Quit Session?",
                        message: "Are you sure you want to quit back to desktop?\nAll unsaved modifications will be lost.",
                        yesText: "Quit Game",
                        noText: "Cancel",
                        onYes: () => Plugin.Log.LogInfo("[ZUI Example] User chose: Quit Game"),
                        onNo: () => Plugin.Log.LogInfo("[ZUI Example] User chose: Cancel"),
                        titleHex: "#FFD700",
                        messageHex: "#E0E0E0",
                        yesButtonHex: "#E53935",
                        yesTextHex: "#FFFFFF",
                        noButtonHex: "#424242",
                        noTextHex: "#BDBDBD",
                        showCloseButton: true,
                        buttonWidth: 200f,
                        squishTextToFit: true
                    );
                }
            ).WithSize(260f, 44f).WithColors("#37474F", "#ECEFF1");

            // Style 2: Alert / Crash Dialog (Single button recreation)
            ZUISettings.AddButton(
                id: "zui_btn_popup_alert",
                label: "Custom Alert / Crash Dialog (Style 2)",
                buttonText: "Show Alert",
                onClick: () =>
                {
                    Popups.ZUIPopup.ShowAlert(
                        title: "Subsystem Crash",
                        message: "An unhandled exception occurred in sector B-12.\nCore systems restored with 0 data corruption.",
                        buttonText: "Acknowledge",
                        onDismiss: () => Plugin.Log.LogInfo("[ZUI Example] Crash alert acknowledged"),
                        titleHex: "#FF5252",
                        messageHex: "#FAFAFA",
                        buttonHex: "#D32F2F",
                        buttonTextHex: "#FFFFFF",
                        buttonWidth: 200f,
                        squishTextToFit: true
                    );
                }
            ).WithSize(260f, 44f).WithColors("#8A1C14", "#FFFFFF");

            // Style 3: Accept / Decline Dialog (Terms of Service / EULA)
            ZUISettings.AddButton(
                id: "zui_btn_popup_tos",
                label: "Custom Accept / Decline Dialog (Style 3)",
                buttonText: "Show Terms",
                onClick: () =>
                {
                    Popups.ZUIPopup.ShowAcceptDecline(
                        title: "Terms of Service Agreement",
                        message: "By continuing, you agree to enable experimental modding features and telemetry.\nDo you accept the Community Guidelines?",
                        acceptText: "Accept TOS",
                        declineText: "Decline",
                        onAccept: () => Plugin.Log.LogInfo("[ZUI Example] User accepted Terms of Service!"),
                        onDecline: () => Plugin.Log.LogInfo("[ZUI Example] User declined Terms of Service!"),
                        titleHex: "#FFD700",
                        messageHex: "#E0E0E0",
                        buttonWidth: 200f,
                        squishTextToFit: true
                    );
                }
            ).WithSize(260f, 44f).WithColors("#1B5E20", "#FFFFFF");

            // Style 4: Loading Spinner Dialog with animated dynamic progress
            ZUISettings.AddButton(
                id: "zui_btn_popup_loading",
                label: "Custom Loading Spinner Dialog (Style 4)",
                buttonText: "Show Loading",
                onClick: () =>
                {
                    var loading = Popups.ZUIPopup.ShowLoading(
                        title: "Syncing Mod Assets",
                        message: "Initializing cache...\nConnecting to backend (1/3)",
                        titleHex: "#80D8FF",
                        messageHex: "#E0E0E0"
                    );

                    Plugin.Instance.StartCoroutine(SimulateLoadingTask(loading));
                }
            ).WithSize(260f, 44f).WithColors("#0D47A1", "#E1F5FE");

            // 6. Section Divider
            ZUISettings.AddSpace(height: 18f);
            ZUISettings.AddDivider(id: "zui_divider_bottom");

            Plugin.Log.LogInfo("[ExampleSettingsModule] Showcase settings registered successfully.");
        }

        private static IEnumerator SimulateLoadingTask(Popup loadingPopup)
        {
            yield return new WaitForSeconds(1.2f);
            if (loadingPopup != null)
            {
                Popups.ZUIPopup.UpdateMessage(loadingPopup, "Compiling custom shaders...\nApplying materials (2/3)");
            }

            yield return new WaitForSeconds(1.2f);
            if (loadingPopup != null)
            {
                Popups.ZUIPopup.UpdateMessage(loadingPopup, "Finalizing settings...\nDone! (3/3)");
            }

            yield return new WaitForSeconds(0.8f);
            if (loadingPopup != null)
            {
                Popups.ZUIPopup.Close(loadingPopup);
            }
        }
    }
}
