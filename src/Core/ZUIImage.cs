using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace ZUI.Core
{
    /// <summary>
    /// Utility for loading custom PNG/JPG images from disk, embedded assembly resources, or byte arrays into Unity Sprites.
    /// Includes memory caching and safe path resolution.
    /// </summary>
    public static class ZUIImage
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads a PNG or JPG image from disk into a Unity Sprite.
        /// If not found on disk, seamlessly falls back to embedded assembly resources.
        /// Supports absolute paths or paths relative to the BepInEx root or game directory.
        /// Results are cached by default for instant reuse.
        /// </summary>
        /// <param name="filePath">Absolute or relative path to the image file, or embedded resource name.</param>
        /// <param name="useCache">Whether to cache and reuse the loaded Sprite (default: true).</param>
        /// <param name="pixelsPerUnit">Pixels per unit scale (default: 100f).</param>
        /// <returns>A Unity Sprite, or null if loading failed.</returns>
        public static Sprite Load(string filePath, bool useCache = true, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            string resolvedPath = ResolvePath(filePath);

            if (useCache && _spriteCache.TryGetValue(resolvedPath, out var cached) && cached != null)
            {
                return cached;
            }

            if (!File.Exists(resolvedPath))
            {
                // Fallback: check if it is an embedded assembly resource
                var embedded = LoadEmbedded(filePath, null, useCache, pixelsPerUnit);
                if (embedded != null)
                {
                    return embedded;
                }

                Plugin.Log.LogWarning($"[ZUIImage] Image file or embedded resource not found: {resolvedPath}");
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(resolvedPath);
                var sprite = LoadFromBytes(fileData, pixelsPerUnit);
                if (sprite != null)
                {
                    sprite.name = Path.GetFileNameWithoutExtension(resolvedPath);
                    if (useCache)
                    {
                        _spriteCache[resolvedPath] = sprite;
                    }
                }
                return sprite;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ZUIImage] Failed to load image from '{resolvedPath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads an embedded PNG or JPG image resource from an assembly into a Unity Sprite.
        /// Searches for exact match, case-insensitive match, or resource names ending with the specified name.
        /// If assembly is null, checks the calling assembly first, then ZUI's executing assembly.
        /// </summary>
        public static Sprite LoadEmbedded(string resourceName, Assembly assembly = null, bool useCache = true, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;

            assembly ??= Assembly.GetCallingAssembly();
            string cacheKey = $"embedded::{assembly.FullName}::{resourceName}";

            if (useCache && _spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            byte[] bytes = ReadManifestResourceBytes(assembly, resourceName);
            if (bytes == null && assembly != typeof(ZUIImage).Assembly)
            {
                bytes = ReadManifestResourceBytes(typeof(ZUIImage).Assembly, resourceName);
            }

            if (bytes == null)
            {
                return null;
            }

            var sprite = LoadFromBytes(bytes, pixelsPerUnit);
            if (sprite != null)
            {
                sprite.name = Path.GetFileNameWithoutExtension(resourceName);
                if (useCache)
                {
                    _spriteCache[cacheKey] = sprite;
                }
            }
            return sprite;
        }

        private static byte[] ReadManifestResourceBytes(Assembly assembly, string name)
        {
            if (assembly == null || string.IsNullOrEmpty(name)) return null;

            string[] names = assembly.GetManifestResourceNames();
            string targetName = null;

            // 1. Exact match
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    targetName = names[i];
                    break;
                }
            }

            // 2. Ends with match (e.g. "teleport.png" matching "ZUI.src.Example.sprites.teleport.png")
            if (targetName == null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].EndsWith(name, StringComparison.OrdinalIgnoreCase) ||
                        names[i].EndsWith("." + name, StringComparison.OrdinalIgnoreCase))
                    {
                        targetName = names[i];
                        break;
                    }
                }
            }

            if (targetName == null) return null;

            using (var stream = assembly.GetManifestResourceStream(targetName))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Converts raw image bytes (PNG or JPG) into a Unity Sprite.
        /// </summary>
        public static Sprite LoadFromBytes(byte[] bytes, float pixelsPerUnit = 100f)
        {
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(texture, bytes))
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ZUIImage] Failed to create sprite from bytes: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clears all cached Sprite and Texture assets.
        /// </summary>
        public static void ClearCache()
        {
            _spriteCache.Clear();
        }

        private static string ResolvePath(string rawPath)
        {
            if (Path.IsPathRooted(rawPath)) return rawPath;

            // 1. Try relative to BepInEx plugins directory
            string pluginPath = Path.Combine(Paths.PluginPath, rawPath);
            if (File.Exists(pluginPath)) return pluginPath;

            // 2. Try relative to BepInEx root
            string bepPath = Path.Combine(Paths.BepInExRootPath, rawPath);
            if (File.Exists(bepPath)) return bepPath;

            // 3. Fallback to current working directory
            return Path.GetFullPath(rawPath);
        }
    }
}
