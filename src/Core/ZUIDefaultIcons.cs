using System.Collections.Generic;
using UnityEngine;

namespace ZUI.Core
{
    /// <summary>
    /// Built-in procedural vector icons provided by ZUI for action buttons and status indicators.
    /// Available in the core library for any mod to use without bundling extra assets.
    /// </summary>
    public static class ZUIDefaultIcons
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Gets a default white circular ring + center dot indicator icon (44x44px).
        /// Perfect for equip toggles, radio buttons, and status indicators tintable via Button ColorBlock.
        /// </summary>
        public static Sprite CircleIndicator => GetOrCreateCircleIndicator(44, Color.white);

        /// <summary>
        /// Generates or retrieves a cached circular ring + center dot indicator icon with custom size and base color.
        /// </summary>
        public static Sprite CreateCircleIndicator(int size = 44, Color? ringColor = null)
        {
            return GetOrCreateCircleIndicator(size, ringColor ?? Color.white);
        }

        private static Sprite GetOrCreateCircleIndicator(int size, Color baseRing)
        {
            string cacheKey = $"circle_{size}_{ColorUtility.ToHtmlStringRGBA(baseRing)}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.42f;
            float innerRadius = size * 0.24f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int idx = y * size + x;

                    if (dist >= innerRadius && dist <= radius)
                    {
                        float ringAlpha = Mathf.SmoothStep(0f, 1f, (radius - dist) / 1.8f) *
                                          Mathf.SmoothStep(0f, 1f, (dist - innerRadius) / 1.8f);
                        pixels[idx] = new Color(baseRing.r, baseRing.g, baseRing.b, ringAlpha);
                    }
                    else if (dist < innerRadius * 0.52f)
                    {
                        float coreAlpha = Mathf.SmoothStep(0f, 1f, (innerRadius * 0.52f - dist) / 1.5f);
                        pixels[idx] = new Color(baseRing.r, baseRing.g, baseRing.b, coreAlpha);
                    }
                    else
                    {
                        pixels[idx] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"ZUI_Default_CircleIndicator_{size}";
            _cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Clears all cached default icon sprites.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}

