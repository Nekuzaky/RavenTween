using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenTween.Tests {
    /// <summary>Integration tests running against the real player loop.</summary>
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

        [UnityTest]
        public IEnumerator PropertyTween_MovesTransform() {
            var go = new GameObject("tween-target");
            Tween tween = Raven.Position(go.transform, new Vector3(0f, 10f, 0f), 0.2f);
            yield return tween.ToYieldInstruction();
            Assert.That(go.transform.position.y, Is.EqualTo(10f).Within(1e-3f));
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator DestroyedTarget_KillsTween_WithoutErrors() {
            var go = new GameObject("doomed");
            bool destroyedCallback = false;
            Tween tween = Raven.Position(go.transform, Vector3.one * 100f, 5f)
                .OnTargetDestroyed(() => destroyedCallback = true);
            yield return null;
            Object.Destroy(go);
            yield return null;
            yield return null;
            Assert.That(tween.IsAlive, Is.False, "Tween must die when its target is destroyed.");
            Assert.That(destroyedCallback, Is.True);
        }

        [UnityTest]
        public IEnumerator UnscaledTween_IgnoresTimeScaleZero() {
            Time.timeScale = 0f;
            Tween tween = Raven.Value(0f, 1f, 0.2f).UnscaledTime();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (tween.IsAlive && Time.realtimeSinceStartup < deadline) { yield return null; }
            Time.timeScale = 1f;
            Assert.That(tween.IsAlive, Is.False, "Unscaled tween must finish while timeScale is 0.");
        }

        [UnityTest]
        public IEnumerator ScaledTween_FreezesAtTimeScaleZero() {
            Time.timeScale = 0f;
            Tween tween = Raven.Value(0f, 1f, 0.1f);
            for (int i = 0; i < 10; i++) { yield return null; }
            Assert.That(tween.IsAlive, Is.True, "Scaled tween must freeze while timeScale is 0.");
            Time.timeScale = 1f;
            yield return tween.ToYieldInstruction();
            Assert.That(tween.IsAlive, Is.False);
        }

        [UnityTest]
        public IEnumerator Coroutine_Yield_WaitsForCompletion() {
            float value = 0f;
            Tween tween = Raven.Value(0f, 5f, 0.2f).OnUpdate(v => value = v);
            yield return tween.ToYieldInstruction();
            Assert.That(value, Is.EqualTo(5f).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator AsyncAwait_ResumesAfterCompletion() {
            bool finished = false;
            AwaitTween();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!finished && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assert.That(finished, Is.True, "await must resume after the tween completes.");

            async void AwaitTween() {
                await Raven.Delay(0.2f);
                finished = true;
            }
        }

        [UnityTest]
        public IEnumerator Template_PlaysOnTarget() {
            var template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.property = PropertyKind.LocalScale;
            template.endValue = new Vector4(2f, 2f, 2f, 0f);
            template.settings = TweenParams.Default;
            template.settings.duration = 0.2f;
            var go = new GameObject("template-target");
            Tween tween = template.Play(go.transform);
            Assert.That(tween.IsAlive, Is.True);
            yield return tween.ToYieldInstruction();
            Assert.That(go.transform.localScale.x, Is.EqualTo(2f).Within(1e-3f));
            Object.Destroy(go);
            Object.Destroy(template);
        }

        [UnityTest]
        public IEnumerator Animator_PlaysEntries_AndRaisesEvent() {
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
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!completed && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assert.That(completed, Is.True);
            Assert.That(go.transform.localPosition.y, Is.EqualTo(3f).Within(1e-3f));
            Object.Destroy(go);
            Object.Destroy(template);
        }
    }
}
