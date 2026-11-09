using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.MainMenu
{
    public enum MainMenuMode
    {
        Vanilla,
        Tabbed
    }

    /// <summary>
    /// Controls the Main Menu button stack, managing the transition between
    /// vanilla buttons and tabbed mod pages with pagination.
    /// </summary>
    public class MainMenuTabController : MonoBehaviour
    {
        private const int MaxSlots = 4;
        private const int PaginatedSlotsPerPage = 3;

        // Vanilla references
        private GameObject _hostButton;
        private GameObject _joinButton;
        private GameObject _settingsButton;
        private GameObject _quitButton;

        // ZUI injected elements
        private GameObject _moreButton;
        private TextMeshProUGUI _moreButtonText;
        private readonly GameObject[] _modSlots = new GameObject[MaxSlots];
        private readonly TextMeshProUGUI[] _modSlotTexts = new TextMeshProUGUI[MaxSlots];
        private readonly Button[] _modSlotButtons = new Button[MaxSlots];
        private readonly CanvasGroup[] _modSlotCanvasGroups = new CanvasGroup[MaxSlots];

        // Pagination row elements
        private GameObject _paginationRow;
        private Button _prevButton;
        private Button _nextButton;
        private TextMeshProUGUI _prevButtonText;
        private TextMeshProUGUI _nextButtonText;
        private TextMeshProUGUI _pageIndicatorText;
        private ArrowButtonHoverEffect _prevHoverEffect;
        private ArrowButtonHoverEffect _nextHoverEffect;

        // Navigation Stack & State
        private readonly Stack<MainMenuDrawer> _navigationStack = new Stack<MainMenuDrawer>();

        // Breadcrumb indicator
        private GameObject _breadcrumbBar;
        private TextMeshProUGUI _breadcrumbText;

        public MainMenuMode CurrentMode { get; private set; } = MainMenuMode.Vanilla;
        public int CurrentPage { get; private set; } = 0;
        public MainMenuDrawer CurrentDrawer => _navigationStack.Count > 0 ? _navigationStack.Peek() : ZUIMainMenu.RootDrawer;

        private bool _isInitialized = false;

        public static MainMenuTabController Instance { get; private set; }

        private void Update()
        {
            if (CurrentMode != MainMenuMode.Tabbed) return;

            // Do not intercept if a modal panel is open or was closed on this frame
            if (Panels.ZUIPanelController.CurrentOpenPanel != null || Panels.ZUIPanelController.LastEscConsumedFrame == Time.frameCount) return;

            // Handle popups in Main Menu
            if (InitUI.PopupManager != null && InitUI.PopupManager.GetPopupCount > 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    var activePopup = InitUI.PopupManager.GetPopup(0);
                    if (activePopup != null)
                    {
                        var behavior = activePopup.GetComponent<Popups.ZUIPopupBehavior>();
                        if (behavior != null && behavior.Options.DismissOnEscape)
                        {
                            Popups.ZUIPopup.LastEscConsumedFrame = Time.frameCount;
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
                            // DismissOnEscape is false (default): consume ESC
                            Popups.ZUIPopup.LastEscConsumedFrame = Time.frameCount;
                        }
                    }
                }
                return;
            }

            // Do not pop drawer if popup was closed on this frame
            if (Popups.ZUIPopup.LastEscConsumedFrame == Time.frameCount) return;

            // Do not intercept if vanilla settings is open
            if (StaticInstance<InitUI>.Instance != null && StaticInstance<InitUI>.Instance.IsSettingsOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PopDrawer();
                StaticInstance<InitUI>.Instance?.PlayClickSFX();
            }
        }

        public void Initialize(
            GameObject hostBtn,
            GameObject joinBtn,
            GameObject settingsBtn,
            GameObject quitBtn)
        {
            if (_isInitialized) return;

            Instance = this;
            _hostButton = hostBtn;
            _joinButton = joinBtn;
            _settingsButton = settingsBtn;
            _quitButton = quitBtn;

            Transform parent = transform;

            // Template to clone from (QuitGameButton has identical styling, Animator, and ButtonAnimation)
            GameObject template = _quitButton;

            // 1. Create 4 Mod Slots
            for (int i = 0; i < MaxSlots; i++)
            {
                var slotObj = Instantiate(template, parent);
                slotObj.name = $"ZUI_ModSlot_{i}";

                // Clean up game-specific scripts
                CleanupClonedButton(slotObj);

                _modSlots[i] = slotObj;
                _modSlotTexts[i] = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                _modSlotButtons[i] = slotObj.GetComponent<Button>();

                // Add CanvasGroup to smoothly show/hide without shifting layout
                var cg = slotObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = slotObj.AddComponent<CanvasGroup>();
                _modSlotCanvasGroups[i] = cg;

                slotObj.SetActive(false);
            }

            // 2. Create MORE / Return Button
            _moreButton = Instantiate(template, parent);
            _moreButton.name = "ZUI_MoreButton";
            CleanupClonedButton(_moreButton);

            _moreButtonText = _moreButton.GetComponentInChildren<TextMeshProUGUI>();
            var moreBtnComponent = _moreButton.GetComponent<Button>();
            moreBtnComponent.onClick.AddListener(OnMoreButtonClicked);
            _moreButton.SetActive(true);

            // 3. Create Pagination Row
            CreatePaginationRow(template, parent);

            // 4. Create Breadcrumb Indicator (under 5th slot)
            CreateBreadcrumbBar(template, parent);

            _isInitialized = true;
            SetMode(MainMenuMode.Vanilla);
        }

        private void CreatePaginationRow(GameObject buttonTemplate, Transform parent)
        {
            _paginationRow = new GameObject("ZUI_PaginationRow", typeof(RectTransform));
            _paginationRow.transform.SetParent(parent, false);

            var rt = _paginationRow.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(589f, 77f);

            var hlg = _paginationRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var font = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>()?.font;
            Color boxBgColor = new Color(0.04f, 0.04f, 0.04f, 0.75f);

            // --- Left Arrow Button [ < ] ---
            GameObject prevObj = CreateArrowButton("PrevPageButton", "<", PreviousPage, out _prevButtonText, out _prevHoverEffect, font, boxBgColor);
            _prevButton = prevObj.GetComponent<Button>();

            // --- Center Page Indicator ---
            GameObject indicatorObj = new GameObject("PageIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicatorObj.transform.SetParent(_paginationRow.transform, false);

            var bgImage = indicatorObj.GetComponent<Image>();
            bgImage.color = boxBgColor;
            bgImage.raycastTarget = false;

            var indLayout = indicatorObj.AddComponent<LayoutElement>();
            indLayout.preferredWidth = 349f;
            indLayout.minWidth = 349f;
            indLayout.flexibleWidth = 1f;

            // Separate child GameObject for Text so Image and TextMeshProUGUI don't conflict
            GameObject textObj = new GameObject("IndicatorText", typeof(RectTransform));
            textObj.transform.SetParent(indicatorObj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            _pageIndicatorText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                _pageIndicatorText.font = font;
            }
            _pageIndicatorText.margin = Vector4.zero;
            _pageIndicatorText.alignment = TextAlignmentOptions.Center;
            _pageIndicatorText.fontSize = 20f;
            _pageIndicatorText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _pageIndicatorText.text = "PAGE 1 / 1";

            // --- Right Arrow Button [ > ] ---
            GameObject nextObj = CreateArrowButton("NextPageButton", ">", NextPage, out _nextButtonText, out _nextHoverEffect, font, boxBgColor);
            _nextButton = nextObj.GetComponent<Button>();

            _paginationRow.SetActive(false);
        }

        private GameObject CreateArrowButton(
            string name,
            string symbol,
            Action onClick,
            out TextMeshProUGUI textComp,
            out ArrowButtonHoverEffect hoverEffect,
            TMP_FontAsset font,
            Color bgColor)
        {
            // 1. Root Button GameObject with clean Image matching PageIndicator plate exactly
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(_paginationRow.transform, false);

            var layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 110f;
            layout.minWidth = 110f;
            layout.flexibleWidth = 0f;

            var baseImg = btnObj.GetComponent<Image>();
            baseImg.color = bgColor;
            baseImg.raycastTarget = true;

            var btn = btnObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = baseImg;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // 2. Child Hover Highlight (Solid Flash Plate with exact #999999 color)
            GameObject flashObj = new GameObject("HoverFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flashObj.transform.SetParent(btnObj.transform, false);

            var flashRt = flashObj.GetComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.sizeDelta = Vector2.zero;
            flashRt.anchoredPosition = Vector2.zero;

            var flashImg = flashObj.GetComponent<Image>();
            // Proper flash color: (153, 153, 153) = 0.6f from user screenshot
            flashImg.color = new Color(ArrowButtonHoverEffect.FlashColor.r, ArrowButtonHoverEffect.FlashColor.g, ArrowButtonHoverEffect.FlashColor.b, 0f);
            flashImg.raycastTarget = false;

            // 3. Child Text (< or >)
            GameObject textObj = new GameObject("ArrowText", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            textComp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) textComp.font = font;
            textComp.text = symbol;
            textComp.margin = Vector4.zero;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.fontSize = 38f;
            textComp.color = ArrowButtonHoverEffect.TextActiveColor;
            textComp.raycastTarget = false;

            // 4. Hover effect component
            hoverEffect = btnObj.AddComponent<ArrowButtonHoverEffect>();
            hoverEffect.Initialize(flashImg, textComp);

            return btnObj;
        }

        private void CreateBreadcrumbBar(GameObject buttonTemplate, Transform parent)
        {
            _breadcrumbBar = new GameObject("ZUI_BreadcrumbBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _breadcrumbBar.transform.SetParent(parent, false);

            var rt = _breadcrumbBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(589f, 34f);

            var layout = _breadcrumbBar.AddComponent<LayoutElement>();
            layout.preferredWidth = 589f;
            layout.minWidth = 589f;
            layout.preferredHeight = 34f;
            layout.minHeight = 34f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            var bgImage = _breadcrumbBar.GetComponent<Image>();
            bgImage.color = new Color(0.04f, 0.04f, 0.04f, 0.75f);
            bgImage.raycastTarget = false;

            var font = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>()?.font;

            GameObject textObj = new GameObject("BreadcrumbText", typeof(RectTransform));
            textObj.transform.SetParent(_breadcrumbBar.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 0f);
            textRt.offsetMax = new Vector2(-16f, 0f);

            _breadcrumbText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                _breadcrumbText.font = font;
            }
            _breadcrumbText.alignment = TextAlignmentOptions.Center;
            _breadcrumbText.fontSize = 15f;
            _breadcrumbText.textWrappingMode = TextWrappingModes.NoWrap;
            _breadcrumbText.overflowMode = TextOverflowModes.Ellipsis;
            _breadcrumbText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            _breadcrumbText.raycastTarget = false;

            _breadcrumbBar.SetActive(false);
        }

        private const string BreadcrumbSeparator = " <color=#555555>/</color> ";
        private const string BreadcrumbEllipsis = "<color=#555555>...</color>";

        private float GetAvailableBreadcrumbWidth()
        {
            if (_breadcrumbText != null)
            {
                float w = _breadcrumbText.rectTransform.rect.width;
                if (w > 50f) return w;
            }
            if (_breadcrumbBar != null)
            {
                var rect = _breadcrumbBar.GetComponent<RectTransform>();
                if (rect != null && rect.rect.width > 50f)
                {
                    return rect.rect.width - 32f;
                }
            }
            return 557f;
        }

        private float GetTextWidth(string formattedText)
        {
            if (_breadcrumbText == null || string.IsNullOrEmpty(formattedText)) return 0f;
            return _breadcrumbText.GetPreferredValues(formattedText).x;
        }

        private string GetBreadcrumbText()
        {
            if (_navigationStack.Count == 0)
            {
                return "<color=#FFFFFF>MORE</color>";
            }

            var drawers = _navigationStack.ToArray();
            Array.Reverse(drawers);

            var entries = new List<string>();

            // Root drawer as ancestor
            entries.Add("<color=#888888>MORE</color>");

            for (int i = 0; i < drawers.Length; i++)
            {
                var d = drawers[i];
                if (d != ZUIMainMenu.RootDrawer)
                {
                    bool isCurrent = (i == drawers.Length - 1);
                    string colorHex = !string.IsNullOrEmpty(d.BreadcrumbHex)
                        ? d.BreadcrumbHex
                        : (isCurrent ? "#FFFFFF" : "#888888");

                    string titleUpper = d.Title.ToUpperInvariant();
                    entries.Add($"<color={colorHex}>{titleUpper}</color>");
                }
            }

            if (entries.Count == 1)
            {
                return "<color=#FFFFFF>MORE</color>";
            }

            float availableWidth = GetAvailableBreadcrumbWidth();

            // 1. Check if the full path fits within available bar width
            string fullPath = string.Join(BreadcrumbSeparator, entries);
            if (GetTextWidth(fullPath) <= availableWidth)
            {
                return fullPath;
            }

            // 2. Path exceeds available width: truncate with ellipsis
            // Target structure:
            // Left: entries[0] ("MORE") and optionally entries[1] (root submenu e.g. "EXAMPLE")
            // Middle: "..."
            // Right: greedily fit tail entries ending at entries[entries.Count - 1]

            int leftCount = 1;
            if (entries.Count > 2)
            {
                string minTest = $"{entries[0]}{BreadcrumbSeparator}{entries[1]}{BreadcrumbSeparator}{BreadcrumbEllipsis}{BreadcrumbSeparator}{entries[entries.Count - 1]}";
                if (GetTextWidth(minTest) <= availableWidth)
                {
                    leftCount = 2;
                }
            }

            // 3. Greedily collect tail entries backwards from the end
            var rightIndices = new List<int>();
            for (int i = entries.Count - 1; i >= leftCount; i--)
            {
                var candidateRight = new List<int>(rightIndices);
                candidateRight.Add(i);
                candidateRight.Reverse();

                var testParts = new List<string>();
                for (int l = 0; l < leftCount; l++) testParts.Add(entries[l]);

                int rightFirst = candidateRight[0];
                if (rightFirst > leftCount)
                {
                    testParts.Add(BreadcrumbEllipsis);
                }
                for (int r = 0; r < candidateRight.Count; r++)
                {
                    testParts.Add(entries[candidateRight[r]]);
                }

                string candidateText = string.Join(BreadcrumbSeparator, testParts);
                if (rightIndices.Count == 0 || GetTextWidth(candidateText) <= availableWidth)
                {
                    rightIndices.Add(i);
                }
                else
                {
                    break;
                }
            }

            rightIndices.Reverse(); // put back in forward chronological order

            // 4. Build final result
            var resultParts = new List<string>();
            for (int i = 0; i < leftCount; i++)
            {
                resultParts.Add(entries[i]);
            }

            int finalRightFirst = rightIndices.Count > 0 ? rightIndices[0] : entries.Count;
            if (finalRightFirst > leftCount)
            {
                resultParts.Add(BreadcrumbEllipsis);
            }

            for (int i = 0; i < rightIndices.Count; i++)
            {
                resultParts.Add(entries[rightIndices[i]]);
            }

            return string.Join(BreadcrumbSeparator, resultParts);
        }

        private void CleanupClonedButton(GameObject btnObj)
        {
            // Remove ButtonFunctions to prevent triggering vanilla logic (e.g. QuitGame)
            var bf = btnObj.GetComponent<ButtonFunctions>();
            if (bf != null) Destroy(bf);

            // Remove LocalizedTextUI so custom labels are not overwritten
            var loc = btnObj.GetComponentInChildren<LocalizedTextUI>();
            if (loc != null) Destroy(loc);

            // Clear all persistent & runtime onClick listeners
            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
            }
        }

        public void PushDrawer(MainMenuDrawer drawer)
        {
            if (drawer == null) return;
            _navigationStack.Push(drawer);
            CurrentPage = 0;
            if (CurrentMode != MainMenuMode.Tabbed)
            {
                SetMode(MainMenuMode.Tabbed);
            }
            else
            {
                RefreshTabbedView();
            }
        }

        public void PopDrawer()
        {
            if (_navigationStack.Count > 0)
            {
                _navigationStack.Pop();
                CurrentPage = 0;
                RefreshTabbedView();
            }
            else
            {
                SetMode(MainMenuMode.Vanilla);
            }
        }

        private string GetReturnLabel()
        {
            if (_navigationStack.Count == 0)
            {
                return "< MAIN MENU";
            }
            var current = _navigationStack.Peek();
            if (current.Parent != null && !string.IsNullOrEmpty(current.Parent.Title))
            {
                return $"< {current.Parent.Title.ToUpperInvariant()}";
            }
            return "< MORE";
        }

        public void SetMode(MainMenuMode mode)
        {
            CurrentMode = mode;

            if (mode == MainMenuMode.Vanilla)
            {
                _navigationStack.Clear();

                // Restore vanilla top buttons
                if (_hostButton != null)
                {
                    _hostButton.SetActive(true);
                    _hostButton.transform.SetSiblingIndex(0);
                }
                if (_joinButton != null)
                {
                    _joinButton.SetActive(true);
                    _joinButton.transform.SetSiblingIndex(1);
                }
                if (_settingsButton != null)
                {
                    _settingsButton.SetActive(true);
                    _settingsButton.transform.SetSiblingIndex(2);
                }

                // Hide all mod slots
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (_modSlots[i] != null) _modSlots[i].SetActive(false);
                }

                // Slot 3: Vanilla MORE button
                if (_moreButton != null)
                {
                    _moreButton.SetActive(true);
                    _moreButton.transform.SetSiblingIndex(3);
                    if (_moreButtonText != null) _moreButtonText.text = "MORE";
                }

                // Slot 4: Vanilla Quit Game button
                if (_quitButton != null)
                {
                    _quitButton.SetActive(true);
                    _quitButton.transform.SetSiblingIndex(4);
                }

                // Hide pagination row
                if (_paginationRow != null) _paginationRow.SetActive(false);

                // Hide breadcrumb bar
                if (_breadcrumbBar != null) _breadcrumbBar.SetActive(false);
            }
            else // Tabbed / Drawer Mode
            {
                // Hide vanilla buttons
                if (_hostButton != null) _hostButton.SetActive(false);
                if (_joinButton != null) _joinButton.SetActive(false);
                if (_settingsButton != null) _settingsButton.SetActive(false);
                if (_quitButton != null) _quitButton.SetActive(false);

                RefreshTabbedView();
            }
        }

        public void ToggleMode()
        {
            SetMode(CurrentMode == MainMenuMode.Vanilla ? MainMenuMode.Tabbed : MainMenuMode.Vanilla);
        }

        private void OnMoreButtonClicked()
        {
            if (CurrentMode == MainMenuMode.Vanilla)
            {
                SetMode(MainMenuMode.Tabbed);
            }
            else
            {
                PopDrawer();
            }
        }

        public void RefreshTabbedView()
        {
            if (CurrentMode != MainMenuMode.Tabbed) return;

            var drawer = CurrentDrawer;
            bool isPaginated = drawer == null || drawer.Paginated;
            int slotsCount = isPaginated ? PaginatedSlotsPerPage : MaxSlots;

            List<MainMenuButtonRegistration> items;
            if (drawer == null || drawer == ZUIMainMenu.RootDrawer)
            {
                items = ZUIMainMenu.GetSortedRegistrations();
            }
            else
            {
                items = drawer.GetSortedButtons();
            }

            int totalItems = items.Count;
            int totalPages = isPaginated ? Mathf.Max(1, Mathf.CeilToInt((float)totalItems / PaginatedSlotsPerPage)) : 1;

            if (CurrentPage >= totalPages) CurrentPage = totalPages - 1;
            if (CurrentPage < 0) CurrentPage = 0;

            int startIndex = isPaginated ? CurrentPage * PaginatedSlotsPerPage : 0;

            // Populate slots
            for (int i = 0; i < MaxSlots; i++)
            {
                if (i < slotsCount)
                {
                    if (_modSlots[i] != null)
                    {
                        _modSlots[i].SetActive(true);
                        _modSlots[i].transform.SetSiblingIndex(i);

                        int itemIndex = startIndex + i;
                        if (itemIndex < totalItems)
                        {
                            var item = items[itemIndex];
                            _modSlotTexts[i].text = item.Label.ToUpperInvariant();
                            _modSlotButtons[i].interactable = item.Interactable;
                            _modSlotButtons[i].onClick.RemoveAllListeners();
                            _modSlotButtons[i].onClick.AddListener(() =>
                            {
                                try
                                {
                                    item.OnClick?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    ZUICrashHandler.HandleException(ex, $"MainMenu button '{item.Id}'", item.OnClick?.Method?.DeclaringType?.Assembly);
                                    Plugin.Log.LogError($"[ZUIMainMenu] Error executing button '{item.Id}' callback: {ex}");
                                }
                            });

                            _modSlotCanvasGroups[i].alpha = 1f;
                            _modSlotCanvasGroups[i].interactable = item.Interactable;
                            _modSlotCanvasGroups[i].blocksRaycasts = true;
                        }
                        else
                        {
                            // Empty slot: keep active in layout hierarchy to retain 77px slot height, but completely invisible & inert
                            _modSlotTexts[i].text = string.Empty;
                            _modSlotButtons[i].onClick.RemoveAllListeners();

                            _modSlotCanvasGroups[i].alpha = 0f;
                            _modSlotCanvasGroups[i].interactable = false;
                            _modSlotCanvasGroups[i].blocksRaycasts = false;
                        }
                    }
                }
                else
                {
                    if (_modSlots[i] != null)
                    {
                        _modSlots[i].SetActive(false);
                    }
                }
            }

            // Configure Return Button
            if (_moreButton != null)
            {
                _moreButton.SetActive(true);
                if (_moreButtonText != null)
                {
                    _moreButtonText.text = GetReturnLabel();
                }
            }

            // Layout based on pagination subtype
            if (isPaginated)
            {
                // Slot 3: Return button
                if (_moreButton != null) _moreButton.transform.SetSiblingIndex(3);

                // Slot 4: Pagination Row
                if (_paginationRow != null)
                {
                    _paginationRow.SetActive(true);
                    _paginationRow.transform.SetSiblingIndex(4);
                }

                // Update pagination controls
                if (_pageIndicatorText != null)
                {
                    _pageIndicatorText.text = $"PAGE {CurrentPage + 1} / {totalPages}";
                }

                if (_prevButton != null)
                {
                    bool canPrev = CurrentPage > 0;
                    _prevButton.interactable = canPrev;
                    if (canPrev) _prevHoverEffect?.SetEnabledState();
                    else _prevHoverEffect?.SetDisabledState();
                }

                if (_nextButton != null)
                {
                    bool canNext = CurrentPage < totalPages - 1;
                    _nextButton.interactable = canNext;
                    if (canNext) _nextHoverEffect?.SetEnabledState();
                    else _nextHoverEffect?.SetDisabledState();
                }
            }
            else
            {
                // Slot 4: Return button (replaces pagination row)
                if (_moreButton != null) _moreButton.transform.SetSiblingIndex(4);

                // Hide pagination row
                if (_paginationRow != null) _paginationRow.SetActive(false);
            }

            // Configure Breadcrumb Indicator (Slot 5, under 5th button)
            if (_breadcrumbBar != null)
            {
                _breadcrumbBar.SetActive(true);
                _breadcrumbBar.transform.SetSiblingIndex(5);
                if (_breadcrumbText != null)
                {
                    _breadcrumbText.text = GetBreadcrumbText();
                }
            }
        }

        public void NextPage()
        {
            var drawer = CurrentDrawer;
            if (drawer != null && !drawer.Paginated) return;

            var items = (drawer == null || drawer == ZUIMainMenu.RootDrawer)
                ? ZUIMainMenu.GetSortedRegistrations()
                : drawer.GetSortedButtons();

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / PaginatedSlotsPerPage));
            if (CurrentPage < totalPages - 1)
            {
                CurrentPage++;
                RefreshTabbedView();
            }
        }

        public void PreviousPage()
        {
            var drawer = CurrentDrawer;
            if (drawer != null && !drawer.Paginated) return;

            if (CurrentPage > 0)
            {
                CurrentPage--;
                RefreshTabbedView();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
