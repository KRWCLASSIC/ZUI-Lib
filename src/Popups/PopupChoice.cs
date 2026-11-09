namespace ZUI.Popups
{
    /// <summary>
    /// Represents the button choices available in modal popups.
    /// Used to specify which action is invoked when a popup is dismissed via the ESC key.
    /// </summary>
    public enum PopupChoice
    {
        /// <summary>
        /// Primary button action (Yes / OK / Accept / Confirm).
        /// </summary>
        Primary,

        /// <summary>
        /// Secondary button action (No / Cancel / Decline).
        /// </summary>
        Secondary
    }
}
