using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;
using ZUI.PauseMenu;

namespace ZUI.Screens
{
    /// <summary>
    /// Manages the cloned UI hierarchy for a single <see cref="ZUIScreen"/> instance.
    /// </summary>
    internal class ZUIScreenView
    {
        public const int MaxSlots = 6;
        public const int PaginatedSlotsPerPage = 5;

        public ZUIScreen Screen { get; }
        public GameObject RootObject { get; private set; }

        // Left Column Elements
        private GameObject _leftHeaderObj;
        private TMP_Text _leftHeaderText;
        private GameObject _buttonTemplate;
        private readonly GameObject[] _modSlots = new GameObject[MaxSlots];
        private readonly TextMeshProUGUI[] _modSlotTexts = new TextMeshProUGUI[MaxSlots];
        private readonly Button[] _modSlotButtons = new Button[MaxSlots];
        private readonly CanvasGroup[] _modSlotCanvasGroups = new CanvasGroup[MaxSlots];

        private GameObject _backButtonObj;
        private TextMeshProUGUI _backButtonText;

        private GameObject _paginationRow;
        private Button _prevButton;
        private Button _nextButton;
        private TextMeshProUGUI _prevButtonText;
        private TextMeshProUGUI _nextButtonText;
        private TextMeshProUGUI _pageIndicatorText;
        private ArrowButtonHoverEffect _prevHoverEffect;
        private ArrowButtonHoverEffect _nextHoverEffect;

        private GameObject _breadcrumbBar;
        private TextMeshProUGUI _breadcrumbText;

        // Right Column Elements
        private GameObject _rightContainerObj;
        private Transform _rightTopTransform;
        private readonly List<GameObject> _spawnedRightItems = new List<GameObject>();

        // Navigation State
        private readonly Stack<PauseMenuDrawer> _navigationStack = new Stack<PauseMenuDrawer>();
        public int CurrentPage { get; private set; } = 0;

        public bool HasNestedDrawers => _navigationStack.Count > 0;
        public PauseMenuDrawer CurrentDrawer => _navigationStack.Count > 0 ? _navigationStack.Peek() : Screen.RootDrawer;
        public PauseMenuRightPanel CurrentRightPanel { get; private set; }

        private ZUIScreenView(ZUIScreen screen)
        {
            Screen = screen;
        }

        public static ZUIScreenView Create(ZUIScreen screen, Transform canvasTransform, GameObject vanillaPausePanel)
        {
            var view = new ZUIScreenView(screen);
            if (!view.BuildHierarchy(canvasTransform, vanillaPausePanel))
            {
                return null;
            }
            return view;
        }

        private bool BuildHierarchy(Transform canvasTransform, GameObject vanillaPausePanel)
        {
            RootObject = UnityEngine.Object.Instantiate(vanillaPausePanel, canvasTransform, false);
            RootObject.name = $"ZUI_CustomScreen_{Screen.Id}";
            RootObject.SetActive(false);

            Transform leftButtonsContainer = RootObject.transform.Find("Buttons/Buttons");
            Transform rightPlayersOuter = RootObject.transform.Find("Players");
            Transform rightPlayersContainer = RootObject.transform.Find("Players/Players");

            if (leftButtonsContainer == null || rightPlayersContainer == null)
            {
                Plugin.Log.LogError($"[ZUIScreen] Cloned pausePanel for '{Screen.Id}' is missing Buttons/Buttons or Players/Players.");
                UnityEngine.Object.Destroy(RootObject);
                RootObject = null;
                return false;
            }

            // 1. Clean up Left Column (Buttons/Buttons)
            var inheritedController = leftButtonsContainer.GetComponent<PauseMenuTabController>();
            if (inheritedController != null)
            {
                UnityEngine.Object.DestroyImmediate(inheritedController);
            }

            Transform headerT = leftButtonsContainer.Find("Header");
            if (headerT != null)
            {
                _leftHeaderObj = headerT.gameObject;
                var loc = _leftHeaderObj.GetComponentInChildren<LocalizedTextUI>(true);
                if (loc != null) UnityEngine.Object.DestroyImmediate(loc);
                _leftHeaderText = _leftHeaderObj.GetComponentInChildren<TMP_Text>(true);
            }

            Transform quitT = leftButtonsContainer.Find("QuitButton");
            if (quitT == null)
            {
                Plugin.Log.LogError($"[ZUIScreen] Cloned pausePanel for '{Screen.Id}' is missing QuitButton template.");
                UnityEngine.Object.Destroy(RootObject);
                RootObject = null;
                return false;
            }

            // Create clean inactive button template attached to RootObject
            _buttonTemplate = UnityEngine.Object.Instantiate(quitT.gameObject, RootObject.transform);
            _buttonTemplate.name = "ZUI_ScreenButtonTemplate";
            CleanupClonedButton(_buttonTemplate);
            _buttonTemplate.SetActive(false);

            // Destroy all children in Buttons/Buttons except Header
            var leftChildrenToDestroy = new List<GameObject>();
            for (int i = 0; i < leftButtonsContainer.childCount; i++)
            {
                Transform ch = leftButtonsContainer.GetChild(i);
                if (ch != headerT)
                {
                    leftChildrenToDestroy.Add(ch.gameObject);
                }
            }
            for (int i = 0; i < leftChildrenToDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(leftChildrenToDestroy[i]);
            }

            // Create 6 Mod Slots
            for (int i = 0; i < MaxSlots; i++)
            {
                var slotObj = UnityEngine.Object.Instantiate(_buttonTemplate, leftButtonsContainer);
                slotObj.name = $"ZUI_ScreenSlot_{i}";
                CleanupClonedButton(slotObj);

                _modSlots[i] = slotObj;
                _modSlotTexts[i] = slotObj.GetComponentInChildren<TextMeshProUGUI>(true);
                _modSlotButtons[i] = slotObj.GetComponent<Button>();
                if (_modSlotButtons[i] != null)
                {
                    _modSlotButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                }

                var cg = slotObj.GetComponent<CanvasGroup>() ?? slotObj.AddComponent<CanvasGroup>();
                _modSlotCanvasGroups[i] = cg;

                slotObj.SetActive(false);
            }

            // Create Back / Close Button
            _backButtonObj = UnityEngine.Object.Instantiate(_buttonTemplate, leftButtonsContainer);
            _backButtonObj.name = "ZUI_ScreenBackButton";
            CleanupClonedButton(_backButtonObj);

            _backButtonText = _backButtonObj.GetComponentInChildren<TextMeshProUGUI>(true);
            var backBtn = _backButtonObj.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                backBtn.onClick.AddListener(OnBackButtonClicked);
            }
            _backButtonObj.SetActive(true);

            // Create Pagination Row & Breadcrumb Bar
            CreatePaginationRow(_buttonTemplate, leftButtonsContainer);
            CreateBreadcrumbBar(_buttonTemplate, leftButtonsContainer);

            // 2. Clean up Right Column (Players/Players)
            if (rightPlayersOuter != null)
            {
                for (int i = rightPlayersOuter.childCount - 1; i >= 0; i--)
                {
                    Transform ch = rightPlayersOuter.GetChild(i);
                    if (ch != rightPlayersContainer)
                    {
                        UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    }
                }
            }

            _rightContainerObj = rightPlayersContainer.gameObject;
            _rightContainerObj.SetActive(true);

            var rightChildrenToDestroy = new List<GameObject>();
            for (int i = 0; i < rightPlayersContainer.childCount; i++)
            {
                Transform ch = rightPlayersContainer.GetChild(i);
                if (ch.name == "Top")
                {
                    _rightTopTransform = ch;
                }
                else
                {
                    rightChildrenToDestroy.Add(ch.gameObject);
                }
            }
            for (int i = 0; i < rightChildrenToDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(rightChildrenToDestroy[i]);
            }

            return true;
        }

        private static void CleanupClonedButton(GameObject btnObj)
        {
            var bf = btnObj.GetComponent<ButtonFunctions>();
            if (bf != null) UnityEngine.Object.DestroyImmediate(bf);

            var loc = btnObj.GetComponentInChildren<LocalizedTextUI>(true);
            if (loc != null) UnityEngine.Object.DestroyImmediate(loc);

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
            }
        }

        public void Show(int siblingIndex)
        {
            if (RootObject == null) return;

            _navigationStack.Clear();
            CurrentPage = 0;
            CurrentRightPanel = null;

            RootObject.transform.SetSiblingIndex(siblingIndex);
            RootObject.SetActive(true);

            RefreshView();
        }

        public void Hide()
        {
            PauseMenuRightPanelController.DetachCustomContentFromList(_spawnedRightItems);
            CurrentRightPanel = null;
            _navigationStack.Clear();
            CurrentPage = 0;

            if (RootObject != null)
            {
                RootObject.SetActive(false);
            }
        }

        public void CleanupBeforeSceneUnload()
        {
            PauseMenuRightPanelController.DetachCustomContentFromList(_spawnedRightItems);
            _spawnedRightItems.Clear();
            CurrentRightPanel = null;
            RootObject = null;
        }

        public void DestroyView()
        {
            PauseMenuRightPanelController.DetachCustomContentFromList(_spawnedRightItems);
            _spawnedRightItems.Clear();
            CurrentRightPanel = null;
            if (RootObject != null)
            {
                UnityEngine.Object.Destroy(RootObject);
                RootObject = null;
            }
        }

        public void PushDrawer(PauseMenuDrawer drawer)
        {
            if (drawer == null) return;
            _navigationStack.Push(drawer);
            CurrentPage = 0;
            RefreshView();
        }

        public void PopDrawer()
        {
            if (_navigationStack.Count > 0)
            {
                _navigationStack.Pop();
                CurrentPage = 0;
                RefreshView();
            }
            else
            {
                Screen.Close();
            }
        }

        private void OnBackButtonClicked()
        {
            PopDrawer();
        }

        public void RefreshView()
        {
            if (RootObject == null || !RootObject.activeSelf) return;

            // 1. Update Left Column Header
            int slotOffset = 0;
            if (_leftHeaderObj != null)
            {
                bool showHeader = Screen.ShowLeftHeader && !string.IsNullOrEmpty(Screen.Title);
                _leftHeaderObj.SetActive(showHeader);
                if (showHeader)
                {
                    _leftHeaderObj.transform.SetSiblingIndex(0);
                    slotOffset = 1;
                    if (_leftHeaderText != null)
                    {
                        _leftHeaderText.richText = true;
                        string upperTitle = Screen.Title.ToUpperInvariant();
                        _leftHeaderText.text = !string.IsNullOrEmpty(Screen.TitleHex)
                            ? $"<color=#{Screen.TitleHex.TrimStart('#')}>{upperTitle}</color>"
                            : upperTitle;
                    }
                }
            }

            // 2. Populate Left Mod Slots
            var drawer = CurrentDrawer;
            bool isPaginated = drawer == null || drawer.Paginated;
            int slotsCount = isPaginated ? PaginatedSlotsPerPage : MaxSlots;

            List<PauseMenuButtonRegistration> items = drawer != null
                ? drawer.GetSortedButtons()
                : new List<PauseMenuButtonRegistration>();

            int totalItems = items.Count;
            int totalPages = isPaginated ? Mathf.Max(1, Mathf.CeilToInt((float)totalItems / PaginatedSlotsPerPage)) : 1;

            if (CurrentPage >= totalPages) CurrentPage = totalPages - 1;
            if (CurrentPage < 0) CurrentPage = 0;

            int startIndex = isPaginated ? CurrentPage * PaginatedSlotsPerPage : 0;

            for (int i = 0; i < MaxSlots; i++)
            {
                if (i < slotsCount)
                {
                    if (_modSlots[i] != null)
                    {
                        _modSlots[i].SetActive(true);
                        _modSlots[i].transform.SetSiblingIndex(i + slotOffset);

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
                                    ZUICrashHandler.HandleException(ex, $"ZUIScreen button '{item.Id}'", item.OnClick?.Method?.DeclaringType?.Assembly);
                                    Plugin.Log.LogError($"[ZUIScreen] Error executing button '{item.Id}' callback: {ex}");
                                }
                            });

                            _modSlotCanvasGroups[i].alpha = 1f;
                            _modSlotCanvasGroups[i].interactable = item.Interactable;
                            _modSlotCanvasGroups[i].blocksRaycasts = true;
                        }
                        else
                        {
                            _modSlotTexts[i].text = string.Empty;
                            _modSlotButtons[i].onClick.RemoveAllListeners();

                            _modSlotCanvasGroups[i].alpha = 0f;
                            _modSlotCanvasGroups[i].interactable = false;
                            _modSlotCanvasGroups[i].blocksRaycasts = false;
                        }
                    }
                }
                else if (_modSlots[i] != null)
                {
                    _modSlots[i].SetActive(false);
                }
            }

            // 3. Configure Back / Close Button
            if (_backButtonObj != null)
            {
                _backButtonObj.SetActive(true);
                if (_backButtonText != null)
                {
                    _backButtonText.text = GetBackButtonLabel();
                }
            }

            if (isPaginated)
            {
                if (_backButtonObj != null) _backButtonObj.transform.SetSiblingIndex(slotOffset + 5);

                if (_paginationRow != null)
                {
                    _paginationRow.SetActive(true);
                    _paginationRow.transform.SetSiblingIndex(slotOffset + 6);
                }

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
                if (_backButtonObj != null) _backButtonObj.transform.SetSiblingIndex(slotOffset + 6);
                if (_paginationRow != null) _paginationRow.SetActive(false);
            }

            // 4. Configure Breadcrumb Bar
            if (_breadcrumbBar != null)
            {
                _breadcrumbBar.SetActive(true);
                _breadcrumbBar.transform.SetSiblingIndex(slotOffset + 7);
                if (_breadcrumbText != null)
                {
                    _breadcrumbText.text = GetBreadcrumbText();
                }
            }

            // 5. Sync Right Column Viewport
            SyncRightPanel();
        }

        public void SyncRightPanel()
        {
            if (_rightContainerObj == null) return;

            var activeDrawer = CurrentDrawer;
            var targetPanel = (activeDrawer != null && activeDrawer != Screen.RootDrawer && activeDrawer.HasRightPanel)
                ? activeDrawer.RightPanel
                : Screen.RightPanel;

            if (CurrentRightPanel != targetPanel || targetPanel.NeedsFullRebuild)
            {
                CurrentRightPanel = targetPanel;
                targetPanel.NeedsFullRebuild = false;
                PauseMenuRightPanelController.PopulateContainer(
                    targetPanel,
                    _rightContainerObj,
                    _rightTopTransform,
                    _spawnedRightItems,
                    _buttonTemplate);
            }
            else
            {
                ApplyRightPanelHeader();
            }
        }

        public void ApplyRightPanelHeader()
        {
            if (CurrentRightPanel == null || _rightTopTransform == null) return;
            PauseMenuRightPanelController.ApplyHeaderToTransform(CurrentRightPanel, _rightTopTransform);
        }

        public void RebuildRightLayout()
        {
            if (_rightContainerObj != null && _rightContainerObj.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rightContainerObj.transform as RectTransform);
            }
        }

        private string GetBackButtonLabel()
        {
            if (_navigationStack.Count == 0)
            {
                return string.IsNullOrEmpty(Screen.CloseButtonLabel) ? "CLOSE" : Screen.CloseButtonLabel.ToUpperInvariant();
            }

            var current = _navigationStack.Peek();
            if (current.Parent != null && current.Parent != Screen.RootDrawer && !string.IsNullOrEmpty(current.Parent.Title))
            {
                return $"< {current.Parent.Title.ToUpperInvariant()}";
            }

            string rootTitle = !string.IsNullOrEmpty(Screen.Title) ? Screen.Title.ToUpperInvariant() : "BACK";
            return $"< {rootTitle}";
        }

        private string GetBreadcrumbText()
        {
            string rootLabel = !string.IsNullOrEmpty(Screen.Title) ? Screen.Title.ToUpperInvariant() : "SCREEN";
            string rootColor = !string.IsNullOrEmpty(Screen.TitleHex) ? Screen.TitleHex : "#FFFFFF";

            if (_navigationStack.Count == 0)
            {
                return $"<color={rootColor}>{rootLabel}</color>";
            }

            var drawers = _navigationStack.ToArray();
            Array.Reverse(drawers);

            var entries = new List<string>
            {
                $"<color=#888888>{rootLabel}</color>"
            };

            for (int i = 0; i < drawers.Length; i++)
            {
                var d = drawers[i];
                if (d != Screen.RootDrawer)
                {
                    bool isCurrent = (i == drawers.Length - 1);
                    string colorHex = !string.IsNullOrEmpty(d.BreadcrumbHex)
                        ? d.BreadcrumbHex
                        : (isCurrent ? "#FFFFFF" : "#888888");
                    entries.Add($"<color={colorHex}>{d.Title.ToUpperInvariant()}</color>");
                }
            }

            return string.Join(" <color=#555555>/</color> ", entries);
        }

        private void NextPage()
        {
            var drawer = CurrentDrawer;
            if (drawer != null && !drawer.Paginated) return;

            var items = drawer != null ? drawer.GetSortedButtons() : new List<PauseMenuButtonRegistration>();
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / PaginatedSlotsPerPage));
            if (CurrentPage < totalPages - 1)
            {
                CurrentPage++;
                RefreshView();
            }
        }

        private void PreviousPage()
        {
            var drawer = CurrentDrawer;
            if (drawer != null && !drawer.Paginated) return;

            if (CurrentPage > 0)
            {
                CurrentPage--;
                RefreshView();
            }
        }

        private void CreatePaginationRow(GameObject buttonTemplate, Transform parent)
        {
            _paginationRow = new GameObject("ZUI_ScreenPaginationRow", typeof(RectTransform));
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

            var font = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
            Color boxBgColor = new Color(0.04f, 0.04f, 0.04f, 0.75f);

            GameObject prevObj = CreateArrowButton("PrevPageButton", "<", PreviousPage, out _prevButtonText, out _prevHoverEffect, font, boxBgColor);
            _prevButton = prevObj.GetComponent<Button>();

            GameObject indicatorObj = new GameObject("PageIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicatorObj.transform.SetParent(_paginationRow.transform, false);

            var bgImage = indicatorObj.GetComponent<Image>();
            bgImage.color = boxBgColor;
            bgImage.raycastTarget = false;

            var indLayout = indicatorObj.AddComponent<LayoutElement>();
            indLayout.preferredWidth = 349f;
            indLayout.minWidth = 349f;
            indLayout.flexibleWidth = 1f;

            GameObject textObj = new GameObject("IndicatorText", typeof(RectTransform));
            textObj.transform.SetParent(indicatorObj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            _pageIndicatorText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _pageIndicatorText.font = font;
            _pageIndicatorText.margin = Vector4.zero;
            _pageIndicatorText.alignment = TextAlignmentOptions.Center;
            _pageIndicatorText.fontSize = 20f;
            _pageIndicatorText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _pageIndicatorText.text = "PAGE 1 / 1";

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
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
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
            _breadcrumbBar = new GameObject("ZUI_ScreenBreadcrumbBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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

            var font = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>(true)?.font;

            GameObject textObj = new GameObject("BreadcrumbText", typeof(RectTransform));
            textObj.transform.SetParent(_breadcrumbBar.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 0f);
            textRt.offsetMax = new Vector2(-16f, 0f);

            _breadcrumbText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) _breadcrumbText.font = font;
            _breadcrumbText.alignment = TextAlignmentOptions.Center;
            _breadcrumbText.fontSize = 15f;
            _breadcrumbText.textWrappingMode = TextWrappingModes.NoWrap;
            _breadcrumbText.overflowMode = TextOverflowModes.Ellipsis;
            _breadcrumbText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            _breadcrumbText.raycastTarget = false;

            _breadcrumbBar.SetActive(false);
        }
    }
}

