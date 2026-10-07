using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace RavenTween.Tests {
    /// <summary>Update phases, cycle modes, time control, callbacks, target control and the wider API.</summary>
    public sealed class FeatureTests {
        GameObject _go;

        sealed class Holder {
            public int Count;
            public float Last;
            public Rect Rect;
            public Quaternion Rotation;
            public Vector3 Position;
        }

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            Raven.UpdatePhase = UpdatePhase.Update;
            Time.timeScale = 1f;
            _go = new GameObject("feature-target");
        }

        [TearDown]
        public void TearDown() {
            LogAssert.ignoreFailingMessages = false;
            TweenEngine.KillAll(false);
            Raven.UpdatePhase = UpdatePhase.Update;
            Time.timeScale = 1f;
            Object.DestroyImmediate(_go);
        }

        static void Step(float delta, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(delta, delta); }
        }

        // ----- Update phases -----

        [Test]
        public void FixedUpdatePhase_IsSteppedOnlyByFixedUpdate() {
            float v = 0f;
            Raven.Value(0f, 10f, 1f).UpdateIn(UpdatePhase.FixedUpdate).OnUpdate(x => v = x);
            TweenEngine.ProcessPhase(UpdatePhase.Update, 0.5f, 0.5f);
            Assert.That(v, Is.EqualTo(0f));
            TweenEngine.ProcessPhase(UpdatePhase.FixedUpdate, 0.5f, 0.5f);
            Assert.That(v, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void GlobalPhase_AppliesToTweensWithoutTheirOwn() {
            Raven.UpdatePhase = UpdatePhase.LateUpdate;
            float a = 0f, b = 0f;
            Raven.Value(0f, 10f, 1f).OnUpdate(x => a = x);
            Raven.Value(0f, 10f, 1f).UpdateIn(UpdatePhase.Update).OnUpdate(x => b = x);
            TweenEngine.ProcessPhase(UpdatePhase.Update, 0.5f, 0.5f);
            Assert.That(a, Is.EqualTo(0f));
            Assert.That(b, Is.EqualTo(5f).Within(1e-4f));
            TweenEngine.ProcessPhase(UpdatePhase.LateUpdate, 0.5f, 0.5f);
            Assert.That(a, Is.EqualTo(5f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator FixedUpdatePhase_RunsInThePlayerLoop() {
            float v = -1f;
            Raven.Value(0f, 1f, 0.2f).UpdateIn(UpdatePhase.FixedUpdate).OnUpdate(x => v = x);
            yield return new WaitForSeconds(0.5f);
            Assert.That(v, Is.EqualTo(1f).Within(1e-4f));
        }

        // ----- Cycle modes -----

        [Test]
        public void Incremental_ContinuesFromTheLastEnd() {
            float v = 0f;
            Raven.Value(0f, 1f, 1f).Cycles(3, CycleMode.Incremental).OnUpdate(x => v = x);
            Step(0.5f, 3);
            Assert.That(v, Is.EqualTo(1.5f).Within(1e-4f));
            Step(0.5f, 4);
            Assert.That(v, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void Incremental_Quaternion_KeepsTurning() {
            Raven.LocalRotation(_go.transform, Quaternion.Euler(0f, 90f, 0f), 1f).Cycles(2, CycleMode.Incremental);
            Step(0.5f, 3);
            Assert.That(Quaternion.Angle(_go.transform.localRotation, Quaternion.Euler(0f, 135f, 0f)), Is.LessThan(0.05f));
            Step(1f);
            Assert.That(Quaternion.Angle(_go.transform.localRotation, Quaternion.Euler(0f, 180f, 0f)), Is.LessThan(0.05f));
        }

        [Test]
        public void PingPong_EasesTheWayBackAsIs_YoyoMirrorsIt() {
            float ping = 0f, yoyo = 0f;
            Raven.Value(0f, 1f, 1f).Ease(Ease.OutQuad).Cycles(2, CycleMode.PingPong).OnUpdate(x => ping = x);
            Raven.Value(0f, 1f, 1f).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo).OnUpdate(x => yoyo = x);
            Step(1.25f);
            Assert.That(ping, Is.EqualTo(1f - 0.4375f).Within(1e-4f));
            Assert.That(yoyo, Is.EqualTo(0.9375f).Within(1e-4f));
        }

        [Test]
        public void Sequence_Incremental_FallsBackToRestart() {
            LogAssert.Expect(LogType.Warning, new Regex("Incremental"));
            Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).Cycles(2, CycleMode.Incremental);
        }

        // ----- Time control -----

        [Test]
        public void TimeScale_SlowsOnlyThatTween() {
            float a = 0f, b = 0f;
            Tween slow = Raven.Value(0f, 10f, 1f).OnUpdate(x => a = x);
            Raven.Value(0f, 10f, 1f).OnUpdate(x => b = x);
            slow.TimeScale = 0.5f;
            Step(0.5f);
            Assert.That(a, Is.EqualTo(2.5f).Within(1e-4f));
            Assert.That(b, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void TimeScale_OnASequenceChild_IsIgnored() {
            Tween child = Raven.Value(0f, 1f, 1f);
            Raven.Sequence().Chain(child);
            LogAssert.Expect(LogType.Warning, new Regex("belongs to a sequence"));
            child.TimeScale = 2f;
            Assert.That(child.TimeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SettingElapsedTime_JumpsBothWays() {
            Tween t = Raven.LocalPosition(_go.transform, new Vector3(10f, 0f, 0f), 1f);
            t.ElapsedTimeTotal = 0.25f;
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(2.5f).Within(1e-4f));
            t.ElapsedTimeTotal = 0.1f;
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(t.Progress, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(t.IsAlive, Is.True);
        }

        [Test]
        public void SettingProgressTotal_MovesAcrossCycles() {
            Tween t = Raven.LocalPosition(_go.transform, new Vector3(10f, 0f, 0f), 1f).Cycles(2);
            t.ProgressTotal = 0.75f;
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(t.CyclesDone, Is.EqualTo(1));
        }

        [Test]
        public void SettingElapsedPastTheEnd_Completes() {
            bool completed = false;
            Tween t = Raven.Value(0f, 1f, 1f).OnComplete(() => completed = true);
            t.ElapsedTimeTotal = 5f;
            Assert.That(completed, Is.True);
            Assert.That(t.IsAlive, Is.False);
        }

        [Test]
        public void SequenceElapsed_ReplaysSamePropertyChildrenInOrder() {
            Transform tr = _go.transform;
            Sequence s = Raven.Sequence()
                .Chain(Raven.LocalPosition(tr, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(tr, new Vector3(2f, 0f, 0f), 1f));
            float[] times = { 1.5f, 0.5f, 1.75f, 0.25f };
            for (int i = 0; i < times.Length; i++) {
                s.ElapsedTimeTotal = times[i];
                Assert.That(tr.localPosition.x, Is.EqualTo(times[i]).Within(1e-4f), "after jumping to " + times[i]);
            }
        }

        [Test]
        public void SequenceElapsed_IntoAYoyoBackwardCycle() {
            Transform tr = _go.transform;
            Sequence s = Raven.Sequence()
                .Chain(Raven.LocalPosition(tr, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(tr, new Vector3(2f, 0f, 0f), 1f))
                .Cycles(2, CycleMode.Yoyo);
            s.ElapsedTimeTotal = 3f;
            Assert.That(tr.localPosition.x, Is.EqualTo(1f).Within(1e-4f));
            s.ElapsedTimeTotal = 3.5f;
            Assert.That(tr.localPosition.x, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void StateProperties_DescribeTheTween() {
            Tween t = Raven.Value(0f, 1f, 2f).Delay(0.5f).Cycles(3).Ease(Ease.InQuad);
            Assert.That(t.Duration, Is.EqualTo(2f));
            Assert.That(t.DurationTotal, Is.EqualTo(6.5f).Within(1e-4f));
            Assert.That(t.CyclesTotal, Is.EqualTo(3));
            Step(2.5f);
            Assert.That(t.CyclesDone, Is.EqualTo(1));
            Assert.That(t.ElapsedTimeTotal, Is.EqualTo(2.5f).Within(1e-4f));
            Assert.That(t.InterpolationFactor, Is.EqualTo(0f).Within(1e-4f));
            Step(1f);
            Assert.That(t.InterpolationFactor, Is.EqualTo(0.25f).Within(1e-4f));
            Tween forever = Raven.Value(0f, 1f, 1f).Infinite();
            Assert.That(float.IsPositiveInfinity(forever.DurationTotal), Is.True);
            Assert.That(forever.ProgressTotal, Is.EqualTo(0f));
        }

        [Test]
        public void SetRemainingCycles_StopsAYoyoLoopAtItsEndValue() {
            float v = 0f;
            bool done = false;
            Tween t = Raven.Value(0f, 1f, 1f).Infinite(CycleMode.Yoyo).OnUpdate(x => v = x).OnComplete(() => done = true);
            Step(1.5f);
            t.SetRemainingCycles(stopAtEndValue: true);
            Step(1f);
            Assert.That(done, Is.False);
            Step(1f);
            Assert.That(done, Is.True);
            Assert.That(v, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void SetRemainingCycles_One_FinishesTheCurrentCycle() {
            bool done = false;
            Tween t = Raven.Value(0f, 1f, 1f).Infinite(CycleMode.Restart).OnComplete(() => done = true);
            Step(2.5f);
            t.SetRemainingCycles(1);
            Step(0.4f);
            Assert.That(done, Is.False);
            Step(0.2f);
            Assert.That(done, Is.True);
        }

        [Test]
        public void GlobalTimeScale_TweensTimeTimeScale() {
            Raven.GlobalTimeScale(0.5f, 1f);
            Step(0.5f);
            Assert.That(Time.timeScale, Is.EqualTo(0.75f).Within(1e-4f));
        }

        [Test]
        public void TweenTimeScale_DrivesAnotherTween_AndStopsWithIt() {
            Tween target = Raven.Value(0f, 1f, 10f);
            Tween driver = Raven.TweenTimeScale(target, 0f, 1f);
            Step(0.5f);
            Assert.That(target.TimeScale, Is.EqualTo(0.5f).Within(1e-4f));
            target.Stop();
            Step(0.1f);
            Assert.That(driver.IsAlive, Is.False);
        }

        // ----- Sequence callbacks -----

        [Test]
        public void ChainAndInsertCallbacks_FireAtTheirTime() {
            var log = new List<string>();
            Raven.Sequence()
                .ChainCallback(() => log.Add("start"))
                .Chain(Raven.Value(0f, 1f, 1f))
                .ChainCallback(() => log.Add("end"))
                .InsertCallback(0.5f, () => log.Add("half"));
            Step(0.25f);
            Assert.That(log, Is.EqualTo(new[] { "start" }));
            Step(0.5f);
            Assert.That(log, Is.EqualTo(new[] { "start", "half" }));
            Step(0.5f);
            Assert.That(log, Is.EqualTo(new[] { "start", "half", "end" }));
        }

        [Test]
        public void Callbacks_FireOncePerCycle() {
            int count = 0;
            Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).InsertCallback(0.5f, () => count++).Cycles(3);
            Step(0.25f, 14);
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void Callbacks_OnYoyo_FireMirroredOnTheWayBack() {
            var frames = new List<int>();
            int frame = 0;
            Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).InsertCallback(0.25f, () => frames.Add(frame)).Cycles(2, CycleMode.Yoyo);
            for (frame = 1; frame <= 9; frame++) { Step(0.25f); }
            Assert.That(frames, Is.EqualTo(new[] { 1, 7 }));
        }

        [Test]
        public void CallbacksOnly_Sequence_FiresAndCompletes() {
            int fired = 0;
            bool done = false;
            Raven.Sequence().ChainCallback(() => fired++).InsertCallback(0f, () => fired++).OnComplete(() => done = true);
            Step(0.1f);
            Assert.That(fired, Is.EqualTo(2));
            Assert.That(done, Is.True);
        }

        [Test]
        public void AliveCount_DoesNotCountSequenceCallbacks() {
            int before = Raven.AliveCount;
            Raven.Sequence().ChainCallback(() => { }).Chain(Raven.Value(0f, 1f, 1f)).InsertCallback(0.5f, () => { });
            Assert.That(Raven.AliveCount, Is.EqualTo(before + 2), "One sequence and one tween.");
        }

        [Test]
        public void NestedSequence_Callbacks_Fire() {
            int n = 0;
            Sequence inner = Raven.Sequence().Chain(Raven.Value(0f, 1f, 0.5f)).ChainCallback(() => n++);
            Raven.Sequence().Chain(inner).Chain(Raven.Value(0f, 1f, 0.5f));
            Step(0.25f, 5);
            Assert.That(n, Is.EqualTo(1));
        }

        [Test]
        public void TargetCallbacks_PassTheTargetBack() {
            var holder = new Holder();
            Raven.Sequence().ChainCallback(holder, h => h.Count++).Chain(Raven.Value(0f, 1f, 0.5f));
            Raven.Value(0f, 1f, 0.5f).OnComplete(holder, h => h.Count += 10);
            Raven.Value(0f, 1f, 1f).OnUpdate(holder, (h, t) => h.Last = t.Progress);
            Step(0.5f);
            Assert.That(holder.Count, Is.EqualTo(11));
            Assert.That(holder.Last, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void TargetCallbacks_AreSkippedForDestroyedTargets() {
            var doomed = new GameObject("doomed");
            int calls = 0;
            Raven.Value(0f, 1f, 0.1f).OnComplete(doomed, g => calls++);
            Object.DestroyImmediate(doomed);
            Step(0.2f);
            Assert.That(calls, Is.EqualTo(0));
        }

        // The same lambdas must run before the measurement: the compiler caches each one on first use.
        static void SpawnTargetCallbackTweens(Holder holder) {
            for (int i = 0; i < 200; i++) {
                Raven.Value(0f, 1f, 0.1f).OnComplete(holder, h => h.Count++).OnUpdate(holder, (h, t) => h.Last = t.Progress);
            }
            Step(0.05f);
            Step(0.1f);
        }

        [Test]
        public void TargetCallbacks_AllocateNothing() {
            var holder = new Holder();
            SpawnTargetCallbackTweens(holder);
            Assert.That(() => SpawnTargetCallbackTweens(holder), Is.Not.AllocatingGCMemory());
            Assert.That(holder.Count, Is.GreaterThanOrEqualTo(400), "Every target callback ran.");
        }

        // ----- Target control, cancellation, capacity -----

        [Test]
        public void StopAll_OnAGameObject_StopsOnlyItsTweens() {
            var other = new GameObject("other");
            Raven.LocalPosition(_go.transform, Vector3.one, 1f);
            Raven.Scale(_go.transform, 2f, 1f);
            Tween keep = Raven.LocalPosition(other.transform, Vector3.one, 1f);
            Assert.That(Raven.CountTweens(_go.transform), Is.EqualTo(2));
            Assert.That(Raven.StopAll(_go), Is.EqualTo(2));
            Assert.That(keep.IsAlive, Is.True);
            Assert.That(Raven.CountTweens(_go.transform), Is.EqualTo(0));
            Object.DestroyImmediate(other);
        }

        [Test]
        public void TweenCreatedAfterStopAllInACallback_IsNotSteppedThatPass() {
            var other = new GameObject("other");
            float value = -1f;
            Tween trigger = Raven.Value(0f, 1f, 0.1f).OnComplete(() => {
                Raven.StopAll(other);
                Raven.Value(0f, 10f, 1f).OnUpdate(v => value = v);
            });
            // The new tween must reuse a slot the pass has not reached yet: stop one placed after the trigger.
            Tween stopped = default;
            for (int i = 0; i < 10000 && stopped.Index <= trigger.Index; i++) {
                stopped = Raven.LocalPosition(other.transform, Vector3.one, 5f);
            }
            Assert.That(stopped.Index, Is.GreaterThan(trigger.Index));
            Step(0.2f);
            Assert.That(value, Is.EqualTo(-1f), "A tween created during a pass waits for the next one.");
            Object.DestroyImmediate(other);
        }

        [Test]
        public void PauseAll_AndResumeAll_OnATarget() {
            Tween t = Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f);
            Assert.That(Raven.PauseAll(_go.transform), Is.EqualTo(1));
            Step(0.5f);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0f));
            Assert.That(t.IsPaused, Is.True);
            Raven.ResumeAll(_go.transform);
            Step(0.5f);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void CompleteAll_OnACustomTarget() {
            var holder = new Holder();
            Raven.Custom(holder, 0f, 5f, 1f, (h, v) => h.Last = v);
            Assert.That(Raven.CompleteAll(holder), Is.EqualTo(1));
            Assert.That(holder.Last, Is.EqualTo(5f));
        }

        [Test]
        public void Cancellation_StopsTheTweenAndFiresOnKill() {
            using (var source = new CancellationTokenSource()) {
                bool killed = false;
                Tween t = Raven.Value(0f, 1f, 1f).WithCancellation(source.Token).OnKill(() => killed = true);
                Step(0.1f);
                Assert.That(t.IsAlive, Is.True);
                source.Cancel();
                Step(0.1f);
                Assert.That(t.IsAlive, Is.False);
                Assert.That(killed, Is.True);
            }
        }

        [Test]
        public void Cancellation_WithAnAlreadyCancelledToken_StopsAtOnce() {
            using (var source = new CancellationTokenSource()) {
                source.Cancel();
                Tween t = Raven.Value(0f, 1f, 1f).WithCancellation(source.Token);
                Assert.That(t.IsAlive, Is.False);
            }
        }

        [Test]
        public void SetCapacity_ThenCreatingTweens_AllocatesNothing() {
            Raven.SetCapacity(Raven.AliveCount + 3000);
            Assert.That(() => {
                for (int i = 0; i < 2500; i++) { Raven.Value(0f, 1f, 1f); }
            }, Is.Not.AllocatingGCMemory());
        }

        // ----- Wider API -----

        [Test]
        public void AxisTweens_TouchOneAxisOnly() {
            Transform t = _go.transform;
            t.position = new Vector3(1f, 2f, 3f);
            Raven.PositionX(t, 5f, 1f);
            Raven.ScaleY(t, 3f, 1f);
            Step(1.1f);
            Assert.That(Vector3.Distance(t.position, new Vector3(5f, 2f, 3f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(t.localScale, new Vector3(1f, 3f, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void AxisTweens_OnDifferentAxes_Combine() {
            Transform t = _go.transform;
            Raven.LocalPositionX(t, 2f, 1f);
            Raven.LocalPositionY(t, 4f, 1f);
            Step(0.5f);
            Assert.That(Vector3.Distance(t.localPosition, new Vector3(1f, 2f, 0f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void AnchoredPositionX_KeepsY() {
            var rt = new GameObject("ui", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 7f);
            Raven.AnchoredPositionX(rt, 10f, 1f);
            Step(1.1f);
            Assert.That(Vector2.Distance(rt.anchoredPosition, new Vector2(10f, 7f)), Is.LessThan(1e-4f));
            Object.DestroyImmediate(rt.gameObject);
        }

        [Test]
        public void SpeedBasedTweens_DeriveTheirDuration() {
            Transform t = _go.transform;
            Assert.That(Raven.PositionAtSpeed(t, new Vector3(10f, 0f, 0f), 5f).Duration, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(Raven.LocalRotationAtSpeed(t, Quaternion.Euler(0f, 90f, 0f), 45f).Duration, Is.EqualTo(2f).Within(1e-3f));
        }

        [Test]
        public void PropertyBlockColor_LeavesTheSharedMaterialAlone() {
            Shader shader = Shader.Find("Unlit/Color");
            Assume.That(shader != null, "Unlit/Color shader is not available.");
            var material = new Material(shader) { color = Color.white };
            var renderer = _go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            int id = Shader.PropertyToID("_Color");
            Raven.PropertyBlockColor(renderer, id, Color.red, 1f);
            Step(0.5f);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(id).g, Is.EqualTo(0.5f).Within(1e-3f), "Starts from the material's color.");
            Step(0.6f);
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(id), Is.EqualTo(Color.red));
            Assert.That(material.color, Is.EqualTo(Color.white));
            Object.DestroyImmediate(material);
        }

        [Test]
        public void Template_PropertyBlockColor_ResolvesTheRenderer() {
            Shader shader = Shader.Find("Unlit/Color");
            Assume.That(shader != null, "Unlit/Color shader is not available.");
            var material = new Material(shader) { color = Color.white };
            var renderer = _go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.PropertyBlockColor;
            template.materialProperty = "_BaseColor"; // Alias of _Color.
            template.endValue = new Vector4(0f, 0f, 1f, 1f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.1f;
            Assert.That(template.Play(_go).IsAlive, Is.True);
            Step(0.2f);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(Shader.PropertyToID("_Color")), Is.EqualTo(Color.blue));
            Object.DestroyImmediate(template);
            Object.DestroyImmediate(material);
        }

        [Test]
        public void ParametricEases_MatchTheirClassicForms() {
            for (int i = 0; i <= 16; i++) {
                float t = i / 16f;
                Assert.That(Easing.Overshoot(1f).Evaluate(t), Is.EqualTo(EaseUtility.Evaluate(Ease.OutBack, t)).Within(1e-5f));
                Assert.That(Easing.Overshoot(0f).Evaluate(t), Is.EqualTo(EaseUtility.Evaluate(Ease.OutCubic, t)).Within(1e-5f));
                Assert.That(Easing.Bounce(1f).Evaluate(t), Is.EqualTo(EaseUtility.Evaluate(Ease.OutBounce, t)).Within(1e-5f));
            }
            Easing elastic = Easing.Elastic(1.5f, 0.25f);
            Assert.That(elastic.Evaluate(0f), Is.EqualTo(0f));
            Assert.That(elastic.Evaluate(1f), Is.EqualTo(1f));
            float peak = 0f;
            for (int i = 0; i <= 100; i++) { peak = Mathf.Max(peak, elastic.Evaluate(i / 100f)); }
            Assert.That(peak, Is.GreaterThan(1.05f), "An elastic ease overshoots its end.");
        }

        [Test]
        public void BounceExact_ReboundsByTheGivenAmount() {
            float v = 0f;
            Raven.Value(0f, 10f, 1f).Ease(Easing.BounceExact(0.5f)).OnUpdate(x => v = x);
            Step(1.5f / 2.75f);
            Assert.That(v, Is.EqualTo(9.5f).Within(1e-3f));
        }

        [Test]
        public void CustomRectAndQuaternion_Interpolate() {
            var holder = new Holder();
            Raven.Custom(holder, new Rect(0f, 0f, 10f, 10f), new Rect(10f, 10f, 20f, 20f), 1f, (h, r) => h.Rect = r);
            Raven.Custom(holder, Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), 1f, (h, q) => h.Rotation = q);
            Step(0.5f);
            Assert.That(holder.Rect, Is.EqualTo(new Rect(5f, 5f, 15f, 15f)));
            Assert.That(Quaternion.Angle(holder.Rotation, Quaternion.Euler(0f, 45f, 0f)), Is.LessThan(0.05f));
        }

        [Test]
        public void CustomToVector3_StartsFromTheValueWhenItStarts() {
            var holder = new Holder();
            Raven.Sequence()
                .Chain(Raven.CustomTo(holder, h => h.Position, new Vector3(1f, 0f, 0f), 1f, (h, p) => h.Position = p))
                .Chain(Raven.CustomTo(holder, h => h.Position, new Vector3(1f, 1f, 0f), 1f, (h, p) => h.Position = p));
            Step(0.5f, 3);
            Assert.That(Vector3.Distance(holder.Position, new Vector3(1f, 0.5f, 0f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void ShakeCamera_ReturnsToRest_EvenWhenRetriggered() {
            var camera = _go.AddComponent<Camera>();
            _go.transform.localPosition = new Vector3(1f, 2f, 3f);
            _go.transform.localRotation = Quaternion.Euler(10f, 20f, 0f);
            Raven.ShakeCamera(camera, 1f, 0.5f);
            Step(0.1f);
            Raven.ShakeCamera(camera, 1f, 0.5f);
            Step(0.1f, 10);
            Assert.That(Vector3.Distance(_go.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(_go.transform.localRotation, Quaternion.Euler(10f, 20f, 0f)), Is.LessThan(0.05f));
            Assert.That(Raven.CountTweens(_go.transform), Is.EqualTo(0));
        }

        [Test]
        public void TweenSettings_PlaysWithItsValues() {
            var settings = new TweenSettings<Vector3>(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), 1f, Ease.Linear);
            Raven.LocalPosition(_go.transform, settings);
            Step(0.5f);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void DelayedChild_WinsTheProperty_WhenItActuallyStarts() {
            Transform tr = _go.transform;
            Raven.Sequence()
                .Insert(0f, Raven.LocalPosition(tr, new Vector3(2f, 0f, 0f), 1f).Delay(1f))
                .Insert(0.2f, Raven.LocalPosition(tr, new Vector3(1f, 0f, 0f), 1f));
            Step(0.5f, 3);
            Assert.That(tr.localPosition.x, Is.EqualTo(1.4f).Within(1e-3f));
        }

        // ----- Second-review regressions -----

        [Test]
        public void JumpingInASequence_IsSilent_PlayingFiresAgain() {
            int callbacks = 0, starts = 0, completes = 0;
            Sequence s = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f).OnStart(() => starts++).OnComplete(() => completes++))
                .InsertCallback(0.2f, () => callbacks++)
                .Chain(Raven.Value(0f, 1f, 1f));
            Step(1f);
            Assert.That(new[] { callbacks, starts, completes }, Is.EqualTo(new[] { 1, 1, 1 }));
            s.ElapsedTime = 1.05f;
            s.ElapsedTime = 0.1f;
            s.ElapsedTime = 0.9f;
            s.ElapsedTime = 0.1f;
            Assert.That(new[] { callbacks, starts, completes }, Is.EqualTo(new[] { 1, 1, 1 }), "Jumps are silent.");
            Step(0.2f);
            Assert.That(callbacks, Is.EqualTo(2), "Playing over the callback again fires it.");
        }

        [Test]
        public void CallbackThatRewindsItsOwnSequence_ReplaysIt() {
            int loops = 0;
            Sequence s = default;
            s = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).ChainCallback(() => {
                if (++loops < 3) { s.ElapsedTimeTotal = 0f; }
            });
            Step(0.5f, 7);
            Assert.That(loops, Is.EqualTo(3));
            Assert.That(s.IsAlive, Is.False);
        }

        [Test]
        public void SettingOwnTimeFromOnUpdate_DoesNotRecurse() {
            Tween t = default;
            t = Raven.Value(0f, 1f, 1f).OnUpdate(() => t.ElapsedTime = 0.5f);
            Assert.DoesNotThrow(() => Step(0.1f));
            Assert.That(t.ElapsedTime, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void NestedMultiCycleSequence_FiresItsCallbackEveryCycle() {
            int n = 0;
            Sequence inner = Raven.Sequence().Chain(Raven.Value(0f, 1f, 0.5f)).ChainCallback(() => n++).Cycles(3);
            Raven.Sequence().Chain(inner);
            Step(0.25f, 8);
            Assert.That(n, Is.EqualTo(3));
        }

        [Test]
        public void NestedYoyoSequence_FiresItsCallbackBothWays() {
            int n = 0;
            Sequence inner = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).InsertCallback(0.25f, () => n++).Cycles(2, CycleMode.Yoyo);
            Raven.Sequence().Chain(inner);
            Step(0.25f, 9);
            Assert.That(n, Is.EqualTo(2));
        }

        [Test]
        public void CompleteAll_WithAThrowingSetter_CompletesEveryOtherTween() {
            LogAssert.ignoreFailingMessages = true;
            var holder = new Holder();
            var others = new List<Tween>();
            for (int i = 0; i < 3; i++) { others.Add(Raven.Value(0f, 1f, 1f)); }
            Raven.Custom(holder, 0f, 1f, 1f, (h, v) => { if (v > 0.5f) { throw new System.InvalidOperationException("boom"); } });
            for (int i = 0; i < 3; i++) { others.Add(Raven.Value(0f, 1f, 1f)); }
            Assert.DoesNotThrow(() => Raven.CompleteAll());
            Assert.That(others.TrueForAll(t => !t.IsAlive), Is.True);
            Assert.That(Raven.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void StoppingInOnUpdate_DoesNotRunTheNextTweensCallbacks() {
            float newValue = -1f;
            bool spawned = false;
            Tween first = default;
            first = Raven.Value(0f, 1f, 1f).OnUpdate((float v) => {
                if (spawned) { return; }
                spawned = true;
                first.Stop();
                Raven.Value(5f, 6f, 1f).OnUpdate(() => newValue = 99f);
            });
            Step(0.1f);
            Assert.That(newValue, Is.EqualTo(-1f), "A tween created in a callback must not receive the old tween's update.");
        }

        [Test]
        public void SequenceChild_ReportsItsProgress() {
            var holder = new Holder();
            Raven.Sequence().ChainDelay(0.5f).Chain(Raven.Value(0f, 1f, 1f).OnUpdate(holder, (h, t) => h.Last = t.Progress));
            Step(1f);
            Assert.That(holder.Last, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void ACurveOrFunction_ReplacesAParametricEase() {
            float v = 0f;
            Raven.Value(0f, 1f, 1f).Ease(Easing.Overshoot(3f)).Ease(t => t).OnUpdate(x => v = x);
            Step(0.5f);
            Assert.That(v, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void ProgressOne_ShowsTheEndOfTheCurrentCycle() {
            Tween t = Raven.LocalPosition(_go.transform, new Vector3(10f, 0f, 0f), 1f).Cycles(3);
            t.Progress = 1f;
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(t.CyclesDone, Is.EqualTo(0));
            Assert.That(t.Progress, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void CallbackOnlyYoyoSequence_FiresOnce() {
            int n = 0;
            Sequence s = Raven.Sequence().ChainCallback(() => n++).Cycles(2, CycleMode.Yoyo);
            Step(0.1f);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(s.IsAlive, Is.False);
        }

        // ----- Third-review regressions -----

        [Test]
        public void SetRemainingCyclesEveryFrame_StillCompletes() {
            bool stop = false, done = false;
            Tween t = default;
            t = Raven.Value(0f, 1f, 1f).Infinite(CycleMode.Yoyo)
                .OnUpdate(() => { if (stop) { t.SetRemainingCycles(1); } })
                .OnComplete(() => done = true);
            Step(0.5f);
            stop = true;
            Step(0.5f, 4);
            Assert.That(done, Is.True);
        }

        [Test]
        public void SetRemainingCyclesFromACallback_DoesNotSkipOrRepeatCycles() {
            int left = 2, calls = 0;
            Sequence s = default;
            s = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).ChainCallback(() => {
                calls++;
                if (--left == 0) { s.SetRemainingCycles(1); }
            }).Infinite();
            Step(0.25f, 20);
            Assert.That(calls, Is.EqualTo(2), "SetRemainingCycles(1) at the end of a cycle makes that cycle the last.");
            Assert.That(s.IsAlive, Is.False);
        }

        [Test]
        public void StickyStopCheckAtTheEndOfASequence_Completes() {
            bool stop = false;
            Sequence s = default;
            s = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).ChainCallback(() => { if (stop) { s.SetRemainingCycles(1); } }).Infinite();
            Step(0.5f);
            stop = true;
            Step(0.5f, 6);
            Assert.That(s.IsAlive, Is.False);
        }

        [Test]
        public void JumpingPastANestedSequence_DoesNotFireItsEndCallbackLater() {
            int n = 0;
            Sequence inner = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).ChainCallback(() => n++);
            Sequence outer = Raven.Sequence().Chain(inner).Chain(Raven.Value(0f, 1f, 3f));
            outer.ElapsedTimeTotal = 2.5f;
            Step(0.1f, 3);
            Assert.That(n, Is.EqualTo(0));
        }

        [Test]
        public void JumpingOntoACallbackOnlyNestedSequence_KeepsItsCompletion() {
            int done = 0;
            Sequence inner = Raven.Sequence().ChainCallback(() => { });
            inner.OnComplete(() => done++);
            Sequence outer = Raven.Sequence().Chain(Raven.Value(0f, 1f, 1f)).Chain(inner).Chain(Raven.Value(0f, 1f, 1f));
            Step(1.5f);
            Assert.That(done, Is.EqualTo(1));
            outer.ElapsedTimeTotal = 1f;
            Assert.That(done, Is.EqualTo(1));
            Step(0.1f);
            Assert.That(done, Is.EqualTo(2), "Playing from exactly its spot completes it again.");
        }

        [Test]
        public void ChildReachingItsEnd_DoesNotCompleteWhenItsUpdateMovedTheRoot() {
            bool jumped = false;
            int completes = 0;
            Sequence root = default;
            Tween a = default;
            a = Raven.Value(0f, 1f, 0.5f)
                .OnUpdate(() => {
                    if (jumped || a.Progress < 1f) { return; }
                    jumped = true;
                    root.ElapsedTimeTotal = 0.1f;
                })
                .OnComplete(() => completes++);
            root = Raven.Sequence().Chain(a).Chain(Raven.Value(0f, 1f, 1f));
            Step(0.25f, 2);
            Assert.That(completes, Is.EqualTo(0));
            Step(0.25f, 2);
            Assert.That(completes, Is.EqualTo(1));
        }

        [Test]
        public void SetRemainingCyclesInAChild_KeepsEvaluatingLaterChildren() {
            Sequence s = default;
            s = Raven.Sequence()
                .Group(Raven.Value(0f, 1f, 1f).OnUpdate(() => s.SetRemainingCycles(1)))
                .Group(Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f));
            Step(0.5f);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void ProgressZero_OnALaterCycle_StaysInThatCycle() {
            Tween t = Raven.LocalPosition(_go.transform, new Vector3(10f, 0f, 0f), 1f).Cycles(3);
            Step(1.5f);
            t.Progress = 0f;
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(t.CyclesDone, Is.EqualTo(1));
            Assert.That(t.Progress, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void JumpingBackToZero_ReplaysTheStartCallbacks() {
            int calls = 0, starts = 0;
            Sequence s = Raven.Sequence()
                .InsertCallback(0f, () => calls++)
                .Chain(Raven.Value(0f, 1f, 1f).OnStart(() => starts++))
                .Chain(Raven.Value(0f, 1f, 1f));
            Step(1.5f);
            s.ProgressTotal = 0f;
            Assert.That(new[] { calls, starts }, Is.EqualTo(new[] { 1, 1 }), "The jump itself is silent.");
            Step(0.1f);
            Assert.That(new[] { calls, starts }, Is.EqualTo(new[] { 2, 2 }), "Playing from 0 again fires what sits at 0.");
        }

        [Test]
        public void NestedCyclesInAYoyoParent_DoNotFireTwiceInOneFrame() {
            int completes = 0;
            Sequence nested = Raven.Sequence().Chain(Raven.Value(0f, 1f, 0.5f).OnComplete(() => completes++)).Cycles(2);
            Raven.Sequence().Chain(nested).Cycles(2, CycleMode.Yoyo);
            Step(0.25f, 9);
            Assert.That(completes, Is.EqualTo(3));
        }

        [Test]
        public void JumpFromDeepInsideANestedSequence_IsNotOverwritten() {
            bool jumped = false;
            Sequence root = default;
            Sequence inner = Raven.Sequence()
                .Chain(Raven.Value(0f, 1f, 0.5f).OnComplete(() => {
                    if (jumped) { return; }
                    jumped = true;
                    root.ElapsedTimeTotal = 0.1f;
                }))
                .Insert(0f, Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f));
            root = Raven.Sequence().Chain(inner);
            Step(0.25f, 2);
            Assert.That(_go.transform.localPosition.x, Is.EqualTo(0.1f).Within(1e-4f));
        }

        [Test]
        public void FinishedChild_ReportsItsTotalTime() {
            Tween child = Raven.Value(0f, 1f, 1f).Cycles(2);
            Raven.Sequence().Chain(child).ChainDelay(1f);
            Step(2.5f);
            Assert.That(child.ElapsedTimeTotal, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(child.ProgressTotal, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ThrowingCustomSetter_StopsThatTween() {
            LogAssert.ignoreFailingMessages = true;
            var holder = new Holder();
            Tween bad = Raven.Custom(holder, 0f, 1f, 1f, (h, v) => throw new System.InvalidOperationException("boom"));
            Step(0.1f);
            Assert.That(bad.IsAlive, Is.False);
        }
    }
}
