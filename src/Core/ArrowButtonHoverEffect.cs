using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZUI.Core
{
    /// <summary>
    /// Custom hover and click effect for pagination arrow buttons.
    /// Replicates the native hover flash and audio without any position movement.
    /// </summary>
    public class ArrowButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        // Color palette:
        // FlashColor matches reference screenshot: #999999 (0.6f, 0.6f, 0.6f, 1f)
        public static readonly Color FlashColor = new Color(0.60f, 0.60f, 0.60f, 1f);
        public static readonly Color TextActiveColor = new Color(0.80f, 0.80f, 0.80f, 1f);
        public static readonly Color TextHoverColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        // Dim subtle gray close to 0.04f background plate so disabled arrows are clearly inactive, not broken
        public static readonly Color TextDisabledColor = new Color(0.14f, 0.14f, 0.14f, 1f);

        private Selectable _selectable;
        private Image _hoverImage;
        private TextMeshProUGUI _arrowText;
        private Coroutine _fadeCoroutine;

        public void Initialize(Image hoverImage, TextMeshProUGUI arrowText)
        {
            _selectable = GetComponent<Selectable>();
            _hoverImage = hoverImage;
            _arrowText = arrowText;
            if (_hoverImage != null)
            {
                var col = FlashColor;
                col.a = 0f;
                _hoverImage.color = col;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable == null || !_selectable.interactable) return;

            try
            {
                StaticInstance<InitUI>.Instance?.PlayHoverSFX();
            }
            catch { }

            StartFade(1f, TextHoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_selectable == null || !_selectable.interactable) return;

            StartFade(0f, TextActiveColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && _selectable.interactable && eventData.button == PointerEventData.InputButton.Left)
            {
                try
                {
                    StaticInstance<InitUI>.Instance?.PlayClickSFX();
                }
                catch { }
            }
        }

        public void SetDisabledState()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (_hoverImage != null)
            {
                var col = FlashColor;
                col.a = 0f;
                _hoverImage.color = col;
            }
            if (_arrowText != null)
            {
                _arrowText.color = TextDisabledColor;
            }
        }

        public void SetEnabledState()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (_hoverImage != null)
            {
                var col = FlashColor;
                col.a = 0f;
                _hoverImage.color = col;
            }
            if (_arrowText != null)
            {
                _arrowText.color = TextActiveColor;
            }
        }

        private void StartFade(float targetAlpha, Color targetTextColor)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, targetTextColor));
        }

        private IEnumerator FadeRoutine(float targetAlpha, Color targetTextColor)
        {
            float duration = 0.08f;
            float elapsed = 0f;
            float startAlpha = _hoverImage != null ? _hoverImage.color.a : 0f;
            Color startTextColor = _arrowText != null ? _arrowText.color : targetTextColor;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (_hoverImage != null)
                {
                    Color col = FlashColor;
                    col.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                    _hoverImage.color = col;
                }

                if (_arrowText != null)
                {
                    _arrowText.color = Color.Lerp(startTextColor, targetTextColor, t);
                }

                yield return null;
            }

            if (_hoverImage != null)
            {
                Color col = FlashColor;
                col.a = targetAlpha;
                _hoverImage.color = col;
            }

            if (_arrowText != null)
            {
                _arrowText.color = targetTextColor;
            }
        }
    }
}

