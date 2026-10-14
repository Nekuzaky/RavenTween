using System.Threading;
using Cysharp.Threading.Tasks;

namespace RavenTween {
    /// <summary>
    /// UniTask support: <c>await tween.ToUniTask(token)</c>. It ends when the tween ends (completed
    /// or stopped); cancelling the token stops the tween and throws OperationCanceledException.
    /// UniTask pools its async state machines, so awaiting does not allocate once warmed up.
    /// </summary>
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
