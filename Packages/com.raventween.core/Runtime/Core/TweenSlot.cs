using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenTween {
    /// <summary>How a tween behaves when it repeats.</summary>
    public enum CycleMode : byte {
        /// <summary>Each cycle restarts from the initial value.</summary>
        Restart = 0,
        /// <summary>Odd cycles play backwards (ping-pong).</summary>
        Yoyo = 1
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
        public float StartTime;
        public float Duration;
        public int ChildIndex;
        public uint ChildVersion;
    }

    /// <summary>
    /// Pooled storage for one live tween or sequence. Instances are allocated once,
    /// recycled forever, and never exposed publicly; handles reference them by index + version.
    /// </summary>
    sealed class TweenSlot {
        public uint Version = 1;
        public SlotState State = SlotState.Free;
        public bool IsSequence;
        public bool OwnedBySequence;

        // Target & property.
        public UnityEngine.Object UnityTarget;
        public bool RequiresTarget;
        public PropertyKind Property = PropertyKind.None;
        public int PropertyId;

        // Custom setter: target + user delegate + cached typed invoker (no per-frame allocation).
        public object CustomTarget;
        public Delegate CustomSetter;
        public Action<object, Delegate, TweenValue> CustomInvoker;

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

        // Easing.
        public Ease Ease = Ease.Linear;
        public AnimationCurve CustomCurve;
        public Func<float, float> CustomEase;

        // Callbacks. Kept as plain delegates; built-in property tweens never allocate per frame.
        public Action OnStart;
        public Action OnUpdate;
        public Action<float> OnUpdateFloat;
        public Action<TweenValue> OnUpdateValue;
        public Action OnComplete;
        public Action OnKill;
        public Action OnTargetDestroyed;
        public Action AwaitContinuations;

        public bool StartFired;
        public bool CompleteNotified; // Child-in-sequence: OnComplete fired for the current cycle.

        // Sequence data (only used when IsSequence is true).
        public List<SequenceItem> Items;
        public float SequenceDuration;
        public float ChainCursor;      // Where the next Chain() lands.
        public float LastInsertTime;   // Where the last added item started; Group() reuses it.

        /// <summary>Total length of one cycle, including the start delay.</summary>
        public float CycleLength {
            get { return StartDelay + (IsSequence ? SequenceDuration : Duration); }
        }

        /// <summary>Resets every field so the slot can be reused. Keeps allocated collections.</summary>
        public void Reset() {
            Debug.Assert(State == SlotState.Free, "Only free slots may be reset.");
            IsSequence = false;
            OwnedBySequence = false;
            UnityTarget = null;
            RequiresTarget = false;
            Property = PropertyKind.None;
            PropertyId = 0;
            CustomTarget = null;
            CustomSetter = null;
            CustomInvoker = null;
            Effect = EffectKind.None;
            EffectStrength = Vector3.zero;
            EffectFrequency = 0f;
            EffectSeed = 0f;
            StartValue = default;
            EndValue = default;
            HasExplicitFrom = false;
            FromCaptured = false;
            Duration = 0f;
            StartDelay = 0f;
            Elapsed = 0f;
            Cycles = 1;
            CyclesDone = 0;
            Mode = CycleMode.Restart;
            UseUnscaledTime = false;
            Ease = Ease.Linear;
            CustomCurve = null;
            CustomEase = null;
            OnStart = null;
            OnUpdate = null;
            OnUpdateFloat = null;
            OnUpdateValue = null;
            OnComplete = null;
            OnKill = null;
            OnTargetDestroyed = null;
            AwaitContinuations = null;
            StartFired = false;
            CompleteNotified = false;
            SequenceDuration = 0f;
            ChainCursor = 0f;
            LastInsertTime = 0f;
            if (Items != null) { Items.Clear(); }
        }
    }
}
