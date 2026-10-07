using NUnit.Framework;
using RavenTween.DOTweenAdapter;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>DOTween-style code running on RavenTween through the adapter.</summary>
    public sealed class DOTweenAdapterTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            _go = new GameObject("adapter-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
        }

        static void Step(float delta) { TweenEngine.Process(delta, delta); }

        [Test]
        public void ShortcutsAndSettings_MapToRavenTween() {
            Tween tween = _go.transform.DOMoveX(4f, 1f).SetEase(Ease.Linear).SetLoops(2, LoopType.Yoyo).SetDelay(0f);
            Assert.That(tween.CyclesTotal, Is.EqualTo(2));
            Step(0.5f);
            Assert.That(_go.transform.position.x, Is.EqualTo(2f).Within(1e-4f));
            Step(1f);
            Assert.That(_go.transform.position.x, Is.EqualTo(2f).Within(1e-4f), "Halfway back on the yoyo cycle.");
        }

        [Test]
        public void Sequence_AppendJoinIntervalCallback() {
            int calls = 0;
            DOTween.Sequence()
                .Append(_go.transform.DOMoveX(1f, 1f).SetEase(Ease.Linear))
                .Join(_go.transform.DOScale(2f, 1f).SetEase(Ease.Linear))
                .AppendInterval(0.5f)
                .AppendCallback(() => calls++);
            Step(1.6f);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(_go.transform.position.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_go.transform.localScale.x, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void DOKill_StopsTheTargetsTweens() {
            _go.transform.DOMove(Vector3.one, 1f);
            _go.transform.DOScale(2f, 1f);
            Assert.That(_go.transform.DOKill(), Is.EqualTo(2));
            Assert.That(Raven.CountTweens(_go.transform), Is.EqualTo(0));
        }

        [Test]
        public void DOTweenTo_DrivesAGetterAndSetter() {
            float value = 0f;
            DOTween.To(() => value, v => value = v, 10f, 1f);
            Step(0.5f);
            Assert.That(value, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void DelayedCall_IgnoresTimeScaleByDefault() {
            bool fired = false;
            DOVirtual.DelayedCall(0.5f, () => fired = true);
            TweenEngine.Process(0f, 0.6f);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void SetEaseWithOvershoot_UsesTheParametricEase() {
            float v = 0f;
            DOVirtual.Float(0f, 1f, 1f, x => v = x).SetEase(Ease.OutBack, 1.70158f);
            Step(0.5f);
            Assert.That(v, Is.EqualTo(EaseUtility.Evaluate(Ease.OutBack, 0.5f)).Within(1e-4f));
        }
    }
}
