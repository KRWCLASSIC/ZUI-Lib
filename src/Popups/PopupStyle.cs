namespace ZUI.Popups
{
    /// <summary>
    /// Supported styles for modal popups in ZUI.
    /// </summary>
    public enum PopupStyle
    {
        /// <summary>
        /// Style 1: Two-button confirmation dialog (e.g. Yes/No, Quit/Stay, Confirm/Cancel).
        /// Recreates the native quit/action confirmation dialog.
        /// </summary>
        Confirmation,

        /// <summary>
        /// Style 2: Single-button alert dialog (e.g. OK, Dismiss, Close).
        /// Recreates the native crash/error/notice dialog.
        /// </summary>
        Alert,

        /// <summary>
        /// Style 3: Accept / Decline dialog (e.g. Terms of Service, EULA, agreements, invitations).
        /// Pre-styled with green Accept and red/gray Decline buttons.
        /// </summary>
        AcceptDecline,

        /// <summary>
        /// Style 4: Loading dialog with an animated rotating spinner.
        /// Buttons are hidden while the spinner rotates. Can auto-close via WaitUntil or manual Close().
        /// </summary>
        Loading
    }
}
