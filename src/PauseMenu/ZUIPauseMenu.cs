using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZUI.Core;

namespace ZUI.PauseMenu
{
    /// <summary>
    /// Public API for registering and controlling custom buttons and drawers in the Zort in-game Pause Menu.
    /// </summary>
    public static class ZUIPauseMenu
    {
        private static readonly List<PauseMenuButtonRegistration> _registrations = new List<PauseMenuButtonRegistration>();
        private static readonly List<ZUIPlayerAction> _playerActions = new List<ZUIPlayerAction>();
        private static string _customHeaderTitle;
        private static string _customHeaderCounter;
        private static string _customHeaderTitleHex;
        private static string _customHeaderCounterHex;
        private static string _cachedVanillaTitle;
        private static string _cachedVanillaCounter;

        private static MonoBehaviour _coroutineHost;
        private static bool _initialized = false;

        public static IReadOnlyList<PauseMenuButtonRegistration> Registrations => _registrations.AsReadOnly();
        public static IReadOnlyList<ZUIPlayerAction> PlayerActions => _playerActions.AsReadOnly();
        public static Func<PlayerController, Sprite> PlayerIconResolver { get; set; }

        public static PauseMenuDrawer RootDrawer { get; } = new PauseMenuDrawer("zui_root_pause_more", "MORE", paginated: true);

        /// <summary>
        /// Whether the native in-game pause menu is currently open.
        /// </summary>
        public static bool IsOpen => InGameUI.IsGamePaused;

        /// <summary>
        /// Closes the in-game pause menu and returns to gameplay.
        /// </summary>
        public static void Close()
        {
            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI != null)
            {
                inGameUI.SetPauseStatus(false);
            }
        }

        /// <summary>
        /// Opens the in-game pause menu.
        /// </summary>
        public static void Open()
        {
            var inGameUI = StaticInstance<InGameUI>.Instance;
            if (inGameUI != null)
            {
                inGameUI.SetPauseStatus(true);
            }
        }

        public static void Initialize(MonoBehaviour host)
        {
            if (_initialized) return;
            _initialized = true;
            _coroutineHost = host;

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(InstallWhenReadyRoutine());
            }
        }

        #region Registration API

        public static PauseMenuButtonRegistration RegisterButton(string id, string label, Action onClick, int priority = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _registrations.RemoveAll(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            var item = new PauseMenuButtonRegistration(id, label, onClick, priority);
            _registrations.Add(item);

            RefreshUI();
            return item;
        }

        /// <summary>
        /// Registers a custom submenu ("Drawer") on the Pause Menu under the MORE section.
        /// Clicking the registered button smoothly transitions into the drawer.
        /// </summary>
        public static PauseMenuDrawer RegisterSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));

            var drawer = new PauseMenuDrawer(id, label, paginated, breadcrumbHex)
            {
                Parent = RootDrawer
            };

            RegisterButton(id, label, () =>
            {
                PauseMenuTabController.Instance?.PushDrawer(drawer);
            }, priority);

            return drawer;
        }

        public static void RegisterButton(PauseMenuButtonRegistration registration)
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));

            _registrations.RemoveAll(r => r.Id.Equals(registration.Id, StringComparison.OrdinalIgnoreCase));
            _registrations.Add(registration);

            RefreshUI();
        }

        public static bool UnregisterButton(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            int removed = _registrations.RemoveAll(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                RefreshUI();
                return true;
            }
            return false;
        }

        public static List<PauseMenuButtonRegistration> GetSortedRegistrations()
        {
            return _registrations
                .Where(r => r.IsVisible)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void RefreshUI()
        {
            if (PauseMenuTabController.Instance != null && PauseMenuTabController.Instance.CurrentMode == PauseMenuMode.Tabbed)
            {
                PauseMenuTabController.Instance.RefreshTabbedView();
            }
        }

        public static void SetMode(PauseMenuMode mode)
        {
            if (PauseMenuTabController.Instance != null)
            {
                PauseMenuTabController.Instance.SetMode(mode);
            }
        }

        public static void PushDrawer(PauseMenuDrawer drawer)
        {
            if (PauseMenuTabController.Instance != null)
            {
                PauseMenuTabController.Instance.PushDrawer(drawer);
            }
        }

        public static void PopDrawer()
        {
            if (PauseMenuTabController.Instance != null)
            {
                PauseMenuTabController.Instance.PopDrawer();
            }
        }

        public static PauseMenuDrawer CurrentDrawer => PauseMenuTabController.Instance?.CurrentDrawer;

        #endregion

        #region Player Row Action Buttons API

        /// <summary>
        /// Registers a custom action button displayed on connected player cards in the Pause Menu.
        /// Appears alongside the vanilla Kick button.
        /// </summary>
        /// <param name="id">Unique identifier for the action.</param>
        /// <param name="icon">Sprite used for the button icon (supports custom PNGs via ZUIImage.Load).</param>
        /// <param name="tooltip">Optional tooltip or display label.</param>
        /// <param name="onClick">Callback invoked when clicked, receiving the target PlayerController.</param>
        /// <param name="hoverHex">Hover tint color in hex (e.g. "#4FC3F7").</param>
        /// <param name="normalHex">Base tint color in hex (default: white).</param>
        /// <param name="alwaysVisible">Whether to keep button visible at all times instead of hover-only.</param>
        /// <param name="visibilityPredicate">Optional predicate controlling if the button should show for a specific player.</param>
        public static ZUIPlayerAction AddPlayerActionButton(
            string id,
            Sprite icon,
            string tooltip,
            Action<PlayerController> onClick,
            string hoverHex = null,
            string normalHex = null,
            bool alwaysVisible = false,
            Func<PlayerController, bool> visibilityPredicate = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            _playerActions.RemoveAll(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            Color? hoverColor = UIUtils.ParseColor(hoverHex);
            Color? normalColor = UIUtils.ParseColor(normalHex);

            var action = new ZUIPlayerAction(id, icon, tooltip, onClick, hoverColor, normalColor, alwaysVisible, visibilityPredicate);
            _playerActions.Add(action);

            RefreshPlayerCards();
            return action;
        }

        /// <summary>
        /// Removes a registered player action button by ID.
        /// </summary>
        public static bool RemovePlayerActionButton(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            int removed = _playerActions.RemoveAll(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                RefreshPlayerCards();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Event invoked whenever player cards are refreshed/rebuilt across the pause menu.
        /// </summary>
        public static event Action OnPlayerCardsRefreshed;

        /// <summary>
        /// Rebuilds custom action buttons on all active player cards currently visible in the Pause Menu.
        /// </summary>
        public static void RefreshPlayerCards()
        {
            var existingUIs = Resources.FindObjectsOfTypeAll<EscapePlayerUI>();
            for (int i = 0; i < existingUIs.Length; i++)
            {
                var ui = existingUIs[i];
                if (ui != null && ui.gameObject.scene.IsValid())
                {
                    var ext = ui.GetComponent<ZUIPlayerCardExtension>();
                    if (ext != null)
                    {
                        ext.RebuildButtons();
                    }
                }
            }

            OnPlayerCardsRefreshed?.Invoke();
        }

        #endregion

        #region Players Panel Header API

        /// <summary>
        /// Customizes the Connected Players panel header title and counter indicator.
        /// Pass null to keep the vanilla title or counter.
        /// </summary>
        public static void SetPlayersHeader(string title = null, string counter = null, string titleHex = null, string counterHex = null)
        {
            _customHeaderTitle = title;
            _customHeaderCounter = counter;
            _customHeaderTitleHex = titleHex;
            _customHeaderCounterHex = counterHex;
            ApplyPlayersHeader();
        }

        /// <summary>
        /// Restores the vanilla "CONNECTED PLAYERS" header and count indicator.
        /// </summary>
        public static void ResetPlayersHeader()
        {
            _customHeaderTitle = null;
            _customHeaderCounter = null;
            _customHeaderTitleHex = null;
            _customHeaderCounterHex = null;
            ApplyPlayersHeader();
        }

        /// <summary>
        /// Applies the custom header to the Pause Menu right panel if currently active.
        /// </summary>
        public static void ApplyPlayersHeader()
        {
            try
            {
                GameObject canvasObj = GameObject.Find("InGameUICanvas");
                if (canvasObj == null) return;

                Transform top = canvasObj.transform.Find("pausePanel/Players/Players/Top");
                if (top == null) return;

                var titleText = top.Find("ConnectedPlayers")?.GetComponent<TMP_Text>();
                var localizedTitle = top.Find("ConnectedPlayers")?.GetComponent<LocalizedTextUI>();
                var counterText = top.Find("ConnectedPlayersCountIndicatorText")?.GetComponent<TMP_Text>();

                if (titleText != null)
                {
                    if (_customHeaderTitle != null)
                    {
                        if (localizedTitle == null || localizedTitle.enabled)
                        {
                            _cachedVanillaTitle = titleText.text;
                        }
                        if (localizedTitle != null) localizedTitle.enabled = false;
                        if (!string.IsNullOrEmpty(_customHeaderTitleHex))
                        {
                            titleText.text = $"<color=#{_customHeaderTitleHex.TrimStart('#')}>{_customHeaderTitle}</color>";
                        }
                        else
                        {
                            titleText.text = _customHeaderTitle;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(_cachedVanillaTitle))
                        {
                            titleText.text = _cachedVanillaTitle;
                        }
                        if (localizedTitle != null)
                        {
                            localizedTitle.enabled = true;
                        }
                    }
                }

                if (counterText != null)
                {
                    if (_customHeaderCounter != null)
                    {
                        if (string.IsNullOrEmpty(_cachedVanillaCounter) || counterText.text.StartsWith("(", StringComparison.Ordinal))
                        {
                            _cachedVanillaCounter = counterText.text;
                        }
                        if (!string.IsNullOrEmpty(_customHeaderCounterHex))
                        {
                            counterText.text = $"<color=#{_customHeaderCounterHex.TrimStart('#')}>{_customHeaderCounter}</color>";
                        }
                        else
                        {
                            counterText.text = _customHeaderCounter;
                        }
                    }
                    else
                    {
                        int playerCount = PlayerController.AllPlayers != null && PlayerController.AllPlayers.Count > 0
                            ? PlayerController.AllPlayers.Count
                            : 1;
                        counterText.text = !string.IsNullOrEmpty(_cachedVanillaCounter) && _cachedVanillaCounter.StartsWith("(", StringComparison.Ordinal)
                            ? $"({playerCount}/4)"
                            : $"({playerCount}/4)";
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[ZUIPauseMenu] Error applying players header: {ex.Message}");
            }
        }

        #endregion

        #region Installation and Lifecycle

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(InstallWhenReadyRoutine());
            }
        }

        public static IEnumerator InstallWhenReadyRoutine()
        {
            for (int i = 0; i < 120; i++)
            {
                if (TryInstall())
                {
                    yield break;
                }
                yield return null;
            }
        }

        public static void OnPauseStatusChanged(bool state)
        {
            if (state)
            {
                TryInstall();
                RefreshUI();
                ApplyPlayersHeader();
                RefreshPlayerCards();
            }
            else
            {
                PauseMenuTabController.Instance?.OnPauseMenuClosed();
            }
        }

        public static bool TryInstall()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "SplashScreen" || activeScene.name == "MainMenu" || string.IsNullOrEmpty(activeScene.name))
            {
                return false;
            }

            Transform buttonsContainer = FindButtonsContainer();
            if (buttonsContainer == null) return false;

            var existingController = buttonsContainer.GetComponent<PauseMenuTabController>();
            if (existingController != null) return true;

            // Find vanilla elements
            Transform headerTransform = buttonsContainer.Find("Header");
            Transform continueTransform = buttonsContainer.Find("ContinueButton");
            Transform inviteTransform = buttonsContainer.Find("InviteButton");
            Transform settingsTransform = buttonsContainer.Find("SettingsButton");
            Transform returnToLobbyTransform = buttonsContainer.Find("ReturnToLobby") ?? buttonsContainer.Find("ReturnToLobbyButton");
            Transform returnToMainMenuTransform = buttonsContainer.Find("ReturnToMainMenu");
            Transform quitTransform = buttonsContainer.Find("QuitButton");

            if (continueTransform == null || settingsTransform == null || quitTransform == null)
            {
                return false;
            }

            var controller = buttonsContainer.gameObject.AddComponent<PauseMenuTabController>();
            controller.Initialize(
                headerTransform != null ? headerTransform.gameObject : null,
                continueTransform.gameObject,
                inviteTransform != null ? inviteTransform.gameObject : null,
                settingsTransform.gameObject,
                returnToLobbyTransform != null ? returnToLobbyTransform.gameObject : null,
                returnToMainMenuTransform != null ? returnToMainMenuTransform.gameObject : null,
                quitTransform.gameObject);

            Plugin.Log.LogInfo("[ZUIPauseMenu] In-game Pause Menu Tabbed Mode successfully installed!");
            return true;
        }

        private static Transform FindButtonsContainer()
        {
            // 1. Through InGameUI instance if active
            if (StaticInstance<InGameUI>.Instance != null)
            {
                try
                {
                    var pauseMenuField = typeof(InGameUI).GetField("pauseMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (pauseMenuField?.GetValue(StaticInstance<InGameUI>.Instance) is GameObject pm && pm != null)
                    {
                        Transform buttons = pm.transform.Find("Buttons/Buttons");
                        if (buttons != null) return buttons;
                    }
                }
                catch { }
            }

            // 2. Direct path check under InGameUICanvas
            GameObject canvasObj = GameObject.Find("InGameUICanvas");
            if (canvasObj != null)
            {
                Transform buttons = canvasObj.transform.Find("pausePanel/Buttons/Buttons");
                if (buttons != null) return buttons;
            }

            // 3. Fallback scan across active scene objects
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "Buttons" && go.transform.parent != null && go.transform.parent.name == "Buttons" && go.scene.IsValid())
                {
                    if (go.transform.Find("ContinueButton") != null && go.transform.Find("QuitButton") != null)
                    {
                        return go.transform;
                    }
                }
            }

            return null;
        }

        public static void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _registrations.Clear();
            _coroutineHost = null;
            _initialized = false;
        }

        #endregion
    }
}
