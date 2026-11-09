using System;
using System.Collections.Generic;
using System.Linq;

namespace ZUI.MainMenu
{
    /// <summary>
    /// Represents a submenu page ("Drawer") in the Main Menu button stack.
    /// Can be configured as either Paginated (3 buttons per page + Back on Slot 3 + Page row on Slot 4)
    /// or Compact (up to 4 custom buttons on Slots 0-3 + Back on Slot 4).
    /// </summary>
    public class MainMenuDrawer
    {
        public string Id { get; }
        public string Title { get; set; }
        public bool Paginated { get; set; } = true;
        public string BreadcrumbHex { get; set; }
        public MainMenuDrawer Parent { get; internal set; }

        private readonly List<MainMenuButtonRegistration> _buttons = new List<MainMenuButtonRegistration>();

        public IReadOnlyList<MainMenuButtonRegistration> Buttons => _buttons.AsReadOnly();

        public MainMenuDrawer(string id, string title, bool paginated = true, string breadcrumbHex = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Paginated = paginated;
            BreadcrumbHex = breadcrumbHex;
        }

        /// <summary>
        /// Adds a button to this drawer. In drawers, buttons strictly follow developer insertion order.
        /// </summary>
        public MainMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _buttons.RemoveAll(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (!Paginated && _buttons.Count >= 4)
            {
                Plugin.Log.LogWarning($"[ZUIMainMenu] Drawer '{Id}' has Paginated=false but already contains {_buttons.Count} buttons. Only the first 4 buttons will be displayed.");
            }

            var item = new MainMenuButtonRegistration(id, label, onClick, priority);
            _buttons.Add(item);

            ZUIMainMenu.RefreshUI();
            return item;
        }

        /// <summary>
        /// Registers a nested sub-drawer inside this drawer. Clicking the button opens the child drawer.
        /// </summary>
        public MainMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null)
        {
            var subDrawer = new MainMenuDrawer(id, label, paginated, breadcrumbHex)
            {
                Parent = this
            };

            AddButton(id, label, () =>
            {
                MainMenuTabController.Instance?.PushDrawer(subDrawer);
            }, priority);

            return subDrawer;
        }

        public void AddButton(MainMenuButtonRegistration registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            _buttons.RemoveAll(b => b.Id.Equals(registration.Id, StringComparison.OrdinalIgnoreCase));
            _buttons.Add(registration);

            ZUIMainMenu.RefreshUI();
        }

        public bool RemoveButton(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            int removed = _buttons.RemoveAll(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                ZUIMainMenu.RefreshUI();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all visible buttons in this drawer sorted by priority (stable sort preserving insertion order).
        /// </summary>
        public List<MainMenuButtonRegistration> GetSortedButtons()
        {
            return _buttons
                .Where(b => b.IsVisible)
                .OrderBy(b => b.Priority)
                .ToList();
        }
    }
}
