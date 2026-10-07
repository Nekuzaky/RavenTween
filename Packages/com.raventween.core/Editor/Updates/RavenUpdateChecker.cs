using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace RavenTween.Editor {
    /// <summary>
    /// Checks GitHub for a newer RavenTween release at most once a day, then offers the update
    /// (or applies it, when automatic updates are enabled). One anonymous request to the public
    /// GitHub API; nothing is sent about the project or the user.
    /// </summary>
    [InitializeOnLoad]
    static class RavenUpdateChecker {
        const string PrefCheckEnabled = "RavenTween.Updates.CheckEnabled";
        const string PrefAutoInstall = "RavenTween.Updates.AutoInstall";
        const string PrefLastCheck = "RavenTween.Updates.LastCheckUtcTicks";
        const string PrefSkipped = "RavenTween.Updates.SkippedTag";
        const string MenuCheckEnabled = "Tools/RavenTween/Updates/Check Automatically";
        const string MenuAutoInstall = "Tools/RavenTween/Updates/Install Automatically";
        const int TimeoutSeconds = 15;

        static UnityWebRequest _request;
        static bool _manual;
        static AddRequest _install;

        static RavenUpdateChecker() {
            // Domain reloads also happen when entering or leaving Play Mode; stay out of those.
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            EditorApplication.delayCall += OnEditorReady;
        }

        static bool CheckEnabled {
            get { return EditorPrefs.GetBool(PrefCheckEnabled, true); }
            set { EditorPrefs.SetBool(PrefCheckEnabled, value); }
        }

        static bool AutoInstall {
            get { return EditorPrefs.GetBool(PrefAutoInstall, false); }
            set { EditorPrefs.SetBool(PrefAutoInstall, value); }
        }

        static void OnEditorReady() {
            if (!CheckEnabled || EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            long.TryParse(EditorPrefs.GetString(PrefLastCheck, "0"), out long last);
            if (!UpdateLogic.IsCheckDue(last, DateTime.UtcNow)) { return; }
            StartCheck(false);
        }

        // ----- Menu -----

        [MenuItem("Tools/RavenTween/Updates/Check Now", priority = 100)]
        static void CheckNow() { StartCheck(true); }

        [MenuItem(MenuCheckEnabled, priority = 120)]
        static void ToggleCheck() { CheckEnabled = !CheckEnabled; }

        [MenuItem(MenuCheckEnabled, true)]
        static bool ToggleCheckValidate() {
            Menu.SetChecked(MenuCheckEnabled, CheckEnabled);
            return true;
        }

        [MenuItem(MenuAutoInstall, priority = 121)]
        static void ToggleAutoInstall() {
            bool enable = !AutoInstall;
            if (enable && !EditorUtility.DisplayDialog("RavenTween",
                    "Install new RavenTween releases automatically, without asking?\n\n" +
                    "Updates only happen for Git and registry installs, at most once a day. " +
                    "Teams that need reproducible builds should leave this off and update on purpose.",
                    "Enable", "Cancel")) {
                return;
            }
            AutoInstall = enable;
            if (enable) { CheckEnabled = true; }
        }

        [MenuItem(MenuAutoInstall, true)]
        static bool ToggleAutoInstallValidate() {
            Menu.SetChecked(MenuAutoInstall, AutoInstall);
            return true;
        }

        // ----- Check -----

        static void StartCheck(bool manual) {
            if (_request != null) {
                _manual |= manual; // A click during a background check still gets its answer.
                return;
            }
            _manual = manual;
            _request = UnityWebRequest.Get(UpdateLogic.LatestReleaseApi);
            _request.SetRequestHeader("Accept", "application/vnd.github+json");
            _request.SetRequestHeader("User-Agent", "RavenTween-UpdateChecker");
            _request.timeout = TimeoutSeconds;
            _request.SendWebRequest();
            EditorApplication.update += PollCheck;
        }

        static void PollCheck() {
            if (_request == null || !_request.isDone) { return; }
            EditorApplication.update -= PollCheck;
            bool ok = _request.result == UnityWebRequest.Result.Success;
            string body = ok ? _request.downloadHandler.text : null;
            string error = ok ? null : _request.error;
            _request.Dispose();
            _request = null;
            // Never pop windows or reinstall packages over a running game; retry on a later load.
            if (!_manual && EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            EditorPrefs.SetString(PrefLastCheck, DateTime.UtcNow.Ticks.ToString());
            if (!ok) {
                if (_manual) { EditorUtility.DisplayDialog("RavenTween", "Could not reach GitHub:\n" + error, "OK"); }
                return;
            }
            HandleRelease(UpdateLogic.ParseRelease(body));
        }

        static void HandleRelease(UpdateLogic.ReleaseInfo release) {
            InstalledPackage installed = InstalledPackage.Find();
            if (release == null || installed == null) {
                if (_manual) { EditorUtility.DisplayDialog("RavenTween", "Could not read the latest release information.", "OK"); }
                return;
            }
            bool editableCopy = installed.Source == InstallSource.Embedded || installed.Source == InstallSource.Local;
            if (!_manual && editableCopy) { return; } // A copy you edit is its own source of truth.
            if (!UpdateLogic.IsNewer(release.tag_name, installed.Version)) {
                if (_manual) {
                    EditorUtility.DisplayDialog("RavenTween", "RavenTween " + installed.Version + " is up to date.", "OK");
                }
                return;
            }
            if (!_manual && EditorPrefs.GetString(PrefSkipped, "") == release.tag_name) { return; }
            string identifier = UpdateLogic.BuildUpdateIdentifier(installed.Source, installed.PackageId, release.tag_name);
            if (!_manual && AutoInstall && identifier != null) {
                Install(identifier, release.tag_name);
                return;
            }
            RavenUpdateWindow.Open(release, installed, identifier);
        }

        // ----- Install -----

        internal static void Install(string identifier, string tag) {
            Debug.Assert(!string.IsNullOrEmpty(identifier), "Update identifier is required.");
            if (_install != null) { return; }
            Debug.Log("[RavenTween] Updating to " + tag + "…");
            _install = Client.Add(identifier);
            EditorApplication.update += PollInstall;
        }

        static void PollInstall() {
            if (_install == null || !_install.IsCompleted) { return; }
            EditorApplication.update -= PollInstall;
            if (_install.Status == StatusCode.Success) {
                Debug.Log("[RavenTween] Updated to " + _install.Result.version + ".");
            } else {
                Debug.LogError("[RavenTween] Update failed: " + (_install.Error != null ? _install.Error.message : "unknown error"));
            }
            _install = null;
        }

        internal static void Skip(string tag) {
            EditorPrefs.SetString(PrefSkipped, tag);
        }
    }

    /// <summary>The installed RavenTween package as Package Manager sees it.</summary>
    sealed class InstalledPackage {
        public string Version;
        public string PackageId;
        public InstallSource Source;

        public static InstalledPackage Find() {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Raven).Assembly);
            if (info == null) { return null; }
            return new InstalledPackage {
                Version = info.version,
                PackageId = info.packageId,
                Source = Map(info.source)
            };
        }

        static InstallSource Map(PackageSource source) {
            switch (source) {
                case PackageSource.Git: return InstallSource.Git;
                case PackageSource.Registry: return InstallSource.Registry;
                case PackageSource.Embedded: return InstallSource.Embedded;
                case PackageSource.Local: return InstallSource.Local;
                default: return InstallSource.Unknown;
            }
        }
    }
}
