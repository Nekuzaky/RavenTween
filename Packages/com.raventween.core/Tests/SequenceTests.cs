using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class SequenceTests {
        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
        }

        static void Step(float delta, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(delta, delta); }
        }

        [Test]
        public void Chain_PlaysTweensOneAfterAnother() {
            float first = 0f;
            float second = 0f;
            Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 1f).OnUpdate(v => first = v))
                .Chain(Raven.Value(0f, 1f, 1f).OnUpdate(v => second = v));
            Step(0.5f);
            Assert.That(first, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(second, Is.EqualTo(0f).Within(1e-3f), "Second chained tween must not run yet.");
            Step(0.5f);
            Step(0.5f);
            Assert.That(second, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void Group_PlaysTweensTogether() {
            float first = 0f;
            float second = 0f;
            Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 1f).OnUpdate(v => first = v))
                .Group(Raven.Value(0f, 2f, 1f).OnUpdate(v => second = v));
            Step(0.5f);
            Assert.That(first, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(second, Is.EqualTo(1f).Within(1e-3f), "Grouped tween must run alongside.");
        }

        [Test]
        public void Insert_PlacesTweenAtAbsoluteTime() {
            float value = 0f;
            Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 2f))
                .Insert(1f, Raven.Value(0f, 1f, 0.5f).OnUpdate(v => value = v));
            Step(0.5f, 2); // t = 1.0
            Assert.That(value, Is.EqualTo(0f).Within(1e-3f));
            Step(0.25f); // t = 1.25
            Assert.That(value, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void Sequence_Completes_AndFiresCallback() {
            bool completed = false;
            Sequence sequence = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f))
                .OnComplete(() => completed = true);
            Step(0.25f, 3);
            Assert.That(completed, Is.True);
            Assert.That(sequence.IsAlive, Is.False);
        }

        [Test]
        public void Sequence_Cycles_RepeatsChildren() {
            int childCompletions = 0;
            Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f).OnComplete(() => childCompletions++))
                .Cycles(2);
            Step(0.1f, 12); // 1.2s > 2 cycles of 0.5s.
            Assert.That(childCompletions, Is.EqualTo(2));
        }

        [Test]
        public void NestedSequence_RunsInsideParent() {
            float value = 0f;
            Sequence inner = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f).OnUpdate(v => value = v));
            Sequence outer = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f))
                .Chain(inner);
            Step(0.25f); // t = 0.25: inner not started.
            Assert.That(value, Is.EqualTo(0f).Within(1e-3f));
            Step(0.25f, 2); // t = 0.75: inner halfway.
            Assert.That(value, Is.EqualTo(0.5f).Within(1e-3f));
            Step(0.5f, 2);
            Assert.That(outer.IsAlive, Is.False);
            Assert.That(value, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void Stop_KillsChildrenToo() {
            Sequence sequence = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 5f))
                .Chain(Raven.Value(0f, 1f, 5f));
            Assert.That(Raven.AliveCount, Is.EqualTo(3), "Sequence plus two children are alive.");
            sequence.Stop();
            Assert.That(Raven.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void Complete_JumpsChildrenToEnd() {
            float value = 0f;
            Sequence sequence = Raven.Sequence()
                .Chain(Raven.Value(0f, 10f, 1f).OnUpdate(v => value = v));
            Step(0.1f);
            sequence.Complete();
            Assert.That(value, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(sequence.IsAlive, Is.False);
        }

        [Test]
        public void InfiniteChild_IsRejected_AndClamped() {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Sequence sequence = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f).Infinite());
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Step(0.25f, 4);
            Assert.That(sequence.IsAlive, Is.False, "Clamped child must let the sequence finish.");
        }

        // Regression: a later child on the same property used to start (and capture) at t = 0,
        // then overwrite the earlier child for its whole duration.
        [Test]
        public void ChainedTweens_OnSameProperty_PlayInOrder() {
            var go = new GameObject("same-property");
            Raven.Sequence()
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 1f, 0f), 1f));
            Step(0.1f, 5);
            Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(0.5f, 0f, 0f)), Is.LessThan(1e-3f),
                "First move must not be overridden by the second.");
            Step(0.1f, 8);
            Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(1f, 0.3f, 0f)), Is.LessThan(1e-3f),
                "Second move must start where the first ended.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void YoyoSequence_SameProperty_RetracesPath() {
            var go = new GameObject("yoyo-same-property");
            Raven.Sequence()
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 1f, 0f), 1f))
                .Cycles(2, CycleMode.Yoyo);
            Step(0.1f, 5);   // t = 0.5 forward
            Vector3 forward = go.transform.localPosition;
            Step(0.1f, 30);  // t = 3.5 -> backward cycle at sequence time 0.5
            Assert.That(Vector3.Distance(go.transform.localPosition, forward), Is.LessThan(1e-3f),
                "Going back must pass through the same pose.");
            Step(0.1f, 6);   // past the end
            Assert.That(go.transform.localPosition.magnitude, Is.LessThan(1e-3f), "Yoyo x2 must end at the start.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RestartSequence_SameProperty_JumpsBackEachCycle() {
            var go = new GameObject("restart-same-property");
            Raven.Sequence()
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(go.transform, new Vector3(1f, 1f, 0f), 1f))
                .Cycles(3);
            Step(0.1f, 25);  // t = 2.5 -> second cycle at 0.5
            Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(0.5f, 0f, 0f)), Is.LessThan(1e-3f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ChainDelay_ShiftsFollowingTweens() {
            float value = 0f;
            Raven.Sequence()
                .ChainDelay(1f)
                .Chain(Raven.Value(0f, 1f, 1f).OnUpdate(v => value = v));
            Step(0.5f);
            Assert.That(value, Is.EqualTo(0f).Within(1e-3f));
            Step(0.5f, 2);
            Assert.That(value, Is.EqualTo(0.5f).Within(1e-3f));
        }
    }
}
