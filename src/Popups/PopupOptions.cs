using System;
using UnityEngine;
using ZUI.Core;

namespace ZUI.Popups
{
    /// <summary>
    /// Configuration options for creating custom popups with ZUI.
    /// </summary>
    public class PopupOptions
    {
        /// <summary>
        /// Dialog header / title text. If null, the native dialog title is used.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Dialog message body / description text. If null, the native dialog description is used.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Dialog style: Confirmation (2 buttons), Alert (1 button), AcceptDecline (2 buttons), or Loading (spinner).
        /// </summary>
        public PopupStyle Style { get; set; } = PopupStyle.Confirmation;

        /// <summary>
        /// Optional native game popup enum to use as the base template.
        /// Defaults to SureYouWantToQuitGame for Confirmation, UnknownError for Alert,
        /// InviteGame for AcceptDecline, or Loading for Loading.
        /// </summary>
        public AllPopups? NativePopupType { get; set; }

        /// <summary>
        /// Label for the primary (Yes / Confirm / Accept / OK) button.
        /// </summary>
        public string YesText { get; set; }

        /// <summary>
        /// Label for the secondary (No / Cancel / Decline) button (used in Confirmation and AcceptDecline styles).
        /// </summary>
        public string NoText { get; set; }

        /// <summary>
        /// Action invoked when the primary button is clicked.
        /// </summary>
        public Action OnYes { get; set; }

        /// <summary>
        /// Action invoked when the secondary button or close button is clicked.
        /// </summary>
        public Action OnNo { get; set; }

        /// <summary>
        /// Condition to wait for when using <see cref="PopupStyle.Loading"/>.
        /// When the condition evaluates to true, <see cref="OnFinished"/> is invoked and the popup automatically disposes.
        /// If null, the loading popup remains active until closed manually via <see cref="ZUIPopup.Close(Popup)"/> or popup.Dispose().
        /// </summary>
        public WaitUntil WaitUntil { get; set; }

        /// <summary>
        /// Action invoked when <see cref="WaitUntil"/> completes in a <see cref="PopupStyle.Loading"/> popup.
        /// </summary>
        public Action OnFinished { get; set; }

        /// <summary>
        /// Optional custom color for the dialog title text.
        /// </summary>
        public Color? TitleColor { get; set; }

        /// <summary>
        /// Optional custom color for the dialog message text.
        /// </summary>
        public Color? MessageColor { get; set; }

        /// <summary>
        /// Optional custom text color for the primary button.
        /// </summary>
        public Color? YesTextColor { get; set; }

        /// <summary>
        /// Optional custom text color for the secondary button.
        /// </summary>
        public Color? NoTextColor { get; set; }

        /// <summary>
        /// Optional custom background tint color for the primary button.
        /// </summary>
        public Color? YesButtonColor { get; set; }

        /// <summary>
        /// Optional custom background tint color for the secondary button.
        /// </summary>
        public Color? NoButtonColor { get; set; }

        /// <summary>
        /// Whether the corner 'X' close button is visible.
        /// If null, defaults to true for Confirmation and AcceptDecline, and false for Alert and Loading.
        /// </summary>
        public bool? ShowCloseButton { get; set; }

        /// <summary>
        /// Whether the popup automatically closes when any button is clicked (default: true).
        /// </summary>
        public bool DisposeOnButtonClick { get; set; } = true;

        /// <summary>
        /// Whether pressing ESC dismisses this popup.
        /// Defaults to false (popups cannot be dismissed with ESC by default).
        /// </summary>
        public bool DismissOnEscape { get; set; } = false;

        /// <summary>
        /// For two-choice popups (Confirmation / AcceptDecline), determines which action
        /// is triggered when dismissed via ESC.
        /// If null and DismissOnEscape is true, logs a warning and defaults to PopupChoice.Secondary (No / Cancel / Decline).
        /// </summary>
        public PopupChoice? EscapeChoice { get; set; } = null;

        /// <summary>
        /// When true (default), forces button labels to fit on a single line by disabling word wrapping
        /// and auto-sizing text down to <see cref="ButtonFontSizeMin"/>.
        /// Prevents multi-line overflow and clipping on buttons with long text.
        /// </summary>
        public bool SquishTextToFit { get; set; } = true;

        /// <summary>
        /// Optional minimum font size when <see cref="SquishTextToFit"/> is active (default: 12pt).
        /// </summary>
        public float? ButtonFontSizeMin { get; set; }

        /// <summary>
        /// Optional maximum font size when <see cref="SquishTextToFit"/> is active (default: 33pt).
        /// </summary>
        public float? ButtonFontSizeMax { get; set; }

        /// <summary>
        /// Optional custom width for the dialog buttons in pixels.
        /// </summary>
        public float? ButtonWidth { get; set; }

        /// <summary>
        /// Optional custom height for the dialog buttons in pixels.
        /// </summary>
        public float? ButtonHeight { get; set; }

        #region Hex Helpers

        public string TitleColorHex
        {
            set => TitleColor = UIUtils.ParseColor(value);
        }

        public string MessageColorHex
        {
            set => MessageColor = UIUtils.ParseColor(value);
        }

        public string YesTextColorHex
        {
            set => YesTextColor = UIUtils.ParseColor(value);
        }

        public string NoTextColorHex
        {
            set => NoTextColor = UIUtils.ParseColor(value);
        }

        public string YesButtonColorHex
        {
            set => YesButtonColor = UIUtils.ParseColor(value);
        }

        public string NoButtonColorHex
        {
            set => NoButtonColor = UIUtils.ParseColor(value);
        }

        #endregion
    }
}
