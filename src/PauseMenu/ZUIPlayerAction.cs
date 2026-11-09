using System;
using UnityEngine;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Configuration for a custom action button displayed on connected player rows in the Pause Menu.
    /// </summary>
    public class ZUIPlayerAction
    {
        public string Id { get; }
        public Sprite Icon { get; set; }
        public string Tooltip { get; set; }
        public Action<PlayerController> OnClick { get; set; }
        public Color? HoverColor { get; set; }
        public Color? NormalColor { get; set; }
        public bool AlwaysVisible { get; set; } = false;
        public Func<PlayerController, bool> VisibilityPredicate { get; set; }

        public ZUIPlayerAction(
            string id,
            Sprite icon,
            string tooltip,
            Action<PlayerController> onClick,
            Color? hoverColor = null,
            Color? normalColor = null,
            bool alwaysVisible = false,
            Func<PlayerController, bool> visibilityPredicate = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Icon = icon;
            Tooltip = tooltip;
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            HoverColor = hoverColor;
            NormalColor = normalColor;
            AlwaysVisible = alwaysVisible;
            VisibilityPredicate = visibilityPredicate;
        }
    }
}
