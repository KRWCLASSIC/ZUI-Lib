
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;
using ZUI.Fixes;
using ZUI.Popups;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Component attached to native EscapePlayerUI rows in the Pause Menu to manage
    /// custom action buttons, 3-slot mini-pagination, VoiceSide dynamic auto-shift / smart collapse,
    /// and player icon overrides.
    /// </summary>
    public class ZUIPlayerCardExtension : MonoBehaviour
    {
        public const int MAX_VISIBLE_SLOTS = 3;

        public PlayerController TargetPlayer { get; private set; }

        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
        private RectTransform _nameRect;
        private TMP_Text _nameText;
        private Image _playerIconImage;
        private GameObject _kickButtonObj;

        // Voice chat controls from vanilla
        private GameObject _voiceSideObj;
        private RectTransform _voiceSideRect;
        private GameObject _voiceIconObj;
        private GameObject _voiceChatSliderObj;
        private Button _voiceIconButton;

        // Mini-Pagination controls
        private GameObject _prevArrowObj;
        private GameObject _nextArrowObj;
        private int _currentPage = 0;
        private bool _isHovered;

        public static ZUIPlayerCardExtension Attach(EscapePlayerUI playerUI, PlayerController player)
        {
            if (playerUI == null) return null;

            var ext = playerUI.gameObject.GetComponent<ZUIPlayerCardExtension>();
            if (ext == null)
            {
                ext = playerUI.gameObject.AddComponent<ZUIPlayerCardExtension>();
            }

            ext.Initialize(player);
            return ext;
        }

        public void Initialize(PlayerController player)
        {
            TargetPlayer = player;

            var nameTransform = transform.Find("PlayerName");
            if (nameTransform != null)
            {
                _nameRect = nameTransform as RectTransform;
                _nameText = nameTransform.GetComponent<TMP_Text>();
            }

            var iconTransform = transform.Find("PlayerIcon");
            if (iconTransform != null)
            {
                _playerIconImage = iconTransform.GetComponent<Image>();
            }

            var kickTransform = transform.Find("KickButton");
            if (kickTransform != null)
            {
                _kickButtonObj = kickTransform.gameObject;
            }

            var voiceSideTransform = transform.Find("VoiceSide");
            if (voiceSideTransform != null)
            {
                _voiceSideObj = voiceSideTransform.gameObject;
                _voiceSideRect = voiceSideTransform as RectTransform;

                var voiceIconT = voiceSideTransform.Find("VoiceIcon");
                if (voiceIconT != null)
                {
                    _voiceIconObj = voiceIconT.gameObject;
                }

                var sliderT = voiceSideTransform.Find("VoiceChatSlider");
                if (sliderT != null)
                {
                    _voiceChatSliderObj = sliderT.gameObject;
                }
            }

            ApplyPlayerIconOverride();
            RebuildButtons();
        }

        /// <summary>
        /// Clears and reconstructs all custom action buttons, arrow pagination, and VoiceSide layout.
        /// </summary>
        public void RebuildButtons()
        {
            // Update displayed username from TargetPlayer
            if (_nameText != null && TargetPlayer != null)
            {
                _nameText.text = TargetPlayer.PlayerData.username;
            }

            // Destroy previously spawned custom buttons and arrows
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    Destroy(_spawnedButtons[i]);
                }
            }
            _spawnedButtons.Clear();

            if (_prevArrowObj != null) Destroy(_prevArrowObj);
            if (_nextArrowObj != null) Destroy(_nextArrowObj);

            var actions = GetApplicableActions();
            int totalActions = actions.Count;

            // 1. Calculate Pagination
            bool isPaginated = totalActions > MAX_VISIBLE_SLOTS;
            int totalPages = isPaginated ? Mathf.CeilToInt((float)totalActions / MAX_VISIBLE_SLOTS) : 1;
            if (_currentPage >= totalPages) _currentPage = Mathf.Max(0, totalPages - 1);
            if (_currentPage < 0) _currentPage = 0;

            int startIndex = _currentPage * MAX_VISIBLE_SLOTS;
            int countOnPage = isPaginated ? Mathf.Min(MAX_VISIBLE_SLOTS, totalActions - startIndex) : totalActions;

            // KickButton is anchored at (1, 0.5) pos x = -34 (spans from -56 to -12)
            // On yourself (!canHaveKick), no kick button exists, so start right against the -12 margin with no gap
            bool canHaveKick = TargetPlayer == null || !TargetPlayer.IsMine;
            float currentRightX = canHaveKick ? -56f : -12f;

            // 2. Spawn Right Arrow (>) if paginated
            if (isPaginated)
            {
                if (canHaveKick) currentRightX -= 4f; // spacing
                float nextArrowCenter = currentRightX - 13f;
                _nextArrowObj = CreateArrowButton("ZUI_NextArrow", ">", new Vector2(nextArrowCenter, 0f), () =>
                {
                    _currentPage = (_currentPage + 1) % totalPages;
                    RebuildButtons();
                });
                currentRightX -= 26f;
            }

            // 3. Spawn Visible Action Buttons for Current Page (placed right-to-left)
            for (int slot = 0; slot < countOnPage; slot++)
            {
                int actionIndex = startIndex + slot;
                if (actionIndex >= totalActions) break;

                var action = actions[actionIndex];
                if (slot > 0 || isPaginated || canHaveKick)
                {
                    currentRightX -= 6f; // spacing
                }
                float slotCenter = currentRightX - 22f;
                GameObject btnObj = CreateActionButton(action, actionIndex, slotCenter);
                if (btnObj != null)
                {
                    _spawnedButtons.Add(btnObj);
                }
                currentRightX -= 44f;
            }

            // 4. Spawn Left Arrow (<) if paginated
            if (isPaginated)
            {
                currentRightX -= 4f; // spacing
                float prevArrowCenter = currentRightX - 13f;
                _prevArrowObj = CreateArrowButton("ZUI_PrevArrow", "<", new Vector2(prevArrowCenter, 0f), () =>
                {
                    _currentPage = (_currentPage - 1 + totalPages) % totalPages;
                    RebuildButtons();
                });
                currentRightX -= 26f;
            }

            float actionAreaLeftEdge = currentRightX;

            // 5. Layout VoiceSide / Audio Button
            LayoutVoiceSide(totalActions, isPaginated, actionAreaLeftEdge);

            UpdateButtonsVisibility();
        }

        private void LayoutVoiceSide(int totalActions, bool isPaginated, float actionAreaLeftEdge)
        {
            bool voiceActive = _voiceSideObj != null && (TargetPlayer == null || !TargetPlayer.IsMine);

            if (!voiceActive)
            {
                if (_voiceSideObj != null) _voiceSideObj.SetActive(false);
                return;
            }

            float voiceStartX = PlayerCardLayoutFix.CalculateVoiceStartX(_nameText, _nameRect);
            float kickLeftEdge = actionAreaLeftEdge < 0f ? actionAreaLeftEdge : -56f;
            float actionAreaLeftX = 989f + kickLeftEdge;
            float requiredRightX = voiceStartX + PlayerCardLayoutFix.FULL_VOICE_SIDE_WIDTH + 10f;

            // Additional Feature: Collapse to Audio Button if custom action buttons reduce space or >= 3 buttons
            bool doesNotFit = requiredRightX > actionAreaLeftX;
            bool shouldCollapse = totalActions >= 3 || isPaginated || doesNotFit;

            if (shouldCollapse)
            {
                // Collapsed mode: Audio Button positioned at voiceStartX
                if (_voiceChatSliderObj != null) _voiceChatSliderObj.SetActive(false);
                _voiceSideObj.SetActive(true);

                if (_voiceSideRect != null)
                {
                    _voiceSideRect.anchorMin = new Vector2(0f, 0.5f);
                    _voiceSideRect.anchorMax = new Vector2(0f, 0.5f);
                    _voiceSideRect.pivot = new Vector2(0f, 0.5f);
                    _voiceSideRect.sizeDelta = new Vector2(36f, 36f);
                    _voiceSideRect.anchoredPosition = new Vector2(voiceStartX, 0f);
                }

                if (_voiceIconObj != null)
                {
                    var iconImage = _voiceIconObj.GetComponent<Image>();
                    if (iconImage != null) iconImage.raycastTarget = true;

                    if (_voiceIconButton == null)
                    {
                        _voiceIconButton = _voiceIconObj.GetComponent<Button>();
                        if (_voiceIconButton == null)
                        {
                            _voiceIconButton = _voiceIconObj.AddComponent<Button>();
                            _voiceIconButton.navigation = new Navigation { mode = Navigation.Mode.None };
                            _voiceIconButton.transition = Selectable.Transition.ColorTint;
                            var cb = _voiceIconButton.colors;
                            cb.normalColor = Color.white;
                            cb.highlightedColor = new Color(0.31f, 0.76f, 0.97f, 1f); // #4FC3F7
                            cb.pressedColor = new Color(0.2f, 0.6f, 0.8f, 1f);
                            _voiceIconButton.colors = cb;
                        }
                    }

                    _voiceIconButton.onClick.RemoveAllListeners();
                    _voiceIconButton.onClick.AddListener(OpenVolumePopup);
                }
            }
            else
            {
                // Expanded mode: remove button component if present and apply alignment fix
                if (_voiceIconButton != null)
                {
                    Destroy(_voiceIconButton);
                    _voiceIconButton = null;
                }

                PlayerCardLayoutFix.Apply(
                    _nameRect,
                    _nameText,
                    _voiceSideRect,
                    _voiceSideObj,
                    _voiceChatSliderObj,
                    isVoiceActive: true
                );
            }
        }

        private void OpenVolumePopup()
        {
            if (TargetPlayer == null) return;
            string username = TargetPlayer.PlayerData.username ?? "Player";
            float currentVol = TargetPlayer.VoicechatVolume;

            OpenVolumePopupDialog(
                username: username,
                currentVolume: currentVol,
                sliderTemplate: _voiceChatSliderObj,
                onVolumeChanged: val =>
                {
                    if (TargetPlayer != null)
                    {
                        TargetPlayer.VoicechatVolume = val;
                    }
                }
            );
        }

        internal static void OpenVolumePopupDialog(string username, float currentVolume, GameObject sliderTemplate, Action<float> onVolumeChanged)
        {
            var popup = ZUIPopup.ShowAlert(
                title: $"{username.ToUpper()} - VOICE VOLUME",
                message: " ", // Overwrite vanilla "Unknown error." text
                buttonText: "DONE",
                titleHex: "#4FC3F7",
                buttonHex: "#2E7D32",
                dismissOnEscape: true
            );

            if (popup == null) return;

            // In vanilla Zort prefab, DescriptionArea is directly under Popup
            Transform descArea = popup.transform.Find("DescriptionArea")
                              ?? popup.transform.Find("MainArea/DescriptionArea");
            if (descArea == null) return;

            // Hide native DescriptionText so "Unknown error." is not displayed
            var descText = descArea.Find("DescriptionText");
            if (descText != null)
            {
                descText.gameObject.SetActive(false);
            }

            // Configure DescriptionArea to have comfortable height and center its children
            var descLe = descArea.GetComponent<LayoutElement>();
            if (descLe == null) descLe = descArea.gameObject.AddComponent<LayoutElement>();
            descLe.minHeight = 150f;
            descLe.preferredHeight = 150f;

            var vlg = descArea.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = descArea.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16f;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            // 1. Percentage readout label
            GameObject labelObj = new GameObject("ZUI_VolumeLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObj.transform.SetParent(descArea, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(400f, 35f);

            var labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontSize = 24f;
            labelTmp.color = new Color(0.31f, 0.76f, 0.97f, 1f); // #4FC3F7
            labelTmp.text = $"Volume: {Mathf.RoundToInt(currentVolume * 100)}%";

            var labelLe = labelObj.GetComponent<LayoutElement>();
            labelLe.preferredWidth = 400f;
            labelLe.preferredHeight = 35f;
            labelLe.minWidth = 400f;
            labelLe.minHeight = 35f;

            // 2. Cloned Slider (restoring native 20px height so the thumb is vertically centered on the track)
            if (sliderTemplate != null)
            {
                var sliderObj = UnityEngine.Object.Instantiate(sliderTemplate, descArea);
                sliderObj.name = "ZUI_ClonedVoiceSlider";
                sliderObj.SetActive(true);

                var sliderRt = sliderObj.GetComponent<RectTransform>();
                if (sliderRt != null)
                {
                    sliderRt.sizeDelta = new Vector2(400f, 20f);
                }

                var sliderLe = sliderObj.GetComponent<LayoutElement>();
                if (sliderLe == null) sliderLe = sliderObj.AddComponent<LayoutElement>();
                sliderLe.preferredWidth = 400f;
                sliderLe.preferredHeight = 20f;
                sliderLe.minWidth = 400f;
                sliderLe.minHeight = 20f;

                var slider = sliderObj.GetComponentInChildren<Slider>(true);
                if (slider != null)
                {
                    slider.minValue = 0f;
                    slider.maxValue = 2f;
                    slider.value = currentVolume;
                    slider.onValueChanged.RemoveAllListeners();
                    slider.onValueChanged.AddListener(val =>
                    {
                        labelTmp.text = $"Volume: {Mathf.RoundToInt(val * 100)}%";
                        onVolumeChanged?.Invoke(val);
                    });
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(popup.transform as RectTransform);
        }

        private GameObject CreateArrowButton(string name, string symbol, Vector2 position, Action onClick)
        {
            GameObject arrowObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            arrowObj.transform.SetParent(transform, false);

            var rt = arrowObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(26f, 36f);
            rt.anchoredPosition = position;

            var img = arrowObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            img.raycastTarget = true;

            var btn = arrowObj.GetComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            cb.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            cb.pressedColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject textObj = new GameObject("ArrowText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(arrowObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (_nameText != null) tmp.font = _nameText.font;
            tmp.text = symbol;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            tmp.raycastTarget = false;

            return arrowObj;
        }

        private GameObject CreateActionButton(ZUIPlayerAction action, int actionIndex, float xCenter)
        {
            GameObject btnObj;

            // Clone KickButton if available to inherit exact styling
            if (_kickButtonObj != null)
            {
                btnObj = Instantiate(_kickButtonObj, transform);
            }
            else
            {
                btnObj = new GameObject($"ZUI_ActionBtn_{action.Id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(transform, false);
            }

            btnObj.name = $"ZUI_ActionBtn_{action.Id}";

            var rect = btnObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(44f, 44f);
                rect.anchoredPosition = new Vector2(xCenter, 0f);
            }

            var image = btnObj.GetComponent<Image>();
            if (image != null)
            {
                if (action.Icon != null)
                {
                    image.sprite = action.Icon;
                }
                image.color = action.NormalColor.GetValueOrDefault(Color.white);
            }

            // Remove cloned button to completely purge persistent inspector calls from KickButton
            var oldButton = btnObj.GetComponent<Button>();
            ColorBlock colors = ColorBlock.defaultColorBlock;
            Selectable.Transition transition = Selectable.Transition.ColorTint;
            Graphic targetGraphic = btnObj.GetComponent<Graphic>();

            if (oldButton != null)
            {
                colors = oldButton.colors;
                transition = oldButton.transition;
                targetGraphic = oldButton.targetGraphic ?? targetGraphic;
                DestroyImmediate(oldButton);
            }

            var button = btnObj.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = colors;
            button.transition = transition;
            button.targetGraphic = targetGraphic;

            button.onClick.AddListener(() =>
            {
                try
                {
                    action.OnClick?.Invoke(TargetPlayer);
                }
                catch (Exception ex)
                {
                    ZUICrashHandler.HandleException(ex, $"ZUIPlayerAction '{action.Id}'", action.OnClick?.Method?.DeclaringType?.Assembly);
                    Plugin.Log.LogError($"[ZUIPlayerCardExtension] Error executing '{action.Id}': {ex}");
                }
            });

            if (action.HoverColor.HasValue)
            {
                var cb = button.colors;
                cb.highlightedColor = action.HoverColor.Value;
                cb.pressedColor = action.HoverColor.Value * 0.8f;
                button.colors = cb;
            }

            return btnObj;
        }

        private List<ZUIPlayerAction> GetApplicableActions()
        {
            var all = ZUIPauseMenu.PlayerActions;
            var result = new List<ZUIPlayerAction>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                var action = all[i];
                if (action == null) continue;
                try
                {
                    if (action.VisibilityPredicate == null || action.VisibilityPredicate(TargetPlayer))
                    {
                        result.Add(action);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[ZUIPlayerCardExtension] Error evaluating VisibilityPredicate for '{action.Id}': {ex.Message}");
                }
            }
            return result;
        }

        public void OnHoverChanged(bool isHovered)
        {
            _isHovered = isHovered;
            UpdateButtonsVisibility();
        }

        private void UpdateButtonsVisibility()
        {
            var actions = GetApplicableActions();
            int startIndex = _currentPage * MAX_VISIBLE_SLOTS;

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                var btn = _spawnedButtons[i];
                if (btn == null) continue;

                int actionIndex = startIndex + i;
                if (actionIndex >= actions.Count)
                {
                    btn.SetActive(false);
                    continue;
                }

                var action = actions[actionIndex];
                bool shouldShow = action.AlwaysVisible || _isHovered;

                btn.SetActive(shouldShow);
            }

            // Pagination arrows are visible if paginated and either always visible or card hovered
            if (_prevArrowObj != null) _prevArrowObj.SetActive(_isHovered);
            if (_nextArrowObj != null) _nextArrowObj.SetActive(_isHovered);
        }

        private void ApplyPlayerIconOverride()
        {
            if (_playerIconImage != null && TargetPlayer != null && ZUIPauseMenu.PlayerIconResolver != null)
            {
                try
                {
                    var customSprite = ZUIPauseMenu.PlayerIconResolver(TargetPlayer);
                    if (customSprite != null)
                    {
                        _playerIconImage.sprite = customSprite;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[ZUIPlayerCardExtension] Error resolving custom player icon: {ex.Message}");
                }
            }
        }
    }
}
