using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.Tests {
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

        [Test]
        public void ShowHide_KeptActive_StartHiddenWhileInactive_ShowsAtTheDesignedPose() {
            _canvas = new GameObject("panel-canvas", typeof(Canvas));
            var panelObject = new GameObject("panel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(_canvas.transform, false);
            panelObject.SetActive(false);
            var panel = panelObject.AddComponent<RavenShowHide>();
            panel.DeactivateWhenHidden = false;
            panel.Hide();
            panel.Show();
            Step(0.6f);
            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(panelObject.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(1e-3f), "Hidden was never mistaken for the rest pose.");
            Assert.That(panelObject.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void ShowHide_HideTwice_RaisesOnHiddenOnce() {
            var panel = _go.AddComponent<RavenShowHide>();
            int hidden = 0;
            panel.OnHidden.AddListener(() => hidden++);
            panel.Hide();
            Step(0.6f);
            panel.Hide();
            panel.Hide();
            Assert.That(hidden, Is.EqualTo(1));
        }

        [Test]
        public void Feedback_TurningItsOwnObjectOff_StopsTheRest() {
            var feedback = _go.AddComponent<RavenFeedback>();
            int calls = 0;
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.SetActive, active = false });
            var after = new FeedbackAction { kind = FeedbackAction.Kind.Event };
            after.onPlay.AddListener(() => calls++);
            feedback.Actions.Add(after);
            var later = new FeedbackAction { kind = FeedbackAction.Kind.Event, delay = 0.1f };
            later.onPlay.AddListener(() => calls++);
            feedback.Actions.Add(later);
            feedback.Play();
            Step(0.3f);
            Assert.That(_go.activeSelf, Is.False);
            Assert.That(calls, Is.EqualTo(0), "Nothing runs once the feedback's object is off.");
        }

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
            Assert.That(Time.timeScale, Is.EqualTo(0.05f).Within(1e-5f), "The second hit stop extends the freeze.");
            Step(0.1f);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-5f), "Restored once the last one ends.");
            Raven.HitStop(0.1f, 5f);
            Raven.StopAll();
            TweenEngine.Phase = UpdatePhase.FixedUpdate;
            Raven.TimeScale = 0f;
            Step(5.1f);
            TweenEngine.Phase = UpdatePhase.Update;
            Raven.TimeScale = 1f;
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-5f), "Neither StopAll, the phase nor Raven.TimeScale keep the game frozen.");
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

        [Test]
        public void Feedback_Chance_Zero_NeverPlays() {
            var feedback = _go.AddComponent<RavenFeedback>();
            int calls = 0;
            var never = new FeedbackAction { kind = FeedbackAction.Kind.Event, chance = 0f };
            never.onPlay.AddListener(() => calls++);
            feedback.Actions.Add(never);
            for (int i = 0; i < 20; i++) { feedback.Play(); }
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void Feedback_LightAndFieldOfView_FlashAndComeBack() {
            var light = _go.AddComponent<Light>();
            light.intensity = 2f;
            var cameraObject = new GameObject("feedback-camera", typeof(Camera));
            try {
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 60f;
                var feedback = _go.AddComponent<RavenFeedback>();
                feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.LightFlash, light = light, strength = 1f, duration = 0.4f });
                feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.FieldOfView, camera = camera, strength = 1f, duration = 0.4f });
                feedback.Play();
                Step(0.2f);
                Assert.That(light.intensity, Is.EqualTo(4f).Within(0.05f), "Twice as bright at the peak.");
                Assert.That(camera.fieldOfView, Is.EqualTo(70f).Within(0.2f), "Ten degrees wider at the peak.");
                Step(0.3f);
                Assert.That(light.intensity, Is.EqualTo(2f).Within(1e-3f));
                Assert.That(camera.fieldOfView, Is.EqualTo(60f).Within(1e-3f));
            } finally {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void Feedback_SlowMotion_SlowsThenRestores() {
            Time.timeScale = 1f;
            var feedback = _go.AddComponent<RavenFeedback>();
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.SlowMotion, timeScale = 0.3f, duration = 1f });
            feedback.Play();
            Step(0.5f);
            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(1e-3f), "Held down in the middle.");
            Step(0.6f);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void Feedback_Spawn_CreatesThePrefabAtTheObject() {
            var prefab = new GameObject("spawn-prefab");
            try {
                _go.transform.position = new Vector3(3f, 4f, 0f);
                var feedback = _go.AddComponent<RavenFeedback>();
                feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.Spawn, prefab = prefab, lifetime = 0f });
                feedback.Play();
                GameObject spawned = GameObject.Find("spawn-prefab(Clone)");
                Assert.That(spawned, Is.Not.Null);
                Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(3f, 4f, 0f)));
                Object.DestroyImmediate(spawned);
            } finally {
                Object.DestroyImmediate(prefab);
            }
        }

        RavenPath MakePath(params Vector3[] points) {
            _go.SetActive(false);
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
