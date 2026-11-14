using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ZUI.Core;
using ZUI.MainMenu;
using ZUI.Panels;
using ZUI.PauseMenu;
using ZUI.Popups;
using ZUI.Screens;
using ZUI.Settings;

namespace ZUI
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        public static Plugin Instance { get; private set; }
        public static ConfigEntry<bool> EnableExampleModules { get; private set; }

        private Harmony _harmony;
        private readonly List<IZUIModule> _loadedModules = new List<IZUIModule>();

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            EnableExampleModules = Config.Bind(
                "General",
                "EnableExampleModules",
                true,
                "Whether to load the built-in ZUI.Example showcase modules on startup."
            );

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} initializing...");

            // 1. Initialize Crash Handler & Harmony Patches
            ZUICrashHandler.Initialize(this);
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();

            // 2. Initialize Settings Subsystem
            ZUISettings.Initialize(this);

            // 3. Initialize Panels Subsystem (Type 2 Official Mod Modals)
            ZUIPanel.Initialize(this);

            // 4. Initialize Main Menu Subsystem (Type 1 Custom Buttons & Drawers)
            ZUIMainMenu.Initialize(this);

            // 5. Initialize Pause Menu Subsystem (In-Game Custom Buttons & Drawers)
            ZUIPauseMenu.Initialize(this);

            // 6. Initialize Standalone Custom Split Screens Subsystem (Phase 5)
            ZUIScreen.Initialize(this);

            // 7. Initialize InfoText Notifications Subsystem
            ZUI.Notifications.ZUIInfoText.Initialize(this);

            // Register standard ZUI-Lib drawer (Compact drawer, paginated: false)
            var zuiDrawer = ZUIMainMenu.RegisterSubmenu(
                id: "zui_lib_drawer",
                label: "ZUI-Lib",
                paginated: false,
                priority: -100
            );

            zuiDrawer.AddButton(
                id: "zui_lib_about",
                label: "About",
                onClick: () =>
                {
                    ZUIPopup.ShowInfo(
                        title: "ZUI-Lib",
                        message: $"ZUI-Lib v{PluginInfo.PLUGIN_VERSION}\nModular UI library for Zort mods.\nDeveloped by KRWCLASSIC.",
                        buttonText: "Awesome",
                        titleHex: "#64B5F6"
                    );
                }
            );

            // Register standard ZUI-Lib drawer on Pause Menu as well
            var zuiPauseDrawer = ZUIPauseMenu.RegisterSubmenu(
                id: "zui_lib_pause_drawer",
                label: "ZUI-Lib",
                paginated: false,
                priority: -100
            );

            zuiPauseDrawer.AddButton(
                id: "zui_lib_pause_about",
                label: "About",
                onClick: () =>
                {
                    ZUIPopup.ShowInfo(
                        title: "ZUI-Lib",
                        message: $"ZUI-Lib v{PluginInfo.PLUGIN_VERSION}\nModular UI library for Zort mods.\nDeveloped by KRWCLASSIC.",
                        buttonText: "Awesome",
                        titleHex: "#64B5F6"
                    );
                }
            );

            // 6. Register Debug Subsection inside ZUI-Lib Pause Menu tab (MORE -> ZUI-LIB -> DEBUG)
            ZUI.Debug.ZUIDebug.RegisterPauseDebugSubsection(zuiPauseDrawer);

            // 7. Discover and load modular feature modules (including Example/ when present)
            LoadModules();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded successfully with {_loadedModules.Count} module(s)!");
        }


        private void LoadModules()
        {
            _loadedModules.Clear();

            try
            {
                var moduleTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => typeof(IZUIModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                bool loadExamples = EnableExampleModules == null || EnableExampleModules.Value;

                foreach (var type in moduleTypes)
                {
                    if (!loadExamples && type.Namespace != null && type.Namespace.StartsWith("ZUI.Example", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    try
                    {
                        var module = (IZUIModule)Activator.CreateInstance(type);
                        module.Initialize();
                        _loadedModules.Add(module);
                        Log.LogInfo($"[ZUI] Loaded modular extension: {module.Name}");
                    }
                    catch (Exception ex)
                    {
                        ZUICrashHandler.HandleException(ex, $"Module '{type.FullName}'", type.Assembly);
                        Log.LogError($"[ZUI] Failed to initialize module {type.FullName}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ZUI] Error scanning for modules: {ex}");
            }
        }

        private void OnDestroy()
        {
            ZUIScreen.Shutdown();
            ZUIPauseMenu.Shutdown();
            ZUIMainMenu.Shutdown();
            ZUISettings.Shutdown();
            ZUICrashHandler.Shutdown();
            _harmony?.UnpatchSelf();
        }
    }
}
