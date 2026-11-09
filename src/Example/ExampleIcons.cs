using System.Collections.Generic;
using UnityEngine;

namespace ZUI.Example
{
    /// <summary>
    /// Procedural icon generators used exclusively by the ZUI Example / Showcase module.
    /// Kept separate from <see cref="ZUI.Core.ZUIImage"/> so core library classes only handle image loading.
    /// </summary>
    public static class ExampleIcons
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Generates a crisp circular ring + center dot indicator icon (white base by default so UI Button ColorBlock can tint it cleanly).
        /// </summary>
        public static Sprite CreateCircleIndicatorIcon(int size = 44, Color? ringColor = null)
        {
            Color baseRing = ringColor ?? Color.white;
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

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.name = "ZUI_Example_CircleIndicator";
            _cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a procedural diamond badge icon for the Skin Changer showcase.
        /// </summary>
        public static Sprite CreateSkinIcon(int size = 44, Color? tint = null)
        {
            Color baseColor = tint ?? new Color(1.0f, 0.78f, 0.25f, 1.0f);
            string cacheKey = $"skin_{size}_{ColorUtility.ToHtmlStringRGBA(baseColor)}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float half = size / 2f;
            float outer = size * 0.42f;
            float inner = size * 0.22f;
            float core = size * 0.10f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float manhattan = Mathf.Abs(x - half) + Mathf.Abs(y - half);
                    int idx = y * size + x;

                    if (manhattan >= inner && manhattan <= outer)
                    {
                        float alpha = Mathf.SmoothStep(0f, 1f, (outer - manhattan) / 1.5f) *
                                      Mathf.SmoothStep(0f, 1f, (manhattan - inner) / 1.5f);
                        pixels[idx] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    }
                    else if (manhattan <= core)
                    {
                        float coreAlpha = Mathf.SmoothStep(0f, 1f, (core - manhattan) / 1.2f);
                        pixels[idx] = new Color(1f, 1f, 1f, coreAlpha);
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

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.name = "ZUI_Example_SkinIcon";
            _cache[cacheKey] = sprite;
            return sprite;
        }
    }
}

