using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Core;

namespace ZUI.Popups
{
    /// <summary>
    /// Static manager for creating, showing, and customizing modal popups in Zort.
    /// Supports 4 distinct styles:
    /// - Style 1 (Confirmation): Recreates native two-button dialogs (e.g. quit game, discard changes).
    /// - Style 2 (Alert): Recreates native single-button dialogs (e.g. error, crash, notice).
    /// - Style 3 (AcceptDecline): Pre-styled two-button dialog (e.g. Terms of Service, EULA, game invite).
    /// - Style 4 (Loading): Animated 360°/s rotating spinner dialog with dynamic progress message and auto-close.
    /// Fully customizable: titles, messages, button texts, text colors, and button background colors.
    /// </summary>
    public static class ZUIPopup
    {
        #region Reflection Caching

        private static readonly FieldInfo ActivePopupsField =
            typeof(PopupManager).GetField("activePopups", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(PopupManager), "activePopups");

        private static readonly FieldInfo TopTextField =
            typeof(Popup).GetField("topText", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "topText");

        private static readonly FieldInfo MiddleTextField =
            typeof(Popup).GetField("middleText", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "middleText");

        private static readonly FieldInfo YesTextField =
            typeof(Popup).GetField("yesText", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "yesText");

        private static readonly FieldInfo NoTextField =
            typeof(Popup).GetField("noText", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "noText");

        private static readonly FieldInfo ButtonHolderField =
            typeof(Popup).GetField("buttonHolder", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "buttonHolder");

        private static readonly FieldInfo YesButtonField =
            typeof(Popup).GetField("yesButton", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "yesButton");

        private static readonly FieldInfo NoButtonField =
            typeof(Popup).GetField("noButton", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "noButton");

        private static readonly FieldInfo CloseButtonField =
            typeof(Popup).GetField("closeButton", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "closeButton");

        private static readonly FieldInfo HaveNoButtonField =
            typeof(Popup).GetField("haveNoButton", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "haveNoButton");

        private static readonly FieldInfo LoadingTransformField =
            typeof(Popup).GetField("loadingTransform", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Popup), "loadingTransform");

        #endregion

        /// <summary>
        /// Number of currently active modal popups.
        /// </summary>
        public static int ActivePopupCount
        {
            get
            {
                if (StaticInstance<InitUI>.Instance == null) return 0;
                var manager = InitUI.PopupManager;
                return manager != null ? manager.GetPopupCount : 0;
            }
        }

        /// <summary>
        /// Frame counter on which an ESC key event was consumed by a popup action.
        /// Used across tab controllers to prevent leaking ESC inputs into drawer back-navigation.
        /// </summary>
        public static int LastEscConsumedFrame { get; internal set; } = -1;

        /// <summary>
        /// Shows a custom modal popup configured with the provided <see cref="PopupOptions"/>.
        /// </summary>
        public static Popup Show(PopupOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (StaticInstance<InitUI>.Instance == null || InitUI.PopupManager == null)
            {
                throw new InvalidOperationException("[ZUI] InitUI.PopupManager is not initialized yet. Popups require the game UI system to be loaded.");
            }

            var manager = InitUI.PopupManager;

            // 1. Select the native template to use as base
            AllPopups baseType;
            if (options.NativePopupType.HasValue)
            {
                baseType = options.NativePopupType.Value;
            }
            else
            {
                switch (options.Style)
                {
                    case PopupStyle.Alert:
                        baseType = AllPopups.UnknownError;
                        break;
                    case PopupStyle.AcceptDecline:
                        baseType = AllPopups.InviteGame;
                        break;
                    case PopupStyle.Loading:
                        baseType = AllPopups.Loading;
                        break;
                    case PopupStyle.Confirmation:
                    default:
                        baseType = AllPopups.SureYouWantToDiscardChanges;
                        break;
                }
            }

            // 2. Spawn popup through native PopupManager
            // This guarantees valid scriptable references, correct parent hierarchy, and background overlay
            Popup popup = manager.ShowPopup(baseType, options.OnYes, options.OnNo, -1f, false);
            if (popup == null)
            {
                throw new InvalidOperationException($"[ZUI] Failed to instantiate native popup '{baseType}'.");
            }

            // Guarantee that popups always render on top of any active panels/drawers
            if (popup.transform.parent != null)
            {
                popup.transform.parent.SetAsLastSibling();
            }

            popup.name = $"ZUI_Popup_{options.Style}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            bool isConfirmation = options.Style == PopupStyle.Confirmation;
            bool isAcceptDecline = options.Style == PopupStyle.AcceptDecline;
            bool isAlert = options.Style == PopupStyle.Alert;
            bool isLoading = options.Style == PopupStyle.Loading;
            bool hasTwoButtons = isConfirmation || isAcceptDecline;

            // Warn if ESC dismissal is enabled on a 2-choice dialog without explicitly defining the escape choice
            if (options.DismissOnEscape && hasTwoButtons && !options.EscapeChoice.HasValue)
            {
                Plugin.Log.LogWarning($"[ZUIPopup] Dialog '{options.Title}' has DismissOnEscape=true but EscapeChoice was not explicitly specified. Defaulting to PopupChoice.Secondary ({options.NoText ?? "Cancel"}).");
            }

            // Attach ZUIPopupBehavior to retain options and configure ESC dismissal
            var behavior = popup.gameObject.AddComponent<ZUIPopupBehavior>();
            behavior.Options = options;

            // 3. Customize Header / Title
            if (!string.IsNullOrEmpty(options.Title))
            {
                var topText = (TMP_Text)TopTextField.GetValue(popup);
                if (topText != null)
                {
                    topText.text = options.Title;
                }
            }

            if (options.TitleColor.HasValue)
            {
                var topText = (TMP_Text)TopTextField.GetValue(popup);
                if (topText != null)
                {
                    topText.color = options.TitleColor.Value;
                }
            }

            // 4. Customize Message Body
            if (!string.IsNullOrEmpty(options.Message))
            {
                var middleText = (TMP_Text)MiddleTextField.GetValue(popup);
                if (middleText != null)
                {
                    middleText.text = options.Message;
                }
            }

            if (options.MessageColor.HasValue)
            {
                var middleText = (TMP_Text)MiddleTextField.GetValue(popup);
                if (middleText != null)
                {
                    middleText.color = options.MessageColor.Value;
                }
            }

            // 5. Configure Buttons & Loading Spinner by Style
            if (isLoading)
            {
                var buttonHolder = (GameObject)ButtonHolderField.GetValue(popup);
                if (buttonHolder != null) buttonHolder.SetActive(false);

                var yesButton = (GameObject)YesButtonField.GetValue(popup);
                if (yesButton != null) yesButton.SetActive(false);

                var noButton = (GameObject)NoButtonField.GetValue(popup);
                if (noButton != null) noButton.SetActive(false);

                var loadingTransform = (RectTransform)LoadingTransformField.GetValue(popup);
                if (loadingTransform != null) loadingTransform.gameObject.SetActive(true);

                bool showClose = options.ShowCloseButton.GetValueOrDefault(false);
                var closeButton = (GameObject)CloseButtonField.GetValue(popup);
                if (closeButton != null)
                {
                    closeButton.SetActive(showClose);
                }
                HaveNoButtonField.SetValue(popup, showClose);

                if (options.WaitUntil != null)
                {
                    popup.SetAsLoadingPopup(options.WaitUntil, options.OnFinished);

                    // SetAsLoadingPopup internally blanks middleText. Re-apply custom message if set:
                    if (!string.IsNullOrEmpty(options.Message))
                    {
                        var middleText = (TMP_Text)MiddleTextField.GetValue(popup);
                        if (middleText != null)
                        {
                            middleText.text = options.Message;
                        }
                    }
                }
            }
            else
            {
                // Ensure spinner is disabled for non-loading dialogs
                var loadingTransform = (RectTransform)LoadingTransformField.GetValue(popup);
                if (loadingTransform != null) loadingTransform.gameObject.SetActive(false);

                var buttonHolder = (GameObject)ButtonHolderField.GetValue(popup);
                if (buttonHolder != null) buttonHolder.SetActive(true);

                // Primary Button (Yes / Accept / OK)
                string yesLabel = options.YesText;
                if (string.IsNullOrEmpty(yesLabel))
                {
                    if (isAcceptDecline) yesLabel = "Accept";
                    else if (isAlert) yesLabel = "OK";
                    else yesLabel = "Yes";
                }

                var yesText = (TMP_Text)YesTextField.GetValue(popup);
                if (yesText != null)
                {
                    yesText.text = yesLabel;
                    if (options.SquishTextToFit)
                    {
                        yesText.textWrappingMode = TextWrappingModes.NoWrap;
                        yesText.enableAutoSizing = true;
                        yesText.fontSizeMin = options.ButtonFontSizeMin ?? 10f;
                        yesText.fontSizeMax = options.ButtonFontSizeMax ?? 33f;
                        yesText.overflowMode = TextOverflowModes.Ellipsis;
                        yesText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Color? yesTextColor = options.YesTextColor;
                if (!yesTextColor.HasValue && isAcceptDecline) yesTextColor = Color.white;
                if (yesTextColor.HasValue && yesText != null)
                {
                    yesText.color = yesTextColor.Value;
                }

                var yesButton = (GameObject)YesButtonField.GetValue(popup);
                if (yesButton != null)
                {
                    yesButton.SetActive(true);

                    if (options.ButtonWidth.HasValue || options.ButtonHeight.HasValue)
                    {
                        var rt = yesButton.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            Vector2 sd = rt.sizeDelta;
                            if (options.ButtonWidth.HasValue) sd.x = options.ButtonWidth.Value;
                            if (options.ButtonHeight.HasValue) sd.y = options.ButtonHeight.Value;
                            rt.sizeDelta = sd;
                        }
                    }

                    Color? yesBtnColor = options.YesButtonColor;
                    if (!yesBtnColor.HasValue && isAcceptDecline)
                    {
                        yesBtnColor = new Color(0.18f, 0.55f, 0.24f, 1f); // Forest Green
                    }

                    if (yesBtnColor.HasValue)
                    {
                        ApplyButtonColor(yesButton, yesBtnColor.Value);
                    }
                }

                // Secondary Button (No / Decline)
                var noButton = (GameObject)NoButtonField.GetValue(popup);
                if (noButton != null)
                {
                    noButton.SetActive(hasTwoButtons);
                    if (hasTwoButtons)
                    {
                        string noLabel = options.NoText;
                        if (string.IsNullOrEmpty(noLabel))
                        {
                            noLabel = isAcceptDecline ? "Decline" : "No";
                        }

                        var noText = (TMP_Text)NoTextField.GetValue(popup);
                        if (noText != null)
                        {
                            noText.text = noLabel;
                            if (options.SquishTextToFit)
                            {
                                noText.textWrappingMode = TextWrappingModes.NoWrap;
                                noText.enableAutoSizing = true;
                                noText.fontSizeMin = options.ButtonFontSizeMin ?? 10f;
                                noText.fontSizeMax = options.ButtonFontSizeMax ?? 33f;
                                noText.overflowMode = TextOverflowModes.Ellipsis;
                                noText.alignment = TextAlignmentOptions.Center;
                            }
                        }

                        Color? noTextColor = options.NoTextColor;
                        if (!noTextColor.HasValue && isAcceptDecline) noTextColor = Color.white;
                        if (noTextColor.HasValue && noText != null)
                        {
                            noText.color = noTextColor.Value;
                        }

                        if (options.ButtonWidth.HasValue || options.ButtonHeight.HasValue)
                        {
                            var rt = noButton.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                Vector2 sd = rt.sizeDelta;
                                if (options.ButtonWidth.HasValue) sd.x = options.ButtonWidth.Value;
                                if (options.ButtonHeight.HasValue) sd.y = options.ButtonHeight.Value;
                                rt.sizeDelta = sd;
                            }
                        }

                        Color? noBtnColor = options.NoButtonColor;
                        if (!noBtnColor.HasValue && isAcceptDecline)
                        {
                            noBtnColor = new Color(0.75f, 0.22f, 0.17f, 1f); // Brick Red
                        }

                        if (noBtnColor.HasValue)
                        {
                            ApplyButtonColor(noButton, noBtnColor.Value);
                        }
                    }
                }

                HaveNoButtonField.SetValue(popup, hasTwoButtons);

                bool showClose = options.ShowCloseButton.GetValueOrDefault(hasTwoButtons);
                var closeButton = (GameObject)CloseButtonField.GetValue(popup);
                if (closeButton != null)
                {
                    closeButton.SetActive(showClose);
                }
            }

            // 6. Behavior & Layout Rebuild
            popup.DisposeOnButtonClick = options.DisposeOnButtonClick;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)popup.transform);

            return popup;
        }

        #region Live Dynamic Updates

        /// <summary>
        /// Dynamically updates the title of an active popup and rebuilds its layout.
        /// </summary>
        public static void UpdateTitle(Popup popup, string title)
        {
            if (popup == null) return;
            var topText = (TMP_Text)TopTextField.GetValue(popup);
            if (topText != null)
            {
                topText.text = title;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)popup.transform);
            }
        }

        /// <summary>
        /// Dynamically updates the message / description text of an active popup and rebuilds its layout.
        /// Especially useful for loading popups to report ongoing progress steps.
        /// </summary>
        public static void UpdateMessage(Popup popup, string message)
        {
            if (popup == null) return;
            var middleText = (TMP_Text)MiddleTextField.GetValue(popup);
            if (middleText != null)
            {
                middleText.text = message;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)popup.transform);
            }
        }

        #endregion

        #region Convenience Helpers: Style 1 (Confirmation)

        /// <summary>
        /// Shows a Style 1 (Confirmation) popup with two buttons (e.g. Yes/No, Quit/Stay).
        /// </summary>
        public static Popup ShowConfirmation(
            string title,
            string message,
            string yesText = "Yes",
            string noText = "No",
            Action onYes = null,
            Action onNo = null,
            Color? titleColor = null,
            Color? messageColor = null,
            Color? yesTextColor = null,
            Color? noTextColor = null,
            Color? yesButtonColor = null,
            Color? noButtonColor = null,
            bool showCloseButton = true,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false,
            PopupChoice? escapeChoice = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Confirmation,
                YesText = yesText,
                NoText = noText,
                OnYes = onYes,
                OnNo = onNo,
                TitleColor = titleColor,
                MessageColor = messageColor,
                YesTextColor = yesTextColor,
                NoTextColor = noTextColor,
                YesButtonColor = yesButtonColor,
                NoButtonColor = noButtonColor,
                ShowCloseButton = showCloseButton,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape,
                EscapeChoice = escapeChoice
            });
        }

        /// <summary>
        /// Shows a Style 1 (Confirmation) popup with hex string color support.
        /// </summary>
        public static Popup ShowConfirmation(
            string title,
            string message,
            string yesText = "Yes",
            string noText = "No",
            Action onYes = null,
            Action onNo = null,
            string titleHex = null,
            string messageHex = null,
            string yesTextHex = null,
            string noTextHex = null,
            string yesButtonHex = null,
            string noButtonHex = null,
            bool showCloseButton = true,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false,
            PopupChoice? escapeChoice = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Confirmation,
                YesText = yesText,
                NoText = noText,
                OnYes = onYes,
                OnNo = onNo,
                TitleColor = !string.IsNullOrEmpty(titleHex) ? UIUtils.ParseColor(titleHex) : (Color?)null,
                MessageColor = !string.IsNullOrEmpty(messageHex) ? UIUtils.ParseColor(messageHex) : (Color?)null,
                YesTextColor = !string.IsNullOrEmpty(yesTextHex) ? UIUtils.ParseColor(yesTextHex) : (Color?)null,
                NoTextColor = !string.IsNullOrEmpty(noTextHex) ? UIUtils.ParseColor(noTextHex) : (Color?)null,
                YesButtonColor = !string.IsNullOrEmpty(yesButtonHex) ? UIUtils.ParseColor(yesButtonHex) : (Color?)null,
                NoButtonColor = !string.IsNullOrEmpty(noButtonHex) ? UIUtils.ParseColor(noButtonHex) : (Color?)null,
                ShowCloseButton = showCloseButton,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape,
                EscapeChoice = escapeChoice
            });
        }

        #endregion

        #region Convenience Helpers: Style 2 (Alert)

        /// <summary>
        /// Shows a Style 2 (Alert / Crash) popup with a single button (e.g. OK, Dismiss, Close).
        /// </summary>
        public static Popup ShowAlert(
            string title,
            string message,
            string buttonText = "OK",
            Action onDismiss = null,
            Color? titleColor = null,
            Color? messageColor = null,
            Color? buttonTextColor = null,
            Color? buttonColor = null,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Alert,
                YesText = buttonText,
                OnYes = onDismiss,
                TitleColor = titleColor,
                MessageColor = messageColor,
                YesTextColor = buttonTextColor,
                YesButtonColor = buttonColor,
                ShowCloseButton = false,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape
            });
        }

        /// <summary>
        /// Shows a Style 2 (Alert / Crash) popup with hex string color support.
        /// </summary>
        public static Popup ShowAlert(
            string title,
            string message,
            string buttonText = "OK",
            Action onDismiss = null,
            string titleHex = null,
            string messageHex = null,
            string buttonTextHex = null,
            string buttonHex = null,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Alert,
                YesText = buttonText,
                OnYes = onDismiss,
                TitleColor = !string.IsNullOrEmpty(titleHex) ? UIUtils.ParseColor(titleHex) : (Color?)null,
                MessageColor = !string.IsNullOrEmpty(messageHex) ? UIUtils.ParseColor(messageHex) : (Color?)null,
                YesTextColor = !string.IsNullOrEmpty(buttonTextHex) ? UIUtils.ParseColor(buttonTextHex) : (Color?)null,
                YesButtonColor = !string.IsNullOrEmpty(buttonHex) ? UIUtils.ParseColor(buttonHex) : (Color?)null,
                ShowCloseButton = false,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape
            });
        }

        /// <summary>
        /// Shows an informational popup (alias for ShowAlert).
        /// </summary>
        public static Popup ShowInfo(
            string title,
            string message,
            string buttonText = "OK",
            Action onDismiss = null,
            string titleHex = null,
            string messageHex = null,
            string buttonTextHex = null,
            string buttonHex = null,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false)
        {
            return ShowAlert(
                title,
                message,
                buttonText,
                onDismiss,
                titleHex,
                messageHex,
                buttonTextHex,
                buttonHex,
                buttonWidth,
                buttonHeight,
                squishTextToFit,
                dismissOnEscape
            );
        }

        #endregion

        #region Convenience Helpers: Style 3 (Accept / Decline)

        /// <summary>
        /// Shows a Style 3 (Accept / Decline) popup (e.g. Terms of Service, EULA, agreements, invitations).
        /// Pre-styled with green Accept and red Decline buttons.
        /// </summary>
        public static Popup ShowAcceptDecline(
            string title,
            string message,
            string acceptText = "Accept",
            string declineText = "Decline",
            Action onAccept = null,
            Action onDecline = null,
            Color? titleColor = null,
            Color? messageColor = null,
            Color? acceptTextColor = null,
            Color? declineTextColor = null,
            Color? acceptButtonColor = null,
            Color? declineButtonColor = null,
            bool showCloseButton = true,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false,
            PopupChoice? escapeChoice = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.AcceptDecline,
                YesText = acceptText,
                NoText = declineText,
                OnYes = onAccept,
                OnNo = onDecline,
                TitleColor = titleColor,
                MessageColor = messageColor,
                YesTextColor = acceptTextColor,
                NoTextColor = declineTextColor,
                YesButtonColor = acceptButtonColor,
                NoButtonColor = declineButtonColor,
                ShowCloseButton = showCloseButton,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape,
                EscapeChoice = escapeChoice
            });
        }

        /// <summary>
        /// Shows a Style 3 (Accept / Decline) popup with hex string color support.
        /// </summary>
        public static Popup ShowAcceptDecline(
            string title,
            string message,
            string acceptText = "Accept",
            string declineText = "Decline",
            Action onAccept = null,
            Action onDecline = null,
            string titleHex = null,
            string messageHex = null,
            string acceptTextHex = null,
            string declineTextHex = null,
            string acceptButtonHex = null,
            string declineButtonHex = null,
            bool showCloseButton = true,
            float? buttonWidth = null,
            float? buttonHeight = null,
            bool squishTextToFit = true,
            bool dismissOnEscape = false,
            PopupChoice? escapeChoice = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.AcceptDecline,
                YesText = acceptText,
                NoText = declineText,
                OnYes = onAccept,
                OnNo = onDecline,
                TitleColor = !string.IsNullOrEmpty(titleHex) ? UIUtils.ParseColor(titleHex) : (Color?)null,
                MessageColor = !string.IsNullOrEmpty(messageHex) ? UIUtils.ParseColor(messageHex) : (Color?)null,
                YesTextColor = !string.IsNullOrEmpty(acceptTextHex) ? UIUtils.ParseColor(acceptTextHex) : (Color?)null,
                NoTextColor = !string.IsNullOrEmpty(declineTextHex) ? UIUtils.ParseColor(declineTextHex) : (Color?)null,
                YesButtonColor = !string.IsNullOrEmpty(acceptButtonHex) ? UIUtils.ParseColor(acceptButtonHex) : (Color?)null,
                NoButtonColor = !string.IsNullOrEmpty(declineButtonHex) ? UIUtils.ParseColor(declineButtonHex) : (Color?)null,
                ShowCloseButton = showCloseButton,
                ButtonWidth = buttonWidth,
                ButtonHeight = buttonHeight,
                SquishTextToFit = squishTextToFit,
                DismissOnEscape = dismissOnEscape,
                EscapeChoice = escapeChoice
            });
        }

        #endregion

        #region Convenience Helpers: Style 4 (Loading)

        /// <summary>
        /// Shows a Style 4 (Loading) popup with an animated 360°/s rotating spinner and optional progress message.
        /// If <paramref name="waitUntil"/> is provided, it automatically closes once the condition is met and invokes <paramref name="onFinished"/>.
        /// If <paramref name="waitUntil"/> is null, the popup stays open until closed manually via <see cref="Close(Popup)"/>.
        /// </summary>
        public static Popup ShowLoading(
            string title,
            string message = null,
            WaitUntil waitUntil = null,
            Action onFinished = null,
            Color? titleColor = null,
            Color? messageColor = null,
            bool showCloseButton = false,
            Action onCancel = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Loading,
                WaitUntil = waitUntil,
                OnFinished = onFinished,
                OnNo = onCancel,
                TitleColor = titleColor,
                MessageColor = messageColor,
                ShowCloseButton = showCloseButton
            });
        }

        /// <summary>
        /// Shows a Style 4 (Loading) popup with hex string color support.
        /// </summary>
        public static Popup ShowLoading(
            string title,
            string message = null,
            WaitUntil waitUntil = null,
            Action onFinished = null,
            string titleHex = null,
            string messageHex = null,
            bool showCloseButton = false,
            Action onCancel = null)
        {
            return Show(new PopupOptions
            {
                Title = title,
                Message = message,
                Style = PopupStyle.Loading,
                WaitUntil = waitUntil,
                OnFinished = onFinished,
                OnNo = onCancel,
                TitleColor = !string.IsNullOrEmpty(titleHex) ? UIUtils.ParseColor(titleHex) : (Color?)null,
                MessageColor = !string.IsNullOrEmpty(messageHex) ? UIUtils.ParseColor(messageHex) : (Color?)null,
                ShowCloseButton = showCloseButton
            });
        }

        #endregion

        #region Closing & Cleanup

        /// <summary>
        /// Closes the specified popup instance.
        /// </summary>
        public static void Close(Popup popup)
        {
            if (popup != null)
            {
                popup.Dispose();
            }
        }

        /// <summary>
        /// Closes all currently active popups.
        /// </summary>
        public static void CloseAll()
        {
            if (StaticInstance<InitUI>.Instance == null) return;
            var manager = InitUI.PopupManager;
            if (manager == null) return;

            var activePopups = (List<Popup>)ActivePopupsField.GetValue(manager);
            if (activePopups == null) return;

            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                if (activePopups[i] != null)
                {
                    activePopups[i].Dispose();
                }
            }
        }

        #endregion

        #region Internal Helpers

        private static void ApplyButtonColor(GameObject buttonObj, Color color)
        {
            if (buttonObj == null) return;

            // Tint non-text graphic components on the button
            var graphics = buttonObj.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic is TMP_Text) continue;
                graphic.color = color;
            }

            // Update button color block for interactive hover and press feedback
            var button = buttonObj.GetComponent<Button>() ?? buttonObj.GetComponentInChildren<Button>();
            if (button != null)
            {
                ColorBlock cb = button.colors;
                cb.normalColor = color;
                cb.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
                cb.pressedColor = Color.Lerp(color, Color.black, 0.25f);
                cb.selectedColor = color;
                button.colors = cb;
            }
        }

        #endregion
    }
}
