using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.Tests {
    /// <summary>Tint, Motion Styles, chaining, Show Hide, Feedback, hit stop and Path.</summary>
    public sealed class NoCodeComponentTests {
        GameObject _go;
        GameObject _canvas;
        MotionStyle _style;
        float _timeScale;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _timeScale = Time.timeScale;
            _go = new GameObject("no-code-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Time.timeScale = _timeScale;
            Object.DestroyImmediate(_go);
            if (_canvas != null) { Object.DestroyImmediate(_canvas); }
            if (_style != null) { Object.DestroyImmediate(_style); }
        }

        static void Step(float seconds) {
            const float Frame = 1f / 60f;
            for (float t = 0f; t < seconds; t += Frame) { TweenEngine.Process(Frame, Frame); }
        }

        // ----- Tint -----

        [Test]
        public void Tint_FlashesTheColorAndComesBackExactly() {
            var sprite = _go.AddComponent<SpriteRenderer>();
            var rest = new Color(0.2f, 0.6f, 0.9f, 0.8f);
            sprite.color = rest;
            Raven.Motion(sprite, MotionPreset.Tint, new MotionSettings { intensity = 1f, color = Color.red, repeat = 1 });
            Step(0.08f);
            Assert.That(sprite.color.r, Is.GreaterThan(rest.r + 0.3f), "Turns toward red.");
            Assert.That(sprite.color.a, Is.EqualTo(rest.a).Within(1e-3f), "Keeps its own alpha.");
            Step(0.6f);
            Assert.That(sprite.color, Is.EqualTo(rest), "Back to the exact rest color.");
        }

        [Test]
        public void Tint_OnAnImage_UsesTheGraphicColor() {
            _canvas = new GameObject("tint-canvas", typeof(Canvas));
            var image = new GameObject("image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(_canvas.transform, false);
            image.color = Color.white;
            Raven.Motion(image, MotionPreset.Tint);
            Step(0.08f);
            Assert.That(image.color.g, Is.LessThan(0.8f), "White turns toward the default red.");
            Step(0.6f);
            Assert.That(image.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void Tint_WithoutAnythingToColor_PulsesInstead() {
            Sequence motion = Raven.Motion(_go.transform, MotionPreset.Tint);
            Step(0.25f);
            Assert.That(_go.transform.localScale.x, Is.GreaterThan(1.01f));
            Step(0.5f);
            Assert.That(motion.IsAlive, Is.False);
            Assert.That(_go.transform.localScale, Is.EqualTo(Vector3.one));
        }

        // ----- Motion Styles and chaining -----

        [Test]
        public void MotionStyle_ReplacesTheComponentsOwnPreset() {
            _style = ScriptableObject.CreateInstance<MotionStyle>();
            _style.Preset = MotionPreset.Float;
            var motion = _go.AddComponent<RavenMotion>();
            motion.Stop();
            motion.Trigger = MotionTrigger.Manual;
            motion.Style = _style;
            Assert.That(motion.EffectivePreset, Is.EqualTo(MotionPreset.Float));
            motion.Play();
            Step(0.6f);
            Assert.That(_go.transform.localPosition.y, Is.Not.EqualTo(0f).Within(1e-4f), "Floats, as the style says.");
            Assert.That(_go.transform.localScale, Is.EqualTo(Vector3.one), "Not the component's own Pop In.");
        }

        [Test]
        public void ThenPlay_StartsTheNextMotion() {
            var first = _go.AddComponent<RavenMotion>();
            first.Stop();
            first.Trigger = MotionTrigger.Manual;
            var second = _go.AddComponent<RavenMotion>();
            second.Stop();
            second.Trigger = MotionTrigger.Manual;
            second.Preset = MotionPreset.Float;
            first.ThenPlay = second;
            first.Play();
            Assert.That(second.IsPlaying, Is.False);
            Step(0.6f);
            Assert.That(first.IsPlaying, Is.False);
            Assert.That(second.IsPlaying, Is.True, "Pop In then Float.");
        }

        // ----- Show Hide -----

        [Test]
        public void ShowHide_HidesThenShowsAtTheDesignedPose() {
            _go.transform.localPosition = new Vector3(1f, 2f, 3f);
            var panel = _go.AddComponent<RavenShowHide>();
            panel.Hide();
            Assert.That(panel.IsShown, Is.False);
            Step(0.6f);
            Assert.That(_go.activeSelf, Is.False, "Deactivated once hidden.");
            panel.Show();
            Assert.That(_go.activeSelf, Is.True);
            Step(0.6f);
            Assert.That(_go.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(_go.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            panel.Toggle();
            Assert.That(panel.IsShown, Is.False);
        }

        [Test]
        public void ShowHide_EventsFire() {
            var panel = _go.AddComponent<RavenShowHide>();
            int shown = 0, hidden = 0;
            panel.OnShown.AddListener(() => shown++);
            panel.OnHidden.AddListener(() => hidden++);
            panel.Hide();
            Step(0.6f);
            panel.Show();
            Step(0.6f);
            Assert.That(hidden, Is.EqualTo(1));
            Assert.That(shown, Is.EqualTo(1));
        }

        // ----- Feedback and hit stop -----

        [Test]
        public void Feedback_RunsEachActionAfterItsDelay() {
            var feedback = _go.AddComponent<RavenFeedback>();
            int calls = 0;
            var now = new FeedbackAction { kind = FeedbackAction.Kind.Punch, duration = 0.3f };
            var later = new FeedbackAction { kind = FeedbackAction.Kind.Event, delay = 0.2f };
            later.onPlay.AddListener(() => calls++);
            feedback.Actions.Add(now);
            feedback.Actions.Add(later);
            feedback.Play();
            Step(0.05f);
            Assert.That(_go.transform.localScale.x, Is.Not.EqualTo(1f).Within(1e-4f), "The punch starts at once.");
            Step(0.05f);
            Assert.That(calls, Is.EqualTo(0), "The event waits for its delay.");
            Step(0.25f);
            Assert.That(calls, Is.EqualTo(1));
            Step(0.5f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f), "The punch settles back.");
        }

        [Test]
        public void HitStop_RestoresTheTimeScale_EvenWhenOverlappedOrStopped() {
            Time.timeScale = 1f;
            Raven.HitStop(0.1f, 0.2f);
            Assert.That(Time.timeScale, Is.EqualTo(0.1f).Within(1e-5f));
            Step(0.1f);
            Raven.HitStop(0.05f, 0.2f);
            Step(0.15f);
            Assert.That(Time.timeScale, Is.EqualTo(0.05f).Within(1e-5f), "The second hit stop is still running.");
            Step(0.1f);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-5f), "Restored once the last one ends.");
            Raven.HitStop(0.1f, 5f);
            Raven.StopAll();
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-5f), "Stopping every tween never leaves the game frozen.");
        }

        [Test]
        public void Feedback_Cooldown_IgnoresRapidRepeats() {
            var feedback = _go.AddComponent<RavenFeedback>();
            var count = new FeedbackAction { kind = FeedbackAction.Kind.Event };
            int calls = 0;
            count.onPlay.AddListener(() => calls++);
            feedback.Actions.Add(count);
            feedback.Cooldown = 10f;
            feedback.Play();
            feedback.Play();
            Assert.That(calls, Is.EqualTo(1));
        }

        // ----- Path -----

        RavenPath MakePath(params Vector3[] points) {
            _go.SetActive(false); // Configure before OnEnable plays it.
            var path = _go.AddComponent<RavenPath>();
            path.Points.Clear();
            path.Points.AddRange(points);
            path.Smooth = false;
            return path;
        }

        [Test]
        public void Path_TravelsAtConstantSpeed() {
            _go.transform.localPosition = new Vector3(1f, 0f, 0f);
            RavenPath path = MakePath(new Vector3(2f, 0f, 0f), new Vector3(2f, 2f, 0f));
            Assert.That(path.Length, Is.EqualTo(4f).Within(1e-3f));
            Assert.That(path.Evaluate(0.75f), Is.EqualTo(new Vector3(3f, 1f, 0f)), "Three quarters of the length: halfway up the second leg.");
            Assert.That(path.Evaluate(1f), Is.EqualTo(new Vector3(3f, 2f, 0f)));
        }

        [Test]
        public void Path_PlaysFromWhereTheObjectStands() {
            RavenPath path = MakePath(new Vector3(4f, 0f, 0f));
            Tween trip = path.CreateTween().Ease(Ease.Linear);
            Step(trip.Duration * 0.5f);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(2f).Within(0.1f));
            Step(trip.Duration);
            Assert.That(_go.transform.localPosition, Is.EqualTo(new Vector3(4f, 0f, 0f)));
        }

        [Test]
        public void Path_Closed_ComesBackToTheStart() {
            RavenPath path = MakePath(new Vector3(2f, 0f, 0f), new Vector3(2f, 2f, 0f), new Vector3(0f, 2f, 0f));
            path.Closed = true;
            Assert.That(path.Length, Is.EqualTo(8f).Within(1e-3f));
            Assert.That(path.Evaluate(1f), Is.EqualTo(Vector3.zero));
        }
    }
}
