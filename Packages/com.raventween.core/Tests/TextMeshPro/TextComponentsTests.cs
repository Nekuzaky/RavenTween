using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

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

        [UnityTest]
        public IEnumerator TextEffect_MovesLetters_AndPutsTheTextBackWhenDisabled() {
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            Vector3 before = text.textInfo.meshInfo[0].vertices[0];
            var effect = _go.AddComponent<RavenTextEffect>(); // Wave.
            effect.Strength = 1f;
            bool moved = false;
            for (int frame = 0; frame < 30 && !moved; frame++) {
                yield return null;
                moved = (text.textInfo.meshInfo[0].vertices[0] - before).sqrMagnitude > 1e-4f;
            }
            Assert.That(moved, Is.True, "The first letter waves.");
            effect.enabled = false;
            Assert.That(text.textInfo.meshInfo[0].vertices[0], Is.EqualTo(before), "Disabled: the text is exactly as TextMeshPro laid it out.");
        }

        [UnityTest]
        public IEnumerator TextEffect_FadesLettersInAsTheyAppear() {
            _go.SetActive(true);
            var text = _go.GetComponent<TMP_Text>();
            text.maxVisibleCharacters = 0;
            var effect = _go.AddComponent<RavenTextEffect>();
            effect.TextEffect = RavenTextEffect.Effect.None;
            effect.AppearStyle = RavenTextEffect.Appear.Fade;
            yield return null;
            text.maxVisibleCharacters = 1;
            yield return null;
            yield return null;
            byte early = text.textInfo.meshInfo[0].colors32[0].a;
            float end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end) { yield return null; }
            byte late = text.textInfo.meshInfo[0].colors32[0].a;
            Assert.That(early, Is.LessThan(255), "The first letter starts transparent.");
            Assert.That(late, Is.EqualTo(255), "Then it is fully shown.");
        }

        [UnityTest]
        public IEnumerator TextEffect_WithTypewriter_LettersAppearAsTyped() {
            var effect = _go.AddComponent<RavenTextEffect>();
            effect.TextEffect = RavenTextEffect.Effect.None;
            effect.AppearStyle = RavenTextEffect.Appear.Fade;
            var writer = _go.AddComponent<RavenTypewriter>();
            writer.LettersPerSecond = 4f;
            _go.SetActive(true); // Types from the start; the text was fully visible before.
            var text = _go.GetComponent<TMP_Text>();
            float end = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < end) { yield return null; }
            int shown = text.maxVisibleCharacters;
            Assert.That(shown, Is.InRange(1, 4));
            TMP_CharacterInfo newest = text.textInfo.characterInfo[shown - 1];
            byte alpha = text.textInfo.meshInfo[newest.materialReferenceIndex].colors32[newest.vertexIndex].a;
            Assert.That(alpha, Is.LessThan(255), "The letter just typed is still fading in.");
        }

        [UnityTest]
        public IEnumerator TextEffect_ClearingTheText_ShowsNothing() {
            _go.SetActive(true);
            var effect = _go.AddComponent<RavenTextEffect>();
            yield return null;
            Assert.That(effect.IsShaping, Is.True);
            var text = _go.GetComponent<TMP_Text>();
            text.text = "";
            yield return null;
            yield return null;
            Assert.That(text.textInfo.characterCount, Is.EqualTo(0));
            Assert.That(effect.IsShaping, Is.False, "TextMeshPro keeps the old vertices of an empty text: they must never be uploaded again.");
        }

        [Test]
        public void Counter_ValueSetBeforeFirstActivation_IsKept() {
            var counter = _go.AddComponent<RavenCounter>(); // Inactive: Awake has not run yet.
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
