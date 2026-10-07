using System;
using NUnit.Framework;
using RavenTween.Editor;

namespace RavenTween.Tests {
    public sealed class UpdateLogicTests {
        const string GitId = "com.raventween.core@https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.2.0";

        [TestCase("v1.2.0", "1.2.0")]
        [TestCase("1.2", "1.2.0")]
        [TestCase("V2.0.1", "2.0.1")]
        [TestCase("1.3.0-beta.2", "1.3.0")]
        [TestCase("1.3.0+build.7", "1.3.0")]
        public void ParsesVersions(string input, string expected) {
            Assert.That(UpdateLogic.TryParseVersion(input, out Version v), Is.True);
            Assert.That(v, Is.EqualTo(Version.Parse(expected)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("latest")]
        [TestCase("v")]
        public void RejectsGarbageVersions(string input) {
            Assert.That(UpdateLogic.TryParseVersion(input, out _), Is.False);
        }

        [TestCase("v1.3.0", "1.2.0", true)]
        [TestCase("v1.2.1", "1.2.0", true)]
        [TestCase("v2.0.0", "1.9.9", true)]
        [TestCase("v1.2.0", "1.2.0", false)]
        [TestCase("v1.1.0", "1.2.0", false)]
        [TestCase("v1.10.0", "1.9.0", true)]
        [TestCase("garbage", "1.2.0", false)]
        public void ComparesVersionsNumerically(string latest, string installed, bool newer) {
            Assert.That(UpdateLogic.IsNewer(latest, installed), Is.EqualTo(newer));
        }

        [Test]
        public void CheckIsDue_NeverChecked() {
            Assert.That(UpdateLogic.IsCheckDue(0, DateTime.UtcNow), Is.True);
        }

        [Test]
        public void CheckIsDue_RespectsInterval() {
            DateTime now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(UpdateLogic.IsCheckDue(now.AddHours(-1).Ticks, now), Is.False);
            Assert.That(UpdateLogic.IsCheckDue(now.AddHours(-25).Ticks, now), Is.True);
        }

        [Test]
        public void CheckIsDue_WhenClockWentBackwards() {
            DateTime now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(UpdateLogic.IsCheckDue(now.AddDays(3).Ticks, now), Is.True, "A last-check date in the future must not block checks forever.");
        }

        [Test]
        public void GitIdentifier_ReplacesTag_KeepsPath() {
            string id = UpdateLogic.BuildUpdateIdentifier(InstallSource.Git, GitId, "v1.3.0");
            Assert.That(id, Is.EqualTo("https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.3.0"));
        }

        [Test]
        public void GitIdentifier_AddsTag_WhenNonePinned() {
            string id = UpdateLogic.BuildUpdateIdentifier(InstallSource.Git,
                "com.raventween.core@https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core", "v1.3.0");
            Assert.That(id, Does.EndWith("?path=/Packages/com.raventween.core#v1.3.0"));
        }

        [Test]
        public void RegistryIdentifier_UsesNameAtVersion() {
            string id = UpdateLogic.BuildUpdateIdentifier(InstallSource.Registry, "com.raventween.core@1.2.0", "v1.3.0");
            Assert.That(id, Is.EqualTo("com.raventween.core@1.3.0"));
        }

        // InstallSource is internal, so the cases are passed as their underlying values.
        [TestCase(3)] // Embedded
        [TestCase(4)] // Local
        [TestCase(0)] // Unknown
        public void EditableOrUnknownSources_AreNotUpdated(int source) {
            Assert.That(UpdateLogic.BuildUpdateIdentifier((InstallSource)source, GitId, "v1.3.0"), Is.Null);
        }

        [Test]
        public void ParsesGitHubRelease() {
            const string json = "{\"tag_name\":\"v1.3.0\",\"name\":\"RavenTween 1.3.0\",\"html_url\":\"https://github.com/x\",\"draft\":false,\"prerelease\":false,\"assets\":[]}";
            UpdateLogic.ReleaseInfo info = UpdateLogic.ParseRelease(json);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.tag_name, Is.EqualTo("v1.3.0"));
        }

        [TestCase("{\"tag_name\":\"v1.3.0\",\"prerelease\":true}")]
        [TestCase("{\"tag_name\":\"v1.3.0\",\"draft\":true}")]
        [TestCase("{\"tag_name\":\"nightly\"}")]
        [TestCase("not json")]
        [TestCase("")]
        public void IgnoresUnusableReleases(string json) {
            Assert.That(UpdateLogic.ParseRelease(json), Is.Null);
        }
    }
}
