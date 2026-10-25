using DG.Tweening;
using UnityEngine;

namespace RavenBench.Adapters {
    sealed class DOTweenLibrary : BenchLibrary {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            BenchRegistry.Register(new DOTweenLibrary());
        }

        public override string Name {
            get { return "DOTween"; }
        }

        public override string Version {
            get { return DOTween.Version; }
        }

        public override void Prepare(BenchScenario scenario, int count) {
            DOTween.Init();
            if (scenario == BenchScenario.Tweens) { DOTween.SetTweensCapacity(count + 100, 50); }
            else { DOTween.SetTweensCapacity(count * 3 + 100, count + 50); }
        }

        public override void StartTweens(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            Vector3[] a = targets.PointA;
            for (int i = 0; i < count; i++) {
                transforms[i].DOMove(a[i], BenchTargets.TweenDuration).SetEase(Ease.InOutQuad);
            }
        }

        public override void StartSequences(BenchTargets targets, int count) {
            Transform[] transforms = targets.Transforms;
            const float Step = BenchTargets.StepDuration;
            for (int i = 0; i < count; i++) {
                Transform t = transforms[i];
                DOTween.Sequence()
                    .Append(t.DOMove(targets.PointA[i], Step).SetEase(Ease.InOutQuad))
                    .Append(t.DOMove(targets.PointB[i], Step).SetEase(Ease.InOutQuad))
                    .Append(t.DOMove(targets.PointC[i], Step).SetEase(Ease.InOutQuad))
                    .SetEase(Ease.Linear);
            }
        }

        public override void StopAll() {
            DOTween.KillAll();
        }
    }
}
