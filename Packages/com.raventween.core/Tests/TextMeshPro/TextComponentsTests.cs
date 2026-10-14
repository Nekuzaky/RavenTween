using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>Raven Typewriter and Raven Counter.</summary>
    public sealed class TextComponentsTests {
        GameObject _go;
        GameObject _canvas;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null) {
                Assert.Ignore("TMP Essential Resources are not imported; text layout needs a font asset.");
            }
            _canvas = new GameObject("text-canvas", typeof(Canvas));
            _go = new GameObject("text", typeof(RectTransform));
            _go.transform.SetParent(_canvas.transform, false);
            _go.SetActive(false); // Configure before OnEnable.
            _go.AddComponent<TextMeshProUGUI>().text = "Hello world";
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            if (_canvas != null) { Object.DestroyImmediate(_canvas); }
        }

        static void Step(float seconds) {
            const float Frame = 1f / 60f;
            for (float t = 0f; t < seconds; t += Frame) { TweenEngine.Process(Frame, Frame); }
        }

        [Test]
        public void Typewriter_RevealsLettersThenEverything() {
            var writer = _go.AddComponent<RavenTypewriter>();
            writer.LettersPerSecond = 10f;
            int done = 0;
            writer.OnComplete.AddListener(() => done++);
            _go.SetActive(true); // Types when enabled.
            var text = _go.GetComponent<TMP_Text>();
            Step(0.35f);
            Assert.That(text.maxVisibleCharacters, Is.InRange(2, 4));
            writer.Skip();
            Assert.That(text.maxVisibleCharacters, Is.GreaterThanOrEqualTo(text.textInfo.characterCount));
            Assert.That(done, Is.EqualTo(1));
        }

        [Test]
        public void Counter_CountsToTheTarget_AndAddKeepsCounting() {
            var counter = _go.AddComponent<RavenCounter>();
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            counter.SetValue(0f);
            counter.CountTo(100f);
            Step(0.4f);
            counter.Add(50f);
            Step(1.2f);
            Assert.That(counter.Value, Is.EqualTo(150f).Within(1e-3f));
            Assert.That(text.text, Is.EqualTo("150"));
        }
    }
}
