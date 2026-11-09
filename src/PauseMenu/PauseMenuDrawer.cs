using System;
using System.Collections.Generic;
using System.Linq;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Represents a submenu page ("Drawer") in the Pause Menu button stack.
    /// Can be configured as either Paginated (5 buttons per page + Back on Slot 6 + Page row on Slot 7)
    /// or Compact (up to 6 custom buttons on Slots 1-6 + Back on Slot 7).
    /// </summary>
    public class PauseMenuDrawer
    {
        public string Id { get; }
        public string Title { get; set; }
        public bool Paginated { get; set; } = true;
        public string BreadcrumbHex { get; set; }
        public PauseMenuDrawer Parent { get; internal set; }
        internal Screens.ZUIScreen OwningScreen { get; set; }

        private readonly List<PauseMenuButtonRegistration> _buttons = new List<PauseMenuButtonRegistration>();
        private PauseMenuRightPanel _rightPanel;

        public IReadOnlyList<PauseMenuButtonRegistration> Buttons => _buttons.AsReadOnly();

        /// <summary>
        /// Gets or lazily creates the custom right-side panel bound to this drawer.
        /// While this drawer is open on the Pause Menu, the vanilla Connected Players list is hidden
        /// and replaced by this custom right-side panel.
        /// </summary>
        public PauseMenuRightPanel RightPanel => _rightPanel ??= new PauseMenuRightPanel(this);

        /// <summary>
        /// Whether this drawer has a custom right-side panel configured with content.
        /// </summary>
        public bool HasRightPanel => _rightPanel != null && _rightPanel.HasContent;

        public PauseMenuDrawer(string id, string title, bool paginated = true, string breadcrumbHex = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Paginated = paginated;
            BreadcrumbHex = breadcrumbHex;
        }

        /// <summary>
        /// Adds a button to this drawer. In drawers, buttons strictly follow developer insertion order.
        /// </summary>
        public PauseMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _buttons.RemoveAll(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (!Paginated && _buttons.Count >= 6)
            {
                Plugin.Log.LogWarning($"[ZUIPauseMenu] Drawer '{Id}' has Paginated=false but already contains {_buttons.Count} buttons. Only the first 6 buttons will be displayed.");
            }

            var item = new PauseMenuButtonRegistration(id, label, onClick, priority);
            _buttons.Add(item);

            NotifyDrawerModified();
            return item;
        }

        /// <summary>
        /// Registers a nested sub-drawer inside this drawer. Clicking the button opens the child drawer.
        /// </summary>
        public PauseMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null)
        {
            var subDrawer = new PauseMenuDrawer(id, label, paginated, breadcrumbHex)
            {
                Parent = this,
                OwningScreen = OwningScreen
            };

            AddButton(id, label, () =>
            {
                if (subDrawer.OwningScreen != null)
                {
                    subDrawer.OwningScreen.PushDrawer(subDrawer);
                }
                else
                {
                    PauseMenuTabController.Instance?.PushDrawer(subDrawer);
                }
            }, priority);

            return subDrawer;
        }

        public void AddButton(PauseMenuButtonRegistration registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            _buttons.RemoveAll(b => b.Id.Equals(registration.Id, StringComparison.OrdinalIgnoreCase));
            _buttons.Add(registration);

            NotifyDrawerModified();
        }

        public bool RemoveButton(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            int removed = _buttons.RemoveAll(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                NotifyDrawerModified();
                return true;
            }
            return false;
        }

        private void NotifyDrawerModified()
        {
            if (OwningScreen != null)
            {
                Screens.ZUIScreenController.RefreshIfOpen(OwningScreen);
            }
            else
            {
                ZUIPauseMenu.RefreshUI();
            }
        }

        /// <summary>
        /// Returns all visible buttons in this drawer sorted by priority (stable sort preserving insertion order).
        /// </summary>
        public List<PauseMenuButtonRegistration> GetSortedButtons()
        {
            return _buttons
                .Where(b => b.IsVisible)
                .OrderBy(b => b.Priority)
                .ToList();
        }
    }
}
