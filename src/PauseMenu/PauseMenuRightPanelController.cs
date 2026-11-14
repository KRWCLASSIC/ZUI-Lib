using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Internal runtime controller that manages the ZUI_CustomRightPanel container alongside
    /// the native Connected Players list (<c>pausePanel/Players/Players</c>) and populates
    /// right-side elements for both Pause Menu drawers and standalone <see cref="Screens.ZUIScreen"/> views.
    /// </summary>
    internal static class PauseMenuRightPanelController
    {
        private static GameObject _vanillaPlayersListObj;
        private static GameObject _customRightPanelObj;
        private static Transform _customTopTransform;
        private static readonly List<GameObject> _spawnedItems = new List<GameObject>();

        internal static PauseMenuRightPanel CurrentActivePanel { get; private set; }

        /// <summary>
        /// Synchronizes the Pause Menu's right-side column with the currently active drawer.
        /// If the active drawer has a configured <see cref="PauseMenuRightPanel"/>, hides the vanilla
        /// player list and displays the custom panel; otherwise restores the vanilla player list.
        /// </summary>
        public static void SyncWithCurrentDrawer()
        {
            var tabController = PauseMenuTabController.Instance;
            bool shouldShowCustom =
                tabController != null &&
                tabController.CurrentMode == PauseMenuMode.Tabbed &&
                tabController.CurrentDrawer != null &&
                tabController.CurrentDrawer.HasRightPanel;

            if (!shouldShowCustom)
            {
                RestoreVanillaPlayersList();
                return;
            }

            if (!EnsureCustomRightPanelContainer())
            {
                return;
            }

            // Hide vanilla Connected Players container (preserving all live EscapePlayerUI instances inside it)
            if (_vanillaPlayersListObj != null && _vanillaPlayersListObj.activeSelf)
            {
                _vanillaPlayersListObj.SetActive(false);
            }

            var targetPanel = tabController.CurrentDrawer.RightPanel;
            bool wasPanelInactive = !_customRightPanelObj.activeSelf;

            if (wasPanelInactive)
            {
                _customRightPanelObj.SetActive(true);
            }

            // Only destroy and respawn elements if we switched to a different drawer's panel or elements were added/removed
            if (CurrentActivePanel != targetPanel || targetPanel.NeedsFullRebuild || wasPanelInactive)
            {
                CurrentActivePanel = targetPanel;
                targetPanel.NeedsFullRebuild = false;
                RebuildCustomPanel(targetPanel);
            }
            else
            {
                ApplyHeader(targetPanel);
            }
        }

        /// <summary>
        /// Hides the custom right panel (if created) and restores the vanilla Connected Players list.
        /// </summary>
        public static void RestoreVanillaPlayersList()
        {
            CurrentActivePanel = null;

            if (_customRightPanelObj != null && _customRightPanelObj.activeSelf)
            {
                // Detach any developer-supplied CustomContentRoot before hiding so it isn't destroyed
                DetachCustomContentObjects();
                _customRightPanelObj.SetActive(false);
            }

            if (_vanillaPlayersListObj == null)
            {
                _vanillaPlayersListObj = FindVanillaPlayersContainer();
            }

            if (_vanillaPlayersListObj != null && !_vanillaPlayersListObj.activeSelf)
            {
                _vanillaPlayersListObj.SetActive(true);
                ZUIPauseMenu.ApplyPlayersHeader();
                ZUIPauseMenu.RefreshPlayerCards();
            }
        }

        internal static void RebuildLayout()
        {
            if (_customRightPanelObj != null && _customRightPanelObj.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_customRightPanelObj.transform as RectTransform);
            }

            Screens.ZUIScreenController.RebuildActiveRightLayout();
        }

        private static bool EnsureCustomRightPanelContainer()
        {
            if (_vanillaPlayersListObj == null)
            {
                _vanillaPlayersListObj = FindVanillaPlayersContainer();
            }

            if (_vanillaPlayersListObj == null) return false;

            if (_customRightPanelObj != null) return true;

            Transform parent = _vanillaPlayersListObj.transform.parent;
            if (parent == null) return false;

            // Clone the native Players container so RectTransform (989px width), VerticalLayoutGroup, and Top header match 1:1
            _customRightPanelObj = UnityEngine.Object.Instantiate(_vanillaPlayersListObj, parent);
            _customRightPanelObj.name = "ZUI_CustomRightPanel";

            // Keep "Top" header, remove any cloned EscapePlayerUI rows that were inside _vanillaPlayersListObj
            var childrenToRemove = new List<GameObject>();
            for (int i = 0; i < _customRightPanelObj.transform.childCount; i++)
            {
                Transform child = _customRightPanelObj.transform.GetChild(i);
                if (child.name == "Top")
                {
                    _customTopTransform = child;
                }
                else
                {
                    childrenToRemove.Add(child.gameObject);
                }
            }

            for (int i = 0; i < childrenToRemove.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(childrenToRemove[i]);
            }

            return true;
        }

        private static void DetachCustomContentObjects()
        {
            if (_customRightPanelObj == null) return;
            DetachCustomContentFromList(_spawnedItems);
        }

        internal static void DetachCustomContentFromList(List<GameObject> spawnedItems)
        {
            if (spawnedItems == null) return;

            for (int i = spawnedItems.Count - 1; i >= 0; i--)
            {
                var item = spawnedItems[i];
                if (item != null && item.name.StartsWith("ZUI_RightCustomContent_", StringComparison.Ordinal))
                {
                    item.SetActive(false);
                    item.transform.SetParent(null, false);
                    spawnedItems.RemoveAt(i);
                }
            }
        }

        private static void RebuildCustomPanel(PauseMenuRightPanel panel)
        {
            if (_customRightPanelObj == null || panel == null) return;
            if (_customTopTransform == null)
            {
                _customTopTransform = _customRightPanelObj.transform.Find("Top");
            }
            PopulateContainer(panel, _customRightPanelObj, _customTopTransform, _spawnedItems, null);
        }

        internal static void PopulateContainer(
            PauseMenuRightPanel panel,
            GameObject containerObj,
            Transform topTransform,
            List<GameObject> spawnedItems,
            GameObject buttonTemplateOverride = null)
        {
            if (containerObj == null || panel == null || spawnedItems == null) return;

            // 1. Detach any custom content root before clearing spawned items
            DetachCustomContentFromList(spawnedItems);

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                {
                    UnityEngine.Object.Destroy(spawnedItems[i]);
                }
            }
            spawnedItems.Clear();

            // 2. Apply Header Title & Counter
            ApplyHeaderToTransform(panel, topTransform ?? containerObj.transform.Find("Top"));

            // 3. Spawn Elements (Cards & Buttons)
            var elements = panel.Elements;
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                GameObject spawned = null;

                if (element is PauseMenuRightCard card)
                {
                    spawned = SpawnCardElement(card, containerObj.transform);
                }
                else if (element is PauseMenuRightButton button)
                {
                    spawned = SpawnButtonElement(button, containerObj.transform, buttonTemplateOverride);
                }

                element.SpawnedObject = spawned;

                if (spawned != null)
                {
                    spawnedItems.Add(spawned);
                }
            }

            // 4. Mount Custom Content Root if provided
            if (panel.CustomContentRoot != null)
            {
                GameObject customRoot = panel.CustomContentRoot;
                if (!customRoot.name.StartsWith("ZUI_RightCustomContent_", StringComparison.Ordinal))
                {
                    customRoot.name = "ZUI_RightCustomContent_" + customRoot.name;
                }
                customRoot.transform.SetParent(containerObj.transform, false);
                customRoot.SetActive(true);
                spawnedItems.Add(customRoot);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(containerObj.transform as RectTransform);
        }

        internal static void ApplyHeader(PauseMenuRightPanel panel)
        {
            if (_customTopTransform == null && _customRightPanelObj != null)
            {
                _customTopTransform = _customRightPanelObj.transform.Find("Top");
            }
            ApplyHeaderToTransform(panel, _customTopTransform);
        }

        internal static void ApplyHeaderToTransform(PauseMenuRightPanel panel, Transform topTransform)
        {
            if (topTransform == null || panel == null) return;

            var titleTransform = topTransform.Find("ConnectedPlayers");
            var counterTransform = topTransform.Find("ConnectedPlayersCountIndicatorText");

            if (titleTransform != null)
            {
                var localized = titleTransform.GetComponent<LocalizedTextUI>();
                if (localized != null) localized.enabled = false;

                var titleText = titleTransform.GetComponent<TMP_Text>();
                if (titleText != null)
                {
                    string rawTitle = panel.HeaderTitle ?? string.Empty;
                    if (!string.IsNullOrEmpty(panel.HeaderTitleHex))
                    {
                        titleText.text = $"<color=#{panel.HeaderTitleHex.TrimStart('#')}>{rawTitle}</color>";
                    }
                    else
                    {
                        titleText.text = rawTitle;
                    }
                }
            }

            if (counterTransform != null)
            {
                var counterText = counterTransform.GetComponent<TMP_Text>();
                if (counterText != null)
                {
                    string rawCounter = panel.HeaderCounter ?? string.Empty;
                    if (!string.IsNullOrEmpty(panel.HeaderCounterHex))
                    {
                        counterText.text = $"<color=#{panel.HeaderCounterHex.TrimStart('#')}>{rawCounter}</color>";
                    }
                    else
                    {
                        counterText.text = rawCounter;
                    }
                }
            }
        }

        private static GameObject SpawnCardElement(PauseMenuRightCard card, Transform parent)
        {
            GameObject template = FindEscapePlayerPrefab();
            if (template == null)
            {
                Plugin.Log.LogError($"[PauseMenuRightPanel] Cannot spawn card '{card.Id}': native EscapePlayerUI template not found.");
                return null;
            }

            GameObject cardObj = UnityEngine.Object.Instantiate(template, parent);
            cardObj.name = $"ZUI_RightCard_{card.Id}";
            // Keep inactive while configuring Button & ColorBlock so Selectable.OnEnable() initializes cleanly without a 1-frame white flash
            cardObj.SetActive(false);

            // Clean up player-specific components from cloned EscapePlayerUI
            var escapeUI = cardObj.GetComponent<EscapePlayerUI>();
            if (escapeUI != null) UnityEngine.Object.DestroyImmediate(escapeUI);

            var cardExt = cardObj.GetComponent<ZUIPlayerCardExtension>();
            if (cardExt != null) UnityEngine.Object.DestroyImmediate(cardExt);

            // Remove any extra MonoBehaviours (such as FakePlayerCardDebugHandler) on root except Image
            var rootBehaviours = cardObj.GetComponents<MonoBehaviour>();
            for (int i = 0; i < rootBehaviours.Length; i++)
            {
                var mb = rootBehaviours[i];
                if (mb != null && !(mb is Image))
                {
                    UnityEngine.Object.DestroyImmediate(mb);
                }
            }

            // Destroy VoiceSide and any leftover ZUI action buttons from template
            var childrenToCleanup = new List<GameObject>();
            for (int i = 0; i < cardObj.transform.childCount; i++)
            {
                Transform ch = cardObj.transform.GetChild(i);
                if (ch.name == "VoiceSide" || ch.name.StartsWith("ZUI_", StringComparison.Ordinal))
                {
                    childrenToCleanup.Add(ch.gameObject);
                }
            }
            for (int i = 0; i < childrenToCleanup.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(childrenToCleanup[i]);
            }

            float height = Mathf.Max(24f, card.Height);

            // Grab KickButton as template for action buttons, then destroy it after spawning
            Transform kickTransform = cardObj.transform.Find("KickButton");
            GameObject kickTemplate = kickTransform != null ? kickTransform.gameObject : null;

            var spawnedHoverOnlyButtons = new List<GameObject>();
            float btnSize = Mathf.Clamp(height - 16f, 26f, 44f);

            // 1. Spawn Left-Aligned Action Buttons
            float currentLeftX = 12f;
            for (int i = 0; i < card.LeftButtons.Count; i++)
            {
                var action = card.LeftButtons[i];
                if (i > 0) currentLeftX += 6f;

                float xCenter = currentLeftX + (btnSize * 0.5f);
                GameObject actionBtnObj = CreateCardActionButton(cardObj.transform, kickTemplate, action, btnSize, xCenter, isLeft: true);
                if (actionBtnObj != null && !action.AlwaysVisible)
                {
                    actionBtnObj.SetActive(false);
                    spawnedHoverOnlyButtons.Add(actionBtnObj);
                }

                currentLeftX += btnSize;
            }

            // 2. Spawn Right-Aligned Action Buttons
            float currentRightX = -12f;
            for (int i = 0; i < card.RightButtons.Count; i++)
            {
                var action = card.RightButtons[i];
                if (i > 0) currentRightX -= 6f;

                float xCenter = currentRightX - (btnSize * 0.5f);
                GameObject actionBtnObj = CreateCardActionButton(cardObj.transform, kickTemplate, action, btnSize, xCenter, isLeft: false);
                if (actionBtnObj != null && !action.AlwaysVisible)
                {
                    actionBtnObj.SetActive(false);
                    spawnedHoverOnlyButtons.Add(actionBtnObj);
                }

                currentRightX -= btnSize;
            }

            if (kickTemplate != null)
            {
                UnityEngine.Object.DestroyImmediate(kickTemplate);
            }

            if (spawnedHoverOnlyButtons.Count > 0)
            {
                var hoverTracker = cardObj.AddComponent<RightPanelCardHoverTracker>();
                hoverTracker.Initialize(spawnedHoverOnlyButtons);
            }

            // Apply size, icon, text, and selection visuals
            ApplyCardVisuals(card, cardObj);

            // Configure Whole-Card Click Action if provided
            var bgImage = cardObj.GetComponent<Image>();
            if (card.OnClick != null)
            {
                if (bgImage != null)
                {
                    bgImage.raycastTarget = true;
                }

                var cardBtn = cardObj.AddComponent<Button>();
                cardBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                cardBtn.transition = Selectable.Transition.ColorTint;
                cardBtn.targetGraphic = bgImage;

                Color defaultCardBg = new Color(0f, 0f, 0f, 0.78f);
                Color selectedBg = card.SelectedColor ?? new Color(0.12f, 0.22f, 0.32f, 0.92f);
                Color activeBg = card.BackgroundColor ?? (card.IsSelected ? selectedBg : defaultCardBg);

                var cb = cardBtn.colors;
                cb.normalColor = activeBg;
                cb.selectedColor = activeBg;
                cb.highlightedColor = new Color(
                    Mathf.Clamp01(activeBg.r + 0.14f),
                    Mathf.Clamp01(activeBg.g + 0.14f),
                    Mathf.Clamp01(activeBg.b + 0.14f),
                    Mathf.Clamp01(activeBg.a + 0.08f)
                );
                cb.pressedColor = new Color(
                    Mathf.Clamp01(activeBg.r + 0.22f),
                    Mathf.Clamp01(activeBg.g + 0.22f),
                    Mathf.Clamp01(activeBg.b + 0.22f),
                    1f
                );
                cardBtn.colors = cb;

                cardBtn.onClick.AddListener(() =>
                {
                    try
                    {
                        card.OnClick?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ZUICrashHandler.HandleException(ex, $"PauseMenuRightCard '{card.Id}'", card.OnClick?.Method?.DeclaringType?.Assembly);
                        Plugin.Log.LogError($"[PauseMenuRightPanel] Error in card '{card.Id}' OnClick: {ex}");
                    }
                });
            }

            cardObj.SetActive(true);
            return cardObj;
        }

        internal static void ApplyCardVisuals(PauseMenuRightCard card, GameObject cardObj)
        {
            if (card == null || cardObj == null) return;

            float height = Mathf.Max(24f, card.Height);

            var cardRt = cardObj.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.sizeDelta = new Vector2(989f, height);
            }

            var le = cardObj.GetComponent<LayoutElement>();
            if (le == null) le = cardObj.AddComponent<LayoutElement>();
            le.minWidth = 989f;
            le.preferredWidth = 989f;
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;

            // 1. Background & Selection Tinting
            Color defaultCardBg = new Color(0f, 0f, 0f, 0.78f);
            Color selectedBg = card.SelectedColor ?? new Color(0.12f, 0.22f, 0.32f, 0.92f);
            Color activeBg = card.BackgroundColor ?? (card.IsSelected ? selectedBg : defaultCardBg);

            var bgImage = cardObj.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = activeBg;
            }

            var cardBtn = cardObj.GetComponent<Button>();
            if (cardBtn != null)
            {
                var cb = cardBtn.colors;
                cb.normalColor = activeBg;
                cb.selectedColor = activeBg;
                cb.highlightedColor = new Color(
                    Mathf.Clamp01(activeBg.r + 0.14f),
                    Mathf.Clamp01(activeBg.g + 0.14f),
                    Mathf.Clamp01(activeBg.b + 0.14f),
                    Mathf.Clamp01(activeBg.a + 0.08f)
                );
                cb.pressedColor = new Color(
                    Mathf.Clamp01(activeBg.r + 0.22f),
                    Mathf.Clamp01(activeBg.g + 0.22f),
                    Mathf.Clamp01(activeBg.b + 0.22f),
                    1f
                );
                cardBtn.colors = cb;
            }

            // 2. Escape Hatch: Mount CustomContentRoot if provided
            if (card.CustomContentRoot != null)
            {
                var customRoot = card.CustomContentRoot;
                if (customRoot.transform.parent != cardObj.transform)
                {
                    customRoot.transform.SetParent(cardObj.transform, false);
                }
                customRoot.SetActive(true);

                if (card.CustomContentOnly)
                {
                    var iconT = cardObj.transform.Find("PlayerIcon");
                    if (iconT != null) iconT.gameObject.SetActive(false);
                    var nameT = cardObj.transform.Find("PlayerName");
                    if (nameT != null) nameT.gameObject.SetActive(false);
                    var rightTextT = cardObj.transform.Find("ZUI_RightText");
                    if (rightTextT != null) rightTextT.gameObject.SetActive(false);
                    return;
                }
            }

            // 3. Leading Zone: Left Buttons + Icon
            float btnSize = Mathf.Clamp(height - 16f, 26f, 44f);
            float currentLeftX = 12f;
            if (card.LeftButtons.Count > 0)
            {
                currentLeftX += card.LeftButtons.Count * (btnSize + 6f);
            }

            Transform iconTransform = cardObj.transform.Find("PlayerIcon");
            float textStartX = currentLeftX + 4f;

            if (iconTransform != null)
            {
                if (card.Icon != null)
                {
                    iconTransform.gameObject.SetActive(true);
                    var iconImg = iconTransform.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = card.Icon;
                        iconImg.color = Color.white;
                    }

                    float iconSize = Mathf.Clamp(height - 16f, 22f, 56f);
                    var iconRt = iconTransform as RectTransform;
                    if (iconRt != null)
                    {
                        iconRt.anchorMin = new Vector2(0f, 0.5f);
                        iconRt.anchorMax = new Vector2(0f, 0.5f);
                        iconRt.pivot = new Vector2(0.5f, 0.5f);
                        iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                        iconRt.anchoredPosition = new Vector2(currentLeftX + iconSize * 0.5f, 0f);
                    }
                    textStartX = currentLeftX + iconSize + 14f;
                }
                else
                {
                    iconTransform.gameObject.SetActive(false);
                }
            }

            // 4. Trailing Zone: Right Buttons + RightText
            float rightReserved = 12f;
            if (card.RightButtons.Count > 0)
            {
                rightReserved += card.RightButtons.Count * (btnSize + 6f);
            }

            Transform nameTransform = cardObj.transform.Find("PlayerName");
            var nameTmp = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;

            Transform rightTextTransform = cardObj.transform.Find("ZUI_RightText");
            if (!string.IsNullOrEmpty(card.RightText))
            {
                TMP_Text rightTmp;
                RectTransform rightRt;

                if (rightTextTransform == null)
                {
                    var rightTextObj = new GameObject("ZUI_RightText");
                    rightTextObj.transform.SetParent(cardObj.transform, false);
                    rightRt = rightTextObj.AddComponent<RectTransform>();
                    rightTmp = rightTextObj.AddComponent<TextMeshProUGUI>();
                    if (nameTmp != null && nameTmp.font != null)
                    {
                        rightTmp.font = nameTmp.font;
                        rightTmp.fontSharedMaterial = nameTmp.fontSharedMaterial;
                    }
                }
                else
                {
                    rightTextTransform.gameObject.SetActive(true);
                    rightRt = rightTextTransform as RectTransform;
                    rightTmp = rightTextTransform.GetComponent<TMP_Text>();
                }

                if (rightTmp != null)
                {
                    if (rightTmp.font == null && nameTmp != null && nameTmp.font != null)
                    {
                        rightTmp.font = nameTmp.font;
                        rightTmp.fontSharedMaterial = nameTmp.fontSharedMaterial;
                    }

                    rightTmp.richText = true;
                    rightTmp.alignment = TextAlignmentOptions.MidlineRight;
                    rightTmp.overflowMode = TextOverflowModes.Ellipsis;
                    rightTmp.textWrappingMode = TextWrappingModes.NoWrap;
                    rightTmp.fontSize = height < 54f ? 14f : 17f;

                    string formattedRightText = card.RightText;
                    if (!string.IsNullOrEmpty(card.RightTextHex))
                    {
                        formattedRightText = $"<color=#{card.RightTextHex.TrimStart('#')}>{formattedRightText}</color>";
                    }
                    else
                    {
                        formattedRightText = $"<color=#AAAAAA>{formattedRightText}</color>";
                    }
                    rightTmp.text = formattedRightText;

                    float prefWidth;
                    try
                    {
                        // Pass unconstrained dimensions (2000f, 100f) because default single-argument GetPreferredValues
                        // uses rectTransform.rect.width (which is 0 on a freshly created GameObject), causing TMP to wrap
                        // immediately and measure only 1-2 characters (~30px) leading to "Jo..." truncation.
                        Vector2 prefValues = rightTmp.GetPreferredValues(card.RightText, 2000f, 100f);
                        float measuredWidth = prefValues.x;
                        float estimatedWidth = card.RightText.Length * (rightTmp.fontSize * 0.68f);
                        if (measuredWidth < card.RightText.Length * 4f)
                        {
                            measuredWidth = estimatedWidth;
                        }
                        prefWidth = Mathf.Clamp(measuredWidth, 30f, 320f);
                    }
                    catch
                    {
                        prefWidth = Mathf.Clamp(card.RightText.Length * (rightTmp.fontSize * 0.68f), 30f, 320f);
                    }

                    if (rightRt != null)
                    {
                        rightRt.anchorMin = new Vector2(1f, 0.5f);
                        rightRt.anchorMax = new Vector2(1f, 0.5f);
                        rightRt.pivot = new Vector2(1f, 0.5f);
                        rightRt.sizeDelta = new Vector2(prefWidth + 6f, height - 8f);
                        rightRt.anchoredPosition = new Vector2(-rightReserved, 0f);
                    }

                    rightReserved += prefWidth + 14f;
                }
            }
            else if (rightTextTransform != null)
            {
                rightTextTransform.gameObject.SetActive(false);
            }

            // 5. Center Zone: Title + Subtitle (PlayerName)
            if (nameTransform != null)
            {
                var nameRt = nameTransform as RectTransform;

                float availableTextWidth = Mathf.Max(60f, 989f - textStartX - rightReserved - 10f);

                if (nameRt != null)
                {
                    nameRt.anchorMin = new Vector2(0f, 0.5f);
                    nameRt.anchorMax = new Vector2(0f, 0.5f);
                    nameRt.pivot = new Vector2(0f, 0.5f);
                    nameRt.anchoredPosition = new Vector2(textStartX, 0f);
                    nameRt.sizeDelta = new Vector2(availableTextWidth, Mathf.Max(20f, height - 8f));
                }

                if (nameTmp != null)
                {
                    nameTmp.richText = true;
                    nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    nameTmp.overflowMode = TextOverflowModes.Ellipsis;
                    nameTmp.textWrappingMode = TextWrappingModes.NoWrap;

                    string formattedTitle = card.Title ?? string.Empty;
                    if (!string.IsNullOrEmpty(card.TitleHex))
                    {
                        formattedTitle = $"<color=#{card.TitleHex.TrimStart('#')}>{formattedTitle}</color>";
                    }

                    if (!string.IsNullOrEmpty(card.Subtitle))
                    {
                        // 2-line layout: calibrated font size + line-height=85% prevents bottom clipping
                        float baseFontSize = height < 54f ? 16f : 21f;
                        nameTmp.fontSize = baseFontSize;

                        string subColor = !string.IsNullOrEmpty(card.SubtitleHex)
                            ? $"#{card.SubtitleHex.TrimStart('#')}"
                            : "#AAAAAA";

                        nameTmp.text = $"{formattedTitle}\n<line-height=85%><size=72%><color={subColor}>{card.Subtitle}</color></size></line-height>";
                    }
                    else
                    {
                        float baseFontSize = height < 54f ? 18f : 23f;
                        nameTmp.fontSize = baseFontSize;
                        nameTmp.text = formattedTitle;
                    }
                }
            }
        }

        private static GameObject CreateCardActionButton(
            Transform cardTransform,
            GameObject kickTemplate,
            PauseMenuRightCardAction action,
            float btnSize,
            float xCenter,
            bool isLeft = false)
        {
            if (kickTemplate == null)
            {
                Plugin.Log.LogError($"[PauseMenuRightPanel] Cannot spawn card action button '{action.Id}': native KickButton template not found.");
                return null;
            }

            GameObject btnObj = UnityEngine.Object.Instantiate(kickTemplate, cardTransform);
            btnObj.name = isLeft ? $"ZUI_CardLeftBtn_{action.Id}" : $"ZUI_CardBtn_{action.Id}";
            btnObj.SetActive(false);

            var rect = btnObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                float anchorX = isLeft ? 0f : 1f;
                rect.anchorMin = new Vector2(anchorX, 0.5f);
                rect.anchorMax = new Vector2(anchorX, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(btnSize, btnSize);
                rect.anchoredPosition = new Vector2(xCenter, 0f);
            }

            // Replace cloned Button to purge persistent onClick listeners from KickButton
            var oldBtn = btnObj.GetComponent<Button>();
            ColorBlock colors = ColorBlock.defaultColorBlock;
            Selectable.Transition transition = Selectable.Transition.ColorTint;
            Graphic targetGraphic = btnObj.GetComponent<Graphic>();

            if (oldBtn != null)
            {
                colors = oldBtn.colors;
                transition = oldBtn.transition;
                targetGraphic = oldBtn.targetGraphic ?? targetGraphic;
                UnityEngine.Object.DestroyImmediate(oldBtn);
            }

            var button = btnObj.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = colors;
            button.transition = transition;
            button.targetGraphic = targetGraphic;

            ApplyRightButtonVisuals(action, btnObj);

            button.onClick.AddListener(() =>
            {
                try
                {
                    action.OnClick?.Invoke();
                }
                catch (Exception ex)
                {
                    ZUICrashHandler.HandleException(ex, $"PauseMenuRightCardAction '{action.Id}'", action.OnClick?.Method?.DeclaringType?.Assembly);
                    Plugin.Log.LogError($"[PauseMenuRightPanel] Error in card button '{action.Id}': {ex}");
                }
            });

            btnObj.SetActive(action.AlwaysVisible);
            action.SpawnedButtonObject = btnObj;

            return btnObj;
        }

        internal static void ApplyRightButtonVisuals(PauseMenuRightCardAction action, GameObject btnObj)
        {
            if (action == null || btnObj == null) return;

            Color normalCol = action.NormalColor.GetValueOrDefault(Color.white);
            Color hoverCol = action.HoverColor.GetValueOrDefault(normalCol);

            var image = btnObj.GetComponent<Image>();
            if (image != null)
            {
                if (action.Icon != null)
                {
                    image.sprite = action.Icon;
                }
                // Keep base Graphic white so Button ColorBlock applies exact normal/hover colors without double-multiplying
                image.color = Color.white;
                if (image.canvasRenderer != null)
                {
                    image.canvasRenderer.SetColor(normalCol);
                }
            }

            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                var cb = button.colors;
                cb.normalColor = normalCol;
                cb.selectedColor = normalCol;
                cb.highlightedColor = hoverCol;
                cb.pressedColor = hoverCol * 0.85f;
                button.colors = cb;
            }
        }

        private static GameObject SpawnButtonElement(PauseMenuRightButton buttonDef, Transform parent, GameObject templateOverride = null)
        {
            GameObject template = templateOverride ?? (PauseMenuTabController.Instance != null
                ? PauseMenuTabController.Instance.ButtonTemplate
                : null);

            if (template == null)
            {
                Plugin.Log.LogError($"[PauseMenuRightPanel] Cannot spawn button '{buttonDef.Id}': native ButtonTemplate not found.");
                return null;
            }

            GameObject btnObj = UnityEngine.Object.Instantiate(template, parent);
            var bf = btnObj.GetComponent<ButtonFunctions>();
            if (bf != null) UnityEngine.Object.DestroyImmediate(bf);

            var loc = btnObj.GetComponentInChildren<LocalizedTextUI>();
            if (loc != null) UnityEngine.Object.DestroyImmediate(loc);

            btnObj.name = $"ZUI_RightBtn_{buttonDef.Id}";
            btnObj.SetActive(true);

            ApplyButtonVisuals(buttonDef, btnObj);

            // Wire Button Click
            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.interactable = buttonDef.Interactable;
                btn.onClick.AddListener(() =>
                {
                    try
                    {
                        buttonDef.OnClick?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ZUICrashHandler.HandleException(ex, $"PauseMenuRightButton '{buttonDef.Id}'", buttonDef.OnClick?.Method?.DeclaringType?.Assembly);
                        Plugin.Log.LogError($"[PauseMenuRightPanel] Error in button '{buttonDef.Id}' OnClick: {ex}");
                    }
                });
            }

            return btnObj;
        }

        internal static void ApplyButtonVisuals(PauseMenuRightButton buttonDef, GameObject btnObj)
        {
            if (buttonDef == null || btnObj == null) return;

            float height = Mathf.Max(24f, buttonDef.Height);

            var rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(989f, height);
            }

            var le = btnObj.GetComponent<LayoutElement>();
            if (le == null) le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = 989f;
            le.preferredWidth = 989f;
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;

            // Optional background tint
            if (!string.IsNullOrEmpty(buttonDef.BackgroundHex))
            {
                Color? bgCol = UIUtils.ParseColor(buttonDef.BackgroundHex);
                if (bgCol.HasValue)
                {
                    var bgImg = btnObj.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.color = bgCol.Value;
                    }
                }
            }

            // Configure Text
            var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.richText = true;
                string upperLabel = buttonDef.Label.ToUpperInvariant();
                if (!string.IsNullOrEmpty(buttonDef.TextHex))
                {
                    tmp.text = $"<color=#{buttonDef.TextHex.TrimStart('#')}>{upperLabel}</color>";
                }
                else
                {
                    tmp.text = upperLabel;
                }

                // Scale font if button is resized smaller than standard 56-77px
                if (height < 56f)
                {
                    tmp.enableAutoSizing = false;
                    tmp.fontSize = Mathf.Clamp(height * 0.52f, 14f, 28f);
                }
            }

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = buttonDef.Interactable;
            }
        }

        private static GameObject FindEscapePlayerPrefab()
        {
            // 1. From InGameUI.escapePlayerPrefab field via reflection
            if (StaticInstance<InGameUI>.Instance != null)
            {
                try
                {
                    var prefabField = typeof(InGameUI).GetField("escapePlayerPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (prefabField?.GetValue(StaticInstance<InGameUI>.Instance) is EscapePlayerUI prefab && prefab != null)
                    {
                        return prefab.gameObject;
                    }
                }
                catch { }
            }

            // 2. From any active EscapePlayerUI in the vanilla players container
            if (_vanillaPlayersListObj != null)
            {
                var existing = _vanillaPlayersListObj.GetComponentInChildren<EscapePlayerUI>(true);
                if (existing != null)
                {
                    return existing.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindVanillaPlayersContainer()
        {
            // 1. From InGameUI.escapePlayerParent field
            if (StaticInstance<InGameUI>.Instance != null)
            {
                try
                {
                    var parentField = typeof(InGameUI).GetField("escapePlayerParent", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (parentField?.GetValue(StaticInstance<InGameUI>.Instance) is Transform t && t != null)
                    {
                        return t.gameObject;
                    }
                }
                catch { }
            }

            // 2. Direct hierarchy lookup
            GameObject canvasObj = GameObject.Find("InGameUICanvas");
            if (canvasObj != null)
            {
                Transform players = canvasObj.transform.Find("pausePanel/Players/Players");
                if (players != null) return players.gameObject;
            }

            return null;
        }
    }

    /// <summary>
    /// Toggles hover-only right-side action buttons when the pointer enters or exits a custom right-panel card.
    /// </summary>
    internal class RightPanelCardHoverTracker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private List<GameObject> _hoverButtons;

        public void Initialize(List<GameObject> hoverButtons)
        {
            _hoverButtons = hoverButtons;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetButtonsActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetButtonsActive(false);
        }

        private void OnDisable()
        {
            SetButtonsActive(false);
        }

        private void SetButtonsActive(bool active)
        {
            if (_hoverButtons == null) return;
            for (int i = 0; i < _hoverButtons.Count; i++)
            {
                if (_hoverButtons[i] != null)
                {
                    _hoverButtons[i].SetActive(active);
                }
            }
        }
    }
}

