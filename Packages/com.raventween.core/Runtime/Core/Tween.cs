using System;
using System.Runtime.CompilerServices;
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
                slot.CustomCurve = curve;
            }
            return this;
        }

        /// <summary>Uses a custom easing delegate mapping [0,1] to the eased factor.</summary>
        public Tween Ease(Func<float, float> easeFunction) {
            Debug.Assert(easeFunction != null, "Easing delegate cannot be null.");
            if (easeFunction != null && TryGetConfigurable(out TweenSlot slot)) {
                slot.Ease = RavenTween.Ease.Custom;
                slot.CustomEase = easeFunction;
            }
            return this;
        }

        public Tween From(float value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector2 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector3 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Vector4 value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Quaternion value) { return SetFrom(new TweenValue(value)); }
        public Tween From(Color value) { return SetFrom(new TweenValue(value)); }

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
