using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZUI.Popups;

namespace ZUI.Core
{
    /// <summary>
    /// Intercepts missing method, field, member, and type-load exceptions caused by version mismatches
    /// between mods and ZUI-Lib, displaying an in-game diagnostic popup instead of silently breaking.
    /// </summary>
    public static class ZUICrashHandler
    {
        private struct PendingCrashReport
        {
            public string ExceptionType;
            public string ModName;
            public string MissingMember;
            public string ExpectedVersion; // null if the mod does not specify one
            public string InstalledVersion;
        }

        private static bool _initialized;
        private static int _lastHandledFrame = -1;
        private static string _lastHandledSignature = string.Empty;
        private static readonly Queue<PendingCrashReport> _pendingReports = new Queue<PendingCrashReport>();

        public static void Initialize(MonoBehaviour host)
        {
            if (_initialized) return;
            _initialized = true;

            Application.logMessageReceived += OnLogMessageReceived;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            Application.logMessageReceived -= OnLogMessageReceived;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Inspects an exception caught during a ZUI callback or module load.
        /// Returns <c>true</c> if it was a missing-member/type compatibility crash and was handled with a popup.
        /// </summary>
        public static bool HandleException(Exception ex, string context = null, Assembly callerHint = null)
        {
            if (ex == null) return false;

            while (ex is TargetInvocationException tie && tie.InnerException != null)
            {
                ex = tie.InnerException;
            }

            if (!IsCompatibilityException(ex))
            {
                return false;
            }

            string missingMember = ExtractMissingMemberText(ex.GetType().Name, ex.Message);
            ResolveModAndExpectedVersion(ex, callerHint, out string modName, out string expectedVersion);

            string expectedLineLog = !string.IsNullOrEmpty(expectedVersion)
                ? $"  Expected ZUI-Lib: {expectedVersion}\n"
                : string.Empty;

            Plugin.Log.LogError(
                $"[ZUICrashHandler] Compatibility crash caught{(string.IsNullOrEmpty(context) ? "" : $" in {context}")}!\n" +
                $"  Mod: {modName}\n" +
                $"  Attempted Call: {missingMember}\n" +
                expectedLineLog +
                $"  Installed ZUI-Lib: v{PluginInfo.PLUGIN_VERSION}\n" +
                $"  Exception: {ex}"
            );

            _lastHandledFrame = Time.frameCount;
            _lastHandledSignature = missingMember;

            EnqueueOrShowPopup(new PendingCrashReport
            {
                ExceptionType = ex.GetType().Name,
                ModName = modName,
                MissingMember = missingMember,
                ExpectedVersion = expectedVersion,
                InstalledVersion = $"v{PluginInfo.PLUGIN_VERSION}"
            });

            return true;
        }

        private static bool IsCompatibilityException(Exception ex)
        {
            return ex is MissingMethodException ||
                   ex is MissingFieldException ||
                   ex is MissingMemberException ||
                   ex is TypeLoadException;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || string.IsNullOrEmpty(condition)) return;

            bool isMissingMember =
                condition.StartsWith("MissingMethodException:", StringComparison.Ordinal) ||
                condition.StartsWith("MissingFieldException:", StringComparison.Ordinal) ||
                condition.StartsWith("MissingMemberException:", StringComparison.Ordinal) ||
                condition.StartsWith("TypeLoadException:", StringComparison.Ordinal);

            if (!isMissingMember) return;

            bool mentionsZui =
                condition.IndexOf("ZUI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(stackTrace) && stackTrace.IndexOf("ZUI", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!mentionsZui) return;

            int colonIdx = condition.IndexOf(':');
            string exTypeName = colonIdx > 0 ? condition.Substring(0, colonIdx).Trim() : "MissingMethodException";
            string rawMsg = colonIdx > 0 && colonIdx + 1 < condition.Length
                ? condition.Substring(colonIdx + 1).Trim()
                : condition;

            string missingMember = ExtractMissingMemberText(exTypeName, rawMsg);

            if (Time.frameCount == _lastHandledFrame && string.Equals(_lastHandledSignature, missingMember, StringComparison.Ordinal))
            {
                return;
            }

            _lastHandledFrame = Time.frameCount;
            _lastHandledSignature = missingMember;

            ResolveModFromStackTraceString(stackTrace, out string modName, out string expectedVersion);

            EnqueueOrShowPopup(new PendingCrashReport
            {
                ExceptionType = exTypeName,
                ModName = modName,
                MissingMember = missingMember,
                ExpectedVersion = expectedVersion,
                InstalledVersion = $"v{PluginInfo.PLUGIN_VERSION}"
            });
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FlushPendingPopups();
        }

        private static void EnqueueOrShowPopup(PendingCrashReport report)
        {
            if (StaticInstance<InitUI>.Instance != null && InitUI.PopupManager != null)
            {
                ShowCrashPopup(report);
            }
            else
            {
                _pendingReports.Enqueue(report);
            }
        }

        internal static void FlushPendingPopups()
        {
            if (StaticInstance<InitUI>.Instance == null || InitUI.PopupManager == null) return;

            while (_pendingReports.Count > 0)
            {
                var report = _pendingReports.Dequeue();
                ShowCrashPopup(report);
            }
        }

        private static void ShowCrashPopup(PendingCrashReport report)
        {
            string expectedSection = !string.IsNullOrEmpty(report.ExpectedVersion)
                ? $"<b>Expected ZUI-Lib:</b> <color=#FF8A65>{report.ExpectedVersion}</color>\n"
                : string.Empty;

            string message =
                $"<b>Mod:</b> <color=#FFD54F>{report.ModName}</color>\n" +
                $"<b>Error:</b> <color=#EF5350>{report.ExceptionType}</color>\n" +
                $"<b>Tried to call missing API:</b>\n" +
                $"<color=#4FC3F7>{report.MissingMember}</color>\n\n" +
                expectedSection +
                $"<b>Installed ZUI-Lib:</b> <color=#81C784>{report.InstalledVersion}</color>";

            ZUIPopup.ShowConfirmation(
                title: "ZUI-LIB CRASH",
                message: message,
                yesText: "Close Game",
                noText: "Ignore",
                onYes: () =>
                {
                    Application.Quit();
                },
                onNo: null,
                titleHex: "#EF5350",
                yesButtonHex: "#C62828",
                noButtonHex: "#455A64",
                dismissOnEscape: true,
                escapeChoice: PopupChoice.Secondary
            );
        }

        private static string ExtractMissingMemberText(string exceptionTypeName, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return exceptionTypeName;
            }

            var quoteMatch = Regex.Match(message, @"'([^']+)'");
            if (quoteMatch.Success && quoteMatch.Groups.Count > 1)
            {
                return quoteMatch.Groups[1].Value.Trim();
            }

            return message.Trim();
        }

        private static void ResolveModAndExpectedVersion(Exception ex, Assembly callerHint, out string modName, out string expectedVersion)
        {
            Assembly culpritAssembly = null;
            var zuiAssembly = typeof(ZUICrashHandler).Assembly;

            try
            {
                var st = new StackTrace(ex, false);
                var frames = st.GetFrames();
                if (frames != null)
                {
                    // Prefer external mod assemblies first
                    for (int i = 0; i < frames.Length; i++)
                    {
                        var asm = frames[i].GetMethod()?.DeclaringType?.Assembly;
                        if (asm == null) continue;

                        if (asm != zuiAssembly && !IsSystemOrUnityAssembly(asm))
                        {
                            culpritAssembly = asm;
                            break;
                        }
                    }

                    // If no external mod assembly was in the stack, fall back to the first non-system assembly (e.g. ZUI-Lib itself)
                    if (culpritAssembly == null)
                    {
                        for (int i = 0; i < frames.Length; i++)
                        {
                            var asm = frames[i].GetMethod()?.DeclaringType?.Assembly;
                            if (asm != null && !IsSystemOrUnityAssembly(asm))
                            {
                                culpritAssembly = asm;
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            if (culpritAssembly == null && callerHint != null)
            {
                culpritAssembly = callerHint;
            }

            if (culpritAssembly != null)
            {
                FormatAssemblyInfo(culpritAssembly, out modName, out expectedVersion);
                return;
            }

            ResolveModFromStackTraceString(ex.StackTrace, out modName, out expectedVersion);
        }

        private static void ResolveModFromStackTraceString(string stackTrace, out string modName, out string expectedVersion)
        {
            if (!string.IsNullOrEmpty(stackTrace) && Chainloader.PluginInfos != null)
            {
                // 1. Check external plugins first
                foreach (var kvp in Chainloader.PluginInfos)
                {
                    var info = kvp.Value;
                    if (info?.Instance == null) continue;

                    var asm = info.Instance.GetType().Assembly;
                    if (asm == typeof(ZUICrashHandler).Assembly) continue;

                    string asmName = asm.GetName().Name;
                    string rootNs = info.Instance.GetType().Namespace;

                    if ((!string.IsNullOrEmpty(asmName) && stackTrace.IndexOf(asmName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(rootNs) && stackTrace.IndexOf(rootNs, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        FormatPluginInfo(info, asm, out modName, out expectedVersion);
                        return;
                    }
                }
            }

            modName = $"{PluginInfo.PLUGIN_NAME} (v{PluginInfo.PLUGIN_VERSION})";
            expectedVersion = null;
        }

        private static void FormatAssemblyInfo(Assembly asm, out string modName, out string expectedVersion)
        {
            if (Chainloader.PluginInfos != null)
            {
                foreach (var kvp in Chainloader.PluginInfos)
                {
                    var info = kvp.Value;
                    if (info?.Instance != null && info.Instance.GetType().Assembly == asm)
                    {
                        FormatPluginInfo(info, asm, out modName, out expectedVersion);
                        return;
                    }
                }
            }

            var name = asm.GetName();
            modName = name.Version != null ? $"{name.Name} (v{name.Version})" : name.Name;
            expectedVersion = GetReferencedZuiVersion(asm);
        }

        private static void FormatPluginInfo(BepInEx.PluginInfo info, Assembly asm, out string modName, out string expectedVersion)
        {
            string name = info.Metadata != null ? info.Metadata.Name : asm.GetName().Name;
            string ver = info.Metadata?.Version != null ? info.Metadata.Version.ToString() : asm.GetName().Version?.ToString();
            modName = !string.IsNullOrEmpty(ver) ? $"{name} (v{ver})" : name;

            // 1. Check [BepInDependency("com.krwclassic.zui", "...")]
            if (info.Dependencies != null)
            {
                foreach (var dep in info.Dependencies)
                {
                    if (dep != null && string.Equals(dep.DependencyGUID, PluginInfo.PLUGIN_GUID, StringComparison.OrdinalIgnoreCase))
                    {
                        if (dep.MinimumVersion != null && dep.MinimumVersion > new Version(0, 0, 0))
                        {
                            expectedVersion = $"v{dep.MinimumVersion}+";
                            return;
                        }
                    }
                }
            }

            // 2. Check compiled AssemblyRef version of ZUI-Lib (if this is an external assembly)
            expectedVersion = asm != typeof(ZUICrashHandler).Assembly ? GetReferencedZuiVersion(asm) : null;
        }

        private static string GetReferencedZuiVersion(Assembly asm)
        {
            if (asm == null) return null;
            try
            {
                var refs = asm.GetReferencedAssemblies();
                for (int i = 0; i < refs.Length; i++)
                {
                    var r = refs[i];
                    if (string.Equals(r.Name, "ZUI-Lib", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.Name, "ZUI", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r.Version != null && r.Version > new Version(0, 0, 0, 0))
                        {
                            return $"v{r.Version.ToString(3)}";
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool IsSystemOrUnityAssembly(Assembly asm)
        {
            string name = asm.GetName().Name ?? string.Empty;
            return name.StartsWith("System", StringComparison.Ordinal) ||
                   name.StartsWith("mscorlib", StringComparison.Ordinal) ||
                   name.StartsWith("netstandard", StringComparison.Ordinal) ||
                   name.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                   name.StartsWith("Unity.", StringComparison.Ordinal) ||
                   name.StartsWith("BepInEx", StringComparison.Ordinal) ||
                   name.StartsWith("0Harmony", StringComparison.Ordinal) ||
                   name.StartsWith("Mono.", StringComparison.Ordinal);
        }
    }
}
