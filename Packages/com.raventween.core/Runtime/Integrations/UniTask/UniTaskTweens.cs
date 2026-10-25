using System.Threading;
using Cysharp.Threading.Tasks;

namespace RavenTween {
    public static class UniTaskTweens {
        public static async UniTask ToUniTask(this Tween tween, CancellationToken cancellationToken = default) {
            if (cancellationToken.CanBeCanceled) { _ = tween.WithCancellation(cancellationToken); }
            await tween;
            cancellationToken.ThrowIfCancellationRequested();
        }

        public static async UniTask ToUniTask(this Sequence sequence, CancellationToken cancellationToken = default) {
            if (cancellationToken.CanBeCanceled) { _ = sequence.WithCancellation(cancellationToken); }
            await sequence;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
