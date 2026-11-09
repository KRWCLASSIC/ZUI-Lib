using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;
using ZUI.Notifications;
using ZUI.PauseMenu;
using ZUI.Popups;
using ZUI.Settings;

namespace ZUI.Debug
{
    public static class ZUIDebug
    {
        private static bool _debugDrawerInitialized = false;
        private static readonly List<string> _dynamicTestButtons = new List<string>();
        private static readonly List<GameObject> _fakePlayerCards = new List<GameObject>();
        private static int _usernamePresetIndex = 0;
        private static readonly string[] _usernamePresets = new[]
        {
            "Zort",
            "Player_123",
            "Sir_LongUsername_The_Third",
            "KRWCLASSIC"
        };
        private static readonly string[] _randomFakeNamesPool = new[]
        {
            "Bob",
            "Zort",
            "Player_7",
            "KRWCLASSIC",
            "BobbyChan_2026",
            "Nightmare_Stalker_99",
            "Sir_LongUsername_The_Third",
            "The_Most_Excessively_Long_Username_Ever"
        };

        /// <summary>
        /// Registers the in-game Debug subsection under the ZUI-Lib Pause Menu tab (MORE -> ZUI-LIB -> DEBUG).
        /// </summary>
        public static void RegisterPauseDebugSubsection(PauseMenuDrawer pauseMenuZuiDrawer)
        {
            if (_debugDrawerInitialized || pauseMenuZuiDrawer == null) return;
            _debugDrawerInitialized = true;

            ZUIPauseMenu.OnPlayerCardsRefreshed += RefreshFakePlayerCards;

            var pauseDebug = pauseMenuZuiDrawer.AddSubmenu(
                id: "zui_lib_pause_debug",
                label: "Debug",
                paginated: true,
                breadcrumbHex: "#4FC3F7"
            );

            // Page 1: Fake Players & Dynamic Player Card Action Buttons
            pauseDebug.AddButton("zui_dbg_fake_add", "Add Fake Player", () =>
            {
                string name = $"Bot_{_fakePlayerCards.Count + 1}";
                SpawnFakePlayerCard(name);
            });

            pauseDebug.AddButton("zui_dbg_fake_rem", "Remove Fake Player", () =>
            {
                if (_fakePlayerCards.Count > 0)
                {
                    var last = _fakePlayerCards[_fakePlayerCards.Count - 1];
                    _fakePlayerCards.RemoveAt(_fakePlayerCards.Count - 1);
                    if (last != null) UnityEngine.Object.Destroy(last);
                    ZUIInfoText.Show($"Removed fake player. Remaining: {_fakePlayerCards.Count}", 2.0f, drawOnTop: true);
                }
                else
                {
                    ZUIInfoText.Show("No fake players to remove.", 2.0f, drawOnTop: true);
                }
            });

            pauseDebug.AddButton("zui_dbg_fake_clear", "Clear Fake Players", () =>
            {
                for (int i = 0; i < _fakePlayerCards.Count; i++)
                {
                    if (_fakePlayerCards[i] != null) UnityEngine.Object.Destroy(_fakePlayerCards[i]);
                }
                _fakePlayerCards.Clear();
                ZUIInfoText.Show("Cleared all fake player rows.", 2.0f, drawOnTop: true);
            });

            pauseDebug.AddButton("zui_dbg_btn_add", "Add Test Action Btn", () =>
            {
                int index = _dynamicTestButtons.Count + 1;
                string id = $"dbg_action_{index}";
                string tip = $"Test Action {index}";

                // Generate a crisp procedural icon with a random vibrant color
                Color randomColor = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.75f, 1.0f);
                Sprite testIcon = CreateDebugButtonIcon(44, randomColor);

                _dynamicTestButtons.Add(id);
                ZUIPauseMenu.AddPlayerActionButton(
                    id: id,
                    icon: testIcon,
                    tooltip: tip,
                    onClick: target =>
                    {
                        string targetName = target != null ? target.PlayerData.username : "Player";
                        ZUIInfoText.Show($"Test Action #{index} clicked on {targetName}!", 2.5f, drawOnTop: true);
                    },
                    hoverHex: "#" + ColorUtility.ToHtmlStringRGB(randomColor),
                    alwaysVisible: false
                );
                ZUIInfoText.Show($"Added '{tip}' ({ZUIPauseMenu.PlayerActions.Count} total actions)", 2.0f, drawOnTop: true);
            });

            pauseDebug.AddButton("zui_dbg_btn_clear", "Clear Test Action Btns", () =>
            {
                foreach (var id in _dynamicTestButtons.ToArray())
                {
                    ZUIPauseMenu.RemovePlayerActionButton(id);
                }
                _dynamicTestButtons.Clear();
                ZUIInfoText.Show("Cleared dynamic test action buttons.", 2.0f, drawOnTop: true);
            });

            // Page 2: Fake Player Name Lengths, Local Username Layout Test, Info Text Banner & Hierarchy Dump
            pauseDebug.AddButton("zui_dbg_fake_rand_len", "Random Fake Name Len", () =>
            {
                _fakePlayerCards.RemoveAll(go => go == null);
                if (_fakePlayerCards.Count == 0)
                {
                    string randomName = _randomFakeNamesPool[UnityEngine.Random.Range(0, _randomFakeNamesPool.Length)];
                    SpawnFakePlayerCard(randomName);
                    return;
                }

                for (int i = 0; i < _fakePlayerCards.Count; i++)
                {
                    var handler = _fakePlayerCards[i].GetComponent<FakePlayerCardDebugHandler>();
                    if (handler != null)
                    {
                        string randomName = _randomFakeNamesPool[UnityEngine.Random.Range(0, _randomFakeNamesPool.Length)];
                        handler.SetFakeName(randomName);
                    }
                }
                ZUIInfoText.Show($"Randomized name lengths for {_fakePlayerCards.Count} fake player(s).", 2.0f, drawOnTop: true);
            });

            pauseDebug.AddButton("zui_dbg_cycle_name", "Cycle Test Username", () =>
            {
                string nextName = _usernamePresets[_usernamePresetIndex % _usernamePresets.Length];
                _usernamePresetIndex++;

                if (PlayerController.PlayerClient.Enabled && PlayerController.PlayerClient.Value != null)
                {
                    var local = PlayerController.PlayerClient.Value;
                    var data = local.PlayerData;
                    data.username = nextName;
                    local.SetPlayerData(data, local.CurrentInterior);
                }
                ZUIPauseMenu.RefreshPlayerCards();
                ZUIInfoText.Show($"Local username set to: {nextName}", 2.5f, drawOnTop: true);
            });

            pauseDebug.AddButton("zui_dbg_info_text", "Test Info Text", () =>
            {
                ZUIInfoText.Show($"[ZUI Debug] Top-layer InfoText banner test ({DateTime.Now:HH:mm:ss})", 3.0f, drawOnTop: true);
            });

            pauseDebug.AddButton("zui_dbg_pause_dump", "Dump Hierarchy", () =>
            {
                DumpHierarchy();
                ZUIPopup.ShowInfo(
                    title: "Hierarchy Dumped",
                    message: "Full Pause Menu & In-Game UI hierarchy exported to BepInEx log.",
                    buttonText: "OK",
                    titleHex: "#64B5F6"
                );
            });

            pauseDebug.AddButton("zui_dbg_crash_missing_method", "Test API Crash", () =>
            {
                typeof(ZUIAPI).InvokeMember(
                    "OpenQuantumHoloMenu_v2",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                    null,
                    null,
                    new object[] { "Sector_7" }
                );
            });
        }

        private static Sprite CreateDebugButtonIcon(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size * 0.42f;
            float innerRadius = size * 0.24f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;

                    if (dist >= innerRadius && dist <= outerRadius)
                    {
                        float alpha = Mathf.SmoothStep(0f, 1f, (outerRadius - dist) / 1.8f) *
                                      Mathf.SmoothStep(0f, 1f, (dist - innerRadius) / 1.8f);
                        pixels[idx] = new Color(color.r, color.g, color.b, alpha);
                    }
                    else if (dist < innerRadius * 0.55f)
                    {
                        float coreAlpha = Mathf.SmoothStep(0f, 1f, (innerRadius * 0.55f - dist) / 1.5f);
                        pixels[idx] = new Color(1f, 1f, 1f, coreAlpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static void RemoveFakePlayer(GameObject card)
        {
            if (card == null) return;
            _fakePlayerCards.Remove(card);
            UnityEngine.Object.Destroy(card);
        }

        private static void SpawnFakePlayerCard(string name)
        {
            Transform playersParent = null;
            GameObject canvas = GameObject.Find("InGameUICanvas");
            if (canvas != null)
            {
                playersParent = canvas.transform.Find("pausePanel/Players/Players");
            }
            if (playersParent == null)
            {
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go.name == "Players" && go.transform.parent != null && go.transform.parent.name == "Players" && go.scene.IsValid())
                    {
                        playersParent = go.transform;
                        break;
                    }
                }
            }

            if (playersParent == null)
            {
                Plugin.Log.LogWarning("[ZUI Debug] Could not find pause menu Players container.");
                return;
            }

            GameObject templateCard = null;
            for (int i = 0; i < playersParent.childCount; i++)
            {
                var child = playersParent.GetChild(i).gameObject;
                if (child.name.StartsWith("EscapePlayerUI", StringComparison.OrdinalIgnoreCase))
                {
                    templateCard = child;
                    break;
                }
            }

            if (templateCard == null)
            {
                Plugin.Log.LogWarning("[ZUI Debug] No EscapePlayerUI card found to clone in Players container.");
                return;
            }

            var clonedObj = UnityEngine.Object.Instantiate(templateCard, playersParent);
            clonedObj.name = $"EscapePlayerUI_Fake_{name}";
            clonedObj.SetActive(true);

            for (int i = clonedObj.transform.childCount - 1; i >= 0; i--)
            {
                var child = clonedObj.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("ZUI_ActionBtn_") || child.name.StartsWith("ZUI_PrevArrow") || child.name.StartsWith("ZUI_NextArrow"))
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }

            var cardImg = clonedObj.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.raycastTarget = true;
            }

            var debugHandler = clonedObj.AddComponent<FakePlayerCardDebugHandler>();
            debugHandler.Setup(name);

            _fakePlayerCards.Add(clonedObj);
            ZUIInfoText.Show($"Spawned fake player '{name}' ({_fakePlayerCards.Count} active)", 2.0f, drawOnTop: true);
        }

        public static void RefreshFakePlayerCards()
        {
            _fakePlayerCards.RemoveAll(go => go == null);
            for (int i = 0; i < _fakePlayerCards.Count; i++)
            {
                var handler = _fakePlayerCards[i].GetComponent<FakePlayerCardDebugHandler>();
                if (handler != null)
                {
                    handler.Rebuild();
                }
            }
        }

        public static void DumpHierarchy()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== [ZUI] SETTINGS HIERARCHY DUMP ===");

            GameObject canvas = ZUISettings.FindCanvas();
            if (canvas == null)
            {
                sb.AppendLine("Settings Canvas could not be found in active scene (Settings panel not yet opened).");
            }
            else
            {
                Transform content = canvas.transform.Find("settingsPanel/Panel/Scroll View/Viewport/Content");
                if (content == null)
                {
                    sb.AppendLine($"Content path not found under canvas '{canvas.name}'!");
                }
                else
                {
                    sb.AppendLine($"Content: '{content.name}' (childCount={content.childCount}, activeSelf={content.gameObject.activeSelf}, activeInHierarchy={content.gameObject.activeInHierarchy})");
                    for (int i = 0; i < content.childCount; i++)
                    {
                        DumpTransform(content.GetChild(i), sb, "  ");
                    }
                }
            }

            sb.AppendLine("=== [ZUI] END SETTINGS HIERARCHY DUMP ===");

            // 2. Popup Manager & Active Popups Dump
            try
            {
                var popupManager = InitUI.PopupManager;
                if (popupManager != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== [ZUI] POPUP HIERARCHY DUMP ===");
                    var parentField = typeof(PopupManager).GetField("popupParent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var popupParent = parentField?.GetValue(popupManager) as Transform;
                    if (popupParent != null)
                    {
                        sb.AppendLine($"PopupParent: '{popupParent.name}' (childCount={popupParent.childCount}, activeSelf={popupParent.gameObject.activeSelf}, inHierarchy={popupParent.gameObject.activeInHierarchy})");
                        for (int i = 0; i < popupParent.childCount; i++)
                        {
                            DumpTransform(popupParent.GetChild(i), sb, "  ");
                        }
                    }
                    else
                    {
                        sb.AppendLine("PopupParent: null");
                    }
                    sb.AppendLine($"Active Popups Reported by Manager: {popupManager.GetPopupCount}");

                    var prefabField = typeof(PopupManager).GetField("popupPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var popupPrefab = prefabField?.GetValue(popupManager) as Popup;
                    if (popupPrefab != null)
                    {
                        sb.AppendLine($"PopupPrefab: '{popupPrefab.name}'");
                        DumpTransform(popupPrefab.transform, sb, "  ");
                    }

                    sb.AppendLine("=== [ZUI] END POPUP HIERARCHY DUMP ===");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ZUI Popup Dump Error] {ex.Message}");
            }

            // 3. Main Menu Buttons & Social Tray Dump
            try
            {
                sb.AppendLine();
                sb.AppendLine("=== [ZUI] MAIN MENU HIERARCHY DUMP ===");

                var mmm = StaticInstance<MainMenuManager>.Instance;
                if (mmm != null)
                {
                    var hostField = typeof(MainMenuManager).GetField("hostButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var hostSelectable = hostField?.GetValue(mmm) as Selectable;
                    if (hostSelectable != null)
                    {
                        var btnContainer = hostSelectable.transform.parent;
                        sb.AppendLine($"Main Menu Buttons Container: '{btnContainer.name}' under parent '{(btnContainer.parent != null ? btnContainer.parent.name : "null")}'");
                        DumpTransform(btnContainer, sb, "  ", maxDepth: 4);
                    }
                    else
                    {
                        sb.AppendLine("hostButton field is null on MainMenuManager!");
                    }
                }
                else
                {
                    sb.AppendLine("MainMenuManager.Instance is null! (Not in MainMenu scene?)");
                }

#pragma warning disable CS0618
                var buttonFunctions = UnityEngine.Object.FindObjectsOfType<ButtonFunctions>();
#pragma warning restore CS0618
                sb.AppendLine($"\nDiscovered {buttonFunctions.Length} ButtonFunctions instances in scene:");
                var dumpedParents = new System.Collections.Generic.HashSet<Transform>();
                foreach (var bf in buttonFunctions)
                {
                    var p = bf.transform.parent ?? bf.transform;
                    if (dumpedParents.Add(p))
                    {
                        sb.AppendLine($"\n--- Tree under '{p.name}' (holding ButtonFunctions on '{bf.gameObject.name}') ---");
                        DumpTransform(p, sb, "  ", maxDepth: 4);
                    }
                }

                sb.AppendLine("=== [ZUI] END MAIN MENU HIERARCHY DUMP ===");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ZUI Main Menu Dump Error] {ex.Message}\n{ex.StackTrace}");
            }

            string fullDump = sb.ToString();
            Plugin.Log.LogInfo(fullDump);
        }

        private static void DumpTransform(Transform t, StringBuilder sb, string indent, int currentDepth = 0, int maxDepth = 6)
        {
            if (currentDepth > maxDepth) return;

            var go = t.gameObject;
            var comps = go.GetComponents<Component>();
            string compList = string.Join(", ", comps.Where(c => c != null).Select(c => c.GetType().Name));

            string rectInfo = "";
            if (t is RectTransform rt)
            {
                rectInfo = $" [size=({rt.sizeDelta.x:0},{rt.sizeDelta.y:0}), pos=({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0}), anchors=({rt.anchorMin.x:0.00},{rt.anchorMin.y:0.00})-({rt.anchorMax.x:0.00},{rt.anchorMax.y:0.00})]";
            }

            string textInfo = "";
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                string fontName = tmp.font != null ? tmp.font.name : "null";
                textInfo = $" [TMP_Text: \"{tmp.text}\", font={fontName}, style={tmp.fontStyle}, color={tmp.color}, enabled={tmp.enabled}]";
            }

            sb.AppendLine($"{indent}- [{t.GetSiblingIndex()}] '{go.name}' (activeSelf={go.activeSelf}, inHierarchy={go.activeInHierarchy}){rectInfo}{textInfo} [{compList}]");

            if (go.TryGetComponent<VerticalLayoutGroup>(out var vlg))
            {
                sb.AppendLine($"{indent}    [VerticalLayoutGroup: spacing={vlg.spacing}, padding=({vlg.padding.left},{vlg.padding.right},{vlg.padding.top},{vlg.padding.bottom}), childControl=({vlg.childControlWidth},{vlg.childControlHeight}), childForceExpand=({vlg.childForceExpandWidth},{vlg.childForceExpandHeight}), childAlignment={vlg.childAlignment}]");
            }
            if (go.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
            {
                sb.AppendLine($"{indent}    [HorizontalLayoutGroup: spacing={hlg.spacing}, padding=({hlg.padding.left},{hlg.padding.right},{hlg.padding.top},{hlg.padding.bottom}), childControl=({hlg.childControlWidth},{hlg.childControlHeight}), childForceExpand=({hlg.childForceExpandWidth},{hlg.childForceExpandHeight}), childAlignment={hlg.childAlignment}]");
            }
            if (go.TryGetComponent<LayoutElement>(out var le))
            {
                sb.AppendLine($"{indent}    [LayoutElement: pref=({le.preferredWidth},{le.preferredHeight}), min=({le.minWidth},{le.minHeight}), flex=({le.flexibleWidth},{le.flexibleHeight})]");
            }
            if (go.TryGetComponent<Button>(out var btn))
            {
                sb.AppendLine($"{indent}    [Button: interactable={btn.interactable}, transition={btn.transition}, targetGraphic={(btn.targetGraphic != null ? btn.targetGraphic.name : "null")}]");
            }

            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransform(t.GetChild(i), sb, indent + "    ", currentDepth + 1, maxDepth);
            }
        }
    }
}
