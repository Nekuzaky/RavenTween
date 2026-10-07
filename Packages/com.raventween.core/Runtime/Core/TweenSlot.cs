using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    /// <summary>How a tween behaves when it repeats.</summary>
    public enum CycleMode : byte {
        /// <summary>Each cycle restarts from the initial value.</summary>
        Restart = 0,
        /// <summary>
        /// Odd cycles play the animation backwards in time, so the way back mirrors the ease
        /// (an OutQuad rise falls back like an InQuad). Same as DOTween's Yoyo.
        /// </summary>
        Yoyo = 1,
        /// <summary>
        /// Each cycle continues from where the previous one ended, adding the same change again
        /// (move 1 m, then 1 m further…). Tweens only; sequences play it as Restart.
        /// </summary>
        Incremental = 2,
        /// <summary>
        /// Odd cycles go from the end value back to the start value with the ease applied as
        /// is (an OutQuad rise also falls back like an OutQuad). Tweens only; sequences play it as Yoyo.
        /// </summary>
        PingPong = 3
    }

    /// <summary>Procedural effect applied on top of the captured start value.</summary>
    enum EffectKind : byte {
        None = 0,
        Shake = 1,
        Punch = 2
    }

    enum SlotState : byte {
        Free = 0,
        Running = 1,
        Paused = 2
    }

    /// <summary>One entry of a sequence: a child tween placed on the sequence timeline.</summary>
    struct SequenceItem {
        public float StartTime;    // Where the item is placed on the timeline.
        public float ActiveStart;  // StartTime plus the child's own delay: when it starts writing.
        public float Duration;     // Child delay + all child cycles.
        public int ChildIndex;
        public uint ChildVersion;
    }

    /// <summary>
    /// Pooled storage for one live tween or sequence. Instances are allocated once,
    /// recycled forever, and never exposed publicly; handles reference them by index + version.
    /// </summary>
    sealed class TweenSlot {
        public uint Version = 1;
        public int Index;              // Position in the engine's slot list; never changes.
        public SlotState State = SlotState.Free;
        public bool IsSequence;
        public bool IsCallback;        // Zero-length sequence item that only runs its callbacks.
        public bool OwnedBySequence;

        // Target & property.
        public UnityEngine.Object UnityTarget;
        public bool RequiresTarget;
        public PropertyKind Property = PropertyKind.None;
        public int PropertyId;

        public long BornPass;          // Engine pass that created this slot; that pass skips it.

        // Custom setter: target + user delegate + cached typed invoker (no per-frame allocation).
        public object CustomTarget;
        public Delegate CustomSetter;
        public Action<object, Delegate, TweenValue> CustomInvoker;
        // Optional getter: when set, the start value is read from the target as the tween starts.
        public Delegate CustomGetterDelegate;
        public Func<object, Delegate, TweenValue> CustomGetter;

        // Another tween whose time scale this tween drives (Raven.TweenTimeScale).
        public int LinkIndex = -1;
        public uint LinkVersion;

        // Procedural effects (shake / punch).
        public EffectKind Effect;
        public Vector3 EffectStrength;
        public float EffectFrequency;
        public float EffectSeed;

        // Values.
        public TweenValue StartValue;
        public TweenValue EndValue;
        public bool HasExplicitFrom;
        public bool FromCaptured;

        // Timing.
        public float Duration;
        public float StartDelay;
        public float Elapsed;          // Includes the start delay.
        public int Cycles = 1;         // -1 means infinite.
        public int CyclesDone;
        public CycleMode Mode = CycleMode.Restart;
        public bool UseUnscaledTime;
        public float TimeScale = 1f;
        public bool HasPhase;          // False: follows Raven.UpdatePhase.
        public UpdatePhase Phase;
        public bool HasCancellation;
        public CancellationToken Cancellation;

        // Easing.
        public Ease Ease = Ease.Linear;
        public AnimationCurve CustomCurve;
        public Func<float, float> CustomEase;
        public EasingKind Parametric;
        public float EaseA;
        public float EaseB;

        // Callbacks. Kept as plain delegates; built-in property tweens never allocate per frame.
        public Action OnStart;
        public Action OnUpdate;
        public Action<float> OnUpdateFloat;
        public Action<TweenValue> OnUpdateValue;
        public Action OnComplete;
        public Action OnKill;
        public Action OnTargetDestroyed;
        public Action AwaitContinuations;

        // Target-based callbacks: target + user delegate + cached typed invoker (no closure).
        public object CompleteTarget;
        public Delegate CompleteDelegate;
        public Action<object, Delegate> CompleteInvoker;
        public object UpdateTarget;
        public Delegate UpdateDelegate;
        public Action<object, Delegate, Tween> UpdateInvoker;

        public bool StartFired;
        public bool Completing;        // Inside Complete(): a nested Complete() from a callback is ignored.
        public bool Moving;            // Inside an ElapsedTime jump: a nested jump from a callback is ignored.
        public int Revision;           // Bumped when user code moves the time or changes the cycles.
        public bool CompleteNotified;  // Child-in-sequence: OnComplete fired for the current cycle.
        public bool Rewound;           // Child-in-sequence: start value already restored after time went back.

        // Sequence data (only used when IsSequence is true).
        public List<SequenceItem> Items;
        public float SequenceDuration;
        public float ChainCursor;      // Where the next Chain() lands.
        public float LastInsertTime;   // Where the last added item started; Group() reuses it.
        public bool HasCallbacks;      // At least one ChainCallback / InsertCallback item.
        public int CallbackCycle = -1; // Cycle the callback cursor belongs to.
        public float CallbackCursor = -1f;

        /// <summary>Length of one cycle, without the start delay.</summary>
        public float CycleDuration {
            get { return IsSequence ? SequenceDuration : Duration; }
        }

        /// <summary>Total length of one cycle, including the start delay.</summary>
        public float CycleLength {
            get { return StartDelay + CycleDuration; }
        }

        /// <summary>
        /// Drops every reference to user objects (targets, callbacks, curves) as the slot is
        /// released, so a large pool never keeps scenes or closures alive.
        /// </summary>
        public void ClearReferences() {
            UnityTarget = null;
            CustomTarget = null;
            CustomSetter = null;
            CustomInvoker = null;
            CustomGetterDelegate = null;
            CustomGetter = null;
            CustomCurve = null;
            CustomEase = null;
            Cancellation = default;
            ClearCallbacks();
        }

        void ClearCallbacks() {
            OnStart = null;
            OnUpdate = null;
            OnUpdateFloat = null;
            OnUpdateValue = null;
            OnComplete = null;
            OnKill = null;
            OnTargetDestroyed = null;
            AwaitContinuations = null;
            CompleteTarget = null;
            CompleteDelegate = null;
            CompleteInvoker = null;
            UpdateTarget = null;
            UpdateDelegate = null;
            UpdateInvoker = null;
        }

        /// <summary>Resets every field so the slot can be reused. Keeps allocated collections.</summary>
        public void Reset() {
            Debug.Assert(State == SlotState.Free, "Only free slots may be reset.");
            IsSequence = false;
            IsCallback = false;
            OwnedBySequence = false;
            RequiresTarget = false;
            Property = PropertyKind.None;
            PropertyId = 0;
            BornPass = 0;
            LinkIndex = -1;
            LinkVersion = 0;
            ClearReferences();
            ResetValues();
            ResetTiming();
            ResetEasing();
            ResetRunState();
            if (Items != null) { Items.Clear(); }
        }

        void ResetValues() {
            Effect = EffectKind.None;
            EffectStrength = Vector3.zero;
            EffectFrequency = 0f;
            EffectSeed = 0f;
            StartValue = default;
            EndValue = default;
            HasExplicitFrom = false;
            FromCaptured = false;
        }

        void ResetTiming() {
            Duration = 0f;
            StartDelay = 0f;
            Elapsed = 0f;
            Cycles = 1;
            CyclesDone = 0;
            Mode = CycleMode.Restart;
            UseUnscaledTime = false;
            TimeScale = 1f;
            HasPhase = false;
            Phase = UpdatePhase.Update;
            HasCancellation = false;
        }

        void ResetEasing() {
            Ease = Ease.Linear;
            Parametric = EasingKind.Standard;
            EaseA = 0f;
            EaseB = 0f;
        }

        void ResetRunState() {
            StartFired = false;
            Completing = false;
            Moving = false;
            Revision = 0;
            CompleteNotified = false;
            Rewound = false;
            SequenceDuration = 0f;
            ChainCursor = 0f;
            LastInsertTime = 0f;
            HasCallbacks = false;
            CallbackCycle = -1;
            CallbackCursor = -1f;
        }
    }
}
