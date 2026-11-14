using ZUI.Panels;
using ZUI.PauseMenu;
using ZUI.Screens;

namespace ZUI
{
    /// <summary>
    /// Global utility shortcuts for managing ZUI and game UI layers.
    /// </summary>
    public static class ZUIAPI
    {
        /// <summary>
        /// Whether the in-game pause menu is currently open.
        /// </summary>
        public static bool IsPauseMenuOpen => ZUIPauseMenu.IsOpen;

        /// <summary>
        /// Whether a custom ZUIPanel modal is currently open.
        /// </summary>
        public static bool IsPanelOpen => ZUIPanel.CurrentOpenPanel != null;

        /// <summary>
        /// Whether a standalone ZUIScreen split screen is currently open.
        /// </summary>
        public static bool IsScreenOpen => ZUIScreen.CurrentOpenScreen != null;

        /// <summary>
        /// Closes any open modal settings panel, standalone split screen, and the in-game pause menu,
        /// restoring normal gameplay.
        /// </summary>
        public static void CloseAll()
        {
            ZUIPanel.CloseActive();
            ZUIScreen.CloseActive();
            ZUIPauseMenu.Close();
        }

        /// <summary>
        /// Closes the in-game pause menu.
        /// </summary>
        public static void ClosePauseMenu()
        {
            ZUIPauseMenu.Close();
        }

        /// <summary>
        /// Closes the active custom settings panel (if any).
        /// </summary>
        public static void CloseActivePanel()
        {
            ZUIPanel.CloseActive();
        }

        /// <summary>
        /// Closes the active standalone split screen (if any).
        /// </summary>
        public static void CloseActiveScreen()
        {
            ZUIScreen.CloseActive();
        }

        /// <summary>
        /// Built-in procedural vector circle indicator icon for equip/radio buttons.
        /// </summary>
        public static UnityEngine.Sprite DefaultCircleIcon => ZUI.Core.ZUIDefaultIcons.CircleIndicator;
    }
}
