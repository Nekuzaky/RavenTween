using System.Threading;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// State queries and changes shared by <see cref="Tween"/> and <see cref="Sequence"/> handles.
    /// Every method is safe on a dead handle: getters return 0 / false, setters do nothing.
    /// </summary>
    static class HandleState {
        public static bool IsPaused(int index, uint version) {
            return TweenEngine.TryGetSlot(index, version, out TweenSlot slot) && slot.State == SlotState.Paused;
        }

        public static float Duration(int index, uint version) {
            return TweenEngine.TryGetSlot(index, version, out TweenSlot slot) ? Mathf.Max(slot.CycleDuration, 0f) : 0f;
        }

        public static float DurationTotal(int index, uint version) {
            if (!TweenEngine.TryGetSlot(index, version, out TweenSlot slot)) { return 0f; }
            if (slot.Cycles < 0) { return float.PositiveInfinity; }
            return slot.StartDelay + Mathf.Max(slot.CycleDuration, 0f) * Mathf.Max(slot.Cycles, 1);
        }

        public static int CyclesDone(int index, uint version) {
            return TweenEngine.TryGetSlot(index, version, out TweenSlot slot) ? slot.CyclesDone : 0;
        }

        public static int CyclesTotal(int index, uint version) {
            return TweenEngine.TryGetSlot(index, version, out TweenSlot slot) ? slot.Cycles : 0;
        }

        public static float ElapsedTime(int index, uint version) {
            if (!TweenEngine.TryGetSlot(index, version, out TweenSlot slot)) { return 0f; }
            return Mathf.Clamp(slot.Elapsed - slot.StartDelay, 0f, Mathf.Max(slot.CycleDuration, 0f));
        }

        public static void SetElapsedTime(int index, uint version, float seconds) {
            if (!TweenEngine.TryGetSlot(index, version, out TweenSlot slot)) { return; }
            float cycle = Mathf.Max(slot.CycleDuration, 0f);
            float inCycle = Mathf.Clamp(seconds, 0f, cycle);
            TweenEngine.SetElapsedTotal(index, version, slot.StartDelay + slot.CyclesDone * cycle + inCycle, inCycle >= cycle);
        }

        public static float Progress(int index, uint version) {
            if (!TweenEngine.TryGetSlot(index, version, out TweenSlot slot)) { return 0f; }
            float cycle = slot.CycleDuration;
            if (cycle <= 0f) { return slot.StartFired ? 1f : 0f; }
            return Mathf.Clamp01((slot.Elapsed - slot.StartDelay) / cycle);
        }

        public static void SetProgress(int index, uint version, float progress) {
            SetElapsedTime(index, version, Mathf.Clamp01(progress) * Duration(index, version));
        }

        public static float ProgressTotal(int index, uint version) {
            float total = DurationTotal(index, version);
            if (total <= 0f || float.IsInfinity(total)) { return 0f; }
            return Mathf.Clamp01(TweenEngine.ElapsedTotal(index, version) / total);
        }

        public static void SetProgressTotal(int index, uint version, float progress) {
            float total = DurationTotal(index, version);
            if (float.IsInfinity(total)) {
                Debug.LogWarning("RavenTween: an infinite tween has no total progress; set ElapsedTime instead.");
                return;
            }
            TweenEngine.SetElapsedTotal(index, version, Mathf.Clamp01(progress) * total);
        }

        public static float TimeScale(int index, uint version) {
            return TweenEngine.TryGetSlot(index, version, out TweenSlot slot) ? slot.TimeScale : 1f;
        }

        public static void SetTimeScale(int index, uint version, float scale) {
            if (!TryGetRoot(index, version, "time scale", out TweenSlot slot)) { return; }
            if (float.IsNaN(scale) || float.IsInfinity(scale)) {
                Debug.LogError("RavenTween: a time scale must be a finite number; ignored.");
                return;
            }
            slot.TimeScale = Mathf.Max(scale, 0f);
        }

        public static void SetPhase(int index, uint version, UpdatePhase phase) {
            if (!TryGetRoot(index, version, "update phase", out TweenSlot slot)) { return; }
            slot.HasPhase = true;
            slot.Phase = phase;
        }

        public static void SetCancellation(int index, uint version, CancellationToken token) {
            if (!TryGetRoot(index, version, "cancellation", out TweenSlot slot)) { return; }
            if (!token.CanBeCanceled) { return; }
            slot.HasCancellation = true;
            slot.Cancellation = token;
            if (token.IsCancellationRequested) { TweenEngine.Kill(index, version, false); }
        }

        public static void SetOnComplete<T>(int index, uint version, T target, System.Action<T> callback) where T : class {
            Debug.Assert(target != null && callback != null, "Target-based OnComplete needs a target and a callback.");
            if (target == null || callback == null || !TweenEngine.TryGetSlot(index, version, out TweenSlot slot)) { return; }
            if (slot.CompleteInvoker != null) {
                Debug.LogError("RavenTween: a tween takes one target-based OnComplete; use OnComplete(Action) for more.");
                return;
            }
            slot.CompleteTarget = target;
            slot.CompleteDelegate = callback;
            slot.CompleteInvoker = TargetCallbacks<T>.Complete;
        }

        // Timing of a sequence child is driven by its sequence.
        static bool TryGetRoot(int index, uint version, string what, out TweenSlot slot) {
            if (!TweenEngine.TryGetSlot(index, version, out slot)) { return false; }
            if (!slot.OwnedBySequence) { return true; }
            Debug.LogWarning("RavenTween: this tween belongs to a sequence, which drives its " + what + "; set it on the sequence.");
            slot = null;
            return false;
        }
    }

    /// <summary>One cached invoker per target type: target-based callbacks never allocate a closure.</summary>
    static class TargetCallbacks<T> where T : class {
        public static readonly System.Action<object, System.Delegate> Complete =
            (target, callback) => ((System.Action<T>)callback)((T)target);
        public static readonly System.Action<object, System.Delegate, Tween> Update =
            (target, callback, tween) => ((System.Action<T, Tween>)callback)((T)target, tween);
    }
}
