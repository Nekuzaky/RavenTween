using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.TestTools;

namespace RavenTween.Tests {
    public sealed class PlayModeTests {
        [SetUp]
        public void SetUp() {
            Raven.StopAll();
            Raven.TimeScale = 1f;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown() {
            Raven.StopAll();
            Time.timeScale = 1f;
        }

        static void Step(float scaled, float unscaled, int frames = 1) {
            for (int i = 0; i < frames; i++) { TweenEngine.Process(scaled, unscaled); }
        }

        [Test]
        public void ReplacedPlayerLoop_EngineReinstallsItself() {
            PlayerLoopSystem original = PlayerLoop.GetCurrentPlayerLoop();
            var go = new GameObject("loop-target");
            try {
                PlayerLoop.SetPlayerLoop(WithoutEngine(original));
                SimulateStalledPlayerLoop();
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("player loop was replaced"));
                Raven.Position(go.transform, new Vector3(5f, 0f, 0f), 0.1f);
                PlayerLoopSystem.UpdateFunction update = FindEngineSystem(PlayerLoop.GetCurrentPlayerLoop(), typeof(Update));
                Assert.That(update, Is.Not.Null, "The engine is back in the Update phase.");
                Step(0.2f, 0.2f);
                Assert.That(go.transform.position.x, Is.EqualTo(5f).Within(1e-3f));
            } finally {
                PlayerLoop.SetPlayerLoop(original);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EngineSystems_SitInUpdatePreLateUpdateAndFixedUpdate() {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            Assert.That(FindEngineSystem(loop, typeof(Update)), Is.Not.Null);
            Assert.That(FindEngineSystem(loop, typeof(PreLateUpdate)), Is.Not.Null);
            Assert.That(FindEngineSystem(loop, typeof(FixedUpdate)), Is.Not.Null);
            Assert.That(IndexIn(loop, typeof(FixedUpdate), typeof(TweenEngine)),
                Is.EqualTo(IndexIn(loop, typeof(FixedUpdate), typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate)) + 1),
                "Fixed tweens run right after the scripts' FixedUpdate, before the physics step.");
        }

        static void SimulateStalledPlayerLoop() {
            const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
            typeof(TweenEngine).GetField("_lastUpdateFrame", Flags).SetValue(null, Time.frameCount - 3);
            typeof(TweenEngine).GetField("_lastLoopCheckFrame", Flags).SetValue(null, -1);
        }

        static PlayerLoopSystem WithoutEngine(PlayerLoopSystem root) {
            var phases = (PlayerLoopSystem[])root.subSystemList.Clone();
            for (int i = 0; i < phases.Length; i++) {
                PlayerLoopSystem[] children = phases[i].subSystemList;
                if (children == null) { continue; }
                var kept = new List<PlayerLoopSystem>(children.Length);
                for (int j = 0; j < children.Length; j++) {
                    if (children[j].type != typeof(TweenEngine)) { kept.Add(children[j]); }
                }
                phases[i].subSystemList = kept.ToArray();
            }
            root.subSystemList = phases;
            return root;
        }

        internal static PlayerLoopSystem.UpdateFunction FindEngineSystem(PlayerLoopSystem root, System.Type phase) {
            int at = IndexIn(root, phase, typeof(TweenEngine));
            if (at < 0) { return null; }
            for (int i = 0; i < root.subSystemList.Length; i++) {
                if (root.subSystemList[i].type == phase) { return root.subSystemList[i].subSystemList[at].updateDelegate; }
            }
            return null;
        }

        static int IndexIn(PlayerLoopSystem root, System.Type phase, System.Type system) {
            for (int i = 0; i < root.subSystemList.Length; i++) {
                if (root.subSystemList[i].type != phase || root.subSystemList[i].subSystemList == null) { continue; }
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList;
                for (int j = 0; j < children.Length; j++) {
                    if (children[j].type == system) { return j; }
                }
            }
            return -1;
        }

        [Test]
        public void PropertyTween_MovesTransform() {
            var go = new GameObject("tween-target");
            Raven.Position(go.transform, new Vector3(0f, 10f, 0f), 0.2f);
            Step(0.25f, 0.25f);
            Assert.That(go.transform.position.y, Is.EqualTo(10f).Within(1e-3f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DestroyedTarget_KillsTween_WithoutErrors() {
            var go = new GameObject("doomed");
            bool destroyedCallback = false;
            Tween tween = Raven.Position(go.transform, Vector3.one * 100f, 5f)
                .OnTargetDestroyed(() => destroyedCallback = true);
            Step(0.016f, 0.016f);
            Object.DestroyImmediate(go);
            Step(0.016f, 0.016f);
            Assert.That(tween.IsAlive, Is.False, "Tween must die when its target is destroyed.");
            Assert.That(destroyedCallback, Is.True);
        }

        [Test]
        public void UnscaledTween_IgnoresTimeScaleZero() {
            Tween tween = Raven.Value(0f, 1f, 0.2f).UnscaledTime();
            Step(0f, 0.05f, 5);
            Assert.That(tween.IsAlive, Is.False, "Unscaled tween must finish while timeScale is 0.");
        }

        [Test]
        public void ScaledTween_FreezesAtTimeScaleZero() {
            Tween tween = Raven.Value(0f, 1f, 0.1f);
            Step(0f, 0.05f, 10);
            Assert.That(tween.IsAlive, Is.True, "Scaled tween must freeze while timeScale is 0.");
            Step(0.05f, 0.05f, 3);
            Assert.That(tween.IsAlive, Is.False);
        }

        [Test]
        public void YieldInstruction_KeepsWaitingUntilTheTweenEnds() {
            float value = 0f;
            Tween tween = Raven.Value(0f, 5f, 0.2f).OnUpdate(v => value = v);
            CustomYieldInstruction wait = tween.ToYieldInstruction();
            Assert.That(wait.keepWaiting, Is.True);
            Step(0.1f, 0.1f);
            Assert.That(wait.keepWaiting, Is.True);
            Step(0.15f, 0.15f);
            Assert.That(wait.keepWaiting, Is.False);
            Assert.That(value, Is.EqualTo(5f).Within(1e-3f));
        }

        [Test]
        public void YieldInstruction_OfASequence_KeepsWaitingUntilItEnds() {
            Sequence sequence = Raven.Sequence().Chain(Raven.Value(0f, 1f, 0.2f)).Chain(Raven.Value(0f, 1f, 0.2f));
            CustomYieldInstruction wait = sequence.ToYieldInstruction();
            Step(0.3f, 0.3f);
            Assert.That(wait.keepWaiting, Is.True);
            Step(0.2f, 0.2f);
            Assert.That(wait.keepWaiting, Is.False);
        }

        [Test]
        public void AsyncAwait_ResumesAfterCompletion() {
            bool finished = false;
            AwaitTween();
            Assert.That(finished, Is.False);
            Step(0.1f, 0.1f);
            Assert.That(finished, Is.False);
            Step(0.15f, 0.15f);
            Assert.That(finished, Is.True, "await must resume after the tween completes.");

            async void AwaitTween() {
                await Raven.Delay(0.2f);
                finished = true;
            }
        }

        [Test]
        public void Template_PlaysOnTarget() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.LocalScale;
            template.endValue = new Vector4(2f, 2f, 2f, 0f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.2f;
            var go = new GameObject("template-target");
            Tween tween = template.Play(go.transform);
            Assert.That(tween.IsAlive, Is.True);
            Step(0.25f, 0.25f);
            Assert.That(tween.IsAlive, Is.False);
            Assert.That(go.transform.localScale.x, Is.EqualTo(2f).Within(1e-3f));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(template);
        }

        [Test]
        public void SequencePlayer_LoopingTemplate_PlaysFinitely_WithoutErrors() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.LocalScale;
            template.endValue = new Vector4(2f, 2f, 2f, 0f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.25f;
            template.settings.cycles = -1;
            template.settings.cycleMode = CycleMode.Yoyo;
            var go = new GameObject("player-host");
            var player = go.AddComponent<RavenSequencePlayer>();
            player.Steps.Add(new RavenSequencePlayer.Step {
                mode = RavenSequencePlayer.StepMode.Chain, target = go.transform, template = template
            });
            player.Play();
            Sequence sequence = player.Current;
            Assert.That(sequence.IsAlive, Is.True);
            Assert.That(sequence.Duration, Is.EqualTo(0.5f).Within(1e-4f), "Yoyo loop becomes one out-and-back.");
            TweenEngine.Process(0.6f, 0.6f);
            Assert.That(sequence.IsAlive, Is.False);
            Assert.That(go.transform.localScale.x, Is.EqualTo(1f).Within(1e-3f), "Out-and-back ends where it started.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(template);
        }

        [Test]
        public void Animator_PlaysEntries_AndRaisesEvent() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.LocalPosition;
            template.endValue = new Vector4(0f, 3f, 0f, 0f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.2f;
            var go = new GameObject("animator-target");
            var animator = go.AddComponent<RavenAnimator>();
            animator.Entries.Add(new RavenAnimator.Entry { target = go.transform, template = template });
            bool completed = false;
            animator.OnAllComplete.AddListener(() => completed = true);
            animator.Play();
            Step(0.1f, 0.1f);
            Assert.That(completed, Is.False);
            Step(0.15f, 0.15f);
            Assert.That(completed, Is.True);
            Assert.That(go.transform.localPosition.y, Is.EqualTo(3f).Within(1e-3f));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(template);
        }
    }
}
