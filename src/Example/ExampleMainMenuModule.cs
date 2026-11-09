using ZUI.Core;
using ZUI.MainMenu;
using ZUI.Panels;
using ZUI.Popups;

namespace ZUI.Example
{
    /// <summary>
    /// Example showcase module demonstrating custom Main Menu button registration,
    /// extreme 16-level deep nested submenus, custom breadcrumb colors, and pagination.
    /// </summary>
    public class ExampleMainMenuModule : IZUIModule
    {
        public string Name => "ExampleMainMenuModule";

        public void Initialize()
        {
            Plugin.Log.LogInfo("[ExampleMainMenuModule] Registering showcase Main Menu buttons & extreme nested abyss...");

            // Level 1: Example Root Drawer (Paginated: true, Gold Breadcrumb)
            var exampleDrawer = ZUIMainMenu.RegisterSubmenu(
                id: "zui_example_drawer",
                label: "Example",
                paginated: true,
                breadcrumbHex: "#FFD54F"
            );

            exampleDrawer.AddButton("zui_ex_btn_1", "Dialog Demo", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Dialog Demo",
                    message: "This is demo button 1 from ExampleMainMenuModule.",
                    buttonText: "OK",
                    titleHex: "#81C784"
                );
            });

            exampleDrawer.AddButton("zui_ex_btn_2", "Confirm Demo", () =>
            {
                ZUIPopup.ShowConfirmation(
                    title: "Confirmation Demo",
                    message: "This is demo button 2 with Yes/No actions.\n(ESC dismisses triggering Cancel)",
                    yesText: "Confirm",
                    noText: "Cancel",
                    titleHex: "#FFD54F",
                    dismissOnEscape: true,
                    escapeChoice: PopupChoice.Secondary
                );
            });

            // Level 2: Nested Submenu (The Caves, Neon Cyan)
            var cavesDrawer = exampleDrawer.AddSubmenu(
                id: "zui_cave_drawer",
                label: "Enter Caves",
                paginated: false,
                breadcrumbHex: "#4DD0E1"
            );

            cavesDrawer.AddButton("zui_cave_btn1", "Shout Echo", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Echoing Caves",
                    message: "Hello... hello... lo... lo...\nYou are 2 levels deep into drawers!",
                    buttonText: "Nice",
                    titleHex: "#4DD0E1"
                );
            });

            // Level 3: Nested Submenu (Catacombs, Violet Purple)
            var catacombsDrawer = cavesDrawer.AddSubmenu(
                id: "zui_catacombs_drawer",
                label: "Catacombs",
                paginated: false,
                breadcrumbHex: "#BA68C8"
            );

            catacombsDrawer.AddButton("zui_cat_btn1", "Ancient Tomb", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Ancient Tomb",
                    message: "Dust and bones everywhere.\nYou are 3 levels deep!",
                    buttonText: "Creepy",
                    titleHex: "#BA68C8"
                );
            });

            // Level 4: Nested Submenu (Molten Core, Deep Coral Orange)
            var moltenDrawer = catacombsDrawer.AddSubmenu(
                id: "zui_molten_drawer",
                label: "Molten Core",
                paginated: false,
                breadcrumbHex: "#FF7043"
            );

            moltenDrawer.AddButton("zui_molten_btn1", "Check Temp", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Molten Core",
                    message: "Current Temperature: 4200°C.\nYou are 4 levels deep!",
                    buttonText: "Hot!",
                    titleHex: "#FF7043"
                );
            });

            // Level 5: Nested Submenu (The Void, Emerald Green)
            var voidDrawer = moltenDrawer.AddSubmenu(
                id: "zui_void_drawer",
                label: "The Void",
                paginated: false,
                breadcrumbHex: "#81C784"
            );

            voidDrawer.AddButton("zui_void_btn1", "Gaze Inside", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "The Void",
                    message: "If you gaze long into an abyss, the abyss also gazes into you.\nDepth: Level 5!",
                    buttonText: "Whoa",
                    titleHex: "#81C784"
                );
            });

            // Level 6: Nested Submenu (Singularity, Hot Pink)
            var singularityDrawer = voidDrawer.AddSubmenu(
                id: "zui_singularity_drawer",
                label: "Singularity",
                paginated: false,
                breadcrumbHex: "#FF4081"
            );

            singularityDrawer.AddButton("zui_sing_btn1", "Compress Reality", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "The Singularity",
                    message: "Spacetime is collapsing into infinite density!\nDepth: Level 6!",
                    buttonText: "Keep Falling",
                    titleHex: "#FF4081"
                );
            });

            // Level 7: Matrix Code (Matrix Neon Green)
            var matrixDrawer = singularityDrawer.AddSubmenu(
                id: "zui_matrix_drawer",
                label: "Matrix Code",
                paginated: false,
                breadcrumbHex: "#00E676"
            );

            matrixDrawer.AddButton("zui_mat_btn1", "Take Red Pill", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "The Matrix",
                    message: "01011001 01001111 01010101\nThere is no spoon.\nDepth: Level 7!",
                    buttonText: "Unplug",
                    titleHex: "#00E676"
                );
            });

            // Level 8: Quantum Foam (Deep Indigo)
            var quantumDrawer = matrixDrawer.AddSubmenu(
                id: "zui_quantum_drawer",
                label: "Quantum Foam",
                paginated: false,
                breadcrumbHex: "#7C4DFF"
            );

            quantumDrawer.AddButton("zui_q_btn1", "Measure Spin", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Quantum Foam",
                    message: "Superposition collapsed upon observation!\nDepth: Level 8!",
                    buttonText: "Entangle",
                    titleHex: "#7C4DFF"
                );
            });

            // Level 9: Null Pointer (Bright Crimson)
            var nullDrawer = quantumDrawer.AddSubmenu(
                id: "zui_null_drawer",
                label: "Null Pointer",
                paginated: false,
                breadcrumbHex: "#FF5252"
            );

            nullDrawer.AddButton("zui_null_btn1", "Dereference", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "NullReferenceException",
                    message: "Object reference not set to an instance of an object.\nJust kidding!\nDepth: Level 9!",
                    buttonText: "Catch",
                    titleHex: "#FF5252"
                );
            });

            // Level 10: Subatomic Realm (Amber Flame)
            var subatomicDrawer = nullDrawer.AddSubmenu(
                id: "zui_subatomic_drawer",
                label: "Subatomic",
                paginated: false,
                breadcrumbHex: "#FFAB00"
            );

            subatomicDrawer.AddButton("zui_sub_btn1", "Split Quark", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Subatomic Realm",
                    message: "Gluons and charm quarks buzzing everywhere!\nDepth: Level 10!",
                    buttonText: "Collide",
                    titleHex: "#FFAB00"
                );
            });

            // Level 11: 4th Dimension (Electric Blue)
            var fourthDimDrawer = subatomicDrawer.AddSubmenu(
                id: "zui_4d_drawer",
                label: "4th Dimension",
                paginated: false,
                breadcrumbHex: "#40C4FF"
            );

            fourthDimDrawer.AddButton("zui_4d_btn1", "Rotate Tesseract", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "4th Dimension",
                    message: "You can see your own past and future simultaneous.\nDepth: Level 11!",
                    buttonText: "Fold Time",
                    titleHex: "#40C4FF"
                );
            });

            // Level 12: Memory Leak (Neon Magenta)
            var memLeakDrawer = fourthDimDrawer.AddSubmenu(
                id: "zui_memleak_drawer",
                label: "Memory Leak",
                paginated: false,
                breadcrumbHex: "#E040FB"
            );

            memLeakDrawer.AddButton("zui_mem_btn1", "Allocate 16GB", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Out of Memory",
                    message: "Breadcrumb RAM consumption: 99.9%.\nDepth: Level 12!",
                    buttonText: "Garbage Collect",
                    titleHex: "#E040FB"
                );
            });

            // Level 13: Kernel Panic (Red Alert)
            var kernelDrawer = memLeakDrawer.AddSubmenu(
                id: "zui_kernel_drawer",
                label: "Kernel Panic",
                paginated: false,
                breadcrumbHex: "#FF1744"
            );

            kernelDrawer.AddButton("zui_kern_btn1", "Dump Registers", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "KERNEL PANIC",
                    message: "CRITICAL PROCESS DIED: breadcrumb_overflow.sys.\nDepth: Level 13!",
                    buttonText: "BSOD",
                    titleHex: "#FF1744"
                );
            });

            // Level 14: Stack Overflow (Neon Mint)
            var stackDrawer = kernelDrawer.AddSubmenu(
                id: "zui_stack_drawer",
                label: "Stack Overflow",
                paginated: false,
                breadcrumbHex: "#69F0AE"
            );

            stackDrawer.AddButton("zui_stack_btn1", "Recurse More", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "StackOverflowException",
                    message: "PushDrawer() called 14 times without popping!\nDepth: Level 14!",
                    buttonText: "Recurse",
                    titleHex: "#69F0AE"
                );
            });

            // Level 15: The Backrooms (Yellow Wallpaper)
            var backroomsDrawer = stackDrawer.AddSubmenu(
                id: "zui_backrooms_drawer",
                label: "The Backrooms",
                paginated: false,
                breadcrumbHex: "#FFFF00"
            );

            backroomsDrawer.AddButton("zui_back_btn1", "Listen to Buzz", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "The Backrooms - Level 0",
                    message: "Monoyellow wallpaper and buzzing fluorescent lights.\nDepth: Level 15!",
                    buttonText: "No-clip Out",
                    titleHex: "#FFFF00"
                );
            });

            // Level 16: Enlightenment (Pure Blinding White)
            var enlightenmentDrawer = backroomsDrawer.AddSubmenu(
                id: "zui_enlightenment_drawer",
                label: "Enlightenment",
                paginated: false,
                breadcrumbHex: "#FFFFFF"
            );

            enlightenmentDrawer.AddButton("zui_enlight_btn1", "Final Boss", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "TRUE ENLIGHTENMENT",
                    message: "=== SUPREME BREADCRUMB OVERLORD ===\nYou descended 16 LEVELS DEEP!\nYour breadcrumb has broken the boundaries of mortal geometry!",
                    buttonText: "BOW TO KRWCLASSIC",
                    titleHex: "#64B5F6"
                );
            });

            // Page 2 Demo on Level 1 Drawer: Open Showcase Modal Panel
            exampleDrawer.AddButton("zui_ex_btn_4", "Custom Panel", () =>
            {
                ZUIPanel.Open("example_custom_panel");
            });

            // Register Example Showcase Modal Panel (Type 2 with dedicated REVERT button)
            var exPanel = ZUIPanel.Register("example_custom_panel", "MOD SHOWCASE PANEL");
            exPanel.ShowRevertButton = true;
            exPanel.AddHeader("Display & Rendering");
            exPanel.AddToggle("ex_bloom", "Cinematic Bloom", true);
            exPanel.AddSlider("ex_fov", "Field of View", 60f, 120f, 90f, wholeNumbers: true, valueFormatter: v => $"{v:F0}°");
            exPanel.AddDropdown("ex_quality", "Shader Quality", new[] { "Low", "Medium", "High", "Ultra" }, defaultIndex: 2);
            exPanel.AddDivider();
            exPanel.AddHeader("Controls & Gameplay");
            exPanel.AddToggle("ex_auto_save", "Auto Save Every 5 Mins", true);
            exPanel.AddSlider("ex_audio_pitch", "Audio Pitch Shift", 0.5f, 1.5f, 1.0f, valueFormatter: v => $"{v:F2}x");

            Plugin.Log.LogInfo("[ExampleMainMenuModule] Showcase extreme 16-level nested abyss & Type 2 panel registered successfully.");
        }
    }
}
