using TMPro;
using UnityEngine;
using ZUI.Core;

namespace ZUI.Settings
{
    /// <summary>
    /// Specialized TextSettingItem preconfigured for section headers and category titles.
    /// Uses native game font by default (avoiding TMP font fallback) and warm accent coloring.
    /// </summary>
    public class HeaderSettingItem : TextSettingItem
    {
        // Default warm golden accent for category headers (#F2D973)
        public static readonly Color DefaultHeaderColor = new Color(0.95f, 0.85f, 0.45f, 1f);

        public HeaderSettingItem(
            string id,
            string title,
            Color? color = null,
            TMP_FontAsset font = null,
            float? fontSize = null,
            FontStyles? fontStyle = null,
            float? height = null)
            : base(
                id,
                title,
                color: color ?? DefaultHeaderColor,
                font: font,
                fontSize: fontSize,
                fontStyle: fontStyle ?? FontStyles.Normal,
                alignment: TextAlignmentOptions.Center,
                height: height ?? 52f)
        {
        }

        public HeaderSettingItem(
            string id,
            string title,
            string hexColor)
            : base(
                id,
                title,
                color: UIUtils.ParseColor(hexColor, DefaultHeaderColor),
                font: null,
                fontSize: null,
                fontStyle: FontStyles.Normal,
                alignment: TextAlignmentOptions.Center,
                height: 52f)
        {
        }
    }
}
