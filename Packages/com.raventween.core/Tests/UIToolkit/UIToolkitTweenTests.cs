using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = NUnit.Framework.Is;

namespace RavenTween.Tests {
    public sealed class UIToolkitTweenTests {
        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
        }

        static void Step(float delta) { TweenEngine.Process(delta, delta); }

        [Test]
        public void Opacity_TranslateScaleRotate_ReachTheirTargets() {
            var element = new VisualElement();
            element.TweenOpacity(0.25f, 1f).Ease(Ease.Linear);
            element.TweenTranslate(new Vector2(40f, -20f), 1f).Ease(Ease.Linear);
            element.TweenScale(2f, 1f).Ease(Ease.Linear);
            element.TweenRotate(90f, 1f).Ease(Ease.Linear);
            Step(0.5f);
            Assert.That(element.style.opacity.value, Is.EqualTo(0.625f).Within(1e-3f));
            Assert.That(element.style.translate.value.x.value, Is.EqualTo(20f).Within(1e-3f));
            Step(0.5f);
            Assert.That(element.style.opacity.value, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(element.style.translate.value.y.value, Is.EqualTo(-20f).Within(1e-4f));
            Assert.That(element.style.scale.value.value.x, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(element.style.rotate.value.angle.ToDegrees(), Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void Colors_AndSize_ReachTheirTargets() {
            var element = new VisualElement();
            element.style.width = 100f;
            element.TweenBackgroundColor(Color.red, 1f);
            element.TweenColor(Color.green, 1f);
            element.TweenWidth(300f, 1f);
            Step(1f);
            Assert.That(element.style.backgroundColor.value, Is.EqualTo(Color.red));
            Assert.That(element.style.color.value, Is.EqualTo(Color.green));
            Assert.That(element.style.width.value.value, Is.EqualTo(300f).Within(1e-3f));
        }

        [Test]
        public void PlayingTweens_AllocateNothing() {
            var element = new VisualElement();
            element.TweenOpacity(0f, 10f).Infinite();
            element.TweenTranslate(new Vector2(5f, 5f), 10f).Infinite();
            element.TweenRotate(45f, 10f).Infinite();
            for (int i = 0; i < 10; i++) { Step(0.016f); }
            Assert.That(() => {
                for (int i = 0; i < 60; i++) { Step(0.016f); }
            }, UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }
    }
}
