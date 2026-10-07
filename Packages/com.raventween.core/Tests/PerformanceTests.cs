using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace RavenTween.Tests {
    /// <summary>
    /// Allocation and throughput guarantees. These are the package's performance contract:
    /// a failing test here is a release blocker.
    /// </summary>
    public sealed class PerformanceTests {
        const int TweenCount = 5000;

        sealed class Box { public float Value; }

        readonly GameObject[] _objects = new GameObject[64];

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            for (int i = 0; i < _objects.Length; i++) { _objects[i] = new GameObject("perf-" + i); }
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            for (int i = 0; i < _objects.Length; i++) { Object.DestroyImmediate(_objects[i]); }
        }

        void SpawnMixedTweens(int count) {
            var box = new Box();
            for (int i = 0; i < count; i++) {
                Transform t = _objects[i % _objects.Length].transform;
                switch (i % 5) {
                    case 0: Raven.Position(t, Vector3.one * i, 10f).Ease(Ease.InOutSine).Infinite(); break;
                    case 1: Raven.Scale(t, 2f, 10f).Ease(Ease.OutBack).Infinite(); break;
                    case 2: Raven.LocalRotation(t, Quaternion.Euler(0f, 90f, 0f), 10f).Infinite(); break;
                    case 3: Raven.ShakePosition(t, Vector3.one, 10f); break;
                    default: Raven.Custom(box, 0f, 1f, 10f, (b, v) => b.Value = v).Infinite(); break;
                }
            }
        }

        [Test]
        public void SteadyStateUpdate_AllocatesZeroBytes() {
            SpawnMixedTweens(TweenCount);
            TweenEngine.Process(0.016f, 0.016f); // Warm-up: first-frame start value capture.
            Assert.That(() => {
                for (int frame = 0; frame < 10; frame++) { TweenEngine.Process(0.016f, 0.016f); }
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SequenceUpdate_AllocatesZeroBytes() {
            Transform t = _objects[0].transform;
            for (int i = 0; i < 200; i++) {
                Raven.Sequence()
                    .Chain(Raven.LocalPosition(t, Vector3.up, 0.5f))
                    .Group(Raven.Scale(t, 1.2f, 0.5f))
                    .Chain(Raven.LocalPosition(t, Vector3.zero, 0.5f))
                    .Infinite();
            }
            // Warm-up must cover one full cycle wrap: the runtime materializes string literals
            // the first time each method runs, which is a one-time cost, not a steady-state one.
            for (int frame = 0; frame < 80; frame++) { TweenEngine.Process(0.016f, 0.016f); }
            Assert.That(() => {
                for (int frame = 0; frame < 120; frame++) { TweenEngine.Process(0.016f, 0.016f); }
            }, Is.Not.AllocatingGCMemory(), "Sequence stepping, including cycle wraps, must not allocate.");
        }

        // Every 1.6 addition on the hot path: axes, property blocks, cycle modes, parametric eases,
        // per-tween time scale, target callbacks and sequence callbacks.
        void SpawnExtendedTweens(Renderer renderer, Box box) {
            int color = Shader.PropertyToID("_Color");
            for (int i = 0; i < 100; i++) {
                Transform t = _objects[i % _objects.Length].transform;
                Tween axis = Raven.PositionX(t, i, 0.4f).Cycles(-1, CycleMode.PingPong);
                axis.TimeScale = 0.9f;
                Raven.ScaleY(t, 2f, 0.4f).Ease(Easing.Elastic(1.2f, 0.3f)).Infinite(CycleMode.Incremental);
                Raven.PropertyBlockColor(renderer, color, Color.red, 0.4f).Infinite();
                Raven.Value(0f, 1f, 0.4f).Infinite().OnUpdate(box, (b, tween) => b.Value = tween.Progress);
                Raven.Sequence().Chain(Raven.Value(0f, 1f, 0.2f)).ChainCallback(box, b => b.Value++).Infinite();
            }
        }

        [Test]
        public void ExtendedFeatures_SteadyState_AllocateZeroBytes() {
            var renderer = _objects[0].AddComponent<MeshRenderer>();
            var box = new Box();
            SpawnExtendedTweens(renderer, box);
            for (int frame = 0; frame < 40; frame++) { TweenEngine.Process(0.016f, 0.016f); }
            Assert.That(() => {
                for (int frame = 0; frame < 60; frame++) { TweenEngine.Process(0.016f, 0.016f); }
            }, Is.Not.AllocatingGCMemory(), "1.6 features must keep the zero-garbage guarantee.");
        }

        [Test]
        public void TweenCreation_FromWarmPool_AllocatesZeroBytes() {
            Transform t = _objects[0].transform;
            for (int i = 0; i < 1000; i++) { Raven.Position(t, Vector3.one, 1f); }
            TweenEngine.KillAll(false); // Slots are now pooled.
            Assert.That(() => {
                for (int i = 0; i < 1000; i++) { Raven.Position(t, Vector3.one, 1f).Ease(Ease.OutQuad); }
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Throughput_5000Tweens_ReportsFrameCost() {
            SpawnMixedTweens(TweenCount);
            TweenEngine.Process(0.016f, 0.016f);
            const int frames = 100;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < frames; i++) { TweenEngine.Process(0.016f, 0.016f); }
            watch.Stop();
            double perFrameMs = watch.Elapsed.TotalMilliseconds / frames;
            UnityEngine.Debug.Log("[RavenTween benchmark] " + TweenCount + " tweens: " + perFrameMs.ToString("0.000") + " ms/frame");
            Assert.That(perFrameMs, Is.LessThan(16.0), "5000 tweens must fit well inside a 60 FPS frame budget.");
        }
    }
}
