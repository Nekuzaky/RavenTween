using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RavenTween.Tests {
    /// <summary>One test per engine finding of the robustness audit; each failed before its fix.</summary>
    public sealed class AuditRegressionTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _go = new GameObject("audit-target");
        }

        [TearDown]
        public void TearDown() {
            LogAssert.ignoreFailingMessages = false;
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
        }

        static void Step(float delta, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(delta, delta); }
        }

        // ----- Reentrancy -----

        [Test]
        public void CompleteSelf_FromOnUpdate_DoesNotRecurse() {
            int completions = 0;
            Tween t = default;
            t = Raven.Value(0f, 1f, 1f).OnUpdate(v => { if (v > 0.05f) { t.Complete(); } }).OnComplete(() => completions++);
            Assert.DoesNotThrow(() => Step(0.1f));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(t.IsAlive, Is.False);
        }

        [Test]
        public void CompleteAll_FromOnComplete_DoesNotRecurse() {
            int completions = 0;
            Raven.Value(0f, 1f, 0.1f).OnComplete(() => { completions++; Raven.CompleteAll(); });
            Assert.DoesNotThrow(() => Step(0.2f));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void StopAll_FromOnKill_DoesNotRecurse() {
            int kills = 0;
            Tween t = Raven.Value(0f, 1f, 1f).OnKill(() => { kills++; Raven.StopAll(); });
            Assert.DoesNotThrow(() => t.Stop());
            Assert.That(kills, Is.EqualTo(1));
        }

        [Test]
        public void TweenCreatedInCallback_AfterStopAll_StaysAlive() {
            Tween created = default;
            Raven.Value(0f, 1f, 0.1f).OnComplete(() => { Raven.StopAll(); created = Raven.Value(0f, 1f, 5f); });
            Step(0.2f);
            Assert.That(created.IsAlive, Is.True, "A tween created in OnComplete must not be released by the engine.");
        }

        [Test]
        public void StopSelf_InOnStart_FiresNoUpdateOrComplete() {
            bool updated = false, completed = false;
            Tween t = default;
            t = Raven.Value(0f, 1f, 0.05f).OnStart(() => t.Stop()).OnUpdate(() => updated = true).OnComplete(() => completed = true);
            Step(0.1f);
            Assert.That(updated, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void TweenCreatedDuringProcess_IsNotSteppedThatFrame() {
            float value = -1f;
            var a = Raven.Value(0f, 1f, 0.1f);
            Raven.Value(0f, 1f, 5f);
            Tween c = Raven.Value(0f, 1f, 5f);
            c.Stop(); // frees a slot with a higher index than a
            a.OnComplete(() => Raven.Value(0f, 10f, 1f).OnUpdate(v => value = v));
            Step(0.2f);
            Assert.That(value, Is.EqualTo(-1f), "A new tween must start on the next frame, whatever slot it reuses.");
            Step(0.5f);
            Assert.That(value, Is.EqualTo(5f).Within(1e-3f));
        }

        // ----- Await on sequence children -----

        [Test]
        public void AwaitingSequenceChild_Resumes_WhenSequenceEnds() {
            bool resumed = false;
            Tween child = Raven.Value(0f, 1f, 0.5f);
            Raven.Sequence().Chain(child);
            child.GetAwaiter().OnCompleted(() => resumed = true);
            Step(0.1f, 10);
            Assert.That(resumed, Is.True);
        }

        // ----- Exception isolation -----

        [Test]
        public void ThrowingEase_StopsOnlyThatTween_EngineKeepsRunning() {
            LogAssert.ignoreFailingMessages = true;
            Raven.Value(0f, 1f, 1f).Ease(x => throw new InvalidOperationException("boom"));
            Step(0.1f);
            float v = -1f;
            Raven.Value(0f, 10f, 1f).OnUpdate(x => v = x);
            Step(0.5f);
            Assert.That(v, Is.EqualTo(5f).Within(1e-3f), "The engine must keep running after a tween throws.");
        }

        [Test]
        public void TemplateWithWrongTarget_IsRejected_NotCrashing() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.CanvasGroupAlpha;
            LogAssert.ignoreFailingMessages = true;
            Tween t = template.Play(_go.transform);
            Assert.That(t.IsAlive, Is.False, "A Transform cannot drive a CanvasGroup alpha.");
            Object.DestroyImmediate(template);
        }

        [Test]
        public void TemplateWithGameObject_ResolvesTheComponent() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.LocalPosition;
            template.endValue = new Vector4(0f, 2f, 0f, 0f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.1f;
            Tween t = template.Play(_go);
            Assert.That(t.IsAlive, Is.True);
            Step(0.2f);
            Assert.That(_go.transform.localPosition.y, Is.EqualTo(2f).Within(1e-3f));
            Object.DestroyImmediate(template);
        }

        // ----- Sequence structure -----

        [Test]
        public void SequenceContainingItself_IsRejected() {
            Sequence s = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f));
            LogAssert.ignoreFailingMessages = true;
            s.Chain(s);
            Raven.StopAll();
            Assert.That(Raven.AliveCount, Is.EqualTo(0), "Nothing may leak after StopAll.");
        }

        [Test]
        public void MutuallyNestedSequences_AreRejected() {
            Sequence a = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f));
            Sequence b = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f));
            LogAssert.ignoreFailingMessages = true;
            a.Chain(b);
            b.Chain(a);
            Raven.StopAll();
            Assert.That(Raven.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void ChangingCyclesOfSequenceChild_IsRejected() {
            int completions = 0;
            Tween t = Raven.Value(0f, 1f, 1f).OnComplete(() => completions++);
            Raven.Sequence().Chain(t);
            LogAssert.ignoreFailingMessages = true;
            t.Cycles(2);
            Step(0.1f, 12);
            Assert.That(completions, Is.EqualTo(1), "The child must still complete with its original length.");
        }

        [Test]
        public void CompleteYoyoSequence_EndsAtStart() {
            Sequence s = Raven.Sequence()
                .Chain(Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(_go.transform, new Vector3(1f, 1f, 0f), 1f))
                .Cycles(2, CycleMode.Yoyo);
            Step(0.1f, 15);
            s.Complete();
            Assert.That(_go.transform.localPosition.magnitude, Is.LessThan(1e-3f), "Yoyo x2 completes back at the start.");
        }

        [Test]
        public void InsertedOutOfOrder_SamePropertyFollowsTimeline() {
            Raven.Sequence()
                .Insert(1f, Raven.LocalPosition(_go.transform, new Vector3(1f, 1f, 0f), 1f))
                .Insert(0f, Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f));
            Step(0.1f, 5);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(0.5f, 0f, 0f)), Is.LessThan(1e-3f));
            Step(0.1f, 10);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(1f, 0.5f, 0f)), Is.LessThan(1e-3f));
        }

        // ----- Extremes -----

        [Test]
        public void HugeDelta_OnInfiniteTween_ReturnsQuickly() {
            Raven.Value(0f, 1f, 0.0001f).Infinite();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Step(1000000f);
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(50));
        }

        [Test]
        public void InfiniteTimeScale_IsIgnored() {
            LogAssert.ignoreFailingMessages = true;
            Raven.TimeScale = float.PositiveInfinity;
            Assert.That(Raven.TimeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ZeroDuration_WithCycles_CompletesImmediately() {
            bool completed = false;
            Raven.Value(0f, 1f, 0f).Cycles(3).OnComplete(() => completed = true);
            Step(0.016f);
            Assert.That(completed, Is.True);
        }

        [Test]
        public void FromWithWrongValueKind_IsRejected() {
            LogAssert.ignoreFailingMessages = true;
            Raven.Scale(_go.transform, Vector3.one * 2f, 1f).From(0.5f);
            Step(0.5f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(1.5f).Within(1e-3f), "The bad From is ignored; the tween starts from the current scale.");
        }

        // ----- Effects -----

        [Test]
        public void YoyoShake_EndsExactlyAtRest() {
            _go.transform.localPosition = new Vector3(1f, 2f, 3f);
            Raven.ShakePosition(_go.transform, Vector3.one, 0.5f).Cycles(2, CycleMode.Yoyo);
            Step(0.1f, 12);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void OverlappingPunches_EndAtTheOriginalRest() {
            _go.transform.localScale = Vector3.one;
            Raven.PunchScale(_go.transform, Vector3.one * 0.3f, 0.5f);
            Step(0.05f, 2);
            Raven.PunchScale(_go.transform, Vector3.one * 0.3f, 0.5f);
            Step(0.05f, 20);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f), "A second punch must not drift the rest scale.");
        }

        // ----- Custom tweens -----

        [Test]
        public void CustomOnDestroyedObject_ReturnsDeadHandle() {
            var doomed = new GameObject("doomed");
            Object.DestroyImmediate(doomed);
            LogAssert.ignoreFailingMessages = true;
            Tween t = Raven.Custom(doomed, 0f, 1f, 1f, (g, v) => { });
            Assert.That(t.IsAlive, Is.False);
        }

        // ----- Components -----

        [Test]
        public void Animator_CompleteNow_WithAFinishedEntry_DoesNotThrow() {
            var fast = MakeTemplate(0.1f);
            var slow = MakeTemplate(1f);
            var animator = _go.AddComponent<RavenAnimator>();
            animator.Entries.Add(new RavenAnimator.Entry { target = _go.transform, template = fast });
            animator.Entries.Add(new RavenAnimator.Entry { target = _go.transform, template = slow });
            int allComplete = 0;
            animator.OnAllComplete.AddListener(() => allComplete++);
            animator.Play();
            Step(0.5f);
            Assert.DoesNotThrow(() => animator.CompleteNow());
            Assert.That(allComplete, Is.EqualTo(1));
            Object.DestroyImmediate(fast);
            Object.DestroyImmediate(slow);
        }

        [Test]
        public void Animator_AllComplete_FiresWhenAnEntryIsKilledElsewhere() {
            var a = MakeTemplate(0.2f);
            var b = MakeTemplate(5f);
            var animator = _go.AddComponent<RavenAnimator>();
            animator.Entries.Add(new RavenAnimator.Entry { target = _go.transform, template = a });
            animator.Entries.Add(new RavenAnimator.Entry { target = _go.transform, template = b });
            int allComplete = 0;
            animator.OnAllComplete.AddListener(() => allComplete++);
            animator.Play();
            Step(0.3f);
            Raven.StopAll();
            Assert.That(allComplete, Is.EqualTo(1));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void SequencePlayer_ReplayFromOnComplete_RunsFully() {
            var template = MakeTemplate(0.2f);
            var player = _go.AddComponent<RavenSequencePlayer>();
            player.Steps.Add(new RavenSequencePlayer.Step { mode = RavenSequencePlayer.StepMode.Chain, target = _go.transform, template = template });
            player.Steps.Add(new RavenSequencePlayer.Step { mode = RavenSequencePlayer.StepMode.Chain, target = _go.transform, template = template });
            int runs = 0;
            player.OnComplete.AddListener(() => { runs++; if (runs == 1) { player.Play(); } });
            player.Play();
            Step(0.1f, 5);
            Assert.That(runs, Is.EqualTo(1));
            Assert.That(player.Current.IsAlive, Is.True, "The replay started in OnComplete must survive.");
            Step(0.1f, 5);
            Assert.That(runs, Is.EqualTo(2));
            Object.DestroyImmediate(template);
        }

        static TweenTemplate MakeTemplate(float duration) {
            var t = ScriptableObject.CreateInstance<TweenTemplate>();
            t.property = PropertyKind.LocalPosition;
            t.endValue = new Vector4(0f, 1f, 0f, 0f);
            t.settings = TweenParams.Default;
            t.settings.duration = duration;
            return t;
        }
    }
}
