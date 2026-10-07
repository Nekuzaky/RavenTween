using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RavenTween {
    /// <summary>Player loop phase in which the engine advances tweens.</summary>
    public enum UpdatePhase : byte {
        Update = 0,
        LateUpdate = 1
    }

    /// <summary>
    /// Central tween engine. Registers itself into the player loop and advances every live
    /// tween with zero steady-state allocation. Not thread-safe by design: all public entry
    /// points must be called from the main thread.
    /// </summary>
    /// <remarks>
    /// Reentrancy contract: user code (callbacks, eases, setters, await continuations) may
    /// create, stop or complete any tween at any time. The engine therefore (1) releases a slot
    /// before invoking its end-of-life callbacks, (2) re-validates every slot by version after
    /// running user code, and (3) never steps a tween in the same pass that created it.
    /// </remarks>
    static class TweenEngine {
        const int InitialCapacity = 64;
        const int MaxSequenceWrapsPerStep = 16;

        static readonly List<TweenSlot> Slots = new List<TweenSlot>(InitialCapacity);
        static readonly Stack<int> FreeIndices = new Stack<int>(InitialCapacity);
        static bool _processing;
        static int _pass;

        /// <summary>Global multiplier applied to every tween delta time.</summary>
        public static float TimeScale = 1f;

        /// <summary>Phase of the player loop in which tweens are stepped.</summary>
        public static UpdatePhase Phase = UpdatePhase.Update;

        /// <summary>Number of live tweens and sequences (including paused ones).</summary>
        public static int AliveCount {
            get {
                int count = 0;
                for (int i = 0; i < Slots.Count; i++) {
                    if (Slots[i].State != SlotState.Free) { count++; }
                }
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset() {
            Slots.Clear();
            FreeIndices.Clear();
            TimeScale = 1f;
            Phase = UpdatePhase.Update;
            _processing = false;
            _sequenceDepth = 0;
            _pass = 0;
            _lastProcessedFrame = -1;
            Install();
        }

        // Runs on every play-mode entry (also without domain reload). The player loop may have
        // been reset by the editor in between, so presence is checked rather than remembered.
        static void Install() {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            if (ContainsEngine(root)) { return; }
            InjectSystem(ref root, typeof(Update), OnUpdate);
            InjectSystem(ref root, typeof(PreLateUpdate), OnLateUpdate);
            PlayerLoop.SetPlayerLoop(root);
        }

        static bool ContainsEngine(PlayerLoopSystem root) {
            if (root.subSystemList == null) { return false; }
            for (int i = 0; i < root.subSystemList.Length; i++) {
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList;
                if (children == null) { continue; }
                for (int j = 0; j < children.Length; j++) {
                    if (children[j].type == typeof(TweenEngine)) { return true; }
                }
            }
            return false;
        }

        static void InjectSystem(ref PlayerLoopSystem root, Type phaseType, PlayerLoopSystem.UpdateFunction callback) {
            Debug.Assert(phaseType != null, "Player loop phase type is required.");
            Debug.Assert(callback != null, "Player loop callback is required.");
            for (int i = 0; i < root.subSystemList.Length; i++) {
                if (root.subSystemList[i].type != phaseType) { continue; }
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                var extended = new PlayerLoopSystem[children.Length + 1];
                Array.Copy(children, extended, children.Length);
                extended[children.Length] = new PlayerLoopSystem {
                    type = typeof(TweenEngine),
                    updateDelegate = callback
                };
                root.subSystemList[i].subSystemList = extended;
                return;
            }
        }

        static void OnUpdate() {
            if (Phase == UpdatePhase.Update) { Tick(); }
        }

        static void OnLateUpdate() {
            if (Phase == UpdatePhase.LateUpdate) { Tick(); }
        }

        static int _lastProcessedFrame = -1;

        static void Tick() {
            if (!Application.isPlaying) { return; }
            if (Time.frameCount == _lastProcessedFrame) { return; }
            _lastProcessedFrame = Time.frameCount;
            Process(Time.deltaTime, Time.unscaledDeltaTime);
        }

        static float Sanitize(float delta) {
            return float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f ? 0f : delta;
        }

        /// <summary>Advances every live root tween. Exposed internally for deterministic tests.</summary>
        public static void Process(float scaledDelta, float unscaledDelta) {
            Debug.Assert(scaledDelta >= 0f, "Delta time cannot be negative.");
            Debug.Assert(unscaledDelta >= 0f, "Unscaled delta time cannot be negative.");
            if (_processing) { return; }
            scaledDelta = Sanitize(scaledDelta);
            unscaledDelta = Sanitize(unscaledDelta);
            _processing = true;
            int pass = ++_pass;
            int count = Slots.Count;
            try {
                for (int i = 0; i < count; i++) {
                    TweenSlot slot = Slots[i];
                    if (slot.State != SlotState.Running || slot.OwnedBySequence || slot.BornPass == pass) { continue; }
                    float delta = Sanitize((slot.UseUnscaledTime ? unscaledDelta : scaledDelta) * TimeScale);
                    StepRootGuarded(slot, i, delta);
                }
            } finally {
                _processing = false;
                _sequenceDepth = 0;
            }
        }

        // One misbehaving tween (throwing ease, wrong target type) is logged and stopped;
        // it never stops the rest of the engine.
        static void StepRootGuarded(TweenSlot slot, int index, float delta) {
            uint version = slot.Version;
            try {
                StepRoot(slot, index, delta);
            } catch (Exception exception) {
                _sequenceDepth = 0;
                Debug.LogException(exception);
                if (IsLive(slot, version)) {
                    Debug.LogError("RavenTween: a tween threw an exception and was stopped.");
                    KillInternal(slot, index, false);
                }
            }
        }

        static bool IsLive(TweenSlot slot, uint version) {
            return slot.State != SlotState.Free && slot.Version == version;
        }

        static void StepRoot(TweenSlot slot, int index, float delta) {
            uint version = slot.Version;
            if (slot.RequiresTarget && slot.UnityTarget == null) {
                HandleTargetDestroyed(slot, index);
                return;
            }
            slot.Elapsed += delta;
            float local = slot.Elapsed - slot.StartDelay;
            if (local < 0f) { return; }
            EnsureStarted(slot);
            if (!IsLive(slot, version)) { return; }
            if (slot.IsSequence) { StepSequence(slot, index, local, version); }
            else { StepTween(slot, index, local, version); }
        }

        static void EnsureStarted(TweenSlot slot) {
            if (slot.StartFired) { return; }
            slot.StartFired = true;
            if (!slot.IsSequence) { CaptureFromIfNeeded(slot); }
            InvokeSafe(slot.OnStart);
        }

        static void CaptureFromIfNeeded(TweenSlot slot) {
            if (slot.FromCaptured) { return; }
            slot.FromCaptured = true;
            if (slot.HasExplicitFrom) { return; }
            if (slot.Property != PropertyKind.None && slot.UnityTarget != null) {
                slot.StartValue = PropertyAccessor.Read(slot.Property, slot.UnityTarget, slot.PropertyId);
            } else if (slot.CustomGetter != null) {
                slot.StartValue = slot.CustomGetter(slot.CustomTarget, slot.CustomGetterDelegate);
            }
        }

        // Whole cycles are skipped in closed form: a huge delta costs the same as a small one.
        static void StepTween(TweenSlot slot, int index, float local, uint version) {
            float duration = Mathf.Max(slot.Duration, 0f);
            if (duration <= 0f) {
                FinishZeroLength(slot, index, version);
                return;
            }
            if (local < duration) {
                ApplyProgress(slot, local / duration, slot.CyclesDone);
                return;
            }
            double whole = Math.Floor(local / duration);
            int remaining = slot.Cycles < 0 ? int.MaxValue : slot.Cycles - slot.CyclesDone;
            if (whole >= remaining) {
                slot.CyclesDone = slot.Cycles;
                ApplyProgress(slot, 1f, slot.Cycles - 1);
                if (IsLive(slot, version)) { CompleteAndRelease(slot, index); }
                return;
            }
            int n = (int)whole;
            AddCycles(slot, n);
            float consumed = n * duration;
            local -= consumed;
            slot.Elapsed -= consumed;
            ApplyProgress(slot, Mathf.Clamp01(local / duration), slot.CyclesDone);
        }

        // Infinite loops keep counting, but wrap before overflow while preserving yoyo parity.
        static void AddCycles(TweenSlot slot, int n) {
            Debug.Assert(n >= 0, "Cycle increments are never negative.");
            long next = (long)slot.CyclesDone + n;
            slot.CyclesDone = next > 1000000000L ? (int)(next & 1L) : (int)next;
        }

        static void FinishZeroLength(TweenSlot slot, int index, uint version) {
            if (slot.Cycles < 0) {
                ApplyProgress(slot, 1f, 0);
                return;
            }
            slot.CyclesDone = Mathf.Max(slot.Cycles, 1);
            ApplyProgress(slot, 1f, slot.CyclesDone - 1);
            if (IsLive(slot, version)) { CompleteAndRelease(slot, index); }
        }

        static void ApplyProgress(TweenSlot slot, float cycleProgress, int cycleIndex) {
            float t = cycleProgress;
            if (slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1) { t = 1f - t; }
            float eased = EvaluateEase(slot, t);
            TweenValue value = slot.Effect == EffectKind.None
                ? TweenValue.Lerp(slot.StartValue, slot.EndValue, eased)
                : EvaluateEffect(slot, eased);
            WriteValue(slot, value);
        }

        // Effects oscillate around the captured start value. The envelope is zero at both ends,
        // so an effect starts and settles exactly on the rest value (also when cycled or rewound).
        static TweenValue EvaluateEffect(TweenSlot slot, float t) {
            Debug.Assert(slot.Effect != EffectKind.None, "EvaluateEffect requires an effect slot.");
            Debug.Assert(slot.StartValue.Kind == ValueKind.Vector3, "Effects operate on Vector3 properties.");
            float clamped = Mathf.Clamp01(t);
            float phase = clamped * slot.EffectFrequency;
            Vector3 offset;
            float envelope;
            if (slot.Effect == EffectKind.Shake) {
                float seed = slot.EffectSeed;
                offset = new Vector3(
                    Mathf.PerlinNoise(seed, phase) - Mathf.PerlinNoise(seed, 0f),
                    Mathf.PerlinNoise(seed + 31.7f, phase) - Mathf.PerlinNoise(seed + 31.7f, 0f),
                    Mathf.PerlinNoise(seed + 63.1f, phase) - Mathf.PerlinNoise(seed + 63.1f, 0f)) * 2f;
                envelope = (1f - clamped) * Mathf.Min(1f, clamped * 20f);
            } else {
                float wave = Mathf.Sin(phase * 2f * Mathf.PI);
                offset = new Vector3(wave, wave, wave);
                envelope = 1f - clamped;
            }
            offset.Scale(slot.EffectStrength * envelope);
            return new TweenValue(slot.StartValue.Vector3 + offset);
        }

        static float EvaluateEase(TweenSlot slot, float t) {
            if (slot.Ease != Ease.Custom) { return EaseUtility.Evaluate(slot.Ease, t); }
            if (slot.CustomEase != null) { return slot.CustomEase(t); }
            if (slot.CustomCurve != null) { return slot.CustomCurve.Evaluate(t); }
            return t;
        }

        static void WriteValue(TweenSlot slot, in TweenValue value) {
            if (slot.Property != PropertyKind.None && slot.UnityTarget != null) {
                PropertyAccessor.Write(slot.Property, slot.UnityTarget, slot.PropertyId, value);
            }
            if (slot.CustomInvoker != null) { InvokeCustom(slot, value); }
            if (slot.OnUpdateFloat != null) { InvokeSafe(slot.OnUpdateFloat, value.Float); }
            if (slot.OnUpdateValue != null) { InvokeSafe(slot.OnUpdateValue, value); }
            InvokeSafe(slot.OnUpdate);
        }

        // ----- Sequences -----

        static void StepSequence(TweenSlot slot, int index, float local, uint version) {
            float duration = Mathf.Max(slot.SequenceDuration, 0f);
            if (duration <= 0f) {
                FinishEmptySequence(slot, index, version);
                return;
            }
            local = SkipExcessCycles(slot, local, duration);
            for (int wraps = 0; wraps <= MaxSequenceWrapsPerStep + 1; wraps++) {
                if (local < duration) {
                    ApplySequenceTime(slot, local, slot.CyclesDone);
                    return;
                }
                AddCycles(slot, 1);
                bool finished = slot.Cycles >= 0 && slot.CyclesDone >= slot.Cycles;
                // Close out the finished cycle so children reach their end state and fire
                // their per-cycle callbacks before everything rewinds.
                ApplySequenceTime(slot, duration, slot.CyclesDone - 1);
                if (!IsLive(slot, version)) { return; }
                if (finished) {
                    CompleteAndRelease(slot, index);
                    return;
                }
                local -= duration;
                slot.Elapsed -= duration;
                ResetSequenceChildren(slot);
            }
        }

        // A huge delta would otherwise replay every cycle (and every child callback) in one frame.
        static float SkipExcessCycles(TweenSlot slot, float local, float duration) {
            double whole = Math.Floor(local / duration);
            if (whole <= MaxSequenceWrapsPerStep) { return local; }
            double skip = whole - MaxSequenceWrapsPerStep;
            if (slot.Cycles >= 0) { skip = Math.Min(skip, Math.Max(slot.Cycles - slot.CyclesDone - 1, 0)); }
            int n = (int)Math.Min(skip, 100000000d);
            AddCycles(slot, n);
            float consumed = n * duration;
            slot.Elapsed -= consumed;
            return local - consumed;
        }

        static void FinishEmptySequence(TweenSlot slot, int index, uint version) {
            if (slot.Cycles < 0) { return; }
            slot.CyclesDone = Mathf.Max(slot.Cycles, 1);
            ApplySequenceTime(slot, 0f, slot.CyclesDone - 1);
            if (IsLive(slot, version)) { CompleteAndRelease(slot, index); }
        }

        // Plain array work stacks: no generic collection on the hot path, grow only when a
        // sequence tree is wider than anything seen before (amortized, never in steady state).
        static TweenSlot[] _resetWork = new TweenSlot[32];
        static TweenSlot[] _walkWork = new TweenSlot[32];

        static void ResetSequenceChildren(TweenSlot slot) {
            Debug.Assert(slot.IsSequence, "Only sequences carry child items.");
            ResetForReplay(slot, false);
        }

        // Clears per-run state of a root and all its descendants. With recapture, children also
        // forget their captured start values (seeking); without, they keep them (cycle wraps).
        static void ResetForReplay(TweenSlot root, bool recapture) {
            Debug.Assert(root != null, "Reset needs a slot.");
            Debug.Assert(_resetWork.Length > 0, "Reset work stack must be allocated.");
            if (recapture) { ClearRunState(root, true); }
            int top = 0;
            _resetWork[top++] = root;
            while (top > 0) {
                TweenSlot current = _resetWork[--top];
                _resetWork[top] = null;
                if (current.Items == null) { continue; }
                for (int i = 0; i < current.Items.Count; i++) {
                    SequenceItem item = current.Items[i];
                    if (!TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                    ClearRunState(child, recapture);
                    if (!child.IsSequence) { continue; }
                    if (top == _resetWork.Length) { Array.Resize(ref _resetWork, _resetWork.Length * 2); }
                    _resetWork[top++] = child;
                }
            }
        }

        static void ClearRunState(TweenSlot slot, bool recapture) {
            slot.StartFired = false;
            slot.CompleteNotified = false;
            slot.Rewound = false;
            slot.CyclesDone = 0;
            if (recapture && !slot.HasExplicitFrom) { slot.FromCaptured = false; }
        }

        /// <summary>True when <paramref name="target"/> is <paramref name="root"/> or nested anywhere inside it.</summary>
        public static bool IsInSubtree(TweenSlot root, TweenSlot target) {
            Debug.Assert(root != null && target != null, "Subtree check needs two slots.");
            int top = 0;
            _walkWork[top++] = root;
            bool found = false;
            while (top > 0) {
                TweenSlot current = _walkWork[--top];
                _walkWork[top] = null;
                if (current == target) { found = true; continue; }
                if (current.Items == null || found) { continue; }
                for (int i = 0; i < current.Items.Count; i++) {
                    SequenceItem item = current.Items[i];
                    if (!TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                    if (top == _walkWork.Length) { Array.Resize(ref _walkWork, _walkWork.Length * 2); }
                    _walkWork[top++] = child;
                }
            }
            return found;
        }

        // Items are kept sorted by start time (see Sequence.AddItem). Rewinds run first, latest
        // start first, so the earliest child's start value wins; active children then run in
        // start order, so the latest active child wins. User code may kill anything at any
        // point, so the sequence is re-validated after every child.
        static void ApplySequenceTime(TweenSlot slot, float time, int cycleIndex) {
            Debug.Assert(slot.IsSequence, "ApplySequenceTime requires a sequence slot.");
            Debug.Assert(slot.Items != null, "Sequence slot is missing its item list.");
            uint version = slot.Version;
            List<SequenceItem> items = slot.Items;
            float t = time;
            if (slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1) { t = slot.SequenceDuration - t; }
            for (int i = items.Count - 1; i >= 0; i--) {
                SequenceItem item = items[i];
                if (t - item.StartTime >= 0f) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child)) { RewindOnce(child); }
                if (!IsLive(slot, version)) { return; }
            }
            for (int i = 0; i < items.Count; i++) {
                SequenceItem item = items[i];
                float childTime = t - item.StartTime;
                if (childTime < 0f) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child)) {
                    child.Rewound = false;
                    EvaluateChildAtTime(child, Mathf.Min(childTime, item.Duration));
                }
                if (!IsLive(slot, version)) { return; }
            }
        }

        static bool TryGetLiveChild(SequenceItem item, out TweenSlot child) {
            if (!TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out child)) { return false; }
            if (!child.RequiresTarget || child.UnityTarget != null) { return true; }
            HandleTargetDestroyed(child, item.ChildIndex);
            child = null;
            return false;
        }

        // Time moved back before a child that already ran (yoyo, scrubbing): show its start
        // value once, then leave the property to whatever plays earlier on the timeline.
        static void RewindOnce(TweenSlot child) {
            if (!child.StartFired || child.Rewound) { return; }
            child.Rewound = true;
            child.CompleteNotified = false;
            EvaluateChildAtTime(child, child.StartDelay);
        }

        const int MaxSequenceDepth = 8;
        static int _sequenceDepth;

        static void EvaluateChildAtTime(TweenSlot child, float childLocal) {
            float local = childLocal - child.StartDelay;
            if (local < 0f) { return; }
            uint version = child.Version;
            EnsureStarted(child);
            if (!IsLive(child, version)) { return; }
            if (child.IsSequence) { EvaluateSequenceChild(child, local); return; }
            float cycleLength = Mathf.Max(child.Duration, 0f);
            if (cycleLength <= 0f) {
                child.CyclesDone = Mathf.Max(child.Cycles, 1);
                ApplyProgress(child, 1f, child.CyclesDone - 1);
                if (IsLive(child, version)) { NotifyChildComplete(child); }
                return;
            }
            int totalCycles = Mathf.Max(child.Cycles, 1);
            int cycleIndex = Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = local - cycleIndex * cycleLength;
            float progress = Mathf.Clamp01(inCycle / cycleLength);
            bool atEnd = local >= cycleLength * totalCycles;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            ApplyProgress(child, atEnd ? 1f : progress, atEnd ? totalCycles - 1 : cycleIndex);
            if (atEnd && IsLive(child, version)) { NotifyChildComplete(child); }
        }

        // Nested sequences are evaluated through a depth-guarded walk; nesting beyond
        // MaxSequenceDepth levels is rejected to keep the call chain strictly bounded.
        static void EvaluateSequenceChild(TweenSlot child, float local) {
            Debug.Assert(child.IsSequence, "EvaluateSequenceChild requires a sequence slot.");
            if (_sequenceDepth >= MaxSequenceDepth) {
                Debug.LogError("RavenTween: sequence nesting exceeds " + MaxSequenceDepth + " levels; deeper levels are skipped.");
                return;
            }
            float cycleLength = Mathf.Max(child.SequenceDuration, 0f);
            if (cycleLength <= 0f) { return; }
            uint version = child.Version;
            int totalCycles = Mathf.Max(child.Cycles, 1);
            int cycleIndex = Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = local - cycleIndex * cycleLength;
            bool atEnd = local >= cycleLength * totalCycles;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            _sequenceDepth++;
            try {
                ApplySequenceTime(child, atEnd ? cycleLength : inCycle, atEnd ? totalCycles - 1 : cycleIndex);
            } finally {
                _sequenceDepth--;
            }
            if (atEnd && IsLive(child, version)) { NotifyChildComplete(child); }
        }

        static void NotifyChildComplete(TweenSlot child) {
            if (child.CompleteNotified) { return; }
            child.CompleteNotified = true; // Fires once per sequence cycle; reset on wrap.
            InvokeSafe(child.OnComplete);
        }

        /// <summary>Computes the full length of one sequence cycle from its items.</summary>
        public static void RecalculateSequenceDuration(TweenSlot slot) {
            Debug.Assert(slot.IsSequence, "Only sequences have a computed duration.");
            float max = 0f;
            for (int i = 0; i < slot.Items.Count; i++) {
                float end = slot.Items[i].StartTime + slot.Items[i].Duration;
                if (end > max) { max = end; }
            }
            slot.SequenceDuration = max;
        }

        // ----- Lifecycle -----

        /// <summary>Rents a slot and returns its index. The slot comes fully reset.</summary>
        public static int Rent(out TweenSlot slot) {
            int index;
            if (FreeIndices.Count > 0) {
                index = FreeIndices.Pop();
                slot = Slots[index];
            } else {
                slot = new TweenSlot();
                Slots.Add(slot);
                index = Slots.Count - 1;
            }
            slot.Reset();
            slot.State = SlotState.Running;
            slot.BornPass = _pass; // Not stepped by the pass that is running right now.
            return index;
        }

        /// <summary>Resolves a handle. Returns false when the tween is dead or recycled.</summary>
        public static bool TryGetSlot(int index, uint version, out TweenSlot slot) {
            return TryGetSlotInternal(index, version, out slot);
        }

        static bool TryGetSlotInternal(int index, uint version, out TweenSlot slot) {
            if (index < 0 || index >= Slots.Count) { slot = null; return false; }
            TweenSlot candidate = Slots[index];
            bool alive = candidate.State != SlotState.Free && candidate.Version == version;
            slot = alive ? candidate : null;
            return alive;
        }

        // End-of-life paths release first and run user code after: inside these callbacks the
        // handle is already dead, so Stop/Complete on it (or StopAll) can never re-enter.

        static void HandleTargetDestroyed(TweenSlot slot, int index) {
            Action callback = slot.OnTargetDestroyed;
            Action continuations = ReleaseCollect(index);
            InvokeSafe(callback);
            InvokeEach(continuations);
        }

        static void CompleteAndRelease(TweenSlot slot, int index) {
            Action callback = slot.OnComplete;
            Action continuations = ReleaseCollect(index);
            InvokeSafe(callback);
            InvokeEach(continuations);
        }

        static void KillInternal(TweenSlot slot, int index, bool fireOnKill) {
            Action callback = fireOnKill ? slot.OnKill : null;
            Action continuations = ReleaseCollect(index);
            InvokeSafe(callback);
            InvokeEach(continuations);
        }

        /// <summary>Stops a tween. Optionally jumps to the final value and fires OnComplete.</summary>
        public static void Kill(int index, uint version, bool complete) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; stop or complete the sequence instead.");
                return;
            }
            if (complete) { ForceComplete(slot, index); }
            else { KillInternal(slot, index, true); }
        }

        // Writing the final value runs user callbacks (OnUpdate, children's OnComplete) that may
        // call Complete() on this same tween again; that nested call is ignored instead of recursing.
        static void ForceComplete(TweenSlot slot, int index) {
            if (slot.Completing) { return; }
            uint version = slot.Version;
            bool targetAlive = !slot.RequiresTarget || slot.UnityTarget != null;
            if (targetAlive) {
                slot.Completing = true;
                try {
                    if (!ApplyFinalState(slot, version)) { return; }
                } finally {
                    if (IsLive(slot, version)) { slot.Completing = false; }
                }
            }
            CompleteAndRelease(slot, index);
        }

        // Returns false when a callback stopped the tween on the way.
        static bool ApplyFinalState(TweenSlot slot, uint version) {
            EnsureStarted(slot);
            if (!IsLive(slot, version)) { return false; }
            int lastCycle = slot.Cycles > 0 ? slot.Cycles - 1 : Mathf.Max(slot.CyclesDone, 0);
            slot.CyclesDone = lastCycle + 1;
            if (slot.IsSequence) { ApplySequenceTime(slot, slot.SequenceDuration, lastCycle); }
            else { ApplyProgress(slot, 1f, lastCycle); }
            return IsLive(slot, version);
        }

        static readonly Stack<int> ReleaseWork = new Stack<int>(16);

        // Iterative release of a root and all its descendants. Returns every await continuation
        // found in the tree (children included) so the caller can resume them afterwards.
        static Action ReleaseCollect(int index) {
            Debug.Assert(Slots[index].State != SlotState.Free, "Slot is already free.");
            Debug.Assert(ReleaseWork.Count == 0, "Release work stack must start empty.");
            Action continuations = null;
            ReleaseWork.Push(index);
            while (ReleaseWork.Count > 0) {
                int current = ReleaseWork.Pop();
                TweenSlot currentSlot = Slots[current];
                if (currentSlot.State == SlotState.Free) { continue; }
                if (currentSlot.AwaitContinuations != null) { continuations += currentSlot.AwaitContinuations; }
                if (currentSlot.IsSequence && currentSlot.Items != null) {
                    for (int i = 0; i < currentSlot.Items.Count; i++) {
                        SequenceItem item = currentSlot.Items[i];
                        if (TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out _)) { ReleaseWork.Push(item.ChildIndex); }
                    }
                    currentSlot.Items.Clear();
                }
                currentSlot.ClearReferences();
                currentSlot.State = SlotState.Free;
                currentSlot.Version++;
                FreeIndices.Push(current);
            }
            return continuations;
        }

        /// <summary>Pauses or resumes a root tween or sequence. Paused tweens keep their slot alive.</summary>
        public static void SetPaused(int index, uint version, bool paused) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; pause the sequence instead.");
                return;
            }
            slot.State = paused ? SlotState.Paused : SlotState.Running;
        }

        /// <summary>Stops every live root tween, optionally completing them first.</summary>
        /// <remarks>Tweens created by callbacks while this runs survive it.</remarks>
        public static void KillAll(bool complete) {
            int pass = ++_pass;
            int count = Slots.Count;
            for (int i = 0; i < count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass == pass) { continue; }
                Kill(i, slot.Version, complete);
            }
        }

        /// <summary>
        /// Stops any running effect (shake / punch) on the same target and property, and returns
        /// the rest value it captured. A new effect then oscillates around the true rest pose
        /// instead of the mid-motion pose (rapid double-clicks, repeated hits).
        /// </summary>
        public static bool TryTakeOverEffect(UnityEngine.Object target, PropertyKind property, out TweenValue rest) {
            rest = default;
            bool found = false;
            for (int i = 0; i < Slots.Count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free || slot.Effect == EffectKind.None) { continue; }
                if (slot.UnityTarget != target || slot.Property != property || slot.OwnedBySequence) { continue; }
                if (slot.FromCaptured && !found) {
                    rest = slot.StartValue;
                    found = true;
                }
                KillInternal(slot, i, true);
            }
            return found;
        }

        // ----- Debug introspection (used by editor tooling) -----

        /// <summary>Snapshot of one live slot, for inspection tools. Never cached.</summary>
        internal struct DebugInfo {
            public int Index;
            public uint Version;
            public bool IsSequence;
            public bool OwnedBySequence;
            public bool Paused;
            public UnityEngine.Object Target;
            public PropertyKind Property;
            public float Elapsed;          // Includes the start delay.
            public float StartDelay;
            public float CycleLength;      // Includes the start delay.
            public int Cycles;
            public int CyclesDone;
            public CycleMode Mode;
        }

        /// <summary>Fills <paramref name="buffer"/> with a snapshot of every live slot.</summary>
        public static void CollectDebugInfo(List<DebugInfo> buffer) {
            Debug.Assert(buffer != null, "Debug buffer cannot be null.");
            if (buffer == null) { return; }
            buffer.Clear();
            for (int i = 0; i < Slots.Count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free) { continue; }
                buffer.Add(new DebugInfo {
                    Index = i,
                    Version = slot.Version,
                    IsSequence = slot.IsSequence,
                    OwnedBySequence = slot.OwnedBySequence,
                    Paused = slot.State == SlotState.Paused,
                    Target = slot.UnityTarget,
                    Property = slot.Property,
                    Elapsed = slot.Elapsed,
                    StartDelay = slot.StartDelay,
                    CycleLength = slot.CycleLength,
                    Cycles = slot.Cycles,
                    CyclesDone = slot.CyclesDone,
                    Mode = slot.Mode
                });
            }
        }

        /// <summary>One child of a sequence, for timeline views.</summary>
        internal struct DebugSequenceItem {
            public float StartTime;
            public float Duration;
            public UnityEngine.Object Target;
            public PropertyKind Property;
            public bool IsSequence;
        }

        /// <summary>Lists the children of a live sequence. Returns false when the handle is dead.</summary>
        public static bool CollectSequenceItems(int index, uint version, List<DebugSequenceItem> buffer) {
            Debug.Assert(buffer != null, "Item buffer cannot be null.");
            buffer.Clear();
            if (!TryGetSlotInternal(index, version, out TweenSlot slot) || !slot.IsSequence || slot.Items == null) { return false; }
            for (int i = 0; i < slot.Items.Count; i++) {
                SequenceItem item = slot.Items[i];
                TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child);
                buffer.Add(new DebugSequenceItem {
                    StartTime = item.StartTime,
                    Duration = item.Duration,
                    Target = child != null ? child.UnityTarget : null,
                    Property = child != null ? child.Property : PropertyKind.None,
                    IsSequence = child != null && child.IsSequence
                });
            }
            return true;
        }

        /// <summary>
        /// Evaluates a root tween or sequence at <paramref name="time"/> seconds from its start
        /// (start delay included), from scratch: every child re-captures its start value, in
        /// timeline order. Used for editor scrubbing; the caller restores target values first.
        /// Tweens honor their cycles and yoyo; sequences cover their first cycle.
        /// </summary>
        public static void Seek(int index, uint version, float time) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            Debug.Assert(!slot.OwnedBySequence, "Seek the root, not a sequence child.");
            Debug.Assert(!float.IsNaN(time), "Seek time must be a number.");
            ResetForReplay(slot, true);
            float local = time - slot.StartDelay;
            if (local < 0f) { return; }
            EnsureStarted(slot);
            if (!IsLive(slot, version)) { return; }
            if (slot.IsSequence) {
                ApplySequenceTime(slot, Mathf.Min(local, Mathf.Max(slot.SequenceDuration, 0f)), 0);
                return;
            }
            float duration = Mathf.Max(slot.Duration, 1e-6f);
            int maxCycles = slot.Cycles < 0 ? int.MaxValue : Mathf.Max(slot.Cycles, 1);
            int cycle = (int)Math.Min(Math.Floor(local / duration), maxCycles - 1d);
            float progress = Mathf.Clamp01((local - cycle * duration) / duration);
            ApplyProgress(slot, progress, cycle);
        }

        static void InvokeCustom(TweenSlot slot, in TweenValue value) {
            Debug.Assert(slot.CustomSetter != null, "Custom invoker without a setter.");
            Debug.Assert(slot.CustomTarget != null, "Custom invoker without a target.");
            try { slot.CustomInvoker(slot.CustomTarget, slot.CustomSetter, value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static float _seedCursor;

        /// <summary>Deterministic, allocation-free per-tween noise seed.</summary>
        public static float NextSeed() {
            _seedCursor = (_seedCursor + 61.803398f) % 1000f;
            return _seedCursor;
        }

        static void InvokeSafe(Action callback) {
            if (callback == null) { return; }
            try { callback(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        // Await continuations from several awaiters: one failing must not strand the others.
        // Only reached when something awaited the tween, which already allocated.
        static void InvokeEach(Action callbacks) {
            if (callbacks == null) { return; }
            Delegate[] list = callbacks.GetInvocationList();
            for (int i = 0; i < list.Length; i++) {
                try { ((Action)list[i])(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        static void InvokeSafe(Action<float> callback, float value) {
            Debug.Assert(callback != null, "Callback must be checked before dispatch.");
            try { callback(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static void InvokeSafe(Action<TweenValue> callback, in TweenValue value) {
            Debug.Assert(callback != null, "Callback must be checked before dispatch.");
            try { callback(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
