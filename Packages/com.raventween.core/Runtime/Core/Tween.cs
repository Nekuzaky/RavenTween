using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    public readonly struct Tween : IEquatable<Tween> {
        internal readonly int Index;
        internal readonly uint Version;

        internal Tween(int index, uint version) {
            Index = index;
            Version = version;
        }

        public bool IsAlive {
            get { return TweenEngine.TryGetSlot(Index, Version, out _); }
        }

        public bool IsPaused {
            get { return HandleState.IsPaused(Index, Version); }
        }

        public Tween Ease(Ease ease) {
            Debug.Assert(ease != RavenTween.Ease.Custom, "Use the AnimationCurve or delegate overload for custom easing.");
            if (TryGetConfigurable(out TweenSlot slot)) { slot.Ease = ease; }
            return this;
        }

        public Tween Ease(AnimationCurve curve) {
            Debug.Assert(curve != null, "Easing curve cannot be null.");
            if (curve != null && TryGetConfigurable(out TweenSlot slot)) {
                slot.Ease = RavenTween.Ease.Custom;
                slot.Parametric = EasingKind.Standard;
                slot.CustomEase = null;
                slot.CustomCurve = curve;
            }
            return this;
        }

        public Tween Ease(Func<float, float> easeFunction) {
            Debug.Assert(easeFunction != null, "Easing delegate cannot be null.");
            if (easeFunction != null && TryGetConfigurable(out TweenSlot slot)) {
                slot.Ease = RavenTween.Ease.Custom;
                slot.Parametric = EasingKind.Standard;
                slot.CustomCurve = null;
                slot.CustomEase = easeFunction;
            }
            return this;
        }

        public Tween Ease(Easing easing) {
            if (!TryGetConfigurable(out TweenSlot slot)) { return this; }
            switch (easing.Kind) {
                case EasingKind.Standard: return Ease(easing.Standard);
                case EasingKind.Curve: return easing.Curve != null ? Ease(easing.Curve) : this;
                default:
                    slot.Ease = RavenTween.Ease.Custom;
                    slot.Parametric = easing.Kind;
                    slot.EaseA = easing.A;
                    slot.EaseB = easing.B;
                    return this;
            }
        }

        public Tween From(float value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector2 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector3 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector4 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Quaternion value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Color value) { return SetFrom(new TweenValue(value)); }

        internal Tween FromValue(in TweenValue value) { return SetFrom(value); }

        Tween SetFrom(in TweenValue value) {
            if (!TryGetConfigurable(out TweenSlot slot)) { return this; }
            Debug.Assert(!slot.StartFired, "From() must be set before the tween starts playing.");
            if (slot.EndValue.Kind != value.Kind) {
                Debug.LogError("RavenTween: From() got a " + value.Kind + " but this tween animates a " + slot.EndValue.Kind + "; ignored.");
                return this;
            }
            slot.StartValue = value;
            slot.HasExplicitFrom = true;
            return this;
        }

        public Tween Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay cannot be negative.");
            if (TryGetTimingConfigurable(out TweenSlot slot)) { slot.StartDelay = Mathf.Max(seconds, 0f); }
            return this;
        }

        public Tween Cycles(int count, CycleMode mode = CycleMode.Restart) {
            Debug.Assert(count == -1 || count >= 1, "Cycle count must be -1 (infinite) or at least 1.");
            if (TryGetTimingConfigurable(out TweenSlot slot)) {
                slot.Cycles = count < 0 ? -1 : Mathf.Max(count, 1);
                slot.Mode = mode;
            }
            return this;
        }

        bool TryGetTimingConfigurable(out TweenSlot slot) {
            if (!TryGetConfigurable(out slot)) { return false; }
            if (!slot.OwnedBySequence) { return true; }
            Debug.LogError("RavenTween: set Delay and Cycles before adding a tween to a sequence; ignored.");
            slot = null;
            return false;
        }

        public Tween Infinite(CycleMode mode = CycleMode.Yoyo) { return Cycles(-1, mode); }

        public Tween UnscaledTime(bool unscaled = true) {
            if (TryGetConfigurable(out TweenSlot slot)) { slot.UseUnscaledTime = unscaled; }
            return this;
        }

        public Tween UpdateIn(UpdatePhase phase) {
            HandleState.SetPhase(Index, Version, phase);
            return this;
        }

        public Tween WithCancellation(CancellationToken token) {
            HandleState.SetCancellation(Index, Version, token);
            return this;
        }

        public Tween SetRemainingCycles(int cycles) {
            TweenEngine.SetRemainingCycles(Index, Version, cycles);
            return this;
        }

        public Tween SetRemainingCycles(bool stopAtEndValue) {
            TweenEngine.SetRemainingCycles(Index, Version, stopAtEndValue);
            return this;
        }

        public float Duration { get { return HandleState.Duration(Index, Version); } }

        public float DurationTotal { get { return HandleState.DurationTotal(Index, Version); } }

        public int CyclesDone { get { return HandleState.CyclesDone(Index, Version); } }

        public int CyclesTotal { get { return HandleState.CyclesTotal(Index, Version); } }

        public float ElapsedTime {
            get { return HandleState.ElapsedTime(Index, Version); }
            set { HandleState.SetElapsedTime(Index, Version, value); }
        }

        public float ElapsedTimeTotal {
            get { return TweenEngine.ElapsedTotal(Index, Version); }
            set { TweenEngine.SetElapsedTotal(Index, Version, value); }
        }

        public float Progress {
            get { return HandleState.Progress(Index, Version); }
            set { HandleState.SetProgress(Index, Version, value); }
        }

        public float ProgressTotal {
            get { return HandleState.ProgressTotal(Index, Version); }
            set { HandleState.SetProgressTotal(Index, Version, value); }
        }

        public float InterpolationFactor { get { return TweenEngine.InterpolationFactor(Index, Version); } }

        public float TimeScale {
            get { return HandleState.TimeScale(Index, Version); }
            set { HandleState.SetTimeScale(Index, Version, value); }
        }

        public Tween OnStart(Action callback) {
            Debug.Assert(callback != null, "OnStart callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnStart += callback; }
            return this;
        }

        public Tween OnUpdate(Action callback) {
            Debug.Assert(callback != null, "OnUpdate callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnUpdate += callback; }
            return this;
        }

        public Tween OnUpdate(Action<float> callback) {
            Debug.Assert(callback != null, "OnUpdate callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnUpdateFloat += callback; }
            return this;
        }

        public Tween OnUpdate(Action<TweenValue> callback) {
            Debug.Assert(callback != null, "OnUpdate callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnUpdateValue += callback; }
            return this;
        }

        public Tween OnComplete(Action callback) {
            Debug.Assert(callback != null, "OnComplete callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnComplete += callback; }
            return this;
        }

        public Tween OnKill(Action callback) {
            Debug.Assert(callback != null, "OnKill callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnKill += callback; }
            return this;
        }

        public Tween OnComplete<T>(T target, Action<T> callback) where T : class {
            HandleState.SetOnComplete(Index, Version, target, callback);
            return this;
        }

        public Tween OnUpdate<T>(T target, Action<T, Tween> callback) where T : class {
            Debug.Assert(target != null && callback != null, "Target-based OnUpdate needs a target and a callback.");
            if (target == null || callback == null || !TryGetConfigurable(out TweenSlot slot)) { return this; }
            if (slot.UpdateInvoker != null) {
                Debug.LogError("RavenTween: a tween takes one target-based OnUpdate; use OnUpdate(Action) for more.");
                return this;
            }
            slot.UpdateTarget = target;
            slot.UpdateDelegate = callback;
            slot.UpdateInvoker = TargetCallbacks<T>.Update;
            return this;
        }

        public Tween OnTargetDestroyed(Action callback) {
            Debug.Assert(callback != null, "OnTargetDestroyed callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnTargetDestroyed += callback; }
            return this;
        }

        public Tween Start() {
            TweenEngine.SetPaused(Index, Version, false);
            return this;
        }

        public void Stop() { TweenEngine.Kill(Index, Version, false); }

        public void Complete() { TweenEngine.Kill(Index, Version, true); }

        public void Pause() { TweenEngine.SetPaused(Index, Version, true); }
        public void Resume() { TweenEngine.SetPaused(Index, Version, false); }

        public TweenAwaiter GetAwaiter() { return new TweenAwaiter(this); }

        public TweenAwaiter ToCompletion() { return GetAwaiter(); }

        public CustomYieldInstruction ToYieldInstruction() { return new TweenYieldInstruction(this); }

        bool TryGetConfigurable(out TweenSlot slot) {
            bool alive = TweenEngine.TryGetSlot(Index, Version, out slot);
            Debug.Assert(alive, "Configuring a dead tween handle has no effect.");
            return alive;
        }

        public bool Equals(Tween other) { return Index == other.Index && Version == other.Version; }
        public override bool Equals(object obj) { return obj is Tween other && Equals(other); }
        public override int GetHashCode() { return (Index * 397) ^ (int)Version; }
    }

    public readonly struct TweenAwaiter : INotifyCompletion {
        readonly Tween _tween;

        internal TweenAwaiter(Tween tween) { _tween = tween; }

        public bool IsCompleted { get { return !_tween.IsAlive; } }

        public void OnCompleted(Action continuation) {
            Debug.Assert(continuation != null, "Await continuation cannot be null.");
            if (continuation == null) { return; }
            if (TweenEngine.TryGetSlot(_tween.Index, _tween.Version, out TweenSlot slot)) {
                slot.AwaitContinuations += continuation;
            } else {
                continuation();
            }
        }

        public void GetResult() { }

        public TweenAwaiter GetAwaiter() { return this; }
    }

    sealed class TweenYieldInstruction : CustomYieldInstruction {
        readonly Tween _tween;
        public TweenYieldInstruction(Tween tween) { _tween = tween; }
        public override bool keepWaiting { get { return _tween.IsAlive; } }
    }
}
