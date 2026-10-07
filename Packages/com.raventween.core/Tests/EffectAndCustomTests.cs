using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class EffectAndCustomTests {
        sealed class Box {
            public float Value;
            public Vector3 Position;
        }

        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _go = new GameObject("effect-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
        }

        static void Step(float delta, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(delta, delta); }
        }

        [Test]
        public void Shake_MovesDuringTween_AndRestsAtOrigin() {
            _go.transform.localPosition = new Vector3(1f, 2f, 3f);
            Raven.ShakePosition(_go.transform, Vector3.one, 1f);
            Step(0.1f, 3);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.GreaterThan(1e-4f),
                "Shake must displace the target mid-tween.");
            Step(0.1f, 10);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-4f),
                "Shake must settle exactly on the start value.");
        }

        [Test]
        public void Punch_StaysWithinStrength_AndRestsAtOrigin() {
            _go.transform.localScale = Vector3.one;
            Raven.PunchScale(_go.transform, Vector3.one * 0.5f, 0.5f);
            float maxDeviation = 0f;
            for (int i = 0; i < 25; i++) {
                Step(0.02f);
                maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(_go.transform.localScale.x - 1f));
            }
            Assert.That(maxDeviation, Is.GreaterThan(0.05f));
            Assert.That(maxDeviation, Is.LessThanOrEqualTo(0.5f + 1e-4f));
            Step(0.1f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Complete_OnEffect_RestoresOrigin() {
            _go.transform.localPosition = Vector3.zero;
            Tween tween = Raven.ShakePosition(_go.transform, Vector3.one * 2f, 1f);
            Step(0.2f);
            tween.Complete();
            Assert.That(_go.transform.localPosition.magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void Custom_Float_WritesThroughSetter() {
            var box = new Box();
            Raven.Custom(box, 0f, 10f, 1f, (b, v) => b.Value = v);
            Step(0.5f);
            Assert.That(box.Value, Is.EqualTo(5f).Within(1e-3f));
            Step(0.5f);
            Assert.That(box.Value, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void Custom_Vector3_WritesThroughSetter() {
            var box = new Box();
            Raven.Custom(box, Vector3.zero, new Vector3(2f, 4f, 6f), 1f, (b, v) => b.Position = v);
            Step(0.5f);
            Assert.That(Vector3.Distance(box.Position, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void Custom_UnityTarget_DiesWithTarget() {
            var victim = new GameObject("custom-victim");
            bool destroyedCallback = false;
            Tween tween = Raven.Custom(victim.transform, 0f, 1f, 5f, (t, v) => t.localPosition = new Vector3(v, 0f, 0f))
                .OnTargetDestroyed(() => destroyedCallback = true);
            Step(0.1f);
            Object.DestroyImmediate(victim);
            Step(0.1f);
            Assert.That(tween.IsAlive, Is.False);
            Assert.That(destroyedCallback, Is.True);
        }
    }
}
