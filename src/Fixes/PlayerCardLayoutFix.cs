using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.Fixes
{
    /// <summary>
    /// Fix Module: Pause Menu Player Card Layout & Voice Slider Alignment.
    /// 
    /// WHY:
    /// Vanilla Zort's 'EscapePlayerUI' row hardcodes 'VoiceSide' at a fixed right-anchored position
    /// (anchors=(1, 0.5), pos.x=-460) with a 370px slider inside a ~71px container.
    /// When any mod uses ZUI to add custom action buttons (e.g. Teleport to Player, Ban, Mute)
    /// to the player card, the vanilla hardcoded layout causes the 370px VoiceChatSlider
    /// to overflow and leak out past the right edge of the card, or collide with custom buttons.
    /// Furthermore, naive fixed clamping of PlayerName width causes long usernames to wrap onto
    /// multiple lines even when generous empty space exists on the card.
    /// 
    /// WHEN:
    /// Applied whenever ZUI initializes or rebuilds 'EscapePlayerUI' player cards in the Pause Menu.
    /// 
    /// HOW:
    /// 1. Sizes 'PlayerName' to its natural single-line text width while preserving full-height
    ///    stretch anchors (0,0)-(0,1) with offsetMin=(88, 0) so usernames never wrap or get culled vertically.
    /// 2. Aligns 'VoiceSide' immediately following PlayerName (voiceStartX = 88 + nameWidth + 12px).
    /// 3. Keeps the vanilla slider at its native fixed width in the left/middle zone right next to the player's name.
    /// </summary>
    public static class PlayerCardLayoutFix
    {
        public const float ICON_OFFSET_X = 88f;
        public const float SPACING = 12f;
        public const float FULL_VOICE_SIDE_WIDTH = 416f;

        /// <summary>
        /// Calculates the starting X position for voice controls following the player's name,
        /// while ensuring PlayerName does not wrap and has full vertical stretch room.
        /// </summary>
        public static float CalculateVoiceStartX(TMP_Text nameText, RectTransform nameRect)
        {
            if (nameText != null)
            {
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            float textWidth = nameText != null ? nameText.preferredWidth : 140f;
            float nameWidth = Mathf.Max(60f, textWidth + 8f);

            if (nameRect != null)
            {
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(0f, 1f);
                nameRect.pivot = new Vector2(0f, 0.5f);
                nameRect.offsetMin = new Vector2(ICON_OFFSET_X, 0f);
                nameRect.offsetMax = new Vector2(ICON_OFFSET_X + nameWidth, 0f);
            }

            return ICON_OFFSET_X + nameWidth + SPACING;
        }

        /// <summary>
        /// Applies the alignment fix to position the volume slider directly after the player's name.
        /// </summary>
        public static void Apply(
            RectTransform nameRect,
            TMP_Text nameText,
            RectTransform voiceSideRect,
            GameObject voiceSideObj,
            GameObject voiceChatSliderObj,
            bool isVoiceActive)
        {
            if (voiceSideObj == null) return;

            float voiceStartX = CalculateVoiceStartX(nameText, nameRect);

            if (!isVoiceActive)
            {
                voiceSideObj.SetActive(false);
                return;
            }

            if (voiceChatSliderObj != null)
            {
                voiceChatSliderObj.SetActive(true);
            }

            voiceSideObj.SetActive(true);

            if (voiceSideRect != null)
            {
                voiceSideRect.anchorMin = new Vector2(0f, 0.5f);
                voiceSideRect.anchorMax = new Vector2(0f, 0.5f);
                voiceSideRect.pivot = new Vector2(0f, 0.5f);
                voiceSideRect.sizeDelta = new Vector2(FULL_VOICE_SIDE_WIDTH, 72f);
                voiceSideRect.anchoredPosition = new Vector2(voiceStartX, 0f);
            }
        }
    }
}
