using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Configures a drawer-bound custom right-side panel (989px wide) on the official Pause Menu or a standalone <see cref="Screens.ZUIScreen"/>.
    /// Automatically hides the vanilla Connected Players list while its owning <see cref="PauseMenuDrawer"/> is active,
    /// and restores the Connected Players list when backing out.
    /// </summary>
    public class PauseMenuRightPanel
    {
        public PauseMenuDrawer Drawer { get; }

        public string HeaderTitle { get; private set; }
        public string HeaderCounter { get; private set; }
        public string HeaderTitleHex { get; private set; }
        public string HeaderCounterHex { get; private set; }

        public float? DefaultItemHeight { get; private set; }
        public GameObject CustomContentRoot { get; private set; }

        internal bool NeedsFullRebuild { get; set; } = true;

        internal bool IsCurrentlyDisplayed =>
            (PauseMenuTabController.Instance != null &&
             PauseMenuTabController.Instance.CurrentMode == PauseMenuMode.Tabbed &&
             PauseMenuTabController.Instance.CurrentDrawer == Drawer &&
             PauseMenuRightPanelController.CurrentActivePanel == this) ||
            Screens.ZUIScreenController.IsDisplayingRightPanel(this);

        private readonly List<PauseMenuRightElement> _elements = new List<PauseMenuRightElement>();
        public IReadOnlyList<PauseMenuRightElement> Elements => _elements.AsReadOnly();

        /// <summary>
        /// Returns true if any header override, elements, or custom content root have been configured on this panel.
        /// </summary>
        public bool HasContent =>
            !string.IsNullOrEmpty(HeaderTitle) ||
            !string.IsNullOrEmpty(HeaderCounter) ||
            _elements.Count > 0 ||
            CustomContentRoot != null;

        public PauseMenuRightPanel(PauseMenuDrawer drawer)
        {
            Drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            HeaderTitle = drawer.Title?.ToUpperInvariant() ?? "CUSTOM PANEL";
            HeaderCounter = string.Empty;
        }

        /// <summary>
        /// Sets the top header title and right-aligned counter badge for this right-side panel.
        /// Updates the header text in-place if the panel is currently open.
        /// </summary>
        public PauseMenuRightPanel SetHeader(
            string title,
            string counter = "",
            string titleHex = null,
            string counterHex = null)
        {
            HeaderTitle = title;
            HeaderCounter = counter ?? string.Empty;
            HeaderTitleHex = titleHex;
            HeaderCounterHex = counterHex;
            if (IsCurrentlyDisplayed)
            {
                if (PauseMenuRightPanelController.CurrentActivePanel == this)
                {
                    PauseMenuRightPanelController.ApplyHeader(this);
                }
                Screens.ZUIScreenController.ApplyRightPanelHeaderIfDisplayed(this);
            }
            return this;
        }

        /// <summary>
        /// Alias for <see cref="SetHeader"/> to set the panel's header title and optional counter text.
        /// </summary>
        public PauseMenuRightPanel SetTitle(
            string title,
            string counter = "",
            string titleHex = null,
            string counterHex = null)
        {
            return SetHeader(title, counter, titleHex, counterHex);
        }

        /// <summary>
        /// Sets a default vertical height (in pixels) applied to newly added elements that don't specify an explicit height.
        /// </summary>
        public PauseMenuRightPanel SetItemHeight(float height)
        {
            DefaultItemHeight = Mathf.Max(24f, height);
            return this;
        }

        /// <summary>
        /// Adds a Style 1 (Player-Bar Card) entry to the right panel.
        /// </summary>
        public PauseMenuRightCard AddCard(
            string id,
            string title,
            string subtitle = null,
            Sprite icon = null,
            Action onClick = null,
            float? height = null,
            string titleHex = null,
            string subtitleHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            _elements.RemoveAll(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            float resolvedHeight = height ?? DefaultItemHeight ?? PauseMenuRightCard.DefaultCardHeight;
            var card = new PauseMenuRightCard(this, id, title, subtitle, icon, onClick, resolvedHeight, titleHex, subtitleHex);
            _elements.Add(card);

            NeedsFullRebuild = true;
            RefreshIfActive();
            return card;
        }

        /// <summary>
        /// Alias for <see cref="AddCard"/> matching the spec's AddEntry terminology.
        /// </summary>
        public PauseMenuRightCard AddEntry(
            string id,
            string title,
            string subtitle = null,
            Sprite icon = null,
            Action onClick = null,
            float? height = null,
            string titleHex = null,
            string subtitleHex = null)
        {
            return AddCard(id, title, subtitle, icon, onClick, height, titleHex, subtitleHex);
        }

        /// <summary>
        /// Adds a Style 2 (Left-Side Pause Button) entry to the right panel.
        /// </summary>
        public PauseMenuRightButton AddButton(
            string id,
            string label,
            Action onClick,
            float? height = null,
            string textHex = null,
            string backgroundHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _elements.RemoveAll(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            float resolvedHeight = height ?? DefaultItemHeight ?? PauseMenuRightButton.DefaultButtonHeight;
            var btn = new PauseMenuRightButton(this, id, label, onClick, resolvedHeight, textHex, backgroundHex);
            _elements.Add(btn);

            NeedsFullRebuild = true;
            RefreshIfActive();
            return btn;
        }

        /// <summary>
        /// Removes an element (card or button) by ID.
        /// </summary>
        public bool RemoveElement(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            int removed = _elements.RemoveAll(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                NeedsFullRebuild = true;
                RefreshIfActive();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all cards and buttons from this right panel.
        /// </summary>
        public PauseMenuRightPanel ClearElements()
        {
            _elements.Clear();
            NeedsFullRebuild = true;
            RefreshIfActive();
            return this;
        }

        /// <summary>
        /// Mounts a custom Unity GameObject hierarchy inside the right panel viewport beneath the header.
        /// Pass null to clear custom content.
        /// </summary>
        public PauseMenuRightPanel SetCustomContent(GameObject customRoot)
        {
            CustomContentRoot = customRoot;
            NeedsFullRebuild = true;
            RefreshIfActive();
            return this;
        }

        internal void RefreshIfActive()
        {
            if (PauseMenuTabController.Instance != null &&
                PauseMenuTabController.Instance.CurrentMode == PauseMenuMode.Tabbed &&
                PauseMenuTabController.Instance.CurrentDrawer == Drawer)
            {
                PauseMenuRightPanelController.SyncWithCurrentDrawer();
            }

            Screens.ZUIScreenController.SyncRightPanelIfDisplayed(this);
        }
    }
}
