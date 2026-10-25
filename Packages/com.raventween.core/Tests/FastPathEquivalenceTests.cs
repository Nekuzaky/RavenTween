using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RavenTween.Tests {
    public sealed class FastPathEquivalenceTests {
        readonly List<Object> _objects = new List<Object>();

        static readonly float[] Deltas = { 0f, 0.013f, 0.1f, 0f, 0.37f, 0.016f, 0.9f, 0.25f, 2.6f, 0.05f, 0.5f, 0.11f, 7.3f, 0.2f };

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            foreach (Object o in _objects) { if (o != null) { Object.DestroyImmediate(o); } }
            _objects.Clear();
        }

        Transform Make(string name, Vector3 position, Transform parent = null) {
            var go = new GameObject(name);
            _objects.Add(go);
            if (parent != null) { go.transform.SetParent(parent, false); }
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.5f, 0.5f, 2f);
            return go.transform;
        }

        Transform MakeParent(string name) {
            Transform parent = Make(name, new Vector3(3f, -2f, 1f));
            parent.localScale = new Vector3(2f, 3f, 0.5f);
            parent.rotation = Quaternion.Euler(10f, 20f, 30f);
            return parent;
        }

        static void Same(Transform fast, Transform generic, string what) {
            Vector3 p = fast.position, q = generic.position;
            Vector3 lp = fast.localPosition, lq = generic.localPosition;
            Vector3 s = fast.localScale, r = generic.localScale;
            bool equal = p.x.Equals(q.x) && p.y.Equals(q.y) && p.z.Equals(q.z)
                && lp.x.Equals(lq.x) && lp.y.Equals(lq.y) && lp.z.Equals(lq.z)
                && s.x.Equals(r.x) && s.y.Equals(r.y) && s.z.Equals(r.z);
            Assert.That(equal, Is.True, what + ": fast " + p.ToString("R") + " " + s.ToString("R") + " vs generic " + q.ToString("R") + " " + r.ToString("R"));
        }

        void RunCase(string name, Func<Transform, Tween> build, Action<Tween> after = null, UpdatePhase phase = UpdatePhase.Update, bool parented = false) {
            Transform fast = Make(name + "-fast", new Vector3(1.25f, 2.5f, -3.75f), parented ? MakeParent(name + "-fast-parent") : null);
            Transform generic = Make(name + "-generic", new Vector3(1.25f, 2.5f, -3.75f), parented ? MakeParent(name + "-generic-parent") : null);
            Same(fast, generic, name + " initial");
            Tween tf = build(fast);
            Tween tg = build(generic).OnUpdate(() => { });
            if (after != null) {
                after(tf);
                after(tg);
            }
            for (int i = 0; i < Deltas.Length; i++) {
                if (phase == UpdatePhase.Update) { TweenEngine.Process(Deltas[i], Deltas[i]); }
                else { TweenEngine.ProcessPhase(phase, Deltas[i], Deltas[i]); }
                Same(fast, generic, name + " step " + i);
                Assert.That(tf.IsAlive, Is.EqualTo(tg.IsAlive), name + " alive at step " + i);
            }
        }

        static Tween Pos(Transform t) { return Raven.Position(t, new Vector3(-7.1f, 13.37f, 0.3f), 1.3f); }
        static Tween LPos(Transform t) { return Raven.LocalPosition(t, new Vector3(4.2f, -1.1f, 9.9f), 0.7f); }
        static Tween Scl(Transform t) { return Raven.Scale(t, new Vector3(3.3f, -0.2f, 1.01f), 0.9f); }

        [Test] public void Linear() { RunCase("linear", Pos); }
        [Test] public void LocalPositionParented() { RunCase("lpos", t => LPos(t).Ease(Ease.InOutCubic), null, UpdatePhase.Update, true); }
        [Test] public void PositionParented() { RunCase("pos-parent", t => Pos(t).Ease(Ease.OutQuad), null, UpdatePhase.Update, true); }
        [Test] public void ScaleOutBack() { RunCase("scale", t => Scl(t).Ease(Ease.OutBack)); }
        [Test] public void Yoyo() { RunCase("yoyo", t => Pos(t).Ease(Ease.OutBack).Cycles(5, CycleMode.Yoyo)); }
        [Test] public void PingPong() { RunCase("pingpong", t => Pos(t).Ease(Ease.InOutElastic).Cycles(6, CycleMode.PingPong)); }
        [Test] public void InfiniteYoyoWithDelay() { RunCase("infinite-yoyo", t => Scl(t).Ease(Ease.InBack).Delay(0.4f).Infinite(CycleMode.Yoyo)); }
        [Test] public void ExplicitFrom() { RunCase("from", t => Pos(t).From(new Vector3(100f, -50f, 0.001f)).Ease(Ease.InOutBack).Cycles(3, CycleMode.Yoyo)); }
        [Test] public void ParametricBounceExact() { RunCase("bounce-exact", t => Pos(t).Ease(Easing.BounceExact(0.75f))); }
        [Test] public void CurveEase() { RunCase("curve", t => Pos(t).Ease(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1.4f), new Keyframe(1f, 1f)))); }
        [Test] public void AtSpeed() { RunCase("speed", t => Raven.PositionAtSpeed(t, new Vector3(9f, 9f, 9f), 3.3f).Ease(Ease.OutSine)); }
        [Test] public void TweenTimeScaleAndUnscaledTime() { RunCase("time-scale", t => Pos(t).UnscaledTime().Cycles(3, CycleMode.Yoyo), tw => tw.TimeScale = 0.37f); }
        [Test] public void FixedPhase() { RunCase("fixed", t => Pos(t).UpdateIn(UpdatePhase.FixedUpdate).Cycles(2, CycleMode.PingPong), null, UpdatePhase.FixedUpdate); }
        [Test] public void ZeroDuration() { RunCase("zero", t => Raven.Position(t, new Vector3(5f, 5f, 5f), 0f).Cycles(3, CycleMode.Yoyo)); }

        [Test]
        public void SeekAndMoveInTime() {
            Transform fast = Make("seek-fast", Vector3.one);
            Transform generic = Make("seek-generic", Vector3.one);
            Func<Transform, Tween> build = t => Pos(t).Ease(Ease.OutBack).Delay(0.2f).Cycles(5, CycleMode.Yoyo);
            Tween tf = build(fast);
            Tween tg = build(generic).OnUpdate(() => { });
            foreach (float total in new[] { 0.1f, 0.5f, 2.9f, 1.4f, 3f, 0f, 4.15f }) {
                tf.ElapsedTimeTotal = total;
                tg.ElapsedTimeTotal = total;
                Same(fast, generic, "elapsed total " + total);
            }
            foreach (float progress in new[] { 0.3f, 0.99f, 0f, 1f }) {
                tf.Progress = progress;
                tg.Progress = progress;
                Same(fast, generic, "progress " + progress);
            }
            foreach (float time in new[] { 0.5f, 3.3f, 0.21f, 100f }) {
                TweenEngine.Seek(tf.Index, tf.Version, time);
                TweenEngine.Seek(tg.Index, tg.Version, time);
                Same(fast, generic, "seek " + time);
            }
            tf.Complete();
            tg.Complete();
            Same(fast, generic, "complete");
        }

        [Test]
        public void CompleteInEveryCycleMode() {
            foreach (CycleMode mode in new[] { CycleMode.Restart, CycleMode.Yoyo, CycleMode.PingPong }) {
                foreach (int cycles in new[] { 1, 2, 3, -1 }) {
                    Transform fast = Make("complete-fast", Vector3.one);
                    Transform generic = Make("complete-generic", Vector3.one);
                    Tween tf = Pos(fast).Ease(Ease.OutBack).Cycles(cycles, mode);
                    Tween tg = Pos(generic).Ease(Ease.OutBack).Cycles(cycles, mode).OnUpdate(() => { });
                    TweenEngine.Process(0.7f, 0.7f);
                    TweenEngine.Process(1.1f, 1.1f);
                    Same(fast, generic, mode + " x" + cycles + " running");
                    tf.Complete();
                    tg.Complete();
                    Same(fast, generic, mode + " x" + cycles + " complete");
                }
            }
        }

        [Test]
        public void InsideSequences() {
            Transform fast = Make("sequence-fast", Vector3.one);
            Transform generic = Make("sequence-generic", Vector3.one);
            Sequence sf = Raven.Sequence().Chain(Pos(fast).Ease(Ease.OutBack).Cycles(2, CycleMode.Yoyo)).Chain(Scl(fast).Cycles(3, CycleMode.PingPong)).Insert(0.3f, LPos(fast).Ease(Ease.InElastic)).Cycles(3, CycleMode.Yoyo);
            Sequence sg = Raven.Sequence().Chain(Pos(generic).Ease(Ease.OutBack).Cycles(2, CycleMode.Yoyo).OnUpdate(() => { })).Chain(Scl(generic).Cycles(3, CycleMode.PingPong).OnUpdate(() => { })).Insert(0.3f, LPos(generic).Ease(Ease.InElastic).OnUpdate(() => { })).Cycles(3, CycleMode.Yoyo);
            for (int i = 0; i < Deltas.Length; i++) {
                TweenEngine.Process(Deltas[i], Deltas[i]);
                Same(fast, generic, "sequence step " + i);
            }
            Assert.That(sf.IsAlive, Is.EqualTo(sg.IsAlive));
        }

        [Test]
        public void DestroyedTarget_StopsBothQuietly() {
            Transform fast = Make("destroyed-fast", Vector3.one);
            Transform generic = Make("destroyed-generic", Vector3.one);
            bool fastStopped = false;
            bool genericStopped = false;
            Tween tf = Pos(fast).Cycles(3, CycleMode.Yoyo).OnTargetDestroyed(() => fastStopped = true);
            Tween tg = Pos(generic).Cycles(3, CycleMode.Yoyo).OnUpdate(() => { }).OnTargetDestroyed(() => genericStopped = true);
            TweenEngine.Process(0.2f, 0.2f);
            Object.DestroyImmediate(fast.gameObject);
            Object.DestroyImmediate(generic.gameObject);
            TweenEngine.Process(0.2f, 0.2f);
            Assert.That(tf.IsAlive || tg.IsAlive, Is.False);
            Assert.That(fastStopped && genericStopped, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
