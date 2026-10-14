using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class UniTaskTweenTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            Raven.StopAll();
            _go = new GameObject("unitask-target");
        }

        [TearDown]
        public void TearDown() {
            Raven.StopAll();
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void ToUniTask_EndsWithTheTween() {
            UniTask task = Raven.Position(_go.transform, Vector3.one, 0.5f).ToUniTask();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Pending));
            Raven.Step(0.6f);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void ToUniTask_Sequence_EndsWithTheSequence() {
            UniTask task = Raven.Sequence()
                .Chain(Raven.Position(_go.transform, Vector3.one, 0.3f))
                .Chain(Raven.Scale(_go.transform, 2f, 0.3f))
                .ToUniTask();
            Raven.Step(0.4f);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Pending));
            Raven.Step(0.3f);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void Cancelling_StopsTheTween_AndCancelsTheTask() {
            var cancel = new CancellationTokenSource();
            Tween tween = Raven.Position(_go.transform, Vector3.one, 1f);
            UniTask task = tween.ToUniTask(cancel.Token);
            Raven.Step(0.2f);
            cancel.Cancel();
            Raven.Step(0.1f);
            Assert.That(tween.IsAlive, Is.False, "The tween stops.");
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            cancel.Dispose();
        }
    }
}
