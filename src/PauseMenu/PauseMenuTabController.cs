using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.PauseMenu
{
    public enum PauseMenuMode
    {
        Vanilla,
        Tabbed
    }

    /// <summary>
    /// Controls the in-game Pause Menu button stack, managing the transition between
    /// vanilla buttons and tabbed mod pages with pagination.
    /// In Vanilla mode: Injects the MORE button after Settings (sibling index 4).
    /// In Tabbed mode: Provides 5 mod slots per page, Slot 6 as Back button,
    /// Slot 7 as Pagination row, and a Breadcrumb bar below Slot 7.
    /// </summary>
    public class PauseMenuTabController : MonoBehaviour
    {
        public const int MaxSlots = 6;
        public const int PaginatedSlotsPerPage = 5;

        // Vanilla references
        private GameObject _header;
        private GameObject _continueButton;
        private GameObject _inviteButton;
        private GameObject _settingsButton;
        private GameObject _returnToLobbyButton;
        private GameObject _returnToMainMenuButton;
        private GameObject _quitButton;
        private bool _returnToLobbyWasActive = false;

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
        private readonly Stack<PauseMenuDrawer> _navigationStack = new Stack<PauseMenuDrawer>();

        // Breadcrumb indicator
        private GameObject _breadcrumbBar;
        private TextMeshProUGUI _breadcrumbText;

        public PauseMenuMode CurrentMode { get; private set; } = PauseMenuMode.Vanilla;
        public int CurrentPage { get; private set; } = 0;
        public PauseMenuDrawer CurrentDrawer => _navigationStack.Count > 0 ? _navigationStack.Peek() : ZUIPauseMenu.RootDrawer;

        internal GameObject ButtonTemplate => _quitButton;

        public bool IsPauseMenuOpen => gameObject.activeInHierarchy;

        private bool _isInitialized = false;
        private int _lastEscapeFrame = -1;

        public static PauseMenuTabController Instance { get; private set; }

        public void Initialize(
            GameObject header,
            GameObject continueBtn,
            GameObject inviteBtn,
            GameObject settingsBtn,
            GameObject returnToLobbyBtn,
            GameObject returnToMainMenuBtn,
            GameObject quitBtn)
        {
            if (_isInitialized) return;

            Instance = this;
            _header = header;
            _continueButton = continueBtn;
            _inviteButton = inviteBtn;
            _settingsButton = settingsBtn;
            _returnToLobbyButton = returnToLobbyBtn;
            _returnToMainMenuButton = returnToMainMenuBtn;
            _quitButton = quitBtn;

            Transform parent = transform;

            // Template to clone from (QuitButton has identical styling, Animator, and ButtonAnimation)
            GameObject template = _quitButton;

            // 1. Create Mod Slots (up to MaxSlots = 6)
            for (int i = 0; i < MaxSlots; i++)
            {
                var slotObj = Instantiate(template, parent);
                slotObj.name = $"ZUI_PauseModSlot_{i}";

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
            _moreButton.name = "ZUI_PauseMoreButton";
            CleanupClonedButton(_moreButton);

            _moreButtonText = _moreButton.GetComponentInChildren<TextMeshProUGUI>();
            var moreBtnComponent = _moreButton.GetComponent<Button>();
            moreBtnComponent.onClick.AddListener(OnMoreButtonClicked);
            _moreButton.SetActive(true);

            // 3. Create Pagination Row
            CreatePaginationRow(template, parent);

            // 4. Create Breadcrumb Indicator (under Slot 7)
            CreateBreadcrumbBar(template, parent);

            _isInitialized = true;
            SetMode(PauseMenuMode.Vanilla);
        }

        private void CreatePaginationRow(GameObject buttonTemplate, Transform parent)
        {
            _paginationRow = new GameObject("ZUI_PausePaginationRow", typeof(RectTransform));
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

            GameObject flashObj = new GameObject("HoverFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flashObj.transform.SetParent(btnObj.transform, false);

            var flashRt = flashObj.GetComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.sizeDelta = Vector2.zero;
            flashRt.anchoredPosition = Vector2.zero;

            var flashImg = flashObj.GetComponent<Image>();
            flashImg.color = new Color(ArrowButtonHoverEffect.FlashColor.r, ArrowButtonHoverEffect.FlashColor.g, ArrowButtonHoverEffect.FlashColor.b, 0f);
            flashImg.raycastTarget = false;

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

            hoverEffect = btnObj.AddComponent<ArrowButtonHoverEffect>();
            hoverEffect.Initialize(flashImg, textComp);

            return btnObj;
        }

        private void CreateBreadcrumbBar(GameObject buttonTemplate, Transform parent)
        {
            _breadcrumbBar = new GameObject("ZUI_PauseBreadcrumbBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            entries.Add("<color=#888888>MORE</color>");

            for (int i = 0; i < drawers.Length; i++)
            {
                var d = drawers[i];
                if (d != ZUIPauseMenu.RootDrawer)
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
            string fullPath = string.Join(BreadcrumbSeparator, entries);
            if (GetTextWidth(fullPath) <= availableWidth)
            {
                return fullPath;
            }

            int leftCount = 1;
            if (entries.Count > 2)
            {
                string minTest = $"{entries[0]}{BreadcrumbSeparator}{entries[1]}{BreadcrumbSeparator}{BreadcrumbEllipsis}{BreadcrumbSeparator}{entries[entries.Count - 1]}";
                if (GetTextWidth(minTest) <= availableWidth)
                {
                    leftCount = 2;
                }
            }

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

            rightIndices.Reverse();

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
            var bf = btnObj.GetComponent<ButtonFunctions>();
            if (bf != null) Destroy(bf);

            var loc = btnObj.GetComponentInChildren<LocalizedTextUI>();
            if (loc != null) Destroy(loc);

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
            }
        }

        public void PushDrawer(PauseMenuDrawer drawer)
        {
            if (drawer == null) return;
            _navigationStack.Push(drawer);
            CurrentPage = 0;
            if (CurrentMode != PauseMenuMode.Tabbed)
            {
                SetMode(PauseMenuMode.Tabbed);
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
                SetMode(PauseMenuMode.Vanilla);
            }
        }

        public bool HandleEscape()
        {
            if (Time.frameCount == _lastEscapeFrame) return true;
            if (Panels.ZUIPanelController.CurrentOpenPanel != null || Panels.ZUIPanelController.LastEscConsumedFrame == Time.frameCount) return true;
            if (Popups.ZUIPopup.LastEscConsumedFrame == Time.frameCount) return true;
            _lastEscapeFrame = Time.frameCount;

            if (CurrentMode == PauseMenuMode.Tabbed)
            {
                PopDrawer();
                return true;
            }
            return false;
        }

        private string GetReturnLabel()
        {
            if (_navigationStack.Count == 0)
            {
                return "< PAUSE MENU";
            }
            var current = _navigationStack.Peek();
            if (current.Parent != null && !string.IsNullOrEmpty(current.Parent.Title))
            {
                return $"< {current.Parent.Title.ToUpperInvariant()}";
            }
            return "< MORE";
        }

        public void SetMode(PauseMenuMode mode)
        {
            CurrentMode = mode;

            if (mode == PauseMenuMode.Vanilla)
            {
                _navigationStack.Clear();

                // Header at index 0
                if (_header != null)
                {
                    _header.SetActive(true);
                    _header.transform.SetSiblingIndex(0);
                }

                // Restore vanilla buttons
                if (_continueButton != null)
                {
                    _continueButton.SetActive(true);
                    _continueButton.transform.SetSiblingIndex(1);
                }
                if (_inviteButton != null)
                {
                    _inviteButton.SetActive(true);
                    _inviteButton.transform.SetSiblingIndex(2);
                }
                if (_settingsButton != null)
                {
                    _settingsButton.SetActive(true);
                    _settingsButton.transform.SetSiblingIndex(3);
                }

                // Slot 4: Injected MORE button
                if (_moreButton != null)
                {
                    _moreButton.SetActive(true);
                    _moreButton.transform.SetSiblingIndex(4);
                    if (_moreButtonText != null) _moreButtonText.text = "MORE";
                }

                // Slot 5: ReturnToLobby (restored to whether it was active)
                if (_returnToLobbyButton != null)
                {
                    _returnToLobbyButton.SetActive(_returnToLobbyWasActive);
                    _returnToLobbyButton.transform.SetSiblingIndex(5);
                }

                // Slot 6: ReturnToMainMenu
                if (_returnToMainMenuButton != null)
                {
                    _returnToMainMenuButton.SetActive(true);
                    _returnToMainMenuButton.transform.SetSiblingIndex(6);
                }

                // Slot 7: QuitButton
                if (_quitButton != null)
                {
                    _quitButton.SetActive(true);
                    _quitButton.transform.SetSiblingIndex(7);
                }

                // Hide all mod slots
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (_modSlots[i] != null) _modSlots[i].SetActive(false);
                }

                // Hide pagination row & breadcrumb bar
                if (_paginationRow != null) _paginationRow.SetActive(false);
                if (_breadcrumbBar != null) _breadcrumbBar.SetActive(false);

                // Restore vanilla Connected Players panel on the right
                PauseMenuRightPanelController.SyncWithCurrentDrawer();
            }
            else // Tabbed / Drawer Mode
            {
                // Record ReturnToLobby active state before hiding
                if (_returnToLobbyButton != null)
                {
                    _returnToLobbyWasActive = _returnToLobbyButton.activeSelf;
                    _returnToLobbyButton.SetActive(false);
                }

                // Keep header visible at index 0
                if (_header != null)
                {
                    _header.SetActive(true);
                    _header.transform.SetSiblingIndex(0);
                }

                // Hide vanilla buttons
                if (_continueButton != null) _continueButton.SetActive(false);
                if (_inviteButton != null) _inviteButton.SetActive(false);
                if (_settingsButton != null) _settingsButton.SetActive(false);
                if (_returnToMainMenuButton != null) _returnToMainMenuButton.SetActive(false);
                if (_quitButton != null) _quitButton.SetActive(false);

                RefreshTabbedView();
            }
        }

        public void ToggleMode()
        {
            SetMode(CurrentMode == PauseMenuMode.Vanilla ? PauseMenuMode.Tabbed : PauseMenuMode.Vanilla);
        }

        private void OnMoreButtonClicked()
        {
            if (CurrentMode == PauseMenuMode.Vanilla)
            {
                SetMode(PauseMenuMode.Tabbed);
            }
            else
            {
                PopDrawer();
            }
        }

        public void RefreshTabbedView()
        {
            if (CurrentMode != PauseMenuMode.Tabbed) return;

            var drawer = CurrentDrawer;
            bool isPaginated = drawer == null || drawer.Paginated;
            int slotsCount = isPaginated ? PaginatedSlotsPerPage : MaxSlots;

            List<PauseMenuButtonRegistration> items;
            if (drawer == null || drawer == ZUIPauseMenu.RootDrawer)
            {
                items = ZUIPauseMenu.GetSortedRegistrations();
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

            // Populate slots (indices 1 to slotsCount, sibling index = i + 1 because index 0 is Header)
            for (int i = 0; i < MaxSlots; i++)
            {
                if (i < slotsCount)
                {
                    if (_modSlots[i] != null)
                    {
                        _modSlots[i].SetActive(true);
                        _modSlots[i].transform.SetSiblingIndex(i + 1);

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
                                    ZUICrashHandler.HandleException(ex, $"PauseMenu button '{item.Id}'", item.OnClick?.Method?.DeclaringType?.Assembly);
                                    Plugin.Log.LogError($"[ZUIPauseMenu] Error executing button '{item.Id}' callback: {ex}");
                                }
                            });

                            _modSlotCanvasGroups[i].alpha = 1f;
                            _modSlotCanvasGroups[i].interactable = item.Interactable;
                            _modSlotCanvasGroups[i].blocksRaycasts = true;
                        }
                        else
                        {
                            // Empty slot: keep active in layout to retain 77px height, but invisible and inert
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

            if (isPaginated)
            {
                // Slot 6: Back / Return button (sibling index 6)
                if (_moreButton != null) _moreButton.transform.SetSiblingIndex(6);

                // Slot 7: Pagination Row (sibling index 7)
                if (_paginationRow != null)
                {
                    _paginationRow.SetActive(true);
                    _paginationRow.transform.SetSiblingIndex(7);
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
                // Slot 7: Back / Return button (sibling index 7, replaces pagination row)
                if (_moreButton != null) _moreButton.transform.SetSiblingIndex(7);

                // Hide pagination row
                if (_paginationRow != null) _paginationRow.SetActive(false);
            }

            // Breadcrumb Bar under Slot 7 (sibling index 8)
            if (_breadcrumbBar != null)
            {
                _breadcrumbBar.SetActive(true);
                _breadcrumbBar.transform.SetSiblingIndex(8);
                if (_breadcrumbText != null)
                {
                    _breadcrumbText.text = GetBreadcrumbText();
                }
            }

            // Sync right-side panel with the active drawer
            PauseMenuRightPanelController.SyncWithCurrentDrawer();
        }

        public void NextPage()
        {
            var drawer = CurrentDrawer;
            if (drawer != null && !drawer.Paginated) return;

            var items = (drawer == null || drawer == ZUIPauseMenu.RootDrawer)
                ? ZUIPauseMenu.GetSortedRegistrations()
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

        public void OnPauseMenuClosed()
        {
            if (CurrentMode == PauseMenuMode.Tabbed)
            {
                SetMode(PauseMenuMode.Vanilla);
            }
            _navigationStack.Clear();
            PauseMenuRightPanelController.RestoreVanillaPlayersList();
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
