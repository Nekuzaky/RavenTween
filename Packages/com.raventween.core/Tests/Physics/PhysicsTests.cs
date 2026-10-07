using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenTween.Tests {
    /// <summary>Rigidbody tweens, played by the real player loop so FixedUpdate and physics run.</summary>
    public sealed class PhysicsTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            _go = new GameObject("physics-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator Rigidbody_MovePositionAndRotation_ReachTheirTargets() {
            var body = _go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            Tween move = body.TweenMovePosition(new Vector3(0f, 0f, 5f), 0.2f);
            body.TweenMoveRotation(Quaternion.Euler(0f, 90f, 0f), 0.2f);
            Assert.That(move.IsAlive, Is.True);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Vector3.Distance(_go.transform.position, new Vector3(0f, 0f, 5f)), Is.LessThan(1e-3f));
            Assert.That(Quaternion.Angle(_go.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.1f));
            Assert.That(move.IsAlive, Is.False);
        }

        [UnityTest]
        public IEnumerator Rigidbody2D_MovePositionAndRotation_ReachTheirTargets() {
            var body = _go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.TweenMovePosition(new Vector2(3f, -2f), 0.2f);
            body.TweenMoveRotation(45f, 0.2f);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Vector2.Distance(body.position, new Vector2(3f, -2f)), Is.LessThan(1e-3f));
            Assert.That(body.rotation, Is.EqualTo(45f).Within(0.1f));
        }

        [Test]
        public void RigidbodyTweens_RunInFixedUpdate() {
            var body = _go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.TweenMovePosition(Vector3.one, 1f);
            Assert.That(Raven.CountTweens(body), Is.EqualTo(1));
            var infos = new System.Collections.Generic.List<TweenEngine.DebugInfo>();
            TweenEngine.CollectDebugInfo(infos);
            Assert.That(infos.Exists(i => i.Phase == UpdatePhase.FixedUpdate), Is.True);
        }
    }
}
