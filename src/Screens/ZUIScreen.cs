using System;
using System.Collections.Generic;
using UnityEngine;
using ZUI.PauseMenu;

namespace ZUI.Screens
{
    /// <summary>
    /// Phase 5: Represents a standalone full-screen custom split interface cloned from <c>pausePanel</c>
    /// under <c>InGameUICanvas</c>, completely independent of the vanilla pause menu state.
    /// Provides a customizable left navigation column (supporting nested drawers, 5-slot pagination, and breadcrumbs)
    /// and a 989px right viewport column (supporting cards, buttons, and raw GameObject viewports).
    /// </summary>
    public class ZUIScreen
    {
        private static readonly Dictionary<string, ZUIScreen> _registry = new Dictionary<string, ZUIScreen>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, ZUIScreen> Registry => _registry;
        public static ZUIScreen CurrentOpenScreen => ZUIScreenController.CurrentOpenScreen;
        public static bool IsAnyScreenOpen => ZUIScreenController.CurrentOpenScreen != null;

        public string Id { get; }
        public string Title { get; set; }
        public string TitleHex { get; set; }

        /// <summary>
        /// Whether to display the top-left header text above the left button stack. Defaults to true.
        /// </summary>
        public bool ShowLeftHeader { get; set; } = true;

        /// <summary>
        /// Label shown on the bottom return button when at the root drawer of this screen. Defaults to "CLOSE".
        /// </summary>
        public string CloseButtonLabel { get; set; } = "CLOSE";

        /// <summary>
        /// The root navigation drawer for the left button column.
        /// </summary>
        public PauseMenuDrawer RootDrawer { get; }

        /// <summary>
        /// The default right-side panel displayed when at the root drawer (or when a sub-drawer does not define its own RightPanel).
        /// </summary>
        public PauseMenuRightPanel RightPanel => RootDrawer.RightPanel;

        public Action OnOpened { get; set; }
        public Action OnClosed { get; set; }

        public bool IsOpen => CurrentOpenScreen == this;

        internal ZUIScreenView ViewInstance { get; set; }

        public ZUIScreen(string id, string title, bool paginated = true, string titleHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            Id = id;
            Title = title ?? string.Empty;
            TitleHex = titleHex;

            RootDrawer = new PauseMenuDrawer($"zui_screen_root_{id}", Title, paginated, titleHex)
            {
                OwningScreen = this
            };

            // Default the right panel header to match the screen title
            RootDrawer.RightPanel.SetHeader(Title.ToUpperInvariant(), string.Empty, titleHex);
        }

        #region Static Registry & Lifecycle

        public static void Initialize(MonoBehaviour host)
        {
            ZUIScreenController.EnsureInitialized(host);
        }

        /// <summary>
        /// Registers or retrieves a standalone split screen by ID.
        /// </summary>
        public static ZUIScreen Register(string id, string title, bool paginated = true, string titleHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            if (_registry.TryGetValue(id, out var existing))
            {
                existing.Title = title ?? string.Empty;
                existing.TitleHex = titleHex;
                existing.RootDrawer.Title = existing.Title;
                existing.RootDrawer.Paginated = paginated;
                existing.RootDrawer.BreadcrumbHex = titleHex;
                return existing;
            }

            var screen = new ZUIScreen(id, title, paginated, titleHex);
            _registry[id] = screen;
            return screen;
        }

        public static ZUIScreen Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var screen);
            return screen;
        }

        public static bool Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_registry.TryGetValue(id, out var screen))
            {
                if (CurrentOpenScreen == screen)
                {
                    screen.Close();
                }
                if (screen.ViewInstance != null)
                {
                    screen.ViewInstance.DestroyView();
                    screen.ViewInstance = null;
                }
                _registry.Remove(id);
                return true;
            }
            return false;
        }

        public static void Open(string id)
        {
            var screen = Get(id);
            if (screen == null)
            {
                Plugin.Log.LogWarning($"[ZUIScreen] Screen with ID '{id}' is not registered.");
                return;
            }
            screen.Open();
        }

        public static void Toggle(string id)
        {
            var screen = Get(id);
            if (screen == null)
            {
                Plugin.Log.LogWarning($"[ZUIScreen] Screen with ID '{id}' is not registered.");
                return;
            }
            screen.Toggle();
        }

        public static void CloseActive()
        {
            if (CurrentOpenScreen != null)
            {
                ZUIScreenController.CloseScreen(CurrentOpenScreen);
            }
        }

        public static void Shutdown()
        {
            CloseActive();
            foreach (var screen in _registry.Values)
            {
                if (screen.ViewInstance != null)
                {
                    screen.ViewInstance.DestroyView();
                    screen.ViewInstance = null;
                }
            }
            _registry.Clear();
        }

        #endregion

        #region Instance Navigation & Controls

        public void Open()
        {
            ZUIScreenController.OpenScreen(this);
        }

        public void Close()
        {
            if (CurrentOpenScreen == this)
            {
                ZUIScreenController.CloseScreen(this);
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void PushDrawer(PauseMenuDrawer drawer)
        {
            if (drawer == null) return;
            drawer.OwningScreen = this;
            ViewInstance?.PushDrawer(drawer);
        }

        public void PopDrawer()
        {
            ViewInstance?.PopDrawer();
        }

        /// <summary>
        /// Updates the left-column header title and optional hex color.
        /// </summary>
        public ZUIScreen SetTitle(string title, string titleHex = null)
        {
            Title = title ?? string.Empty;
            if (titleHex != null) TitleHex = titleHex;
            RootDrawer.Title = Title;
            if (titleHex != null) RootDrawer.BreadcrumbHex = titleHex;
            ZUIScreenController.RefreshIfOpen(this);
            return this;
        }

        /// <summary>
        /// Adds a button to the left navigation column of this screen's root drawer.
        /// </summary>
        public PauseMenuButtonRegistration AddButton(string id, string label, Action onClick, int priority = 0)
        {
            return RootDrawer.AddButton(id, label, onClick, priority);
        }

        /// <summary>
        /// Registers a nested submenu drawer in the left navigation column of this screen.
        /// </summary>
        public PauseMenuDrawer AddSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null)
        {
            return RootDrawer.AddSubmenu(id, label, paginated, priority, breadcrumbHex);
        }

        /// <summary>
        /// Removes a button from the left navigation column by ID.
        /// </summary>
        public bool RemoveButton(string id)
        {
            return RootDrawer.RemoveButton(id);
        }

        #endregion

        #region Right Viewport Column Shortcuts

        /// <summary>
        /// Sets the top header title and counter badge on the right viewport column.
        /// </summary>
        public ZUIScreen SetRightHeader(string title, string counter = "", string titleHex = null, string counterHex = null)
        {
            RightPanel.SetHeader(title, counter, titleHex, counterHex);
            return this;
        }

        /// <summary>
        /// Mounts a custom GameObject viewport into the right 989px column beneath the header.
        /// </summary>
        public ZUIScreen SetContent(GameObject customRoot)
        {
            RightPanel.SetCustomContent(customRoot);
            return this;
        }

        /// <summary>
        /// Adds a Style 1 (Player-Bar Card) entry to the screen's right column.
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
            return RightPanel.AddCard(id, title, subtitle, icon, onClick, height, titleHex, subtitleHex);
        }

        /// <summary>
        /// Alias for <see cref="AddCard"/>.
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
            return RightPanel.AddCard(id, title, subtitle, icon, onClick, height, titleHex, subtitleHex);
        }

        /// <summary>
        /// Adds a Style 2 (Official Button Style) entry to the screen's right column.
        /// </summary>
        public PauseMenuRightButton AddRightButton(
            string id,
            string label,
            Action onClick,
            float? height = null,
            string textHex = null,
            string backgroundHex = null)
        {
            return RightPanel.AddButton(id, label, onClick, height, textHex, backgroundHex);
        }

        #endregion
    }
}

