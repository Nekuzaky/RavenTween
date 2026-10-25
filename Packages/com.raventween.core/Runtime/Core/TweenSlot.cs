using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    public enum CycleMode : byte {
        Restart = 0,
        Yoyo = 1,
        Incremental = 2,
        PingPong = 3
    }

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

    struct SequenceItem {
        public float StartTime;
        public float ActiveStart;
        public float Duration;
        public int ChildIndex;
        public uint ChildVersion;
    }

    sealed class TweenSlot {
        public uint Version = 1;
        public int Index;
        public SlotState State = SlotState.Free;
        public bool IsSequence;
        public bool IsCallback;
        public bool OwnedBySequence;

        public UnityEngine.Object UnityTarget;
        public bool RequiresTarget;
        public PropertyKind Property = PropertyKind.None;
        public int PropertyId;

        public long BornPass;

        public object CustomTarget;
        public Delegate CustomSetter;
        public Action<object, Delegate, TweenValue> CustomInvoker;
        public Delegate CustomGetterDelegate;
        public Func<object, Delegate, TweenValue> CustomGetter;

        public int LinkIndex = -1;
        public uint LinkVersion;

        public EffectKind Effect;
        public Vector3 EffectStrength;
        public float EffectFrequency;
        public float EffectSeed;

        public TweenValue StartValue;
        public TweenValue EndValue;
        public bool HasExplicitFrom;
        public bool FromCaptured;

        public float Duration;
        public float StartDelay;
        public float Elapsed;
        public int Cycles = 1;
        public int CyclesDone;
        public CycleMode Mode = CycleMode.Restart;
        public bool UseUnscaledTime;
        public float TimeScale = 1f;
        public bool HasPhase;
        public UpdatePhase Phase;
        public bool HasCancellation;
        public CancellationToken Cancellation;

        public Ease Ease = Ease.Linear;
        public AnimationCurve CustomCurve;
        public Func<float, float> CustomEase;
        public EasingKind Parametric;
        public float EaseA;
        public float EaseB;

        public Action OnStart;
        public Action OnUpdate;
        public Action<float> OnUpdateFloat;
        public Action<TweenValue> OnUpdateValue;
        public Action OnComplete;
        public Action OnKill;
        public Action OnTargetDestroyed;
        public Action AwaitContinuations;

        public object CompleteTarget;
        public Delegate CompleteDelegate;
        public Action<object, Delegate> CompleteInvoker;
        public object UpdateTarget;
        public Delegate UpdateDelegate;
        public Action<object, Delegate, Tween> UpdateInvoker;

        public bool StartFired;
        public bool Completing;
        public bool Moving;
        public int Revision;
        public bool CompleteNotified;
        public bool Rewound;

        public List<SequenceItem> Items;
        public float SequenceDuration;
        public float ChainCursor;
        public float LastInsertTime;
        public bool HasCallbacks;
        public int CallbackCycle = -1;
        public float CallbackCursor = -1f;

        public float CycleDuration {
            get { return IsSequence ? SequenceDuration : Duration; }
        }

        public float CycleLength {
            get { return StartDelay + CycleDuration; }
        }

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
