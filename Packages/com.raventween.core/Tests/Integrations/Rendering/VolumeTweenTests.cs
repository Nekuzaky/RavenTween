using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace RavenTween.Tests {
    public sealed class VolumeTweenTests {
        GameObject _go;
        Volume _volume;

        [SetUp]
        public void SetUp() {
            Raven.StopAll();
            _go = new GameObject("volume-target");
            _volume = _go.AddComponent<Volume>();
            _volume.weight = 0.2f;
        }

        [TearDown]
        public void TearDown() {
            Raven.StopAll();
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void TweenWeight_ReachesTheTarget() {
            _volume.TweenWeight(1f, 0.5f);
            Raven.Step(0.6f);
            Assert.That(_volume.weight, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void VolumeFlash_GoesUpAndBack_EvenWhenRepeated() {
            var feedback = _go.AddComponent<RavenFeedback>();
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.VolumeFlash, effect = _volume, strength = 0.9f, duration = 0.4f });
            feedback.Play();
            Raven.Step(0.1f);
            feedback.Play();
            Raven.Step(0.2f);
            Assert.That(_volume.weight, Is.EqualTo(0.9f).Within(0.02f), "Peak.");
            Raven.Step(0.3f);
            Assert.That(_volume.weight, Is.EqualTo(0.2f).Within(1e-4f), "Back to its own weight, not to a mid-flash value.");
        }
    }
}
