using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
using UnityEngine;

namespace RavenBench.Adapters {
    sealed class LitMotionLibrary : BenchLibrary {
        MotionHandle[] _handles = new MotionHandle[0];
        int _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            BenchRegistry.Register(new LitMotionLibrary());
        }

        public override string Name {
            get { return "LitMotion"; }
        }

        public override void Prepare(BenchScenario scenario, int count) {
            _handles = new MotionHandle[count];
            MotionDispatcher.EnsureStorageCapacity<Vector3, NoOptions, Vector3MotionAdapter>(scenario == BenchScenario.Tweens ? count : count * 3);
        }

        public override void StartTweens(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            Vector3[] a = targets.PointA;
            for (int i = 0; i < count; i++) {
                Transform t = transforms[i];
                _handles[i] = LMotion.Create(t.position, a[i], BenchTargets.TweenDuration)
                    .WithEase(Ease.InOutQuad)
                    .BindToPosition(t);
            }
            _started = count;
        }

        public override void StartSequences(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            const float Step = BenchTargets.StepDuration;
            for (int i = 0; i < count; i++) {
                Transform t = transforms[i];
                Vector3 a = targets.PointA[i];
                Vector3 b = targets.PointB[i];
                _handles[i] = LSequence.Create()
                    .Append(LMotion.Create(t.position, a, Step).WithEase(Ease.InOutQuad).BindToPosition(t))
                    .Append(LMotion.Create(a, b, Step).WithEase(Ease.InOutQuad).BindToPosition(t))
                    .Append(LMotion.Create(b, targets.PointC[i], Step).WithEase(Ease.InOutQuad).BindToPosition(t))
                    .Run();
            }
            _started = count;
        }

        public override void StopAll() {
            for (int i = 0; i < _started; i++) {
                _handles[i].TryCancel();
            }
            _started = 0;
        }
    }
}
