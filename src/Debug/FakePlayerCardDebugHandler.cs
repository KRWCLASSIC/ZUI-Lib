using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZUI.Fixes;
using ZUI.Notifications;
using ZUI.PauseMenu;

namespace ZUI.Debug
{
    /// <summary>
    /// Debug-only component for testing fake player rows in the pause menu without multiplayer.
    /// Exclusively lives in the Debug subsystem and does not affect production player cards.
    /// </summary>
    internal class FakePlayerCardDebugHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string FakeName;
        public float FakeVolume = 1.0f;

        private RectTransform _nameRect;
        private TMPro.TMP_Text _nameText;
        private RectTransform _voiceSideRect;
        private GameObject _kickBtn;
        private GameObject _voiceSideObj;
        private GameObject _voiceChatSliderObj;
        private GameObject _voiceIconObj;
        private Button _voiceIconBtn;
        private readonly List<GameObject> _actionBtns = new List<GameObject>();
        private GameObject _prevArrow;
        private GameObject _nextArrow;
        private int _page = 0;
        private bool _isHovered;

        public void Setup(string name)
        {
            FakeName = name;

            var vanillaUI = GetComponent<EscapePlayerUI>();
            if (vanillaUI != null) Destroy(vanillaUI);

            var prodExt = GetComponent<ZUIPlayerCardExtension>();
            if (prodExt != null) Destroy(prodExt);

            var nameTransform = transform.Find("PlayerName");
            if (nameTransform != null)
            {
                _nameRect = nameTransform as RectTransform;
                _nameText = nameTransform.GetComponent<TMPro.TMP_Text>();
                if (_nameText != null) _nameText.text = name;
            }

            var kickTransform = transform.Find("KickButton");
            if (kickTransform != null)
            {
                _kickBtn = kickTransform.gameObject;
                _kickBtn.SetActive(false);

                var oldBtn = _kickBtn.GetComponent<Button>();
                if (oldBtn != null)
                {
                    var colors = oldBtn.colors;
                    var transition = oldBtn.transition;
                    var targetGraphic = oldBtn.targetGraphic;
                    DestroyImmediate(oldBtn);

                    var btn = _kickBtn.AddComponent<Button>();
                    btn.navigation = new Navigation { mode = Navigation.Mode.None };
                    btn.colors = colors;
                    btn.transition = transition;
                    btn.targetGraphic = targetGraphic;
                    btn.onClick.AddListener(() =>
                    {
                        ZUIInfoText.Show($"Kicked {FakeName} (Fake Player)!", drawOnTop: true);
                        ZUIDebug.RemoveFakePlayer(gameObject);
                    });
                }
            }

            var voiceSideT = transform.Find("VoiceSide");
            if (voiceSideT != null)
            {
                _voiceSideObj = voiceSideT.gameObject;
                _voiceSideRect = voiceSideT as RectTransform;
                _voiceSideObj.SetActive(true);

                var sliderT = voiceSideT.Find("VoiceChatSlider");
                if (sliderT != null)
                {
                    _voiceChatSliderObj = sliderT.gameObject;
                    var slider = _voiceChatSliderObj.GetComponent<Slider>();
                    if (slider != null)
                    {
                        slider.minValue = 0f;
                        slider.maxValue = 2f;
                        slider.value = FakeVolume;
                        slider.onValueChanged.RemoveAllListeners();
                        slider.onValueChanged.AddListener(val =>
                        {
                            FakeVolume = val;
                            Plugin.Log.LogInfo($"[ZUI Debug] {FakeName} volume: {val:F2}");
                        });
                    }
                }

                var iconT = voiceSideT.Find("VoiceIcon");
                if (iconT != null)
                {
                    _voiceIconObj = iconT.gameObject;
                }
            }

            Rebuild();
        }

        public void SetFakeName(string name)
        {
            FakeName = name;
            if (_nameText != null)
            {
                _nameText.text = name;
                _nameText.ForceMeshUpdate();
            }
            Rebuild();
        }

        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name.StartsWith("ZUI_ActionBtn_") || child.name.StartsWith("ZUI_PrevArrow") || child.name.StartsWith("ZUI_NextArrow"))
                {
                    DestroyImmediate(child);
                }
            }
            _actionBtns.Clear();
            _prevArrow = null;
            _nextArrow = null;

            var actions = ZUIPauseMenu.PlayerActions;
            int totalActions = actions.Count;
            const int maxSlots = 3;

            bool isPaginated = totalActions > maxSlots;
            int totalPages = isPaginated ? Mathf.CeilToInt((float)totalActions / maxSlots) : 1;
            if (_page >= totalPages) _page = Mathf.Max(0, totalPages - 1);

            int startIndex = _page * maxSlots;
            int countOnPage = isPaginated ? Mathf.Min(maxSlots, totalActions - startIndex) : totalActions;

            float currentRightX = -56f;

            if (isPaginated)
            {
                currentRightX -= 4f;
                float nextArrowX = currentRightX - 13f;
                _nextArrow = CreateArrow("ZUI_NextArrow", ">", new Vector2(nextArrowX, 0f), () =>
                {
                    _page = (_page + 1) % totalPages;
                    Rebuild();
                });
                currentRightX -= 26f;
            }

            for (int slot = 0; slot < countOnPage; slot++)
            {
                int actionIndex = startIndex + slot;
                if (actionIndex >= totalActions) break;

                var action = actions[actionIndex];
                currentRightX -= 6f;
                float slotCenter = currentRightX - 22f;
                var btnObj = SpawnActionButton(action, slotCenter);
                if (btnObj != null) _actionBtns.Add(btnObj);
                currentRightX -= 44f;
            }

            if (isPaginated)
            {
                currentRightX -= 4f;
                float prevArrowX = currentRightX - 13f;
                _prevArrow = CreateArrow("ZUI_PrevArrow", "<", new Vector2(prevArrowX, 0f), () =>
                {
                    _page = (_page - 1 + totalPages) % totalPages;
                    Rebuild();
                });
                currentRightX -= 26f;
            }

            float actionAreaLeftEdge = currentRightX;

            float voiceStartX = PlayerCardLayoutFix.CalculateVoiceStartX(_nameText, _nameRect);
            float kickLeftEdge = actionAreaLeftEdge < 0f ? actionAreaLeftEdge : -56f;
            float actionAreaLeftX = 989f + kickLeftEdge;
            float requiredRightX = voiceStartX + PlayerCardLayoutFix.FULL_VOICE_SIDE_WIDTH + 10f;

            bool doesNotFit = requiredRightX > actionAreaLeftX;
            bool shouldCollapse = totalActions >= 3 || isPaginated || doesNotFit;

            if (shouldCollapse)
            {
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

                    if (_voiceIconBtn == null)
                    {
                        _voiceIconBtn = _voiceIconObj.GetComponent<Button>();
                        if (_voiceIconBtn == null)
                        {
                            _voiceIconBtn = _voiceIconObj.AddComponent<Button>();
                            _voiceIconBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                            _voiceIconBtn.transition = Selectable.Transition.ColorTint;
                            var cb = _voiceIconBtn.colors;
                            cb.normalColor = Color.white;
                            cb.highlightedColor = new Color(0.31f, 0.76f, 0.97f, 1f); // #4FC3F7
                            cb.pressedColor = new Color(0.2f, 0.6f, 0.8f, 1f);
                            _voiceIconBtn.colors = cb;
                        }
                    }

                    _voiceIconBtn.onClick.RemoveAllListeners();
                    _voiceIconBtn.onClick.AddListener(OpenVolumePopup);
                }
            }
            else
            {
                if (_voiceIconBtn != null)
                {
                    Destroy(_voiceIconBtn);
                    _voiceIconBtn = null;
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

            UpdateHoverVisibility();
        }

        private void OpenVolumePopup()
        {
            ZUIPlayerCardExtension.OpenVolumePopupDialog(
                username: FakeName,
                currentVolume: FakeVolume,
                sliderTemplate: _voiceChatSliderObj,
                onVolumeChanged: val =>
                {
                    FakeVolume = val;
                    Plugin.Log.LogInfo($"[ZUI Debug] {FakeName} volume changed to: {val:F2}");
                }
            );
        }

        private GameObject SpawnActionButton(ZUIPlayerAction action, float xCenter)
        {
            GameObject btnObj;
            if (_kickBtn != null)
            {
                btnObj = Instantiate(_kickBtn, transform);
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
                if (action.Icon != null) image.sprite = action.Icon;
                image.color = action.NormalColor.GetValueOrDefault(Color.white);
            }

            var oldBtn = btnObj.GetComponent<Button>();
            ColorBlock colors = ColorBlock.defaultColorBlock;
            Selectable.Transition transition = Selectable.Transition.ColorTint;
            Graphic targetGraphic = btnObj.GetComponent<Graphic>();
            if (oldBtn != null)
            {
                colors = oldBtn.colors;
                transition = oldBtn.transition;
                targetGraphic = oldBtn.targetGraphic ?? targetGraphic;
                DestroyImmediate(oldBtn);
            }

            var btn = btnObj.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.colors = colors;
            btn.transition = transition;
            btn.targetGraphic = targetGraphic;
            btn.onClick.AddListener(() =>
            {
                ZUIInfoText.Show($"Action '{action.Id}' clicked on {FakeName} (Fake UI Player)!", drawOnTop: true);
            });

            if (action.HoverColor.HasValue)
            {
                var cb = btn.colors;
                cb.highlightedColor = action.HoverColor.Value;
                cb.pressedColor = action.HoverColor.Value * 0.8f;
                btn.colors = cb;
            }

            return btnObj;
        }

        private GameObject CreateArrow(string name, string symbol, Vector2 pos, Action onClick)
        {
            GameObject arrowObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            arrowObj.transform.SetParent(transform, false);

            var rt = arrowObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(26f, 36f);
            rt.anchoredPosition = pos;

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

            GameObject textObj = new GameObject("ArrowText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObj.transform.SetParent(arrowObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = symbol;
            tmp.fontSize = 22f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            return arrowObj;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateHoverVisibility();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateHoverVisibility();
        }

        private void UpdateHoverVisibility()
        {
            if (_kickBtn != null) _kickBtn.SetActive(_isHovered);

            var actions = ZUIPauseMenu.PlayerActions;
            const int maxSlots = 3;
            int startIndex = _page * maxSlots;
            for (int i = 0; i < _actionBtns.Count; i++)
            {
                if (_actionBtns[i] == null) continue;
                int actionIndex = startIndex + i;
                bool always = actionIndex < actions.Count && actions[actionIndex].AlwaysVisible;
                _actionBtns[i].SetActive(always || _isHovered);
            }
            if (_prevArrow != null) _prevArrow.SetActive(_isHovered);
            if (_nextArrow != null) _nextArrow.SetActive(_isHovered);
        }
    }
}

