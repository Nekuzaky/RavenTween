using System;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>How the package was installed; decides how an update is applied.</summary>
    enum InstallSource {
        Unknown = 0,
        Git = 1,
        Registry = 2,
        Embedded = 3,
        Local = 4
    }

    /// <summary>Pure, side-effect-free update rules. Kept separate so it is fully unit-tested.</summary>
    static class UpdateLogic {
        public const string PackageName = "com.raventween.core";
        public const string LatestReleaseApi = "https://api.github.com/repos/Nekuzaky/RavenTween/releases/latest";
        public const string ReleasesPage = "https://github.com/Nekuzaky/RavenTween/releases";
        public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        /// <summary>Parses "v1.2.0", "1.2.0" or "1.2.0-beta.1" into a comparable version.</summary>
        public static bool TryParseVersion(string text, out Version version) {
            return TryParseVersion(text, out version, out _);
        }

        /// <summary>Same, and reports whether the version is a pre-release ("-beta.1"; "+build" is not).</summary>
        public static bool TryParseVersion(string text, out Version version, out bool prerelease) {
            version = null;
            prerelease = false;
            if (string.IsNullOrEmpty(text)) { return false; }
            string trimmed = text.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)) { trimmed = trimmed.Substring(1); }
            int build = trimmed.IndexOf('+');
            if (build >= 0) { trimmed = trimmed.Substring(0, build); }
            int dash = trimmed.IndexOf('-');
            if (dash >= 0) {
                prerelease = true;
                trimmed = trimmed.Substring(0, dash);
            }
            if (!Version.TryParse(trimmed, out Version parsed)) { return false; }
            version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
            return true;
        }

        /// <summary>
        /// True when <paramref name="latestTag"/> is strictly newer than <paramref name="installed"/>.
        /// A release is newer than a pre-release of the same number (1.5.0 &gt; 1.5.0-beta.2).
        /// </summary>
        public static bool IsNewer(string latestTag, string installed) {
            if (!TryParseVersion(latestTag, out Version latest, out bool latestPre)) { return false; }
            if (!TryParseVersion(installed, out Version current, out bool currentPre)) { return false; }
            if (latest != current) { return latest > current; }
            return currentPre && !latestPre;
        }

        /// <summary>True when the last check is older than the interval (or never happened).</summary>
        public static bool IsCheckDue(long lastCheckUtcTicks, DateTime nowUtc) {
            Debug.Assert(nowUtc.Kind != DateTimeKind.Local, "Pass UTC time.");
            if (lastCheckUtcTicks <= 0) { return true; }
            var last = new DateTime(lastCheckUtcTicks, DateTimeKind.Utc);
            return nowUtc - last >= CheckInterval || last > nowUtc;
        }

        /// <summary>
        /// Builds the Package Manager identifier that installs <paramref name="tag"/>.
        /// Git: keeps the repository URL and its ?path=, replaces any #fragment.
        /// Registry: name@version. Returns null when the source can't be updated this way.
        /// </summary>
        public static string BuildUpdateIdentifier(InstallSource source, string packageId, string tag) {
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(tag)) { return null; }
            if (source == InstallSource.Registry) {
                return TryParseVersion(tag, out Version v) ? PackageName + "@" + v.ToString(3) : null;
            }
            if (source != InstallSource.Git) { return null; }
            int at = packageId.IndexOf('@');
            if (at < 0 || at == packageId.Length - 1) { return null; }
            string url = packageId.Substring(at + 1);
            int fragment = url.IndexOf('#');
            if (fragment >= 0) { url = url.Substring(0, fragment); }
            return url + "#" + tag;
        }

        /// <summary>Response shape of the GitHub "latest release" endpoint (only the fields used).</summary>
        [Serializable]
        public sealed class ReleaseInfo {
            public string tag_name;
            public string name;
            public string html_url;
            public bool prerelease;
            public bool draft;
        }

        /// <summary>Parses the GitHub JSON. Returns null on malformed input or drafts / pre-releases.</summary>
        public static ReleaseInfo ParseRelease(string json) {
            if (string.IsNullOrEmpty(json)) { return null; }
            ReleaseInfo info;
            try { info = JsonUtility.FromJson<ReleaseInfo>(json); }
            catch (ArgumentException) { return null; }
            if (info == null || info.draft || info.prerelease) { return null; }
            return TryParseVersion(info.tag_name, out _) ? info : null;
        }
    }
}
