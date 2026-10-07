using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Lightweight handle to a live tween. Handles are structs: copying them is free and a
    /// handle to a finished tween is safely dead (<see cref="IsAlive"/> returns false).
    /// Tweens play automatically; <see cref="Start"/> exists for explicit call sites.
    /// </summary>
    public readonly struct Tween : IEquatable<Tween> {
        internal readonly int Index;
        internal readonly uint Version;

        internal Tween(int index, uint version) {
            Index = index;
            Version = version;
        }

        /// <summary>True while the tween is running or paused.</summary>
        public bool IsAlive {
            get { return TweenEngine.TryGetSlot(Index, Version, out _); }
        }

        /// <summary>True when the tween is alive and paused.</summary>
        public bool IsPaused {
            get { return TweenEngine.TryGetSlot(Index, Version, out TweenSlot slot) && slot.State == SlotState.Paused; }
        }

        // ----- Fluent configuration -----

        /// <summary>Sets the easing function.</summary>
        public Tween Ease(Ease ease) {
            Debug.Assert(ease != RavenTween.Ease.Custom, "Use the AnimationCurve or delegate overload for custom easing.");
            if (TryGetConfigurable(out TweenSlot slot)) { slot.Ease = ease; }
            return this;
        }

        /// <summary>Uses a custom AnimationCurve as the easing function.</summary>
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

        /// <summary>Uses a custom easing delegate mapping [0,1] to the eased factor.</summary>
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

        /// <summary>
        /// Uses a parametric ease: <c>Easing.Overshoot(2f)</c>, <c>Easing.Bounce(0.5f)</c>,
        /// <c>Easing.BounceExact(0.2f)</c>, <c>Easing.Elastic(1.5f, 0.2f)</c>, or any standard ease.
        /// </summary>
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

        /// <summary>Delays the start of the tween by <paramref name="seconds"/>.</summary>
        public Tween Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay cannot be negative.");
            if (TryGetTimingConfigurable(out TweenSlot slot)) { slot.StartDelay = Mathf.Max(seconds, 0f); }
            return this;
        }

        /// <summary>Repeats the tween. Use -1 for an infinite loop.</summary>
        public Tween Cycles(int count, CycleMode mode = CycleMode.Restart) {
            Debug.Assert(count == -1 || count >= 1, "Cycle count must be -1 (infinite) or at least 1.");
            if (TryGetTimingConfigurable(out TweenSlot slot)) {
                slot.Cycles = count < 0 ? -1 : Mathf.Max(count, 1);
                slot.Mode = mode;
            }
            return this;
        }

        // A sequence measures its children when they are added; changing a child's length
        // afterwards would desynchronize the timeline.
        bool TryGetTimingConfigurable(out TweenSlot slot) {
            if (!TryGetConfigurable(out slot)) { return false; }
            if (!slot.OwnedBySequence) { return true; }
            Debug.LogError("RavenTween: set Delay and Cycles before adding a tween to a sequence; ignored.");
            slot = null;
            return false;
        }

        /// <summary>Loops forever. Equivalent to Cycles(-1, mode).</summary>
        public Tween Infinite(CycleMode mode = CycleMode.Yoyo) { return Cycles(-1, mode); }

        /// <summary>Uses unscaled time so the tween ignores Time.timeScale.</summary>
        public Tween UnscaledTime(bool unscaled = true) {
            if (TryGetConfigurable(out TweenSlot slot)) { slot.UseUnscaledTime = unscaled; }
            return this;
        }

        /// <summary>Runs the tween in another player loop phase: FixedUpdate for physics, LateUpdate after your scripts.</summary>
        public Tween UpdateIn(UpdatePhase phase) {
            HandleState.SetPhase(Index, Version, phase);
            return this;
        }

        /// <summary>Stops the tween (firing OnKill) as soon as <paramref name="token"/> is cancelled. No allocation.</summary>
        public Tween WithCancellation(CancellationToken token) {
            HandleState.SetCancellation(Index, Version, token);
            return this;
        }

        /// <summary>Changes how many cycles remain, the current one included (1 = this is the last, -1 = forever).</summary>
        public Tween SetRemainingCycles(int cycles) {
            TweenEngine.SetRemainingCycles(Index, Version, cycles);
            return this;
        }

        /// <summary>
        /// For Yoyo and PingPong loops: completes the next time the tween reaches its end value
        /// (true) or its start value (false).
        /// </summary>
        public Tween SetRemainingCycles(bool stopAtEndValue) {
            TweenEngine.SetRemainingCycles(Index, Version, stopAtEndValue);
            return this;
        }

        // ----- State -----

        /// <summary>Length of one cycle, in seconds, without the delay.</summary>
        public float Duration { get { return HandleState.Duration(Index, Version); } }

        /// <summary>Delay + every cycle, in seconds. Infinity when the tween loops forever.</summary>
        public float DurationTotal { get { return HandleState.DurationTotal(Index, Version); } }

        /// <summary>Cycles completed so far.</summary>
        public int CyclesDone { get { return HandleState.CyclesDone(Index, Version); } }

        /// <summary>Number of cycles; -1 when infinite.</summary>
        public int CyclesTotal { get { return HandleState.CyclesTotal(Index, Version); } }

        /// <summary>Seconds into the current cycle. Setting it jumps the tween there.</summary>
        public float ElapsedTime {
            get { return HandleState.ElapsedTime(Index, Version); }
            set { HandleState.SetElapsedTime(Index, Version, value); }
        }

        /// <summary>Seconds since the tween was created, delay included. Setting it jumps there (past the end completes it).</summary>
        public float ElapsedTimeTotal {
            get { return TweenEngine.ElapsedTotal(Index, Version); }
            set { TweenEngine.SetElapsedTotal(Index, Version, value); }
        }

        /// <summary>0–1 through the current cycle. Settable.</summary>
        public float Progress {
            get { return HandleState.Progress(Index, Version); }
            set { HandleState.SetProgress(Index, Version, value); }
        }

        /// <summary>0–1 through the whole tween (0 when infinite). Settable.</summary>
        public float ProgressTotal {
            get { return HandleState.ProgressTotal(Index, Version); }
            set { HandleState.SetProgressTotal(Index, Version, value); }
        }

        /// <summary>The eased factor shown right now: 0 on the start value, 1 on the end value (beyond 1 while overshooting).</summary>
        public float InterpolationFactor { get { return TweenEngine.InterpolationFactor(Index, Version); } }

        /// <summary>Speed multiplier for this tween only (1 = normal, 0 = frozen). Tween it with Raven.TweenTimeScale.</summary>
        public float TimeScale {
            get { return HandleState.TimeScale(Index, Version); }
            set { HandleState.SetTimeScale(Index, Version, value); }
        }

        // ----- Callbacks -----

        public Tween OnStart(Action callback) {
            Debug.Assert(callback != null, "OnStart callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnStart += callback; }
            return this;
        }

        /// <summary>Called every frame after the value has been applied.</summary>
        public Tween OnUpdate(Action callback) {
            Debug.Assert(callback != null, "OnUpdate callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnUpdate += callback; }
            return this;
        }

        /// <summary>Receives the interpolated float value. Intended for Raven.Value tweens.</summary>
        public Tween OnUpdate(Action<float> callback) {
            Debug.Assert(callback != null, "OnUpdate callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnUpdateFloat += callback; }
            return this;
        }

        /// <summary>Receives the full interpolated value for any value kind.</summary>
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

        /// <summary>Called when the tween is stopped before completing.</summary>
        public Tween OnKill(Action callback) {
            Debug.Assert(callback != null, "OnKill callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnKill += callback; }
            return this;
        }

        /// <summary>
        /// Allocation-free OnComplete: <paramref name="target"/> is passed back to a non-capturing
        /// lambda, e.g. <c>OnComplete(this, self =&gt; self.Open())</c>. Skipped if the target is a
        /// destroyed Unity object. One per tween.
        /// </summary>
        public Tween OnComplete<T>(T target, Action<T> callback) where T : class {
            HandleState.SetOnComplete(Index, Version, target, callback);
            return this;
        }

        /// <summary>
        /// Allocation-free OnUpdate: receives <paramref name="target"/> and this tween every frame,
        /// e.g. <c>OnUpdate(bar, (b, t) =&gt; b.fill = t.Progress)</c>. One per tween.
        /// </summary>
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

        /// <summary>Called when the tween target is destroyed while the tween is alive.</summary>
        public Tween OnTargetDestroyed(Action callback) {
            Debug.Assert(callback != null, "OnTargetDestroyed callback cannot be null.");
            if (callback != null && TryGetConfigurable(out TweenSlot slot)) { slot.OnTargetDestroyed += callback; }
            return this;
        }

        // ----- Control -----

        /// <summary>Tweens play automatically; Start() documents intent and resumes a paused tween.</summary>
        public Tween Start() {
            TweenEngine.SetPaused(Index, Version, false);
            return this;
        }

        /// <summary>Stops the tween where it is and fires OnKill.</summary>
        public void Stop() { TweenEngine.Kill(Index, Version, false); }

        /// <summary>Jumps to the final value and fires OnComplete.</summary>
        public void Complete() { TweenEngine.Kill(Index, Version, true); }

        public void Pause() { TweenEngine.SetPaused(Index, Version, true); }
        public void Resume() { TweenEngine.SetPaused(Index, Version, false); }

        // ----- Async & coroutines -----

        /// <summary>Awaits tween completion (or kill). Usage: await tween;</summary>
        public TweenAwaiter GetAwaiter() { return new TweenAwaiter(this); }

        /// <summary>Awaits tween completion. Alias for await-friendliness in expression contexts.</summary>
        public TweenAwaiter ToCompletion() { return GetAwaiter(); }

        /// <summary>Returns a coroutine instruction that waits until the tween dies.</summary>
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

    /// <summary>Awaiter that completes when its tween dies (completed or killed).</summary>
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

        /// <summary>Allows "await tween.ToCompletion()".</summary>
        public TweenAwaiter GetAwaiter() { return this; }
    }

    sealed class TweenYieldInstruction : CustomYieldInstruction {
        readonly Tween _tween;
        public TweenYieldInstruction(Tween tween) { _tween = tween; }
        public override bool keepWaiting { get { return _tween.IsAlive; } }
    }
}
