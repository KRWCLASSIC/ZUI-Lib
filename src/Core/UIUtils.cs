using TMPro;
using UnityEngine;

namespace ZUI.Core
{
    public static class UIUtils
    {
        /// <summary>
        /// Safely applies the current game font to the given TMP_Text.
        /// If FontManager hasn't been loaded yet by the game, catches the NRE gracefully.
        /// </summary>
        public static void ApplyFontSafe(params TMP_Text[] texts)
        {
            if (texts == null) return;
            try
            {
                FontManager.ApplyGameFontToCurrent(texts);
            }
            catch
            {
                // FontManager not yet loaded by game on early initialization
            }
        }

        /// <summary>
        /// Parses a hex color string (e.g. "#FFCC00", "FF5555", "yellow", "cyan") into a Color.
        /// Returns fallback if parsing fails.
        /// </summary>
        public static Color ParseColor(string colorStr, Color fallback)
        {
            if (string.IsNullOrEmpty(colorStr)) return fallback;
            if (ColorUtility.TryParseHtmlString(colorStr, out var c)) return c;
            if (ColorUtility.TryParseHtmlString("#" + colorStr.TrimStart('#'), out var c2)) return c2;
            return fallback;
        }

        /// <summary>
        /// Parses a hex color string (e.g. "#FFCC00", "FF5555", "yellow", "cyan") into a nullable Color.
        /// Returns null if parsing fails.
        /// </summary>
        public static Color? ParseColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr)) return null;
            if (ColorUtility.TryParseHtmlString(colorStr, out var c)) return c;
            if (ColorUtility.TryParseHtmlString("#" + colorStr.TrimStart('#'), out var c2)) return c2;
            return null;
        }
    }
}
