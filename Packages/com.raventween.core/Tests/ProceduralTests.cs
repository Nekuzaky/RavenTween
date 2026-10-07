using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace RavenTween.Tests {
    public sealed class ProceduralTests {
        GameObject _root;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            TweenEngine.TimeScale = 1f;
            _root = new GameObject("procedural-root");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_root);
        }

        // One frame of the procedural layer (LateUpdate).
        static void Frame(RavenLookAt lookAt, float dt) {
            lookAt.Evaluate(dt);
        }

        static void Frame(RavenSpringChain chain, float dt) {
            chain.Evaluate(dt);
        }

        RavenLookAt CreateHead(out Transform target) {
            var head = new GameObject("head");
            head.transform.SetParent(_root.transform, false);
            var lookAt = head.AddComponent<RavenLookAt>();
            target = new GameObject("target").transform;
            target.SetParent(_root.transform, false);
            lookAt.Target = target;
            lookAt.SmoothTime = 0f;
            return lookAt;
        }

        [Test]
        public void LookAt_PointsForwardAxisAtTarget() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            target.position = new Vector3(3f, 1f, 2f);
            Frame(lookAt, 0.016f);
            Vector3 forward = lookAt.transform.forward;
            Vector3 toTarget = (target.position - lookAt.transform.position).normalized;
            Assert.That(Vector3.Angle(forward, toTarget), Is.LessThan(0.5f));
        }

        [Test]
        public void LookAt_ClampsToMaxAngle() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            lookAt.MaxAngle = 45f;
            target.position = new Vector3(0f, 0f, -5f); // directly behind
            Frame(lookAt, 0.016f);
            float turned = Vector3.Angle(Vector3.forward, lookAt.transform.forward);
            Assert.That(turned, Is.EqualTo(45f).Within(0.5f));
        }

        [Test]
        public void LookAt_WeightZero_LeavesPoseUntouched() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            target.position = new Vector3(5f, 0f, 0f);
            lookAt.Weight = 0f;
            Frame(lookAt, 0.016f);
            Assert.That(Quaternion.Angle(lookAt.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void LookAt_DoesNotAccumulateAcrossFrames() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            lookAt.MaxAngle = 30f;
            target.position = new Vector3(5f, 0f, 0f);
            for (int i = 0; i < 50; i++) { Frame(lookAt, 0.016f); }
            float turned = Vector3.Angle(Vector3.forward, lookAt.transform.forward);
            Assert.That(turned, Is.EqualTo(30f).Within(0.5f), "Restoring the pose each frame must keep the clamp absolute.");
        }

        [Test]
        public void LookAt_TweenWeight_BlendsIn() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            lookAt.Weight = 0f;
            target.position = new Vector3(5f, 0f, 0f);
            lookAt.TweenWeight(1f, 1f);
            TweenEngine.Process(0.5f, 0.5f);
            Assert.That(lookAt.Weight, Is.EqualTo(0.5f).Within(1e-3f));
            TweenEngine.Process(0.5f, 0.5f);
            Assert.That(lookAt.Weight, Is.EqualTo(1f).Within(1e-3f));
        }

        RavenSpringChain CreateChain(int bones, float spacing) {
            Transform parent = _root.transform;
            Transform first = null;
            for (int i = 0; i < bones; i++) {
                var bone = new GameObject("bone" + i).transform;
                bone.SetParent(parent, false);
                bone.localPosition = i == 0 ? Vector3.zero : new Vector3(spacing, 0f, 0f);
                if (first == null) { first = bone; }
                parent = bone;
            }
            var chain = first.gameObject.AddComponent<RavenSpringChain>();
            chain.Build();
            return chain;
        }

        [Test]
        public void Spring_Gravity_MakesTipDroop() {
            RavenSpringChain chain = CreateChain(4, 0.5f);
            chain.Gravity = new Vector3(0f, -9.81f, 0f);
            chain.Stiffness = 0.005f;
            Transform tip = chain.transform.GetChild(0).GetChild(0).GetChild(0);
            for (int i = 0; i < 180; i++) { Frame(chain, 1f / 60f); }
            Assert.That(tip.position.y, Is.LessThan(-0.1f), "A soft chain under gravity must droop.");
        }

        [Test]
        public void Spring_SurvivesTeleport_AndResetSnapsBack() {
            RavenSpringChain chain = CreateChain(5, 0.4f);
            chain.Gravity = new Vector3(0f, -9.81f, 0f);
            Transform tip = chain.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
            _root.transform.position = new Vector3(1000f, -500f, 250f);
            for (int i = 0; i < 30; i++) { Frame(chain, 1f / 60f); }
            Vector3 p = tip.position;
            Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), Is.False, "A teleport must never produce NaN.");
            chain.Weight = 0f;
            chain.ResetPhysics();
            Frame(chain, 1f / 60f);
            Assert.That(Vector3.Distance(tip.position, new Vector3(1001.6f, -500f, 250f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void Spring_LagsBehindMovingRoot_ThenSettles() {
            RavenSpringChain chain = CreateChain(4, 0.5f);
            chain.Stiffness = 0.1f;
            chain.Damping = 0.2f;
            Transform tip = chain.transform.GetChild(0).GetChild(0).GetChild(0);
            _root.transform.position = new Vector3(0f, 2f, 0f); // sudden move
            Frame(chain, 1f / 60f);
            float lag = Vector3.Distance(tip.position, new Vector3(1.5f, 2f, 0f));
            Assert.That(lag, Is.GreaterThan(0.05f), "The tip must lag behind a sudden move.");
            for (int i = 0; i < 600; i++) { Frame(chain, 1f / 60f); }
            Assert.That(Vector3.Distance(tip.position, new Vector3(1.5f, 2f, 0f)), Is.LessThan(0.01f),
                "Without gravity the chain must settle back on its animated pose.");
        }

        [Test]
        public void Spring_WeightZero_LeavesPoseUntouched() {
            RavenSpringChain chain = CreateChain(3, 0.5f);
            chain.Gravity = new Vector3(0f, -9.81f, 0f);
            chain.Weight = 0f;
            Transform tip = chain.transform.GetChild(0).GetChild(0);
            for (int i = 0; i < 60; i++) { Frame(chain, 1f / 60f); }
            Assert.That(Vector3.Distance(tip.position, new Vector3(1f, 0f, 0f)), Is.LessThan(1e-4f));
        }

        // Audit regressions.

        [Test]
        public void LookAt_KeepsRotationSetByOthers_AsNewBasePose() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            lookAt.Weight = 0f;
            target.position = new Vector3(5f, 0f, 0f);
            Frame(lookAt, 0.016f);
            lookAt.transform.localRotation = Quaternion.Euler(0f, 45f, 0f); // e.g. a finished tween
            for (int i = 0; i < 5; i++) { Frame(lookAt, 0.016f); }
            Assert.That(Quaternion.Angle(lookAt.transform.localRotation, Quaternion.Euler(0f, 45f, 0f)), Is.LessThan(0.01f),
                "A rotation written by someone else must stick, not snap back to the OnEnable pose.");
        }

        [Test]
        public void LookAt_TweenWeight_ReadsWeightWhenItStarts() {
            RavenLookAt lookAt = CreateHead(out Transform target);
            lookAt.Weight = 1f;
            Raven.Sequence().Chain(lookAt.TweenWeight(0f, 0.5f)).Chain(lookAt.TweenWeight(1f, 0.5f));
            TweenEngine.Process(0.5f, 0.5f);
            TweenEngine.Process(0.25f, 0.25f);
            Assert.That(lookAt.Weight, Is.EqualTo(0.5f).Within(1e-3f), "The second blend must start from 0, not from the weight at build time.");
        }

        [Test]
        public void Spring_DestroyedBone_RebuildsInsteadOfThrowing() {
            RavenSpringChain chain = CreateChain(4, 0.5f);
            for (int i = 0; i < 5; i++) { Frame(chain, 1f / 60f); }
            Object.DestroyImmediate(chain.transform.GetChild(0).GetChild(0).gameObject);
            Assert.DoesNotThrow(() => { for (int i = 0; i < 5; i++) { Frame(chain, 1f / 60f); } });
            Assert.That(chain.BoneCount, Is.EqualTo(2));
        }

        [Test]
        public void Spring_Build_DoesNotCaptureSwungPose() {
            RavenSpringChain chain = CreateChain(3, 0.5f);
            chain.Gravity = new Vector3(0f, -9.81f, 0f);
            chain.Stiffness = 0.005f;
            for (int i = 0; i < 120; i++) { Frame(chain, 1f / 60f); }
            chain.Build();
            chain.Gravity = Vector3.zero;
            chain.Stiffness = 0.3f;
            for (int i = 0; i < 600; i++) { Frame(chain, 1f / 60f); }
            Transform tip = chain.transform.GetChild(0).GetChild(0);
            Assert.That(Vector3.Distance(tip.position, new Vector3(1f, 0f, 0f)), Is.LessThan(0.01f),
                "Rebuilding mid-swing must keep the original base pose.");
        }

        [Test]
        public void ProceduralFrame_AllocatesZeroBytes() {
            RavenSpringChain chain = CreateChain(8, 0.3f);
            chain.Gravity = new Vector3(0f, -9.81f, 0f);
            RavenLookAt lookAt = CreateHead(out Transform target);
            target.position = new Vector3(1f, 2f, 3f);
            for (int i = 0; i < 10; i++) { Frame(chain, 1f / 60f); Frame(lookAt, 1f / 60f); }
            Assert.That(() => {
                for (int i = 0; i < 120; i++) { Frame(chain, 1f / 60f); Frame(lookAt, 1f / 60f); }
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
