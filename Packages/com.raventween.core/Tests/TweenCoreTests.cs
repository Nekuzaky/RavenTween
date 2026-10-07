using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>
    /// Deterministic engine tests: the engine is stepped manually through
    /// TweenEngine.Process so results do not depend on frame timing.
    /// </summary>
    public sealed class TweenCoreTests {
        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
        }

        static void Step(float delta, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(delta, delta); }
        }

        [Test]
        public void ValueTween_ReachesEndValue_AndCompletes() {
            float value = -1f;
            bool completed = false;
            Raven.Value(0f, 10f, 1f)
                .OnUpdate(v => value = v)
                .OnComplete(() => completed = true);
            Step(0.5f);
            Assert.That(value, Is.EqualTo(5f).Within(1e-3f));
            Step(0.5f);
            Assert.That(value, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(completed, Is.True);
        }

        [Test]
        public void Handle_IsDead_AfterCompletion() {
            Tween tween = Raven.Value(0f, 1f, 0.2f);
            Assert.That(tween.IsAlive, Is.True);
            Step(0.3f);
            Assert.That(tween.IsAlive, Is.False);
        }

        [Test]
        public void RecycledSlot_DoesNotResurrectOldHandle() {
            Tween first = Raven.Value(0f, 1f, 0.1f);
            Step(0.2f);
            Assert.That(first.IsAlive, Is.False);
            Tween second = Raven.Value(0f, 1f, 10f);
            Assert.That(second.IsAlive, Is.True);
            Assert.That(first.IsAlive, Is.False, "Old handle must stay dead after slot reuse.");
            second.Stop();
        }

        [Test]
        public void Delay_PostponesStart() {
            bool started = false;
            Raven.Value(0f, 1f, 1f).Delay(0.5f).OnStart(() => started = true);
            Step(0.4f);
            Assert.That(started, Is.False);
            Step(0.2f);
            Assert.That(started, Is.True);
        }

        [Test]
        public void Cycles_Restart_RunsExpectedNumberOfTimes() {
            int completions = 0;
            float value = 0f;
            Raven.Value(0f, 1f, 0.5f)
                .Cycles(3)
                .OnUpdate(v => value = v)
                .OnComplete(() => completions++);
            Step(0.1f, 20); // 2.0 seconds total, enough for 3 cycles of 0.5s.
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void Yoyo_EvenCycleCount_EndsAtStartValue() {
            float value = -1f;
            Raven.Value(0f, 1f, 0.5f)
                .Cycles(2, CycleMode.Yoyo)
                .OnUpdate(v => value = v);
            Step(0.1f, 15); // 1.5s > 1.0s total.
            Assert.That(value, Is.EqualTo(0f).Within(1e-3f), "Yoyo with 2 cycles must end where it started.");
        }

        [Test]
        public void InfiniteTween_KeepsRunning() {
            Tween tween = Raven.Value(0f, 1f, 0.25f).Infinite();
            Step(0.1f, 50);
            Assert.That(tween.IsAlive, Is.True);
            tween.Stop();
            Assert.That(tween.IsAlive, Is.False);
        }

        [Test]
        public void Pause_FreezesProgress_ResumeContinues() {
            float value = -1f;
            Tween tween = Raven.Value(0f, 1f, 1f).OnUpdate(v => value = v);
            Step(0.25f);
            tween.Pause();
            float frozen = value;
            Step(0.25f, 4);
            Assert.That(value, Is.EqualTo(frozen), "Paused tween must not advance.");
            tween.Resume();
            Step(0.25f, 4);
            Assert.That(tween.IsAlive, Is.False);
            Assert.That(value, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void Stop_FiresOnKill_NotOnComplete() {
            bool killed = false;
            bool completed = false;
            Tween tween = Raven.Value(0f, 1f, 1f)
                .OnKill(() => killed = true)
                .OnComplete(() => completed = true);
            Step(0.2f);
            tween.Stop();
            Assert.That(killed, Is.True);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void Complete_JumpsToEnd_AndFiresOnComplete() {
            float value = -1f;
            bool completed = false;
            Tween tween = Raven.Value(0f, 10f, 1f)
                .OnUpdate(v => value = v)
                .OnComplete(() => completed = true);
            Step(0.1f);
            tween.Complete();
            Assert.That(value, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(completed, Is.True);
            Assert.That(tween.IsAlive, Is.False);
        }

        [Test]
        public void From_OverridesStartValue() {
            float value = -1f;
            Raven.Value(0f, 1f, 1f).From(5f).OnUpdate(v => value = v);
            Step(0.0001f);
            Assert.That(value, Is.GreaterThan(4f), "First update must start near the explicit from value.");
        }

        [Test]
        public void GlobalTimeScale_SlowsTweens() {
            Raven.TimeScale = 0.5f;
            Tween tween = Raven.Value(0f, 1f, 1f);
            Step(0.5f, 2); // 1.0s real time -> 0.5s tween time.
            Assert.That(tween.IsAlive, Is.True);
            Step(0.5f, 3);
            Assert.That(tween.IsAlive, Is.False);
            Raven.TimeScale = 1f;
        }

        [Test]
        public void ZeroDuration_CompletesOnFirstUpdate() {
            bool completed = false;
            Raven.Value(0f, 1f, 0f).OnComplete(() => completed = true);
            Step(0.016f);
            Assert.That(completed, Is.True);
        }

        [Test]
        public void CallbackException_DoesNotBreakOtherTweens() {
            bool otherCompleted = false;
            Raven.Value(0f, 1f, 0.1f).OnComplete(() => throw new System.InvalidOperationException("boom"));
            Raven.Value(0f, 1f, 0.1f).OnComplete(() => otherCompleted = true);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Step(0.2f);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.That(otherCompleted, Is.True);
        }

        [Test]
        public void StopAll_KillsEverything() {
            Raven.Value(0f, 1f, 5f);
            Raven.Value(0f, 1f, 5f);
            Assert.That(Raven.AliveCount, Is.EqualTo(2));
            Raven.StopAll();
            Assert.That(Raven.AliveCount, Is.EqualTo(0));
        }
    }
}
