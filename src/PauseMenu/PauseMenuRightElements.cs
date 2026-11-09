using System;
using System.Collections.Generic;
using UnityEngine;
using ZUI.Core;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Base class for elements added to a custom right-side panel on the in-game Pause Menu or a standalone <see cref="Screens.ZUIScreen"/>.
    /// </summary>
    public abstract class PauseMenuRightElement
    {
        public string Id { get; }
        public float Height { get; set; }
        internal GameObject SpawnedObject { get; set; }

        protected PauseMenuRightElement(string id, float defaultHeight)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Height = defaultHeight;
        }
    }

    /// <summary>
    /// Represents a small right-aligned icon action button inside a <see cref="PauseMenuRightCard"/>.
    /// </summary>
    public class PauseMenuRightCardAction
    {
        public string Id { get; }
        public Sprite Icon { get; set; }
        public string Tooltip { get; set; }
        public Action OnClick { get; set; }
        public Color? HoverColor { get; set; }
        public Color? NormalColor { get; set; }
        public bool AlwaysVisible { get; set; }
        internal GameObject SpawnedButtonObject { get; set; }

        public PauseMenuRightCardAction(
            string id,
            Sprite icon,
            string tooltip,
            Action onClick,
            Color? hoverColor = null,
            Color? normalColor = null,
            bool alwaysVisible = true)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Icon = icon;
            Tooltip = tooltip;
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            HoverColor = hoverColor;
            NormalColor = normalColor;
            AlwaysVisible = alwaysVisible;
        }
    }

    /// <summary>
    /// Style 1: Player-Bar Card element on the custom right-side panel (cloned from EscapePlayerUI).
    /// Supports an optional left icon, title, subtitle, full-row click action, right-aligned icon buttons,
    /// and custom vertical height (default 72px).
    /// </summary>
    public class PauseMenuRightCard : PauseMenuRightElement
    {
        public const float DefaultCardHeight = 72f;

        private readonly PauseMenuRightPanel _owner;
        private readonly List<PauseMenuRightCardAction> _rightButtons = new List<PauseMenuRightCardAction>();

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string TitleHex { get; set; }
        public string SubtitleHex { get; set; }
        public Sprite Icon { get; set; }
        public Action OnClick { get; set; }

        public IReadOnlyList<PauseMenuRightCardAction> RightButtons => _rightButtons.AsReadOnly();

        public PauseMenuRightCard(
            PauseMenuRightPanel owner,
            string id,
            string title,
            string subtitle = null,
            Sprite icon = null,
            Action onClick = null,
            float height = DefaultCardHeight,
            string titleHex = null,
            string subtitleHex = null)
            : base(id, height > 0f ? height : DefaultCardHeight)
        {
            _owner = owner;
            Title = title ?? string.Empty;
            Subtitle = subtitle;
            Icon = icon;
            OnClick = onClick;
            TitleHex = titleHex;
            SubtitleHex = subtitleHex;
        }

        /// <summary>
        /// Sets the vertical height (in pixels) of this card row and updates it in-place if open.
        /// </summary>
        public PauseMenuRightCard SetHeight(float height)
        {
            Height = Mathf.Max(24f, height);
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyCardVisuals(this, SpawnedObject);
                PauseMenuRightPanelController.RebuildLayout();
            }
            return this;
        }

        /// <summary>
        /// Updates the title and optional subtitle of this card in-place without rebuilding the panel.
        /// </summary>
        public PauseMenuRightCard SetText(string title, string subtitle = null)
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle;
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyCardVisuals(this, SpawnedObject);
            }
            return this;
        }

        /// <summary>
        /// Updates the left-side icon sprite of this card in-place without rebuilding the panel.
        /// </summary>
        public PauseMenuRightCard SetIcon(Sprite icon)
        {
            Icon = icon;
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyCardVisuals(this, SpawnedObject);
            }
            return this;
        }

        /// <summary>
        /// Adds a right-aligned icon action button (KickButton style) to this card row.
        /// </summary>
        public PauseMenuRightCard AddRightButton(
            string id,
            Sprite icon,
            string tooltip,
            Action onClick,
            string hoverHex = "#4FC3F7",
            string normalHex = null,
            bool alwaysVisible = true)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _rightButtons.RemoveAll(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            var action = new PauseMenuRightCardAction(
                id,
                icon,
                tooltip,
                onClick,
                UIUtils.ParseColor(hoverHex),
                UIUtils.ParseColor(normalHex),
                alwaysVisible
            );
            _rightButtons.Add(action);
            if (_owner != null)
            {
                _owner.NeedsFullRebuild = true;
                _owner.RefreshIfActive();
            }
            return this;
        }

        /// <summary>
        /// Updates the base/normal and hover tint colors of a right-aligned card button in-place without rebuilding the panel.
        /// </summary>
        public PauseMenuRightCard SetRightButtonColors(string id, string normalHex, string hoverHex = null)
        {
            if (string.IsNullOrEmpty(id)) return this;

            for (int i = 0; i < _rightButtons.Count; i++)
            {
                var action = _rightButtons[i];
                if (action.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    action.NormalColor = UIUtils.ParseColor(normalHex);
                    if (hoverHex != null)
                    {
                        action.HoverColor = UIUtils.ParseColor(hoverHex);
                    }
                    if (_owner != null && _owner.IsCurrentlyDisplayed && action.SpawnedButtonObject != null)
                    {
                        PauseMenuRightPanelController.ApplyRightButtonVisuals(action, action.SpawnedButtonObject);
                    }
                    break;
                }
            }
            return this;
        }

        /// <summary>
        /// Updates the icon sprite of a right-aligned card button in-place without rebuilding the panel.
        /// </summary>
        public PauseMenuRightCard SetRightButtonIcon(string id, Sprite icon)
        {
            if (string.IsNullOrEmpty(id)) return this;

            for (int i = 0; i < _rightButtons.Count; i++)
            {
                var action = _rightButtons[i];
                if (action.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    action.Icon = icon;
                    if (_owner != null && _owner.IsCurrentlyDisplayed && action.SpawnedButtonObject != null)
                    {
                        PauseMenuRightPanelController.ApplyRightButtonVisuals(action, action.SpawnedButtonObject);
                    }
                    break;
                }
            }
            return this;
        }
    }

    /// <summary>
    /// Style 2: Left-Side Button element on the custom right-side panel (cloned from native Pause Menu buttons).
    /// Supports native uppercase typography, hover animation, optional background tint, and custom vertical height (default 56px).
    /// </summary>
    public class PauseMenuRightButton : PauseMenuRightElement
    {
        public const float DefaultButtonHeight = 56f;

        private readonly PauseMenuRightPanel _owner;

        public string Label { get; set; }
        public Action OnClick { get; set; }
        public string TextHex { get; set; }
        public string BackgroundHex { get; set; }
        public bool Interactable { get; set; } = true;

        public PauseMenuRightButton(
            PauseMenuRightPanel owner,
            string id,
            string label,
            Action onClick,
            float height = DefaultButtonHeight,
            string textHex = null,
            string backgroundHex = null)
            : base(id, height > 0f ? height : DefaultButtonHeight)
        {
            _owner = owner;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            TextHex = textHex;
            BackgroundHex = backgroundHex;
        }

        /// <summary>
        /// Sets the vertical height (in pixels) of this button row and updates it in-place if open.
        /// </summary>
        public PauseMenuRightButton SetHeight(float height)
        {
            Height = Mathf.Max(24f, height);
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyButtonVisuals(this, SpawnedObject);
                PauseMenuRightPanelController.RebuildLayout();
            }
            return this;
        }

        /// <summary>
        /// Updates the button label text in-place without replaying button animations.
        /// </summary>
        public PauseMenuRightButton SetLabel(string label)
        {
            Label = label ?? string.Empty;
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyButtonVisuals(this, SpawnedObject);
            }
            return this;
        }

        /// <summary>
        /// Sets whether the button is interactable in-place without replaying button animations.
        /// </summary>
        public PauseMenuRightButton SetInteractable(bool interactable)
        {
            Interactable = interactable;
            if (_owner != null && _owner.IsCurrentlyDisplayed && SpawnedObject != null)
            {
                PauseMenuRightPanelController.ApplyButtonVisuals(this, SpawnedObject);
            }
            return this;
        }
    }
}

