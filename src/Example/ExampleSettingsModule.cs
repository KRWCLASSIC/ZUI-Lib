using System;
using System.Collections;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
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
                text: "Showcase of ZUI controls and callback handling.",
                hexColor: "#CCCCCC"
            );

            // 2. Category: Toggles
            ZUISettings.AddSpace(height: 14f);

            ZUISettings.AddCategory(
                id: "zui_cat_toggles",
                title: "Category: Toggles",
                hexColor: "#55FF99"
            );

            // Real BepInEx ConfigFile for showcase settings (saved to BepInEx/config/ZUI.Example.cfg)
            var exampleConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "ZUI.Example.cfg"), true);
            var autoHeal = exampleConfig.Bind("Gameplay", "AutoHeal", false, "Heals player over time");
            var fovConfig = exampleConfig.Bind("Graphics", "FOV", 90f, "Camera field of view");
            var qualityConfig = exampleConfig.Bind("Graphics", "Quality", ExampleQualityPreset.High, "Graphics Quality Preset");

            // Standalone Toggle (Simple callback)
            ZUISettings.AddToggle(
                id: "zui_example_toggle_raw",
                label: "Quick Toggle",
                defaultValue: true,
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Toggle applied: {val}")
            );

            // Real BepInEx Config Toggle
            ZUISettings.AddToggle(
                id: "mymod_autoheal",
                label: "Auto-Heal",
                defaultValue: autoHeal.Value,
                onApply: val =>
                {
                    autoHeal.Value = val;
                    exampleConfig.Save();
                    Plugin.Log.LogInfo($"[ZUI Example] Saved to BepInEx config: AutoHeal = {val}");
                }
            );

            // 3. Category: Sliders
            ZUISettings.AddSpace(height: 14f);

            ZUISettings.AddCategory(
                id: "zui_cat_sliders",
                title: "Category: Sliders",
                hexColor: "#FFAA33"
            );

            // Integer Slider (0 - 100)
            ZUISettings.AddSlider(
                id: "zui_example_integer",
                label: "Example Integer",
                minValue: 0,
                maxValue: 100,
                defaultValue: 42,
                wholeNumbers: true,
                valueFormatter: v => $"{v:0}",
                onApply: val => Plugin.Log.LogInfo($"[ZUI Example] Integer applied: {val:0}")
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

            // Real BepInEx Config Slider
            ZUISettings.AddSlider(
                id: "mymod_fov",
                label: "Field of View",
                minValue: 60f,
                maxValue: 120f,
                defaultValue: fovConfig.Value,
                wholeNumbers: true,
                valueFormatter: v => $"{v:0}°",
                onApply: val =>
                {
                    fovConfig.Value = val;
                    exampleConfig.Save();
                    Plugin.Log.LogInfo($"[ZUI Example] Saved to BepInEx config: FOV = {val}");
                }
            );

            // 4. Category: Dropdowns
            ZUISettings.AddSpace(height: 20f);

            ZUISettings.AddCategory(
                id: "zui_cat_dropdowns",
                title: "Category: Dropdowns",
                hexColor: "#55CCFF"
            );
            ZUISettings.AddSpace(height: 10f);

            // Standalone Dropdown
            ZUISettings.AddDropdown(
                id: "zui_example_dropdown",
                label: "Example Dropdown",
                options: new[] { "Option 1", "Option 2", "Option 3" },
                defaultIndex: 1,
                onApply: idx => Plugin.Log.LogInfo($"[ZUI Example] Dropdown option applied: index {idx}")
            );

            // Real BepInEx Config Enum Dropdown
            ZUISettings.AddDropdown<ExampleQualityPreset>(
                id: "mymod_quality_preset",
                label: "Quality Preset",
                defaultValue: qualityConfig.Value,
                onApply: val =>
                {
                    qualityConfig.Value = val;
                    exampleConfig.Save();
                    Plugin.Log.LogInfo($"[ZUI Example] Saved to BepInEx config: Quality Preset = {val}");
                }
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

    public enum ExampleQualityPreset
    {
        Low,
        Medium,
        High,
        Ultra
    }
}
