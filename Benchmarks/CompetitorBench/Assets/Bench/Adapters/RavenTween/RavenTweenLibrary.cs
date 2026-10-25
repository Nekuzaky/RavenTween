using RavenTween;
using UnityEngine;

namespace RavenBench.Adapters {
    sealed class RavenTweenLibrary : BenchLibrary {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            BenchRegistry.Register(new RavenTweenLibrary());
        }

        public override string Name {
            get { return "RavenTween"; }
        }

        public override void Prepare(BenchScenario scenario, int count) {
            Raven.SetCapacity(scenario == BenchScenario.Tweens ? count : count * 4);
        }

        public override void StartTweens(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            Vector3[] a = targets.PointA;
            for (int i = 0; i < count; i++) {
                Raven.Position(transforms[i], a[i], BenchTargets.TweenDuration).Ease(Ease.InOutQuad);
            }
        }

        public override void StartSequences(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            const float Step = BenchTargets.StepDuration;
            for (int i = 0; i < count; i++) {
                Transform t = transforms[i];
                Raven.Sequence()
                    .Chain(Raven.Position(t, targets.PointA[i], Step).Ease(Ease.InOutQuad))
                    .Chain(Raven.Position(t, targets.PointB[i], Step).Ease(Ease.InOutQuad))
                    .Chain(Raven.Position(t, targets.PointC[i], Step).Ease(Ease.InOutQuad));
            }
        }

        public override void StopAll() {
            Raven.StopAll();
        }
    }
}
