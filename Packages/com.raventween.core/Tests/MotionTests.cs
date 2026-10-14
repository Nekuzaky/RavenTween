using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.Tests {
    /// <summary>Ready-made motions, Raven Motion and Raven Stagger.</summary>
    public sealed class MotionTests {
        static readonly Vector3 RestPosition = new Vector3(1f, 2f, 3f);
        static readonly Quaternion RestRotation = Quaternion.Euler(0f, 30f, 0f);
        static readonly Vector3 RestScale = new Vector3(2f, 2f, 2f);

        GameObject _go;
        GameObject _canvas;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _go = new GameObject("motion-target");
            _go.transform.localPosition = RestPosition;
            _go.transform.localRotation = RestRotation;
            _go.transform.localScale = RestScale;
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
            if (_canvas != null) { Object.DestroyImmediate(_canvas); }
        }

        static void Step(float seconds) {
            const float Frame = 1f / 60f;
            for (float t = 0f; t < seconds; t += Frame) { TweenEngine.Process(Frame, Frame); }
        }

        static void AssertAtRest(Transform t, Vector3 position, Quaternion rotation, Vector3 scale, string context) {
            Assert.That(Vector3.Distance(t.localPosition, position), Is.LessThan(1e-3f), context + ": position");
            Assert.That(Quaternion.Angle(t.localRotation, rotation), Is.LessThan(0.05f), context + ": rotation");
            Assert.That(Vector3.Distance(t.localScale, scale), Is.LessThan(1e-3f), context + ": scale");
        }

        void ResetPose() {
            TweenEngine.KillAll(false);
            _go.transform.localPosition = RestPosition;
            _go.transform.localRotation = RestRotation;
            _go.transform.localScale = RestScale;
        }

        // ----- Every preset on a scene object -----

        [Test]
        public void EntrancesAndAttention_EndExactlyOnTheRestPose() {
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                MotionCategory category = MotionLibrary.Category(preset);
                if (category != MotionCategory.Entrance && category != MotionCategory.Attention) { continue; }
                ResetPose();
                Sequence motion = Raven.Motion(_go.transform, preset);
                Assert.That(motion.IsAlive, Is.True, preset + " starts");
                Step(MotionLibrary.DefaultDuration(preset) + 0.1f);
                Assert.That(motion.IsAlive, Is.False, preset + " finishes");
                AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, preset.ToString());
            }
        }

        [Test]
        public void Entrances_HideTheObjectAtOnce() {
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                if (MotionLibrary.Category(preset) != MotionCategory.Entrance) { continue; }
                ResetPose();
                Raven.Motion(_go.transform, preset, new MotionSettings { intensity = 1f, delay = 0.5f, repeat = 1 });
                Transform t = _go.transform;
                bool moved = Vector3.Distance(t.localPosition, RestPosition) > 1e-3f
                    || Quaternion.Angle(t.localRotation, RestRotation) > 0.05f
                    || Vector3.Distance(t.localScale, RestScale) > 1e-3f;
                Assert.That(moved, Is.True, preset + " shows its start pose during the delay, not the rest pose");
            }
        }

        [Test]
        public void Exits_FinishAwayFromTheRestPose() {
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                if (MotionLibrary.Category(preset) != MotionCategory.Exit) { continue; }
                ResetPose();
                Sequence motion = Raven.Motion(_go.transform, preset);
                Step(MotionLibrary.DefaultDuration(preset) + 0.1f);
                Assert.That(motion.IsAlive, Is.False, preset + " finishes");
                Transform t = _go.transform;
                bool hidden = Vector3.Distance(t.localPosition, RestPosition) > 1e-3f
                    || Quaternion.Angle(t.localRotation, RestRotation) > 0.05f
                    || Vector3.Distance(t.localScale, RestScale) > 1e-3f;
                Assert.That(hidden, Is.True, preset + " leaves the rest pose (no alpha here, so it must move, turn or shrink)");
            }
        }

        [Test]
        public void Loops_ComeBackToTheRestPoseEveryCycle() {
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                if (MotionLibrary.Category(preset) != MotionCategory.Loop) { continue; }
                ResetPose();
                float cycle = MotionLibrary.DefaultDuration(preset);
                Sequence motion = Raven.Motion(_go.transform, preset);
                TweenEngine.Process(cycle * 0.4f, cycle * 0.4f);
                TweenEngine.Process(cycle * 2.6f, cycle * 2.6f);
                Assert.That(motion.IsAlive, Is.True, preset + " keeps looping");
                AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, preset + " after 3 cycles");
            }
        }

        // ----- UI -----

        RectTransform MakeUi(float alpha) {
            _canvas = new GameObject("motion-canvas", typeof(Canvas));
            var panel = new GameObject("panel", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(_canvas.transform, false);
            rect.sizeDelta = new Vector2(200f, 100f);
            rect.anchoredPosition = new Vector2(30f, 40f);
            panel.GetComponent<CanvasGroup>().alpha = alpha;
            return rect;
        }

        [Test]
        public void UiMotions_UseAnchoredPositionAndCanvasGroupAlpha() {
            RectTransform rect = MakeUi(0.6f);
            var group = rect.GetComponent<CanvasGroup>();
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                MotionCategory category = MotionLibrary.Category(preset);
                if (category == MotionCategory.Loop) { continue; }
                TweenEngine.KillAll(false);
                rect.anchoredPosition = new Vector2(30f, 40f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                group.alpha = 0.6f;
                Raven.Motion(rect, preset);
                Step(MotionLibrary.DefaultDuration(preset) + 0.1f);
                if (category == MotionCategory.Exit) {
                    Assert.That(group.alpha, Is.LessThan(0.01f), preset + " fades the panel out");
                } else {
                    Assert.That(Vector2.Distance(rect.anchoredPosition, new Vector2(30f, 40f)), Is.LessThan(1e-2f), preset + ": anchored position");
                    Assert.That(group.alpha, Is.EqualTo(0.6f).Within(1e-3f), preset + " restores the panel's own alpha");
                }
            }
        }

        // ----- Raven Motion -----

        [Test]
        public void RavenMotion_PlaysOnEnable_AndARetriggerDoesNotDrift() {
            var motion = _go.AddComponent<RavenMotion>(); // Pop In, when enabled: already playing.
            Assert.That(motion.IsPlaying, Is.True);
            Step(0.15f);
            motion.Play();
            Step(0.6f);
            AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, "after a retrigger");
        }

        [Test]
        public void RavenMotion_ExitThenEntrance_SharesTheRestPose() {
            var popIn = _go.AddComponent<RavenMotion>();
            popIn.Stop();
            popIn.Trigger = MotionTrigger.Manual;
            var popOut = _go.AddComponent<RavenMotion>();
            popOut.Stop();
            popOut.Trigger = MotionTrigger.Manual;
            popOut.Preset = MotionPreset.PopOut;
            popOut.Play();
            Step(0.6f);
            Assert.That(_go.transform.localScale.x, Is.LessThan(0.01f), "Hidden after the exit.");
            popIn.Play();
            Step(0.6f);
            AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, "entrance after an exit");
        }

        [Test]
        public void RavenMotion_DeactivateWhenFinished_RestoresTheRestPose() {
            var motion = _go.AddComponent<RavenMotion>();
            motion.Stop();
            motion.Preset = MotionPreset.ZoomOut;
            motion.WhenFinished = MotionFinish.Deactivate;
            motion.Play();
            Step(0.7f);
            Assert.That(_go.activeSelf, Is.False);
            AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, "deactivated object, ready to be shown again");
        }

        [Test]
        public void RavenMotion_PointerRelay_OnlyForPointerTriggers() {
            var motion = _go.AddComponent<RavenMotion>();
            Assert.That(_go.GetComponent<RavenMotionPointerRelay>(), Is.Null, "No relay: it would swallow clicks meant for parents.");
            motion.Trigger = MotionTrigger.OnPointerEnter;
            Assert.That(_go.GetComponent<RavenMotionPointerRelay>(), Is.Not.Null);
        }

        [Test]
        public void RavenMotion_Stop_PutsALoopBackOnTheRestPose() {
            var motion = _go.AddComponent<RavenMotion>();
            motion.Stop();
            motion.Preset = MotionPreset.Float;
            motion.Play();
            Step(0.5f);
            Assert.That(Vector3.Distance(_go.transform.localPosition, RestPosition), Is.GreaterThan(1e-3f));
            motion.Stop();
            AssertAtRest(_go.transform, RestPosition, RestRotation, RestScale, "stopped loop");
        }

        // ----- Raven Button Feel -----

        [Test]
        public void RavenButtonFeel_GrowsSinksAndSettles() {
            var feel = _go.AddComponent<RavenButtonFeel>(); // Bouncy: 1.06 hover, 0.94 pressed.
            feel.OnPointerEnter(null);
            Step(0.3f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(RestScale.x * 1.06f).Within(1e-3f), "hover");
            feel.OnPointerDown(null);
            Step(0.3f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(RestScale.x * 0.94f).Within(1e-3f), "pressed");
            feel.OnPointerUp(null);
            feel.OnPointerExit(null);
            Step(0.3f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(RestScale.x).Within(1e-3f), "back at rest");
        }

        [Test]
        public void RavenButtonFeel_IgnoresANonInteractableButton() {
            _canvas = new GameObject("feel-canvas", typeof(Canvas));
            var button = new GameObject("button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(_canvas.transform, false);
            button.GetComponent<Button>().interactable = false;
            var feel = button.AddComponent<RavenButtonFeel>();
            feel.OnPointerEnter(null);
            Step(0.3f);
            Assert.That(button.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f));
        }

        // ----- Raven Stagger -----

        [Test]
        public void RavenStagger_StartsChildrenOneAfterAnother() {
            var children = new Transform[3];
            for (int i = 0; i < children.Length; i++) {
                children[i] = new GameObject("child-" + i).transform;
                children[i].SetParent(_go.transform, false);
            }
            var stagger = _go.AddComponent<RavenStagger>(); // Rise In, 0.06 s apart, when enabled.
            Assert.That(stagger.IsPlaying, Is.True);
            TweenEngine.Process(0.07f, 0.07f);
            Assert.That(children[0].localPosition.y, Is.GreaterThan(children[2].localPosition.y), "The first child is ahead of the last.");
            Step(1f);
            for (int i = 0; i < children.Length; i++) {
                Assert.That(children[i].localPosition.sqrMagnitude, Is.LessThan(1e-6f), "Child " + i + " ends at rest.");
            }
            Assert.That(stagger.IsPlaying, Is.False);
        }
    }
}
