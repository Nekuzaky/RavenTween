using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class TextMeshProTweenTests {
        GameObject _go;
        TMP_Text _text;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _go = new GameObject("tmp-target", typeof(RectTransform));
            _text = _go.AddComponent<TextMeshProUGUI>();
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
        public void FontSize_ReachesTarget() {
            _text.fontSize = 10f;
            _text.TweenFontSize(30f, 1f);
            Step(0.5f);
            Assert.That(_text.fontSize, Is.EqualTo(20f).Within(1e-3f));
            Step(0.5f);
            Assert.That(_text.fontSize, Is.EqualTo(30f).Within(1e-3f));
        }

        [Test]
        public void MaxVisibleCharacters_StepsThroughWholeCounts() {
            _text.TweenMaxVisibleCharacters(0, 10, 1f);
            Step(0.5f);
            Assert.That(_text.maxVisibleCharacters, Is.EqualTo(5));
            Step(0.5f);
            Assert.That(_text.maxVisibleCharacters, Is.EqualTo(10));
        }

        [Test]
        public void CharacterSpacing_ReachesTarget() {
            _text.characterSpacing = 0f;
            _text.TweenCharacterSpacing(8f, 1f);
            Step(1f);
            Assert.That(_text.characterSpacing, Is.EqualTo(8f).Within(1e-3f));
        }

        [Test]
        public void Number_WritesFormattedValue() {
            if (TMP_Settings.instance == null) {
                Assert.Ignore("TMP Essential Resources are not imported; SetText needs TMP Settings.");
            }
            _text.TweenNumber(0f, 100f, 1f);
            Step(1f);
            Assert.That(_text.text, Is.EqualTo("100"));
        }

        [Test]
        public void Tween_DiesWithDestroyedText() {
            Tween tween = _text.TweenFontSize(30f, 5f);
            Step(0.1f);
            Object.DestroyImmediate(_go);
            Step(0.1f);
            Assert.That(tween.IsAlive, Is.False);
        }
    }
}
