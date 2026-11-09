using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZUI.MainMenu
{
    /// <summary>
    /// Public API for registering and controlling custom buttons in the Zort Main Menu.
    /// </summary>
    public static class ZUIMainMenu
    {
        private static readonly List<MainMenuButtonRegistration> _registrations = new List<MainMenuButtonRegistration>();
        private static MonoBehaviour _coroutineHost;
        private static bool _initialized = false;

        public static IReadOnlyList<MainMenuButtonRegistration> Registrations => _registrations.AsReadOnly();

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

        public static MainMenuDrawer RootDrawer { get; } = new MainMenuDrawer("zui_root_more", "MORE", paginated: true);

        #region Registration API

        public static MainMenuButtonRegistration RegisterButton(string id, string label, Action onClick, int priority = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            // Remove existing registration with same ID if present
            _registrations.RemoveAll(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            var item = new MainMenuButtonRegistration(id, label, onClick, priority);
            _registrations.Add(item);

            RefreshUI();
            return item;
        }

        /// <summary>
        /// Registers a custom submenu ("Drawer") on the Main Menu under the MORE section.
        /// Clicking the registered button smoothly transitions into the drawer.
        /// </summary>
        public static MainMenuDrawer RegisterSubmenu(string id, string label, bool paginated = true, int priority = 0, string breadcrumbHex = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));

            var drawer = new MainMenuDrawer(id, label, paginated, breadcrumbHex)
            {
                Parent = RootDrawer
            };

            RegisterButton(id, label, () =>
            {
                MainMenuTabController.Instance?.PushDrawer(drawer);
            }, priority);

            return drawer;
        }

        public static void RegisterButton(MainMenuButtonRegistration registration)
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

        public static List<MainMenuButtonRegistration> GetSortedRegistrations()
        {
            return _registrations
                .Where(r => r.IsVisible)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void RefreshUI()
        {
            if (MainMenuTabController.Instance != null && MainMenuTabController.Instance.CurrentMode == MainMenuMode.Tabbed)
            {
                MainMenuTabController.Instance.RefreshTabbedView();
            }
        }

        public static void SetMode(MainMenuMode mode)
        {
            if (MainMenuTabController.Instance != null)
            {
                MainMenuTabController.Instance.SetMode(mode);
            }
        }

        public static void PushDrawer(MainMenuDrawer drawer)
        {
            if (MainMenuTabController.Instance != null)
            {
                MainMenuTabController.Instance.PushDrawer(drawer);
            }
        }

        public static void PopDrawer()
        {
            if (MainMenuTabController.Instance != null)
            {
                MainMenuTabController.Instance.PopDrawer();
            }
        }

        public static MainMenuDrawer CurrentDrawer => MainMenuTabController.Instance?.CurrentDrawer;

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

        public static bool TryInstall()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "SplashScreen" || string.IsNullOrEmpty(activeScene.name))
            {
                return false;
            }

            // Find Buttons container under MainMenuCanvas
            Transform buttonsContainer = FindButtonsContainer();
            if (buttonsContainer == null) return false;

            // Check if already installed
            var existingController = buttonsContainer.GetComponent<MainMenuTabController>();
            if (existingController != null) return true;

            // Find vanilla buttons
            Transform hostTransform = buttonsContainer.Find("StartHostButton");
            Transform joinTransform = buttonsContainer.Find("FindGameButton");
            Transform settingsTransform = buttonsContainer.Find("SettingsButton");
            Transform quitTransform = buttonsContainer.Find("QuitGameButton");

            if (hostTransform == null || joinTransform == null || settingsTransform == null || quitTransform == null)
            {
                return false;
            }

            var controller = buttonsContainer.gameObject.AddComponent<MainMenuTabController>();
            controller.Initialize(
                hostTransform.gameObject,
                joinTransform.gameObject,
                settingsTransform.gameObject,
                quitTransform.gameObject);

            Plugin.Log.LogInfo("[ZUIMainMenu] Main Menu Tabbed Mode successfully installed!");
            return true;
        }

        private static Transform FindButtonsContainer()
        {
            // 1. Direct path check
            GameObject canvasObj = GameObject.Find("MainMenuCanvas");
            if (canvasObj != null)
            {
                Transform buttons = canvasObj.transform.Find("Buttons");
                if (buttons != null) return buttons;
            }

            // 2. Through MainMenuManager instance if active
            if (StaticInstance<MainMenuManager>.Instance != null)
            {
                try
                {
                    var hostField = typeof(MainMenuManager).GetField("hostButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var hostBtn = hostField?.GetValue(StaticInstance<MainMenuManager>.Instance) as UnityEngine.UI.Selectable;
                    if (hostBtn != null && hostBtn.transform.parent != null)
                    {
                        return hostBtn.transform.parent;
                    }
                }
                catch { }
            }

            // 3. Fallback scan in active scenes
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "Buttons" && go.transform.parent != null && go.transform.parent.name == "MainMenuCanvas" && go.scene.IsValid())
                {
                    return go.transform;
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
