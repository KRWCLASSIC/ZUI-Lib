using UnityEngine;
using ZUI.Core;
using ZUI.Dialogue;
using ZUI.Notifications;
using ZUI.Panels;
using ZUI.PauseMenu;
using ZUI.Popups;
using ZUI.Screens;

namespace ZUI.Example
{
    /// <summary>
    /// Example showcase module demonstrating custom Pause Menu button registration,
    /// 5-slot paginated drawers, nested submenus, custom breadcrumbs, in-game modal panel shortcuts,
    /// right-column views, and standalone custom split screens.
    /// </summary>
    public class ExamplePauseMenuModule : IZUIModule
    {
        public string Name => "ExamplePauseMenuModule";

        public void Initialize()
        {
            Plugin.Log.LogInfo("[ExamplePauseMenuModule] Registering showcase Pause Menu buttons & drawers...");

            // 1. Register a dedicated showcase settings modal panel for this example mod
            var demoPanel = ZUIPanel.Register("zui_example_demo_panel", "SHOWCASE SETTINGS");
            demoPanel.AddHeader("Gameplay Tweaks (Demo)");
            demoPanel.AddToggle("ex_opt_godmode", "Invulnerability", false, val =>
            {
                Plugin.Log.LogInfo($"[ExamplePauseMenuModule] Invulnerability set to: {val}");
            });
            demoPanel.AddSlider("ex_opt_speed", "Walk Speed", 1.0f, 3.0f, 1.0f, valueFormatter: v => $"{v:F1}x");
            demoPanel.AddDivider();
            demoPanel.AddHeader("Atmosphere & UI");
            demoPanel.AddDropdown("ex_opt_weather", "Weather Preset", new[] { "Default", "Heavy Rain", "Dense Fog", "Blood Moon" }, 0);
            demoPanel.AddToggle("ex_custom_player_header", "Custom Survivors Header", false, val =>
            {
                if (val)
                {
                    ZUIPauseMenu.SetPlayersHeader("SURVIVORS", "[CO-OP]", titleHex: "#81C784", counterHex: "#4FC3F7");
                }
                else
                {
                    ZUIPauseMenu.ResetPlayersHeader();
                }
            });

            // 2. Register custom action buttons on official connected player rows:
            // (a) Teleport to Player — hidden on yourself (!p.IsMine), just like the vanilla Kick button
            ZUIPauseMenu.AddPlayerActionButton(
                id: "ex_tp_to_player",
                icon: ZUIImage.LoadEmbedded("teleport.png"),
                tooltip: "Teleport to Player",
                onClick: targetPlayer =>
                {
                    if (targetPlayer == null) return;

                    var localPlayer = PlayerController.PlayerClient.Value;
                    if (localPlayer == null) return;

                    localPlayer.Teleport(targetPlayer.transform.position + Vector3.up * 0.25f);
                    string username = targetPlayer.PlayerData.username ?? "Player";
                    ZUIInfoText.Show($"Teleported to {username}!");
                    ZUIPauseMenu.Close();
                },
                hoverHex: "#4FC3F7",
                alwaysVisible: false,
                visibilityPredicate: p => p == null || !p.IsMine
            );

            // 3. Root "Example" Drawer under MORE -> houses all example showcases, skin changer, and dialogue tests
            var exampleDrawer = ZUIPauseMenu.RegisterSubmenu(
                id: "zui_ex_root",
                label: "Example",
                paginated: false,
                priority: 0,
                breadcrumbHex: "#FFD54F"
            );

            // 3a. Phase 4 Showcase: Skin Changer Drawer under MORE / Example /
            var skinChangerDrawer = BuildSkinChangerDrawer(exampleDrawer);

            // (b) Skin Changer button on player cards — opens MORE -> Example -> Skin Changer!
            ZUIPauseMenu.AddPlayerActionButton(
                id: "ex_open_skin_changer",
                icon: ExampleIcons.CreateSkinIcon(44),
                tooltip: "Open Skin Changer",
                onClick: targetPlayer =>
                {
                    string targetName = targetPlayer != null && !string.IsNullOrEmpty(targetPlayer.PlayerData.username)
                        ? targetPlayer.PlayerData.username
                        : "Local Player";

                    skinChangerDrawer.RightPanel.SetHeader(
                        title: $"SKIN CHANGER — {targetName.ToUpperInvariant()}",
                        counter: "(4 SKINS)",
                        titleHex: "#FFD54F",
                        counterHex: "#4FC3F7"
                    );
                    ZUIPauseMenu.SetMode(PauseMenuMode.Vanilla);
                    ZUIPauseMenu.PushDrawer(exampleDrawer);
                    ZUIPauseMenu.PushDrawer(skinChangerDrawer);
                },
                hoverHex: "#FFD54F",
                alwaysVisible: false,
                visibilityPredicate: null
            );

            // 3b. Showcase Drawer under MORE / Example / (Paginated with 5 slots per page)
            var showcaseDrawer = exampleDrawer.AddSubmenu(
                id: "zui_ex_pause_drawer",
                label: "Showcase",
                paginated: true,
                priority: 0,
                breadcrumbHex: "#FFD54F"
            );

            // Page 1 - Slots 1 to 5
            showcaseDrawer.AddButton("zui_pause_demo_1", "Info Dialog", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Pause Menu Dialog",
                    message: "This is a demo dialog spawned from the in-game Pause Menu!",
                    buttonText: "Awesome",
                    titleHex: "#81C784"
                );
            });

            showcaseDrawer.AddButton("zui_pause_demo_2", "Confirmation", () =>
            {
                ZUIPopup.ShowConfirmation(
                    title: "Pause Action",
                    message: "Do you want to execute this test action?\n(ESC dismisses triggering Cancel)",
                    yesText: "Confirm",
                    noText: "Cancel",
                    titleHex: "#FFD54F",
                    dismissOnEscape: true,
                    escapeChoice: PopupChoice.Secondary
                );
            });

            // Opens the showcase module's dedicated modal panel
            showcaseDrawer.AddButton("zui_pause_demo_3", "Mod Settings", () =>
            {
                ZUIPanel.Open("zui_example_demo_panel");
            });

            showcaseDrawer.AddButton("zui_pause_demo_4", "Hierarchy Dump", () =>
            {
                ZUI.Debug.ZUIDebug.DumpHierarchy();
                ZUIPopup.ShowInfo(
                    title: "Exported",
                    message: "In-game hierarchy dumped to BepInEx log.",
                    buttonText: "OK",
                    titleHex: "#64B5F6"
                );
            });

            // Level 2: Nested Submenu
            var subDrawer = showcaseDrawer.AddSubmenu(
                id: "zui_pause_sub_drawer",
                label: "Submenu",
                paginated: false,
                breadcrumbHex: "#4DD0E1"
            );

            subDrawer.AddButton("zui_sub_1", "Deep Echo", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Deep Echo",
                    message: "You are inside a nested Pause Menu drawer!",
                    buttonText: "Cool",
                    titleHex: "#4DD0E1"
                );
            });

            // Level 3: Nested Abyss
            var abyssDrawer = subDrawer.AddSubmenu(
                id: "zui_pause_abyss_drawer",
                label: "Sanctum",
                paginated: false,
                breadcrumbHex: "#BA68C8"
            );

            abyssDrawer.AddButton("zui_abyss_1", "Sanctum Light", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Sanctum Light",
                    message: "3 levels deep into the Pause Menu stack!",
                    buttonText: "Radiant",
                    titleHex: "#BA68C8"
                );
            });

            // Page 2 items for showcaseDrawer (exceeds 5 slots -> triggers pagination on Slot 7!)
            showcaseDrawer.AddButton("zui_pause_page2_1", "Page 2 Item A", () =>
            {
                ZUIPopup.ShowInfo("Page 2", "Page 2 Slot 1 action executed!", "OK");
            });

            showcaseDrawer.AddButton("zui_pause_page2_2", "Page 2 Item B", () =>
            {
                ZUIPopup.ShowInfo("Page 2", "Page 2 Slot 2 action executed!", "OK");
            });

            showcaseDrawer.AddButton("zui_pause_page2_3", "Page 2 Item C", () =>
            {
                ZUIPopup.ShowInfo("Page 2", "Page 2 Slot 3 action executed!", "OK");
            });

            // 3c. Dialogue & Native Alerts Drawer under MORE / Example /
            var dialogueDrawer = exampleDrawer.AddSubmenu(
                id: "zui_ex_pause_dialogue",
                label: "Dialogue",
                paginated: true,
                priority: -10,
                breadcrumbHex: "#4FC3F7"
            );

            // Full Multi-Line Dialogue 1: Telerick & Jimmy (The Elevator Trick)
            dialogueDrawer.AddButton("zui_dia_full1", "Telerick: Elevator Trick", () =>
            {
                ExampleDialogueShowcase.TriggerDialogueWithEpisodePrompt("Elevator Trick", ExampleDialogueShowcase.BuildElevatorTrickSequence);
            });

            // Full Multi-Line Dialogue 2: Telerick First Meet
            dialogueDrawer.AddButton("zui_dia_full2", "Telerick: First Meet", () =>
            {
                ExampleDialogueShowcase.TriggerDialogueWithEpisodePrompt("First Meet", ExampleDialogueShowcase.BuildFirstMeetSequence);
            });

            // One-off Subtitle 1: Jimmy Warning
            dialogueDrawer.AddButton("zui_dia_test1", "Jimmy Subtitle", () =>
            {
                ZUIPauseMenu.Close();
                ZUISubtitle.Show("Jimmy", "Don't touch that button, it's for the upper factory!", "#4FC3F7", 5.0f);
            });

            // One-off Subtitle 2: Mudur Threat
            dialogueDrawer.AddButton("zui_dia_test2", "Mudur Subtitle", () =>
            {
                ZUIPauseMenu.Close();
                ZUISubtitle.Show("Mudur", "I can smell your fear...", "#E53935", 5.0f);
            });

            // One-off Banner: Keycard Alert
            dialogueDrawer.AddButton("zui_dia_test3", "Keycard Alert", () =>
            {
                ZUIPauseMenu.Close();
                ZUIInfoText.Show("Security Override Required: Level 2 Keycard", 3.0f);
            });

            // Page 2: Checkpoint Banner
            dialogueDrawer.AddButton("zui_dia_test4", "Checkpoint Banner", () =>
            {
                ZUIPauseMenu.Close();
                ZUIInfoText.Show("Checkpoint Reached!", 2.5f);
            });

            // Page 2: Alert + Subtitle Combo
            dialogueDrawer.AddButton("zui_dia_test5", "Combo Sequence", () =>
            {
                ZUIPauseMenu.Close();
                ZUIInfoText.Show("Air filtration restored", 3.0f);
                ZUISubtitle.Show("Jimmy", "Way is clear! Move move move!", "#81C784", 4.5f);
            });

            // 3d. Phase 5 Showcase: Standalone Split Screen ("Map Terminal") under MORE / Example /
            var terminalScreen = BuildMapTerminalScreen();
            exampleDrawer.AddButton("zui_ex_open_terminal", "Map Terminal", () =>
            {
                terminalScreen.Open();
            });
        }

        #region Phase 5: Standalone Custom Split Screen Showcase (ZUIScreen)

        private static ZUIScreen BuildMapTerminalScreen()
        {
            var screen = ZUIScreen.Register(
                id: "zui_ex_map_terminal",
                title: "MAP TERMINAL",
                paginated: false,
                titleHex: "#4FC3F7"
            );

            screen.SetRightHeader("SECTOR OVERVIEW", "[ONLINE]", titleHex: "#4FC3F7", counterHex: "#81C784");

            PauseMenuRightCard cardAlpha = null;
            PauseMenuRightCard cardBeta = null;
            PauseMenuRightCard cardGamma = null;

            void SelectSector(string sectorName, PauseMenuRightCard activeCard)
            {
                if (cardAlpha != null)
                {
                    cardAlpha.SetText("Sector Alpha — Main Facility", activeCard == cardAlpha ? "[SELECTED] Power: 100%" : "Click to focus sector");
                }
                if (cardBeta != null)
                {
                    cardBeta.SetText("Sector Beta — Underground Labs", activeCard == cardBeta ? "[SELECTED] Power: 64%" : "Click to focus sector");
                }
                if (cardGamma != null)
                {
                    cardGamma.SetText("Sector Gamma — Quantum Elevator", activeCard == cardGamma ? "[SELECTED] Power: OFFLINE" : "Click to focus sector");
                }
                ZUIInfoText.Show($"Terminal focused on {sectorName}", 2.0f, drawOnTop: true);
            }

            cardAlpha = screen.AddCard(
                id: "term_sec_alpha",
                title: "Sector Alpha — Main Facility",
                subtitle: "[SELECTED] Power: 100%",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.31f, 0.76f, 0.97f)),
                onClick: () => SelectSector("Sector Alpha", cardAlpha),
                height: 76f,
                titleHex: "#4FC3F7"
            );

            cardBeta = screen.AddCard(
                id: "term_sec_beta",
                title: "Sector Beta — Underground Labs",
                subtitle: "Click to focus sector",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(1.0f, 0.80f, 0.16f)),
                onClick: () => SelectSector("Sector Beta", cardBeta),
                height: 76f,
                titleHex: "#FFD54F"
            );

            cardGamma = screen.AddCard(
                id: "term_sec_gamma",
                title: "Sector Gamma — Quantum Elevator",
                subtitle: "Click to focus sector",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.90f, 0.22f, 0.21f)),
                onClick: () => SelectSector("Sector Gamma", cardGamma),
                height: 76f,
                titleHex: "#EF5350"
            );

            screen.AddRightButton(
                id: "term_ping_btn",
                label: "Ping Active Sector Beacons",
                onClick: () => ZUIInfoText.Show("Sector beacons pinged! 3 signals detected.", 2.5f, drawOnTop: true),
                height: 52f,
                textHex: "#81C784"
            );

            // Left column navigation buttons on the standalone screen
            screen.AddButton("term_left_alpha", "Sector Alpha", () => SelectSector("Sector Alpha", cardAlpha));
            screen.AddButton("term_left_beta", "Sector Beta", () => SelectSector("Sector Beta", cardBeta));
            screen.AddButton("term_left_gamma", "Sector Gamma", () => SelectSector("Sector Gamma", cardGamma));

            // Nested Submenu inside the standalone screen with its own dedicated right-side view
            var diagnosticsSub = screen.AddSubmenu(
                id: "term_sub_diag",
                label: "Diagnostics",
                paginated: false,
                breadcrumbHex: "#81C784"
            );

            diagnosticsSub.RightPanel.SetHeader("SYSTEM DIAGNOSTICS", "[3 LOGS]", titleHex: "#81C784", counterHex: "#FFD54F");
            diagnosticsSub.RightPanel.AddCard(
                id: "diag_log_1",
                title: "REACTOR CORE",
                subtitle: "Nominal output (4,200 kW)",
                height: 48f,
                titleHex: "#81C784"
            );
            diagnosticsSub.RightPanel.AddCard(
                id: "diag_log_2",
                title: "AIR FILTRATION",
                subtitle: "Secondary scrubbers active",
                height: 48f,
                titleHex: "#4FC3F7"
            );
            diagnosticsSub.RightPanel.AddCard(
                id: "diag_log_3",
                title: "ELEVATOR LINK",
                subtitle: "Awaiting 3 cabin generators",
                height: 48f,
                titleHex: "#FFD54F"
            );

            diagnosticsSub.AddButton("diag_run_scan", "Run Full Scan", () =>
            {
                ZUIInfoText.Show("Diagnostics scan complete: All primary systems nominal.", 2.5f, drawOnTop: true);
            });

            return screen;
        }

        #endregion

        #region Phase 4: Custom Right-Side Panel Showcase (Skin Changer & Vertical Resizing)

        private static PauseMenuDrawer BuildSkinChangerDrawer(PauseMenuDrawer parentDrawer)
        {
            var skinDrawer = parentDrawer.AddSubmenu(
                id: "zui_ex_skin_changer",
                label: "Skin Changer",
                paginated: false,
                priority: -5,
                breadcrumbHex: "#FFD54F"
            );

            var rightPanel = skinDrawer.RightPanel;
            rightPanel.SetHeader("SKIN CHANGER", "(4 SKINS)", titleHex: "#FFD54F", counterHex: "#4FC3F7");

            PauseMenuRightCard cardBlue = null;
            PauseMenuRightCard cardRed = null;
            PauseMenuRightCard cardYellow = null;
            PauseMenuRightCard cardGreen = null;

            const string EquippedBtnHex = "#66BB6A";
            const string EquippedHoverHex = "#A5D6A7";
            const string UnequippedBtnHex = "#546E7A";
            const string UnequippedHoverHex = "#90A4AE";

            void SelectSkin(string skinName, PauseMenuRightCard selectedCard)
            {
                if (cardBlue != null)
                {
                    bool eq = selectedCard == cardBlue;
                    cardBlue.SetText("Default Blue", eq ? "[EQUIPPED]" : "Click to equip");
                    cardBlue.SetRightButtonColors("equip_blue", eq ? EquippedBtnHex : UnequippedBtnHex, eq ? EquippedHoverHex : UnequippedHoverHex);
                }
                if (cardRed != null)
                {
                    bool eq = selectedCard == cardRed;
                    cardRed.SetText("Red", eq ? "[EQUIPPED]" : "Click to equip");
                    cardRed.SetRightButtonColors("equip_red", eq ? EquippedBtnHex : UnequippedBtnHex, eq ? EquippedHoverHex : UnequippedHoverHex);
                }
                if (cardYellow != null)
                {
                    bool eq = selectedCard == cardYellow;
                    cardYellow.SetText("Yellow", eq ? "[EQUIPPED]" : "Click to equip");
                    cardYellow.SetRightButtonColors("equip_yellow", eq ? EquippedBtnHex : UnequippedBtnHex, eq ? EquippedHoverHex : UnequippedHoverHex);
                }
                if (cardGreen != null)
                {
                    bool eq = selectedCard == cardGreen;
                    cardGreen.SetText("Green", eq ? "[EQUIPPED]" : "Click to equip");
                    cardGreen.SetRightButtonColors("equip_green", eq ? EquippedBtnHex : UnequippedBtnHex, eq ? EquippedHoverHex : UnequippedHoverHex);
                }

                ZUIInfoText.Show($"Equipped Skin: {skinName}", 2.5f, drawOnTop: true);
            }

            Sprite circleIndicatorIcon = ExampleIcons.CreateCircleIndicatorIcon(44, Color.white);

            // --- Style 1: Player-Bar Card Elements (72px height, icon, subtitle, whole-card click + right circle status button) ---
            cardBlue = rightPanel.AddCard(
                id: "skin_blue",
                title: "Default Blue",
                subtitle: "[EQUIPPED]",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.31f, 0.76f, 0.97f)),
                onClick: () => SelectSkin("Default Blue", cardBlue),
                height: 72f,
                titleHex: "#4FC3F7"
            );
            cardBlue.AddRightButton(
                id: "equip_blue",
                icon: circleIndicatorIcon,
                tooltip: "Equip Default Blue",
                onClick: () => SelectSkin("Default Blue", cardBlue),
                hoverHex: EquippedHoverHex,
                normalHex: EquippedBtnHex
            );

            cardRed = rightPanel.AddCard(
                id: "skin_red",
                title: "Red",
                subtitle: "Click to equip",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.90f, 0.22f, 0.21f)),
                onClick: () => SelectSkin("Red", cardRed),
                height: 72f,
                titleHex: "#EF5350"
            );
            cardRed.AddRightButton(
                id: "equip_red",
                icon: circleIndicatorIcon,
                tooltip: "Equip Red",
                onClick: () => SelectSkin("Red", cardRed),
                hoverHex: UnequippedHoverHex,
                normalHex: UnequippedBtnHex
            );

            cardYellow = rightPanel.AddCard(
                id: "skin_yellow",
                title: "Yellow",
                subtitle: "Click to equip",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(1.0f, 0.80f, 0.16f)),
                onClick: () => SelectSkin("Yellow", cardYellow),
                height: 72f,
                titleHex: "#FFD54F"
            );
            cardYellow.AddRightButton(
                id: "equip_yellow",
                icon: circleIndicatorIcon,
                tooltip: "Equip Yellow",
                onClick: () => SelectSkin("Yellow", cardYellow),
                hoverHex: UnequippedHoverHex,
                normalHex: UnequippedBtnHex
            );

            cardGreen = rightPanel.AddCard(
                id: "skin_green",
                title: "Green",
                subtitle: "Click to equip",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.51f, 0.78f, 0.52f)),
                onClick: () => SelectSkin("Green", cardGreen),
                height: 72f,
                titleHex: "#81C784"
            );
            cardGreen.AddRightButton(
                id: "equip_green",
                icon: circleIndicatorIcon,
                tooltip: "Equip Green",
                onClick: () => SelectSkin("Green", cardGreen),
                hoverHex: UnequippedHoverHex,
                normalHex: UnequippedBtnHex
            );

            // --- Style 2: Left-Side Button Elements on the Right Panel (with custom vertical heights!) ---
            rightPanel.AddButton(
                id: "skin_btn_random",
                label: "Randomize Skin",
                onClick: () =>
                {
                    var pool = new[]
                    {
                        ("Default Blue", cardBlue),
                        ("Red", cardRed),
                        ("Yellow", cardYellow),
                        ("Green", cardGreen)
                    };
                    var pick = pool[UnityEngine.Random.Range(0, pool.Length)];
                    SelectSkin(pick.Item1, pick.Item2);
                },
                height: 52f,
                textHex: "#FFD54F"
            );

            rightPanel.AddButton(
                id: "skin_btn_reset",
                label: "Reset to Default Blue",
                onClick: () => SelectSkin("Default Blue", cardBlue),
                height: 44f,
                textHex: "#B0BEC5"
            );

            // Left-side drawer buttons while in Skin Changer
            skinDrawer.AddButton("skin_left_random", "Quick Randomize", () =>
            {
                var pool = new[]
                {
                    ("Default Blue", cardBlue),
                    ("Red", cardRed),
                    ("Yellow", cardYellow),
                    ("Green", cardGreen)
                };
                var pick = pool[UnityEngine.Random.Range(0, pool.Length)];
                SelectSkin(pick.Item1, pick.Item2);
            });

            skinDrawer.AddButton("skin_left_reset", "Reset Default", () =>
            {
                SelectSkin("Default Blue", cardBlue);
            });

            // Nested Submenu: Vertical Resizing Showcase (Compact 40px, Standard 72px, Tall 104px)
            var sizingDrawer = skinDrawer.AddSubmenu(
                id: "zui_ex_right_sizing",
                label: "Sizing Demo",
                paginated: false,
                breadcrumbHex: "#4FC3F7"
            );

            var sizingPanel = sizingDrawer.RightPanel;
            sizingPanel.SetHeader("VERTICAL SIZING DEMO", "[RESIZABLE]", titleHex: "#4FC3F7", counterHex: "#81C784");

            // Compact 40px Cards
            sizingPanel.AddCard(
                id: "size_compact_1",
                title: "COMPACT CARD (40PX)",
                subtitle: "Single-line dense log entry",
                icon: ExampleIcons.CreateSkinIcon(32, new Color(0.31f, 0.76f, 0.97f)),
                onClick: () => ZUIInfoText.Show("Clicked 40px Compact Card A", drawOnTop: true),
                height: 40f,
                titleHex: "#4FC3F7"
            );

            sizingPanel.AddCard(
                id: "size_compact_2",
                title: "COMPACT CARD B (40PX)",
                subtitle: "No-icon compact status row",
                icon: null,
                onClick: () => ZUIInfoText.Show("Clicked 40px Compact Card B", drawOnTop: true),
                height: 40f
            );

            // Standard 72px Card
            sizingPanel.AddCard(
                id: "size_standard",
                title: "Standard 72px Card",
                subtitle: "Matches native EscapePlayerUI height",
                icon: ExampleIcons.CreateSkinIcon(44, new Color(0.51f, 0.78f, 0.52f)),
                onClick: () => ZUIInfoText.Show("Clicked 72px Standard Card", drawOnTop: true),
                height: 72f,
                titleHex: "#81C784"
            );

            // Tall 108px Multi-Line Card
            sizingPanel.AddCard(
                id: "size_tall",
                title: "Tall Dossier Card (108px)",
                subtitle: "Supports multi-line wrapped descriptions for quest objectives, item inspect cards, or detailed mod changelogs.",
                icon: ExampleIcons.CreateSkinIcon(56, new Color(1.0f, 0.80f, 0.16f)),
                onClick: () => ZUIInfoText.Show("Clicked 108px Tall Card", drawOnTop: true),
                height: 108f,
                titleHex: "#FFD54F"
            );

            // Compact 38px Button & Tall 68px Button
            sizingPanel.AddButton(
                id: "size_btn_compact",
                label: "Compact Button Style (38px)",
                onClick: () => ZUIInfoText.Show("Clicked 38px Compact Button!", drawOnTop: true),
                height: 38f,
                textHex: "#80DEEA"
            );

            sizingPanel.AddButton(
                id: "size_btn_tall",
                label: "Tall Action Button Style (68px)",
                onClick: () => ZUIInfoText.Show("Clicked 68px Tall Button!", drawOnTop: true),
                height: 68f,
                textHex: "#FFD54F"
            );

            sizingDrawer.AddButton("sizing_left_info", "About Sizing", () =>
            {
                ZUIPopup.ShowInfo(
                    title: "Vertical Resizing",
                    message: "Every Card and Button on the right panel accepts any custom height in pixels via height parameter or .SetHeight(px)!",
                    buttonText: "Got It",
                    titleHex: "#4FC3F7"
                );
            });

            return skinDrawer;
        }

        #endregion
    }
}
