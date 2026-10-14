using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if RAVEN_CINEMACHINE3
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

namespace RavenTween.Tests {
    public sealed class CameraImpulseTests {
        [Test]
        public void CameraImpulse_FiresTheImpulseSource_WithoutWarnings() {
            Assert.That(FeedbackHooks.CameraImpulse, Is.Not.Null, "Registered when Play Mode starts.");
            var go = new GameObject("impulse");
            try {
                var source = go.AddComponent<CinemachineImpulseSource>();
                var feedback = go.AddComponent<RavenFeedback>();
                feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.CameraImpulse, effect = source, strength = 1f });
                feedback.Play();
                LogAssert.NoUnexpectedReceived();
            } finally {
                Object.DestroyImmediate(go);
            }
        }
    }
}
