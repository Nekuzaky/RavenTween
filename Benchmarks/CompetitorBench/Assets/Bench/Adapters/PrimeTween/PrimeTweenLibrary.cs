using PrimeTween;
using UnityEngine;

namespace RavenBench.Adapters {
    sealed class PrimeTweenLibrary : BenchLibrary {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            BenchRegistry.Register(new PrimeTweenLibrary());
        }

        public override string Name {
            get { return "PrimeTween"; }
        }

        public override void Prepare(BenchScenario scenario, int count) {
            PrimeTweenConfig.SetTweensCapacity(scenario == BenchScenario.Tweens ? count : count * 4);
        }

        public override void StartTweens(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            Vector3[] a = targets.PointA;
            for (int i = 0; i < count; i++) {
                Tween.Position(transforms[i], a[i], BenchTargets.TweenDuration, Ease.InOutQuad);
            }
        }

        public override void StartSequences(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            const float Step = BenchTargets.StepDuration;
            for (int i = 0; i < count; i++) {
                Transform t = transforms[i];
                Sequence.Create()
                    .Chain(Tween.Position(t, targets.PointA[i], Step, Ease.InOutQuad))
                    .Chain(Tween.Position(t, targets.PointB[i], Step, Ease.InOutQuad))
                    .Chain(Tween.Position(t, targets.PointC[i], Step, Ease.InOutQuad));
            }
        }

        public override void StopAll() {
            Tween.StopAll();
        }
    }
}
