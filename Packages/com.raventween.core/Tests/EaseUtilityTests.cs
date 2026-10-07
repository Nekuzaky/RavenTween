using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class EaseUtilityTests {
        static readonly Ease[] AllStandardEases = {
            Ease.Linear,
            Ease.InSine, Ease.OutSine, Ease.InOutSine,
            Ease.InQuad, Ease.OutQuad, Ease.InOutQuad,
            Ease.InCubic, Ease.OutCubic, Ease.InOutCubic,
            Ease.InQuart, Ease.OutQuart, Ease.InOutQuart,
            Ease.InQuint, Ease.OutQuint, Ease.InOutQuint,
            Ease.InExpo, Ease.OutExpo, Ease.InOutExpo,
            Ease.InCirc, Ease.OutCirc, Ease.InOutCirc,
            Ease.InBack, Ease.OutBack, Ease.InOutBack,
            Ease.InElastic, Ease.OutElastic, Ease.InOutElastic,
            Ease.InBounce, Ease.OutBounce, Ease.InOutBounce
        };

        [Test]
        public void EveryEase_StartsAtZero_EndsAtOne() {
            foreach (Ease ease in AllStandardEases) {
                Assert.That(EaseUtility.Evaluate(ease, 0f), Is.EqualTo(0f).Within(1e-4f), ease + " at t=0");
                Assert.That(EaseUtility.Evaluate(ease, 1f), Is.EqualTo(1f).Within(1e-4f), ease + " at t=1");
            }
        }

        [Test]
        public void EveryEase_IsFiniteAcrossTheDomain() {
            foreach (Ease ease in AllStandardEases) {
                for (int i = 0; i <= 100; i++) {
                    float value = EaseUtility.Evaluate(ease, i / 100f);
                    Assert.That(float.IsNaN(value), Is.False, ease + " produced NaN");
                    Assert.That(float.IsInfinity(value), Is.False, ease + " produced infinity");
                }
            }
        }

        [Test]
        public void Linear_IsIdentity() {
            Assert.That(EaseUtility.Evaluate(Ease.Linear, 0.25f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(EaseUtility.Evaluate(Ease.Linear, 0.75f), Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void Evaluate_ClampsOutOfRangeInput() {
            Assert.That(EaseUtility.Evaluate(Ease.OutQuad, -1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(EaseUtility.Evaluate(Ease.OutQuad, 2f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void TweenValue_Lerp_InterpolatesVectors() {
            var from = new TweenValue(new Vector3(0f, 0f, 0f));
            var to = new TweenValue(new Vector3(10f, 20f, 30f));
            TweenValue mid = TweenValue.Lerp(from, to, 0.5f);
            Assert.That(mid.Vector3, Is.EqualTo(new Vector3(5f, 10f, 15f)));
        }

        [Test]
        public void TweenValue_Lerp_SlerpsQuaternions() {
            var from = new TweenValue(Quaternion.identity);
            var to = new TweenValue(Quaternion.Euler(0f, 90f, 0f));
            TweenValue mid = TweenValue.Lerp(from, to, 1f);
            Assert.That(Quaternion.Angle(mid.Quaternion, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
        }
    }
}
