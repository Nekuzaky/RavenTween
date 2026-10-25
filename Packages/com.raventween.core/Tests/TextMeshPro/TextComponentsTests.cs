using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace RavenTween.Tests {
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
            _go.SetActive(false);
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
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            Step(0.35f);
            Assert.That(text.maxVisibleCharacters, Is.InRange(2, 4));
            writer.Skip();
            Assert.That(text.maxVisibleCharacters, Is.GreaterThanOrEqualTo(text.textInfo.characterCount));
            Assert.That(done, Is.EqualTo(1));
        }

        [Test]
        public void TextEffect_MovesLetters_AndPutsTheTextBackWhenDisabled() {
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            Vector3 before = text.textInfo.meshInfo[0].vertices[0];
            var effect = _go.AddComponent<RavenTextEffect>();
            effect.Strength = 1f;
            effect.ClockOverride = 0.25f;
            effect.Refresh();
            Assert.That((text.textInfo.meshInfo[0].vertices[0] - before).sqrMagnitude, Is.GreaterThan(1e-4f), "The first letter waves.");
            effect.enabled = false;
            Assert.That(text.textInfo.meshInfo[0].vertices[0], Is.EqualTo(before), "Disabled: the text is exactly as TextMeshPro laid it out.");
        }

        [Test]
        public void TextEffect_FadesLettersInAsTheyAppear() {
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            text.maxVisibleCharacters = 0;
            var effect = _go.AddComponent<RavenTextEffect>();
            effect.TextEffect = RavenTextEffect.Effect.None;
            effect.AppearStyle = RavenTextEffect.Appear.Fade;
            effect.ClockOverride = 1f;
            text.maxVisibleCharacters = 1;
            text.ForceMeshUpdate();
            byte early = text.textInfo.meshInfo[0].colors32[0].a;
            effect.ClockOverride = 1.5f;
            effect.Refresh();
            byte late = text.textInfo.meshInfo[0].colors32[0].a;
            Assert.That(early, Is.LessThan(255), "The first letter starts transparent.");
            Assert.That(late, Is.EqualTo(255), "Then it is fully shown.");
        }

        [Test]
        public void TextEffect_WithTypewriter_LettersAppearAsTyped() {
            var effect = _go.AddComponent<RavenTextEffect>();
            effect.TextEffect = RavenTextEffect.Effect.None;
            effect.AppearStyle = RavenTextEffect.Appear.Fade;
            effect.ClockOverride = 0f;
            var writer = _go.AddComponent<RavenTypewriter>();
            writer.LettersPerSecond = 4f;
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            Step(0.6f);
            effect.ClockOverride = 0.6f;
            text.ForceMeshUpdate();
            effect.Refresh();
            int shown = text.maxVisibleCharacters;
            Assert.That(shown, Is.InRange(1, 4));
            TMP_CharacterInfo newest = text.textInfo.characterInfo[shown - 1];
            byte alpha = text.textInfo.meshInfo[newest.materialReferenceIndex].colors32[newest.vertexIndex].a;
            Assert.That(alpha, Is.LessThan(255), "The letter just typed is still fading in.");
        }

        [Test]
        public void TextEffect_ClearingTheText_ShowsNothing() {
            _go.SetActive(true);
            var effect = _go.AddComponent<RavenTextEffect>();
            Assert.That(effect.IsShaping, Is.True);
            var text = _go.GetComponent<TMP_Text>();
            text.text = "";
            text.ForceMeshUpdate();
            effect.Refresh();
            Assert.That(text.textInfo.characterCount, Is.EqualTo(0));
            Assert.That(effect.IsShaping, Is.False, "TextMeshPro keeps the old vertices of an empty text: they must never be uploaded again.");
        }

        [Test]
        public void Counter_ValueSetBeforeFirstActivation_IsKept() {
            var counter = _go.AddComponent<RavenCounter>();
            counter.SetValue(500f);
            _go.SetActive(true);
            Assert.That(counter.Value, Is.EqualTo(500f));
            Assert.That(_go.GetComponent<TMP_Text>().text, Is.EqualTo("500"));
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
