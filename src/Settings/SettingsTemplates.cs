using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.Settings
{
    public class SettingsTemplates
    {
        public Transform LineTemplate { get; set; }
        public Transform ToggleTemplate { get; set; }
        public Transform SliderTemplate { get; set; }
        public Transform DropdownTemplate { get; set; }
        public Transform ButtonTemplate { get; set; }

        public static SettingsTemplates Discover(Transform content, Transform canvasRoot)
        {
            var templates = new SettingsTemplates();
            if (content == null) return templates;

            // 1. Line template
            templates.LineTemplate = content.Find("Line");
            if (templates.LineTemplate == null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    if (child.name.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        templates.LineTemplate = child;
                        break;
                    }
                }
            }

            // 2. Toggle template
            templates.ToggleTemplate = content.Find("subtitlesToggle");
            if (templates.ToggleTemplate == null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    if (child.GetComponentInChildren<Toggle>(true) != null)
                    {
                        templates.ToggleTemplate = child;
                        break;
                    }
                }
            }

            // 3. Slider template
            templates.SliderTemplate = content.Find("sensitivityMouseSlider")
                                    ?? content.Find("masterVolumeSlider")
                                    ?? content.Find("voicechatVolumeSlider");
            if (templates.SliderTemplate == null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    if (child.GetComponentInChildren<Slider>(true) != null)
                    {
                        templates.SliderTemplate = child;
                        break;
                    }
                }
            }

            // 4. Dropdown template
            templates.DropdownTemplate = content.Find("targetFpsDropdown")
                                      ?? content.Find("graphicsDrowdown")
                                      ?? content.Find("m_VSyncDropdown")
                                      ?? content.Find("voicechatDropdown");
            if (templates.DropdownTemplate == null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    if (child.GetComponentInChildren<TMPro.TMP_Dropdown>(true) != null)
                    {
                        templates.DropdownTemplate = child;
                        break;
                    }
                }
            }

            // 4. Button template
            if (canvasRoot != null)
            {
                foreach (var btn in canvasRoot.GetComponentsInChildren<Button>(true))
                {
                    if (btn.gameObject.name == "ApplyButton")
                    {
                        templates.ButtonTemplate = btn.transform;
                        break;
                    }
                }
            }

            return templates;
        }
    }
}
