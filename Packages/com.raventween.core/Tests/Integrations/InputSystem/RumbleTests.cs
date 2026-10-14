using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenTween.Tests {
    public sealed class RumbleTests {
        Gamepad _pad;

        [SetUp]
        public void SetUp() {
            Raven.StopAll();
            _pad = InputSystem.AddDevice<Gamepad>();
        }

        [TearDown]
        public void TearDown() {
            Raven.StopAll();
            InputSystem.RemoveDevice(_pad);
        }

        [Test]
        public void Rumble_RunsAndStops_WithoutErrors() {
            RavenRumble.Play(0.8f, 0.4f, 0.2f);
            RavenRumble.Play(0.2f, 0.9f, 0.1f); // Overlapping: keeps the longer end.
            Raven.Step(0.3f);
            RavenRumble.Stop();
            Assert.Pass();
        }

        [Test]
        public void FeedbackRumble_IsServedByTheInputSystemModule() {
            Assert.That(FeedbackHooks.Rumble, Is.Not.Null, "Registered when Play Mode starts.");
            var go = new GameObject("rumble-feedback");
            try {
                var feedback = go.AddComponent<RavenFeedback>();
                feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.Rumble, duration = 0.1f });
                feedback.Play();
                Raven.Step(0.2f);
            } finally {
                Object.DestroyImmediate(go);
            }
        }
    }
}
