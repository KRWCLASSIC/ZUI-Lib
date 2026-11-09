using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZUI.Panels;
using ZUI.PauseMenu;
using ZUI.Popups;
using ZUI.Settings;

namespace ZUI.Screens
{
    /// <summary>
    /// Runtime manager for standalone custom split screens (<see cref="ZUIScreen"/>).
    /// Handles cloning <c>pausePanel</c> into isolated screen instances, managing cursor lock
    /// and player input freeze without activating the vanilla pause menu, and handling ESC navigation.
    /// </summary>
    public class ZUIScreenController : MonoBehaviour
    {
        public static ZUIScreenController Instance { get; private set; }
        public static ZUIScreen CurrentOpenScreen { get; private set; }
        public static int LastEscConsumedFrame { get; internal set; } = -1;

        private static readonly PropertyInfo IsGamePausedProp =
            typeof(InGameUI).GetProperty(nameof(InGameUI.IsGamePaused), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        public static void EnsureInitialized(MonoBehaviour host)
        {
            if (Instance != null) return;

            var go = host != null ? host.gameObject : new GameObject("ZUIScreenController");
            Instance = go.GetComponent<ZUIScreenController>() ?? go.AddComponent<ZUIScreenController>();
            DontDestroyOnLoad(Instance.gameObject);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentOpenScreen = null;

            foreach (var screen in ZUIScreen.Registry.Values)
            {
                if (screen.ViewInstance != null)
                {
                    screen.ViewInstance.CleanupBeforeSceneUnload();
                    screen.ViewInstance = null;
                }
            }
        }

        public bool HandleEscape()
        {
            if (CurrentOpenScreen == null) return false;
            if (Time.frameCount == LastEscConsumedFrame) return true;

            if (ZUIPanelController.CurrentOpenPanel != null || ZUIPanelController.LastEscConsumedFrame == Time.frameCount)
            {
                return true;
            }
            if (ZUIPopup.LastEscConsumedFrame == Time.frameCount)
            {
                return true;
            }

            LastEscConsumedFrame = Time.frameCount;
            StaticInstance<InitUI>.Instance?.PlayClickSFX();

            var view = CurrentOpenScreen.ViewInstance;
            if (view != null && view.HasNestedDrawers)
            {
                view.PopDrawer();
            }
            else
            {
                CloseScreen(CurrentOpenScreen);
            }
            return true;
        }

        public static void OpenScreen(ZUIScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            if (CurrentOpenScreen == screen)
            {
                screen.ViewInstance?.RefreshView();
                return;
            }

            // 1. If a different ZUIScreen is open, close it first
            if (CurrentOpenScreen != null)
            {
                HideScreenInternal(CurrentOpenScreen);
            }

            // 2. If a ZUIPanel modal is open, close it
            if (ZUIPanel.CurrentOpenPanel != null)
            {
                ZUIPanel.CloseActive();
            }

            // 3. If the vanilla Pause Menu is currently open, close it so they don't overlap
            if (ZUIPauseMenu.IsOpen)
            {
                ZUIPauseMenu.Close();
            }

            GameObject canvas = ZUISettings.FindCanvas();
            if (canvas == null)
            {
                Plugin.Log.LogError($"[ZUIScreen] Cannot open screen '{screen.Id}': InGameUICanvas not found in active scene.");
                return;
            }

            GameObject vanillaPausePanel = FindVanillaPausePanel(canvas);
            if (vanillaPausePanel == null)
            {
                Plugin.Log.LogError($"[ZUIScreen] Cannot open screen '{screen.Id}': vanilla pausePanel template not found.");
                return;
            }

            if (screen.ViewInstance == null || screen.ViewInstance.RootObject == null)
            {
                screen.ViewInstance = ZUIScreenView.Create(screen, canvas.transform, vanillaPausePanel);
            }

            if (screen.ViewInstance == null || screen.ViewInstance.RootObject == null)
            {
                Plugin.Log.LogError($"[ZUIScreen] Failed to build screen view for '{screen.Id}'.");
                return;
            }

            CurrentOpenScreen = screen;
            screen.ViewInstance.Show(vanillaPausePanel.transform.GetSiblingIndex() + 1);

            InGameUI.RecalculateCursorLock();
            ApplyCursorAndInputLock();

            screen.OnOpened?.Invoke();
        }

        public static void CloseScreen(ZUIScreen screen)
        {
            if (screen == null) return;

            if (Input.GetKey(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Escape))
            {
                LastEscConsumedFrame = Time.frameCount;
            }

            HideScreenInternal(screen);
            InGameUI.RecalculateCursorLock();
        }

        private static void HideScreenInternal(ZUIScreen screen)
        {
            if (screen.ViewInstance != null)
            {
                screen.ViewInstance.Hide();
            }

            if (CurrentOpenScreen == screen)
            {
                CurrentOpenScreen = null;
            }

            screen.OnClosed?.Invoke();
        }

        internal static void RefreshIfOpen(ZUIScreen screen)
        {
            if (screen != null && CurrentOpenScreen == screen && screen.ViewInstance != null)
            {
                screen.ViewInstance.RefreshView();
            }
        }

        internal static bool IsDisplayingRightPanel(PauseMenuRightPanel panel)
        {
            return panel != null &&
                   CurrentOpenScreen != null &&
                   CurrentOpenScreen.ViewInstance != null &&
                   CurrentOpenScreen.ViewInstance.CurrentRightPanel == panel;
        }

        internal static void SyncRightPanelIfDisplayed(PauseMenuRightPanel panel)
        {
            if (IsDisplayingRightPanel(panel))
            {
                CurrentOpenScreen.ViewInstance.SyncRightPanel();
            }
        }

        internal static void ApplyRightPanelHeaderIfDisplayed(PauseMenuRightPanel panel)
        {
            if (IsDisplayingRightPanel(panel))
            {
                CurrentOpenScreen.ViewInstance.ApplyRightPanelHeader();
            }
        }

        internal static void RebuildActiveRightLayout()
        {
            if (CurrentOpenScreen != null && CurrentOpenScreen.ViewInstance != null)
            {
                CurrentOpenScreen.ViewInstance.RebuildRightLayout();
            }
        }

        internal static void ApplyCursorAndInputLock()
        {
            if (CurrentOpenScreen == null) return;

            try
            {
                IsGamePausedProp?.SetValue(null, true);
            }
            catch { }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static GameObject FindVanillaPausePanel(GameObject canvas)
        {
            if (StaticInstance<InGameUI>.Instance != null)
            {
                try
                {
                    var pauseMenuField = typeof(InGameUI).GetField("pauseMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (pauseMenuField?.GetValue(StaticInstance<InGameUI>.Instance) is GameObject pm && pm != null)
                    {
                        return pm;
                    }
                }
                catch { }
            }

            if (canvas != null)
            {
                Transform t = canvas.transform.Find("pausePanel");
                if (t != null) return t.gameObject;
            }

            return null;
        }
    }
}
