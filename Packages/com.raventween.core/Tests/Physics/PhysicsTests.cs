using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    public sealed class PhysicsTests {
        const float FixedStep = 0.02f;

        GameObject _go;
        SimulationMode _simulation;
        SimulationMode2D _simulation2D;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            _simulation = Physics.simulationMode;
            _simulation2D = Physics2D.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
            _go = new GameObject("physics-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
            Physics.simulationMode = _simulation;
            Physics2D.simulationMode = _simulation2D;
        }

        static void FixedSteps(float seconds, bool physics2D) {
            for (float t = 0f; t < seconds; t += FixedStep) {
                TweenEngine.ProcessPhase(UpdatePhase.FixedUpdate, FixedStep, FixedStep);
                if (physics2D) { Physics2D.Simulate(FixedStep); }
                else { Physics.Simulate(FixedStep); }
            }
        }

        [Test]
        public void Rigidbody_MovePositionAndRotation_ReachTheirTargets() {
            var body = _go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            Tween move = body.TweenMovePosition(new Vector3(0f, 0f, 5f), 0.2f);
            body.TweenMoveRotation(Quaternion.Euler(0f, 90f, 0f), 0.2f);
            Assert.That(move.IsAlive, Is.True);
            FixedSteps(0.3f, false);
            Assert.That(Vector3.Distance(_go.transform.position, new Vector3(0f, 0f, 5f)), Is.LessThan(1e-3f));
            Assert.That(Quaternion.Angle(_go.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.1f));
            Assert.That(move.IsAlive, Is.False);
        }

        [Test]
        public void Rigidbody2D_MovePositionAndRotation_ReachTheirTargets() {
            var body = _go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.TweenMovePosition(new Vector2(3f, -2f), 0.2f);
            body.TweenMoveRotation(45f, 0.2f);
            FixedSteps(0.3f, true);
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
            TweenEngine.ProcessPhase(UpdatePhase.Update, 0.5f, 0.5f);
            Physics.Simulate(FixedStep);
            Assert.That(_go.transform.position, Is.EqualTo(Vector3.zero), "Update does not step a Rigidbody tween.");
        }
    }
}
