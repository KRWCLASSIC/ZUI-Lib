using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZUI.Popups;
using ZUI.Settings;

namespace ZUI.Panels
{
    /// <summary>
    /// Runtime manager that handles cloning InGameUICanvas/settingsPanel,
    /// instantiating custom mod panel items, wiring Apply/Close actions,
    /// and managing ESC key dismissal and scene lifecycles.
    /// </summary>
    public class ZUIPanelController : MonoBehaviour
    {
        public static ZUIPanelController Instance { get; private set; }
        public static ZUIPanel CurrentOpenPanel { get; private set; }
        public static int LastEscConsumedFrame { get; internal set; } = -1;

        public static void EnsureInitialized(MonoBehaviour host)
        {
            if (Instance != null) return;

            var go = host != null ? host.gameObject : new GameObject("ZUIPanelController");
            Instance = go.GetComponent<ZUIPanelController>() ?? go.AddComponent<ZUIPanelController>();
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
            CurrentOpenPanel = null;

            // Invalidate cloned GameObjects since canvas belongs to the old scene
            foreach (var panel in ZUIPanel.Registry.Values)
            {
                panel.ClonedObject = null;
                panel.ApplyButton = null;
                panel.CloseButton = null;
                panel.RevertButton = null;
                panel.TitleText = null;
            }
        }

        private void Update()
        {
            if (CurrentOpenPanel != null && Input.GetKeyDown(KeyCode.Escape))
            {
                // If a popup dialog is currently active, let it handle ESC first
                if (InitUI.PopupManager != null && InitUI.PopupManager.GetPopupCount > 0)
                {
                    return;
                }

                LastEscConsumedFrame = Time.frameCount;
                ClosePanel(CurrentOpenPanel);
            }
        }

        public static void OpenPanel(ZUIPanel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            if (CurrentOpenPanel == panel) return;

            if (CurrentOpenPanel != null)
            {
                HidePanel(CurrentOpenPanel);
            }

            GameObject canvas = ZUISettings.FindCanvas();
            if (canvas == null)
            {
                Plugin.Log.LogError("[ZUIPanel] Cannot open panel: InGameUICanvas could not be found in active scene.");
                return;
            }

            Transform vanillaSettingsPanel = canvas.transform.Find("settingsPanel");
            if (vanillaSettingsPanel == null)
            {
                foreach (Transform child in canvas.transform)
                {
                    if (child.name.Equals("settingsPanel", StringComparison.OrdinalIgnoreCase))
                    {
                        vanillaSettingsPanel = child;
                        break;
                    }
                }
            }

            if (vanillaSettingsPanel == null)
            {
                Plugin.Log.LogError("[ZUIPanel] Cannot open panel: vanilla 'settingsPanel' template was not found under InGameUICanvas.");
                return;
            }

            // If vanilla settings panel is currently open, hide it
            if (vanillaSettingsPanel.gameObject.activeSelf)
            {
                vanillaSettingsPanel.gameObject.SetActive(false);
            }

            // Create or bind clone
            if (panel.ClonedObject == null)
            {
                BuildClonedPanel(panel, canvas, vanillaSettingsPanel);
            }

            if (panel.ClonedObject == null)
            {
                Plugin.Log.LogError($"[ZUIPanel] Failed to clone settings panel for '{panel.Id}'.");
                return;
            }

            // Refresh UI and initial button states
            panel.RefreshUI();
            if (panel.ApplyButton != null)
            {
                panel.ApplyButton.interactable = panel.HasPendingChanges;
                if (!panel.HasPendingChanges)
                {
                    ZUIPanel.ResetButtonVisuals(panel.ApplyButton);
                }
            }
            if (panel.RevertButton != null)
            {
                panel.RevertButton.interactable = panel.HasPendingChanges;
                if (!panel.HasPendingChanges)
                {
                    ZUIPanel.ResetButtonVisuals(panel.RevertButton);
                }
            }

            // Activate and display
            panel.ClonedObject.SetActive(true);
            int vanillaIndex = vanillaSettingsPanel.GetSiblingIndex();
            panel.ClonedObject.transform.SetSiblingIndex(vanillaIndex + 1);
            CurrentOpenPanel = panel;

            // Focus first selectable for controller/keyboard support
            panel.ClonedObject.GetComponentInChildren<Selectable>()?.Select();

            panel.OnOpened?.Invoke();
        }

        public static void ClosePanel(ZUIPanel panel)
        {
            if (panel == null) return;

            if (Input.GetKey(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Escape))
            {
                LastEscConsumedFrame = Time.frameCount;
            }

            // If a popup dialog is currently active in the game, do not stack another popup
            if (InitUI.PopupManager != null && InitUI.PopupManager.GetPopupCount > 0)
            {
                return;
            }

            if (panel.HasPendingChanges)
            {
                ZUIPopup.ShowConfirmation(
                    title: "DISCARD CHANGES",
                    message: "You have unsaved changes. Are you sure you want to discard them?",
                    yesText: "Discard",
                    noText: "Cancel",
                    onYes: () =>
                    {
                        panel.Revert();
                        HidePanel(panel);
                    },
                    onNo: null,
                    titleHex: "#FF5252",
                    dismissOnEscape: true,
                    escapeChoice: PopupChoice.Secondary
                );
            }
            else
            {
                HidePanel(panel);
            }
        }

        private static void HidePanel(ZUIPanel panel)
        {
            if (panel.ClonedObject != null)
            {
                panel.ClonedObject.SetActive(false);
            }

            if (CurrentOpenPanel == panel)
            {
                CurrentOpenPanel = null;
            }

            panel.OnClosed?.Invoke();
        }

        private static void BuildClonedPanel(ZUIPanel panel, GameObject canvas, Transform vanillaSettingsPanel)
        {
            GameObject clone = UnityEngine.Object.Instantiate(vanillaSettingsPanel.gameObject, canvas.transform, false);
            clone.name = $"ZUI_Panel_{panel.Id}";
            panel.ClonedObject = clone;

            // Locate content containers
            Transform originalContent = vanillaSettingsPanel.Find("Panel/Scroll View/Viewport/Content");
            var templates = SettingsTemplates.Discover(originalContent, canvas.transform);

            Transform clonedContent = clone.transform.Find("Panel/Scroll View/Viewport/Content");
            if (clonedContent == null)
            {
                foreach (var t in clone.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Content", StringComparison.OrdinalIgnoreCase) && t.parent != null && t.parent.name.Equals("Viewport", StringComparison.OrdinalIgnoreCase))
                    {
                        clonedContent = t;
                        break;
                    }
                }
            }

            if (clonedContent != null)
            {
                // Clear cloned vanilla setting items
                for (int i = clonedContent.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(clonedContent.GetChild(i).gameObject);
                }

                // Instantiate panel items
                foreach (var item in panel.Items)
                {
                    item.Instantiate(clonedContent, templates);
                }

                // Hook dynamic item additions
                panel.Container.OnItemAdded += newItem =>
                {
                    if (panel.ClonedObject != null)
                    {
                        Transform c = panel.ClonedObject.transform.Find("Panel/Scroll View/Viewport/Content");
                        if (c != null && c.Find(newItem.Id) == null)
                        {
                            newItem.Instantiate(c, templates);
                        }
                    }
                };
            }

            // Configure Title Text
            var tmps = clone.GetComponentsInChildren<TextMeshProUGUI>(true);
            var title = tmps.FirstOrDefault(t => clonedContent == null || !t.transform.IsChildOf(clonedContent) &&
                (t.gameObject.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 t.gameObject.name.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 t.text.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0));

            if (title == null)
            {
                var panelTrans = clone.transform.Find("Panel");
                if (panelTrans != null)
                {
                    title = panelTrans.GetComponentsInChildren<TextMeshProUGUI>(true)
                        .FirstOrDefault(t => clonedContent == null || !t.transform.IsChildOf(clonedContent));
                }
            }

            if (title != null)
            {
                var loc = title.GetComponent<LocalizedTextUI>();
                if (loc != null) UnityEngine.Object.Destroy(loc);

                title.text = panel.Title.ToUpperInvariant();
                panel.TitleText = title;
            }

            // Configure Action Buttons
            var buttons = clone.GetComponentsInChildren<Button>(true)
                .Where(b => clonedContent == null || !b.transform.IsChildOf(clonedContent))
                .ToList();

            var applyBtn = buttons.FirstOrDefault(b => b.name.IndexOf("apply", StringComparison.OrdinalIgnoreCase) >= 0);
            if (applyBtn != null)
            {
                panel.ApplyButton = applyBtn;
                applyBtn.onClick = new Button.ButtonClickedEvent();
                applyBtn.onClick.AddListener(() =>
                {
                    panel.Apply();
                });
                applyBtn.interactable = panel.HasPendingChanges;

                // Spawn optional dedicated REVERT button
                if (panel.ShowRevertButton)
                {
                    var revertObj = UnityEngine.Object.Instantiate(applyBtn.gameObject, applyBtn.transform.parent, false);
                    revertObj.name = "RevertButton";
                    revertObj.transform.SetSiblingIndex(applyBtn.transform.GetSiblingIndex() + 1);

                    var revertBtn = revertObj.GetComponent<Button>();
                    panel.RevertButton = revertBtn;
                    revertBtn.onClick = new Button.ButtonClickedEvent();
                    revertBtn.onClick.AddListener(() =>
                    {
                        panel.Revert();
                    });
                    revertBtn.interactable = panel.HasPendingChanges;

                    var revertText = revertObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (revertText != null)
                    {
                        var loc = revertText.GetComponent<LocalizedTextUI>();
                        if (loc != null) UnityEngine.Object.Destroy(loc);
                        revertText.text = "REVERT";
                    }
                }
            }

            var closeBtn = buttons.FirstOrDefault(b => b != applyBtn &&
                (b.name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 b.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0));

            if (closeBtn == null)
            {
                closeBtn = buttons.FirstOrDefault(b => b != applyBtn);
            }

            if (closeBtn != null)
            {
                panel.CloseButton = closeBtn;
                closeBtn.onClick = new Button.ButtonClickedEvent();
                closeBtn.onClick.AddListener(() =>
                {
                    ClosePanel(panel);
                });
            }
        }
    }
}
