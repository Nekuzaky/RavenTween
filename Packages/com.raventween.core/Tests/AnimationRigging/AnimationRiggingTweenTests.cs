using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RavenTween.Tests {
    public sealed class AnimationRiggingTweenTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _go = new GameObject("rig-host");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void ConstraintWeight_Tweens() {
            var ik = _go.AddComponent<TwoBoneIKConstraint>();
            ik.weight = 0f;
            ik.TweenWeight(1f, 1f);
            TweenEngine.Process(0.5f, 0.5f);
            Assert.That(ik.weight, Is.EqualTo(0.5f).Within(1e-3f));
            TweenEngine.Process(0.5f, 0.5f);
            Assert.That(ik.weight, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void RigWeight_Tweens() {
            var rig = _go.AddComponent<Rig>();
            rig.weight = 1f;
            rig.TweenWeight(0f, 1f);
            TweenEngine.Process(1f, 1f);
            Assert.That(rig.weight, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Reach_MovesTarget_AndBlendsIn() {
            var ik = _go.AddComponent<TwoBoneIKConstraint>();
            var goal = new GameObject("ik-target").transform;
            goal.SetParent(_go.transform, false);
            ik.data.target = goal;
            ik.weight = 0f;
            Sequence reach = ik.TweenReach(new Vector3(1f, 2f, 3f), 0.5f);
            Assert.That(reach.IsAlive, Is.True);
            TweenEngine.Process(0.6f, 0.6f);
            Assert.That(Vector3.Distance(goal.position, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-3f));
            Assert.That(ik.weight, Is.EqualTo(1f).Within(1e-3f));
            ik.TweenRelease(0.2f);
            TweenEngine.Process(0.3f, 0.3f);
            Assert.That(ik.weight, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Reach_WithoutTarget_ReturnsDeadHandle() {
            var ik = _go.AddComponent<TwoBoneIKConstraint>();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Sequence reach = ik.TweenReach(Vector3.one, 0.5f);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.That(reach.IsAlive, Is.False);
        }
    }
}
