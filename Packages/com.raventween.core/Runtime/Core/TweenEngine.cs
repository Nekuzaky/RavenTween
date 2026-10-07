using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RavenTween {
    /// <summary>Player loop phase in which the engine advances tweens.</summary>
    public enum UpdatePhase : byte {
        Update = 0,
        LateUpdate = 1,
        /// <summary>Physics rate, with Time.fixedDeltaTime. Use it for Rigidbody tweens.</summary>
        FixedUpdate = 2
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
        // Increases on every update or global stop; a slot born at or after a pass started is
        // skipped by that pass, even when a callback inside it started another pass.
        static long _pass;

        /// <summary>Global multiplier applied to every tween delta time.</summary>
        public static float TimeScale = 1f;

        /// <summary>Phase of the player loop in which tweens are stepped.</summary>
        public static UpdatePhase Phase = UpdatePhase.Update;

        /// <summary>Number of live tweens and sequences (including paused ones); sequence callbacks are not counted.</summary>
        public static int AliveCount {
            get {
                int count = 0;
                for (int i = 0; i < Slots.Count; i++) {
                    if (Slots[i].State != SlotState.Free && !Slots[i].IsCallback) { count++; }
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
            _lastUpdateFrame = -1;
            _lastLateUpdateFrame = -1;
            _evaluatedRoot = null;
            _evaluatedRootRevision = 0;
            Install();
        }

        // Runs on every play-mode entry (also without domain reload). The player loop may have
        // been reset by the editor in between, so presence is checked rather than remembered.
        static void Install() {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            if (ContainsEngine(root)) { return; }
            // Fixed tweens run right after scripts' FixedUpdate, before the physics step, so a
            // Rigidbody moved by a tween is simulated in the same step.
            InjectSystem(ref root, typeof(FixedUpdate), OnFixedUpdate, typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate));
            InjectSystem(ref root, typeof(Update), OnUpdate, null);
            InjectSystem(ref root, typeof(PreLateUpdate), OnLateUpdate, null);
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

        // Inserts the engine right after afterType inside the phase (or at its end when null or absent).
        static void InjectSystem(ref PlayerLoopSystem root, Type phaseType, PlayerLoopSystem.UpdateFunction callback, Type afterType) {
            Debug.Assert(phaseType != null, "Player loop phase type is required.");
            Debug.Assert(callback != null, "Player loop callback is required.");
            for (int i = 0; i < root.subSystemList.Length; i++) {
                if (root.subSystemList[i].type != phaseType) { continue; }
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                int at = InsertionIndex(children, afterType);
                var extended = new PlayerLoopSystem[children.Length + 1];
                Array.Copy(children, 0, extended, 0, at);
                extended[at] = new PlayerLoopSystem { type = typeof(TweenEngine), updateDelegate = callback };
                Array.Copy(children, at, extended, at + 1, children.Length - at);
                root.subSystemList[i].subSystemList = extended;
                return;
            }
        }

        static int InsertionIndex(PlayerLoopSystem[] children, Type afterType) {
            if (afterType == null) { return children.Length; }
            for (int i = 0; i < children.Length; i++) {
                if (children[i].type == afterType) { return i + 1; }
            }
            return children.Length;
        }

        static int _lastUpdateFrame = -1;
        static int _lastLateUpdateFrame = -1;

        static void OnFixedUpdate() {
            if (!Application.isPlaying) { return; }
            ProcessPhase(UpdatePhase.FixedUpdate, Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        }

        static void OnUpdate() {
            if (!Application.isPlaying || Time.frameCount == _lastUpdateFrame) { return; }
            _lastUpdateFrame = Time.frameCount;
            ProcessPhase(UpdatePhase.Update, Time.deltaTime, Time.unscaledDeltaTime);
        }

        static void OnLateUpdate() {
            if (!Application.isPlaying || Time.frameCount == _lastLateUpdateFrame) { return; }
            _lastLateUpdateFrame = Time.frameCount;
            ProcessPhase(UpdatePhase.LateUpdate, Time.deltaTime, Time.unscaledDeltaTime);
        }

        static float Sanitize(float delta) {
            return float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f ? 0f : delta;
        }

        /// <summary>Phase a root tween runs in: its own, or the global <see cref="Phase"/>.</summary>
        static UpdatePhase EffectivePhase(TweenSlot slot) {
            return slot.HasPhase ? slot.Phase : Phase;
        }

        /// <summary>Advances every live root tween, whatever its phase. Exposed internally for deterministic tests.</summary>
        public static void Process(float scaledDelta, float unscaledDelta) {
            ProcessFiltered(-1, scaledDelta, unscaledDelta);
        }

        /// <summary>Advances the live root tweens that run in <paramref name="phase"/>.</summary>
        public static void ProcessPhase(UpdatePhase phase, float scaledDelta, float unscaledDelta) {
            ProcessFiltered((int)phase, scaledDelta, unscaledDelta);
        }

        static void ProcessFiltered(int phase, float scaledDelta, float unscaledDelta) {
            Debug.Assert(scaledDelta >= 0f, "Delta time cannot be negative.");
            Debug.Assert(unscaledDelta >= 0f, "Unscaled delta time cannot be negative.");
            if (_processing) { return; }
            scaledDelta = Sanitize(scaledDelta);
            unscaledDelta = Sanitize(unscaledDelta);
            _processing = true;
            long pass = ++_pass;
            int count = Slots.Count;
            try {
                for (int i = 0; i < count; i++) {
                    TweenSlot slot = Slots[i];
                    if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                    if (phase >= 0 && (int)EffectivePhase(slot) != phase) { continue; }
                    if (StopIfCancelled(slot, i) || slot.State != SlotState.Running) { continue; }
                    float delta = Sanitize((slot.UseUnscaledTime ? unscaledDelta : scaledDelta) * TimeScale * slot.TimeScale);
                    StepRootGuarded(slot, i, delta);
                }
            } finally {
                _processing = false;
                _sequenceDepth = 0;
            }
        }

        // Cancellation is polled, never registered: no allocation, and a token cancelled on
        // another thread is only acted on here, on the main thread.
        static bool StopIfCancelled(TweenSlot slot, int index) {
            if (!slot.HasCancellation || !slot.Cancellation.IsCancellationRequested) { return false; }
            KillInternal(slot, index, true);
            return true;
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
            if (TargetLost(slot)) {
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

        // A destroyed Unity target, or a dead tween whose time scale this one drives.
        static bool TargetLost(TweenSlot slot) {
            if (slot.RequiresTarget && slot.UnityTarget == null) { return true; }
            return slot.LinkIndex >= 0 && !TryGetSlotInternal(slot.LinkIndex, slot.LinkVersion, out _);
        }

        /// <summary>How much user code an evaluation of the timeline may run.</summary>
        enum Quiet : byte {
            /// <summary>Playing: every callback fires.</summary>
            None = 0,
            /// <summary>A nested sequence played backwards: its timeline callbacks are skipped.</summary>
            Callbacks = 1,
            /// <summary>Showing start values again: nothing fires and nothing is marked as done.</summary>
            Rewind = 2,
            /// <summary>Jumping to a time: nothing fires, and what was passed over counts as done.</summary>
            Jump = 3
        }

        static void EnsureStarted(TweenSlot slot) {
            EnsureStarted(slot, Quiet.None);
        }

        static void EnsureStarted(TweenSlot slot, Quiet quiet) {
            if (slot.StartFired) { return; }
            slot.StartFired = true;
            if (!slot.IsSequence) { CaptureFromIfNeeded(slot); }
            if (quiet < Quiet.Rewind) { InvokeSafe(slot.OnStart); }
        }

        static void CaptureFromIfNeeded(TweenSlot slot) {
            if (slot.FromCaptured) { return; }
            slot.FromCaptured = true;
            if (slot.HasExplicitFrom) { return; }
            if (slot.Property != PropertyKind.None && slot.UnityTarget != null) {
                slot.StartValue = PropertyAccessor.Read(slot.Property, slot.UnityTarget, slot.PropertyId);
            } else if (slot.CustomGetter != null) {
                slot.StartValue = slot.CustomGetter(slot.CustomTarget, slot.CustomGetterDelegate);
            } else if (slot.LinkIndex >= 0 && TryGetSlotInternal(slot.LinkIndex, slot.LinkVersion, out TweenSlot linked)) {
                slot.StartValue = new TweenValue(linked.TimeScale);
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
                CompleteAfterFinalValue(slot, index, version, slot.Cycles - 1);
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
            CompleteAfterFinalValue(slot, index, version, Mathf.Max(slot.Cycles, 1) - 1);
        }

        // The last OnUpdate runs while Completing: a Complete(), a time jump or a change of
        // cycles requested from it is ignored, so a finishing tween always finishes.
        static void CompleteAfterFinalValue(TweenSlot slot, int index, uint version, int lastCycle) {
            slot.CyclesDone = lastCycle + 1;
            slot.Elapsed = slot.StartDelay + Mathf.Max(slot.Duration, 0f);
            slot.Completing = true;
            ApplyProgress(slot, 1f, lastCycle);
            if (!IsLive(slot, version)) { return; }
            slot.Completing = false;
            CompleteAndRelease(slot, index);
        }

        static void ApplyProgress(TweenSlot slot, float cycleProgress, int cycleIndex) {
            WriteValue(slot, ValueAt(slot, cycleProgress, cycleIndex));
        }

        // Yoyo replays the cycle backwards in time; PingPong eases from the end back to the
        // start; Incremental shifts each cycle by the change of one cycle.
        static TweenValue ValueAt(TweenSlot slot, float cycleProgress, int cycleIndex) {
            bool odd = (cycleIndex & 1) == 1;
            float t = slot.Mode == CycleMode.Yoyo && odd ? 1f - cycleProgress : cycleProgress;
            float eased = EvaluateEase(slot, t);
            if (slot.Effect != EffectKind.None) { return EvaluateEffect(slot, eased); }
            if (slot.Mode == CycleMode.PingPong && odd) { return TweenValue.Lerp(slot.EndValue, slot.StartValue, eased); }
            TweenValue value = TweenValue.Lerp(slot.StartValue, slot.EndValue, eased);
            if (slot.Mode != CycleMode.Incremental) { return value; }
            return TweenValue.AddCycles(value, slot.StartValue, slot.EndValue, cycleIndex);
        }

        /// <summary>The eased factor a tween shows right now (0 at its start value, 1 at its end value).</summary>
        public static float InterpolationFactor(int index, uint version) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot) || slot.IsSequence) { return 0f; }
            float duration = slot.Duration;
            float local = Mathf.Max(slot.Elapsed - slot.StartDelay, 0f);
            // A finished sequence child rests on the end of its last cycle.
            bool finished = slot.Cycles > 0 && slot.CyclesDone >= slot.Cycles;
            float progress = finished || duration <= 0f ? 1f : Mathf.Clamp01(local / duration);
            bool odd = ((finished ? slot.Cycles - 1 : slot.CyclesDone) & 1) == 1;
            if (slot.Mode == CycleMode.Yoyo && odd) { return EvaluateEase(slot, 1f - progress); }
            float eased = EvaluateEase(slot, progress);
            return slot.Mode == CycleMode.PingPong && odd ? 1f - eased : eased;
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
            if (slot.Parametric > EasingKind.Curve) {
                float distance = slot.Parametric == EasingKind.BounceExact ? TweenValue.Distance(slot.StartValue, slot.EndValue) : 1f;
                return EaseUtility.EvaluateParametric(slot.Parametric, t, slot.EaseA, slot.EaseB, distance);
            }
            if (slot.CustomEase != null) { return slot.CustomEase(t); }
            if (slot.CustomCurve != null) { return slot.CustomCurve.Evaluate(t); }
            return t;
        }

        // Setters and eases run unguarded: an exception stops the tween (see StepRootGuarded)
        // instead of being logged again every frame. User callbacks are contained.
        // Each piece of user code may stop this tween and reuse its slot at once: the slot is
        // checked again before the next one runs.
        static void WriteValue(TweenSlot slot, in TweenValue value) {
            uint version = slot.Version;
            if (slot.Property != PropertyKind.None && slot.UnityTarget != null) {
                PropertyAccessor.Write(slot.Property, slot.UnityTarget, slot.PropertyId, value);
            }
            if (slot.LinkIndex >= 0) { WriteLinkedTimeScale(slot, value.Float); }
            if (slot.CustomInvoker != null) {
                slot.CustomInvoker(slot.CustomTarget, slot.CustomSetter, value);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.OnUpdateFloat != null) {
                InvokeSafe(slot.OnUpdateFloat, value.Float);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.OnUpdateValue != null) {
                InvokeSafe(slot.OnUpdateValue, value);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.UpdateInvoker != null) {
                InvokeTargetUpdate(slot);
                if (!IsLive(slot, version)) { return; }
            }
            InvokeSafe(slot.OnUpdate);
        }

        static void WriteLinkedTimeScale(TweenSlot slot, float scale) {
            if (TryGetSlotInternal(slot.LinkIndex, slot.LinkVersion, out TweenSlot linked)) {
                linked.TimeScale = Mathf.Max(scale, 0f);
            }
        }

        static void InvokeTargetUpdate(TweenSlot slot) {
            if (IsDestroyedUnityObject(slot.UpdateTarget)) { return; }
            try { slot.UpdateInvoker(slot.UpdateTarget, slot.UpdateDelegate, new Tween(slot.Index, slot.Version)); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static bool IsDestroyedUnityObject(object target) {
            return target is UnityEngine.Object unityObject && unityObject == null;
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
                    ApplySequenceTime(slot, local, slot.CyclesDone, Quiet.None);
                    return;
                }
                int closing = slot.CyclesDone;
                int revision = slot.Revision;
                // Close out the cycle so children reach their end state and fire their per-cycle
                // callbacks before everything rewinds. It is still the current cycle while its
                // callbacks run: SetRemainingCycles(1) there makes it the last one.
                ApplySequenceTime(slot, duration, closing, Quiet.None);
                // A callback may have stopped the sequence or moved its time.
                if (!IsLive(slot, version) || slot.Revision != revision) { return; }
                AddCycles(slot, 1);
                if (slot.Cycles >= 0 && slot.CyclesDone >= slot.Cycles) {
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

        // A zero-length sequence (only callbacks, or nothing) happens all at once: its callbacks
        // fire once, then it completes. An infinite one stays alive without firing again.
        static void FinishEmptySequence(TweenSlot slot, int index, uint version) {
            int revision = slot.Revision;
            ApplySequenceTime(slot, 0f, 0, Quiet.None);
            if (slot.Cycles < 0 || !IsLive(slot, version) || slot.Revision != revision) { return; }
            slot.CyclesDone = Mathf.Max(slot.Cycles, 1);
            CompleteAndRelease(slot, index);
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
            slot.CallbackCycle = -1;
            slot.CallbackCursor = -1f;
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

        // Items are sorted by ActiveStart, the moment a child starts writing (its own delay
        // included). Rewinds run first, latest first, so the earliest child's start value wins;
        // active children then run in order, so the latest active child wins. time is the time
        // played in this cycle; Yoyo maps odd cycles backwards. User code may stop anything or
        // move the time at any point, so the sequence is re-validated after every child.
        static void ApplySequenceTime(TweenSlot slot, float time, int cycleIndex, Quiet quiet) {
            Debug.Assert(slot.IsSequence, "ApplySequenceTime requires a sequence slot.");
            Debug.Assert(slot.Items != null, "Sequence slot is missing its item list.");
            bool isRoot = !slot.OwnedBySequence;
            TweenSlot outerRoot = _evaluatedRoot;
            int outerRevision = _evaluatedRootRevision;
            if (isRoot) {
                _evaluatedRoot = slot;
                _evaluatedRootRevision = slot.Revision;
            }
            try {
                EvaluateTimeline(slot, time, cycleIndex, quiet);
            } finally {
                if (isRoot) {
                    _evaluatedRoot = outerRoot;
                    _evaluatedRootRevision = outerRevision;
                }
            }
        }

        // The root whose timeline is being evaluated, and its revision when that began: a jump
        // of the root requested from deep inside a nested sequence stops every level at once.
        static TweenSlot _evaluatedRoot;
        static int _evaluatedRootRevision;

        static void EvaluateTimeline(TweenSlot slot, float time, int cycleIndex, Quiet quiet) {
            uint version = slot.Version;
            int revision = slot.Revision;
            bool backwards = slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1;
            float t = backwards ? slot.SequenceDuration - time : time;
            // Nested sequences only play their timeline callbacks while this one runs forward.
            Quiet childQuiet = quiet == Quiet.None && backwards ? Quiet.Callbacks : quiet;
            if (!RewindPass(slot, version, revision, t)) { return; }
            if (!ActivePass(slot, version, revision, t, childQuiet)) { return; }
            if (slot.HasCallbacks) { FireCallbacks(slot, version, revision, time, cycleIndex, backwards, quiet); }
        }

        static bool StillCurrent(TweenSlot slot, uint version, int revision) {
            return IsLive(slot, version) && slot.Revision == revision && RootUnchanged();
        }

        static bool RewindPass(TweenSlot slot, uint version, int revision, float t) {
            List<SequenceItem> items = slot.Items;
            for (int i = items.Count - 1; i >= 0; i--) {
                SequenceItem item = items[i];
                if (t >= item.ActiveStart) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child) && !child.IsCallback) { RewindOnce(child); }
                if (!StillCurrent(slot, version, revision)) { return false; }
            }
            return true;
        }

        static bool ActivePass(TweenSlot slot, uint version, int revision, float t, Quiet quiet) {
            List<SequenceItem> items = slot.Items;
            for (int i = 0; i < items.Count; i++) {
                SequenceItem item = items[i];
                if (t < item.ActiveStart) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child) && !child.IsCallback) {
                    child.Rewound = false;
                    EvaluateChildAtTime(child, Mathf.Min(t - item.StartTime, item.Duration), quiet);
                }
                if (!StillCurrent(slot, version, revision)) { return false; }
            }
            return true;
        }

        // Callbacks fire once per cycle, when the time played in that cycle passes their spot
        // (mirrored on Yoyo's backward cycles). A spot exactly on a Yoyo turning point fires
        // only on the cycle that reaches it, not again on the cycle that leaves it. When quiet,
        // the cursor still moves, so what was passed over doesn't fire later.
        static void FireCallbacks(TweenSlot slot, uint version, int revision, float time, int cycleIndex, bool backwards, Quiet quiet) {
            if (cycleIndex != slot.CallbackCycle) {
                slot.CallbackCycle = cycleIndex;
                slot.CallbackCursor = -1f;
            }
            float from = slot.CallbackCursor;
            // A jump lands just before its target on the root timeline: callbacks exactly at
            // that time still fire when playing resumes. Nested timelines were passed over.
            bool landing = quiet == Quiet.Jump && !slot.OwnedBySequence;
            slot.CallbackCursor = Mathf.Max(landing ? time - JumpMargin : time, from);
            if (quiet != Quiet.None || time <= from) { return; }
            bool skipTurningPoint = slot.Mode == CycleMode.Yoyo && cycleIndex > 0;
            List<SequenceItem> items = slot.Items;
            for (int i = 0; i < items.Count; i++) {
                int at = backwards ? items.Count - 1 - i : i;
                SequenceItem item = items[at];
                float spot = backwards ? slot.SequenceDuration - item.StartTime : item.StartTime;
                if (spot <= from || spot > time || (skipTurningPoint && spot <= 0f)) { continue; }
                if (TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child) && child.IsCallback) {
                    InvokeCompleteCallbacks(child.OnComplete, child.CompleteTarget, child.CompleteDelegate, child.CompleteInvoker);
                }
                if (!StillCurrent(slot, version, revision)) { return; }
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
            EvaluateChildAtTime(child, child.StartDelay, Quiet.Rewind);
            if (child.IsSequence) { child.CallbackCycle = -1; }
        }

        const int MaxSequenceDepth = 8;
        const float JumpMargin = 1e-5f;
        static int _sequenceDepth;

        static void EvaluateChildAtTime(TweenSlot child, float childLocal, Quiet quiet) {
            float local = childLocal - child.StartDelay;
            if (local < 0f) { return; }
            uint version = child.Version;
            if (quiet == Quiet.Jump && local <= 0f && !child.StartFired) {
                ShowUnstarted(child, quiet);
                return;
            }
            EnsureStarted(child, quiet);
            if (!IsLive(child, version)) { return; }
            if (child.IsSequence) { EvaluateSequenceChild(child, local, quiet); return; }
            float cycleLength = Mathf.Max(child.Duration, 0f);
            int totalCycles = Mathf.Max(child.Cycles, 1);
            bool atEnd = cycleLength <= 0f || local >= cycleLength * totalCycles;
            int cycleIndex = atEnd ? totalCycles - 1 : Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = atEnd ? cycleLength : local - cycleIndex * cycleLength;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            child.Elapsed = child.StartDelay + inCycle; // Keeps Progress / ElapsedTime meaningful on children.
            ApplyProgress(child, atEnd ? 1f : Mathf.Clamp01(inCycle / cycleLength), cycleIndex);
            if (atEnd && IsLive(child, version) && RootUnchanged()) { NotifyChildComplete(child, quiet); }
        }

        // A jump that lands exactly on a child's start shows its start value but leaves it
        // unstarted, so its OnStart (and a zero-length child's OnComplete) fire when playing resumes.
        static void ShowUnstarted(TweenSlot child, Quiet quiet) {
            if (child.IsSequence) {
                ShowUnstartedSequence(child, quiet);
                return;
            }
            CaptureFromIfNeeded(child);
            if (child.Duration > 0f) { ApplyProgress(child, 0f, 0); }
        }

        // A nested sequence at its very start: its own children show their start values, and it
        // stays unstarted with its callbacks armed, as if never played.
        static void ShowUnstartedSequence(TweenSlot child, Quiet quiet) {
            if (_sequenceDepth >= MaxSequenceDepth) { return; }
            _sequenceDepth++;
            try {
                ApplySequenceTime(child, 0f, 0, quiet);
            } finally {
                _sequenceDepth--;
            }
            child.CallbackCycle = -1;
            child.CallbackCursor = -1f;
        }

        // False once user code moved the time of the root being evaluated: results computed
        // before that (a child reaching its end) are out of date and must not fire.
        static bool RootUnchanged() {
            return _evaluatedRoot == null || _evaluatedRoot.Revision == _evaluatedRootRevision;
        }

        // Nested sequences are evaluated through a depth-guarded walk; nesting beyond
        // MaxSequenceDepth levels is rejected to keep the call chain strictly bounded.
        static void EvaluateSequenceChild(TweenSlot child, float local, Quiet quiet) {
            Debug.Assert(child.IsSequence, "EvaluateSequenceChild requires a sequence slot.");
            if (_sequenceDepth >= MaxSequenceDepth) {
                Debug.LogError("RavenTween: sequence nesting exceeds " + MaxSequenceDepth + " levels; deeper levels are skipped.");
                return;
            }
            uint version = child.Version;
            bool atEnd;
            _sequenceDepth++;
            try {
                atEnd = EvaluateNestedTimeline(child, local, quiet);
            } finally {
                _sequenceDepth--;
            }
            if (atEnd && IsLive(child, version) && RootUnchanged()) { NotifyChildComplete(child, quiet); }
        }

        // Returns true when the nested sequence is at its very end.
        static bool EvaluateNestedTimeline(TweenSlot child, float local, Quiet quiet) {
            float cycleLength = Mathf.Max(child.SequenceDuration, 0f);
            int totalCycles = Mathf.Max(child.Cycles, 1);
            if (cycleLength <= 0f) {
                // Zero-length nested sequence (only callbacks, or empty): it happens all at once.
                child.CyclesDone = totalCycles;
                child.Elapsed = child.StartDelay;
                ApplySequenceTime(child, 0f, 0, quiet);
                return true;
            }
            bool atEnd = local >= cycleLength * totalCycles;
            int cycleIndex = atEnd ? totalCycles - 1 : Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            if (!CloseEarlierCycle(child, cycleIndex, cycleLength, quiet)) { return false; }
            float inCycle = atEnd ? cycleLength : local - cycleIndex * cycleLength;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            child.Elapsed = child.StartDelay + inCycle;
            ApplySequenceTime(child, inCycle, cycleIndex, quiet);
            return atEnd;
        }

        // Moving forward into a later cycle of a nested sequence: finish the cycle that was
        // showing and any cycle skipped by a long frame (children reach their end, callbacks
        // fire), resetting per-cycle state in between, as a root sequence does when it wraps.
        // Not while rewinding or playing backwards. Returns false if user code stopped it.
        static bool CloseEarlierCycle(TweenSlot child, int cycleIndex, float cycleLength, Quiet quiet) {
            if (quiet == Quiet.Rewind || quiet == Quiet.Callbacks) { return true; }
            int shown = Mathf.Min(child.CyclesDone, Mathf.Max(child.Cycles, 1) - 1);
            uint version = child.Version;
            int last = Mathf.Min(cycleIndex, shown + MaxSequenceWrapsPerStep);
            for (int cycle = shown; cycle < last; cycle++) {
                ApplySequenceTime(child, cycleLength, cycle, quiet);
                if (!IsLive(child, version)) { return false; }
                ResetSequenceChildren(child);
            }
            return true;
        }

        static void NotifyChildComplete(TweenSlot child, Quiet quiet) {
            if (quiet == Quiet.Rewind || child.CompleteNotified) { return; }
            child.CompleteNotified = true; // Once per sequence cycle; reset on wrap.
            if (quiet == Quiet.Jump) { return; }
            InvokeCompleteCallbacks(child.OnComplete, child.CompleteTarget, child.CompleteDelegate, child.CompleteInvoker);
        }

        static void InvokeCompleteCallbacks(Action callback, object target, Delegate targetCallback, Action<object, Delegate> invoker) {
            InvokeSafe(callback);
            if (invoker == null || IsDestroyedUnityObject(target)) { return; }
            try { invoker(target, targetCallback); }
            catch (Exception exception) { Debug.LogException(exception); }
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
            slot.Index = index;
            slot.State = SlotState.Running;
            slot.BornPass = _pass; // Not stepped by the pass that is running right now.
            return index;
        }

        /// <summary>Pre-creates pooled slots so that the first <paramref name="capacity"/> live tweens never allocate.</summary>
        public static void EnsureCapacity(int capacity) {
            Debug.Assert(capacity >= 0, "Capacity cannot be negative.");
            const int Limit = 1 << 20;
            int target = Mathf.Min(capacity, Limit);
            if (Slots.Capacity < target) { Slots.Capacity = target; }
            while (Slots.Count < target) {
                var slot = new TweenSlot { Index = Slots.Count };
                Slots.Add(slot);
                FreeIndices.Push(slot.Index);
            }
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
            object target = slot.CompleteTarget;
            Delegate targetCallback = slot.CompleteDelegate;
            Action<object, Delegate> invoker = slot.CompleteInvoker;
            Action continuations = ReleaseCollect(index);
            InvokeCompleteCallbacks(callback, target, targetCallback, invoker);
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
            if (complete) { CompleteGuarded(slot, index); }
            else { KillInternal(slot, index, true); }
        }

        // A throwing setter or ease while completing stops that tween only: Complete(),
        // CompleteAll() and CompleteAll(target) never abort half-way through.
        static void CompleteGuarded(TweenSlot slot, int index) {
            uint version = slot.Version;
            try {
                ForceComplete(slot, index);
            } catch (Exception exception) {
                StopAfterException(slot, index, version, exception);
            }
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
            if (slot.IsSequence) { ApplySequenceTime(slot, slot.SequenceDuration, lastCycle, Quiet.None); }
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
            long pass = ++_pass;
            int count = Slots.Count;
            for (int i = 0; i < count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                Kill(i, slot.Version, complete);
            }
        }

        public enum TargetAction : byte { Count, Stop, Complete, Pause, Resume }

        /// <summary>
        /// Applies <paramref name="action"/> to every live root tween animating
        /// <paramref name="target"/> (its Unity target or custom target; for a GameObject, also
        /// its components). A null target means every root tween and sequence. Returns how many matched.
        /// </summary>
        public static int ForEachOnTarget(object target, TargetAction action) {
            long pass = ++_pass;
            int count = Slots.Count;
            int matched = 0;
            for (int i = 0; i < count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                if (target != null && !Targets(slot, target)) { continue; }
                matched++;
                ApplyTargetAction(slot, i, action);
            }
            return matched;
        }

        static bool Targets(TweenSlot slot, object target) {
            if (ReferenceEquals(slot.CustomTarget, target)) { return true; }
            if (slot.UnityTarget is null) { return false; }
            if (ReferenceEquals(slot.UnityTarget, target)) { return true; }
            var go = target as GameObject;
            return !ReferenceEquals(go, null) && slot.UnityTarget is Component component && component != null &&
                   ReferenceEquals(component.gameObject, go);
        }

        static void ApplyTargetAction(TweenSlot slot, int index, TargetAction action) {
            switch (action) {
                case TargetAction.Stop: KillInternal(slot, index, true); break;
                case TargetAction.Complete: CompleteGuarded(slot, index); break;
                case TargetAction.Pause: slot.State = SlotState.Paused; break;
                case TargetAction.Resume: slot.State = SlotState.Running; break;
                default: break;
            }
        }

        // ----- Time position (ElapsedTime setters) -----

        /// <summary>Time since the tween was created, start delay included (0 before it starts).</summary>
        public static float ElapsedTotal(int index, uint version) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return 0f; }
            // Once every cycle is done, Elapsed already holds the last one.
            bool finished = slot.Cycles > 0 && slot.CyclesDone >= slot.Cycles;
            int before = finished ? slot.Cycles - 1 : slot.CyclesDone;
            return slot.Elapsed + (float)before * Mathf.Max(slot.CycleDuration, 0f);
        }

        /// <summary>
        /// Moves a root tween or sequence to <paramref name="total"/> seconds since it was created
        /// (start delay included), forwards or backwards, and shows that pose immediately. Past
        /// the end of a finite tween it completes. A jump is silent: timeline callbacks and the
        /// OnStart / OnComplete of children passed over don't fire; what comes after the new time
        /// fires normally as the tween plays on.
        /// </summary>
        public static void SetElapsedTotal(int index, uint version, float total) {
            SetElapsedTotal(index, version, total, false);
        }

        /// <summary>
        /// Same, where <paramref name="endOfCycle"/> resolves a time that falls exactly between two
        /// cycles to the end of the earlier one (ElapsedTime = Duration, Progress = 1) instead of
        /// the start of the next one.
        /// </summary>
        public static void SetElapsedTotal(int index, uint version, float total, bool endOfCycle) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; move the sequence's time instead.");
                return;
            }
            // A jump requested by a callback of this same jump or completion is ignored: no recursion.
            if (slot.Moving || slot.Completing || float.IsNaN(total) || float.IsInfinity(total)) { return; }
            if (TargetLost(slot)) {
                HandleTargetDestroyed(slot, index);
                return;
            }
            slot.Revision++;
            slot.Moving = true;
            try {
                MoveTo(slot, index, version, Mathf.Max(total, 0f), endOfCycle);
            } catch (Exception exception) {
                StopAfterException(slot, index, version, exception);
            } finally {
                if (IsLive(slot, version)) { slot.Moving = false; }
            }
        }

        static void MoveTo(TweenSlot slot, int index, uint version, float total, bool endOfCycle) {
            float cycle = Mathf.Max(slot.CycleDuration, 0f);
            double local = total - slot.StartDelay;
            if (slot.Cycles >= 0 && local >= (double)cycle * Mathf.Max(slot.Cycles, 1)) {
                slot.Moving = false; // Completing runs callbacks normally.
                ForceComplete(slot, index);
                return;
            }
            if (local < 0d) { MoveIntoDelay(slot, version, total); return; }
            int cycleIndex = cycle > 0f ? (int)Math.Min(Math.Floor(local / cycle), int.MaxValue - 1) : 0;
            if (endOfCycle && cycleIndex > 0 && local - (double)cycleIndex * cycle < cycle * 1e-6) { cycleIndex--; }
            float inCycle = Mathf.Min((float)(local - (double)cycleIndex * cycle), cycle);
            if (slot.IsSequence) { MoveSequence(slot, version, cycleIndex, inCycle); }
            else { MoveTween(slot, version, cycleIndex, inCycle); }
        }

        static void StopAfterException(TweenSlot slot, int index, uint version, Exception exception) {
            Debug.LogException(exception);
            if (!IsLive(slot, version)) { return; }
            Debug.LogError("RavenTween: a tween threw an exception and was stopped.");
            KillInternal(slot, index, false);
        }

        static void MoveIntoDelay(TweenSlot slot, uint version, float total) {
            if (slot.StartFired) { ShowStart(slot, version); }
            if (!IsLive(slot, version)) { return; }
            slot.CyclesDone = 0;
            slot.Elapsed = total;
        }

        static void ShowStart(TweenSlot slot, uint version) {
            if (slot.IsSequence) { RewindSequence(slot, version); return; }
            ApplyProgress(slot, 0f, 0);
        }

        static void MoveTween(TweenSlot slot, uint version, int cycleIndex, float inCycle) {
            EnsureStarted(slot);
            if (!IsLive(slot, version)) { return; }
            slot.CyclesDone = cycleIndex;
            slot.Elapsed = slot.StartDelay + inCycle;
            float duration = Mathf.Max(slot.Duration, 1e-6f);
            ApplyProgress(slot, Mathf.Clamp01(inCycle / duration), cycleIndex);
        }

        static void MoveSequence(TweenSlot slot, uint version, int cycleIndex, float inCycle) {
            EnsureStarted(slot);
            if (!IsLive(slot, version)) { return; }
            RewindSequence(slot, version);
            if (!IsLive(slot, version)) { return; }
            slot.CyclesDone = cycleIndex;
            slot.Elapsed = slot.StartDelay + inCycle;
            ApplySequenceTime(slot, inCycle, cycleIndex, Quiet.Jump);
        }

        // Puts every child back on its start value, then forgets per-cycle state. Silent.
        static void RewindSequence(TweenSlot slot, uint version) {
            ApplySequenceTime(slot, 0f, 0, Quiet.Jump);
            if (!IsLive(slot, version)) { return; }
            ResetSequenceChildren(slot);
            slot.CallbackCycle = -1;
            slot.CallbackCursor = -1f;
        }

        /// <summary>
        /// Changes how many cycles remain, the current one included (1 = finish this cycle and
        /// complete, -1 = loop forever).
        /// </summary>
        public static void SetRemainingCycles(int index, uint version, int remaining) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogError("RavenTween: set the cycles of the sequence, not of its children; ignored.");
                return;
            }
            if (slot.Completing) { return; } // Already finishing: its last callbacks can't extend it.
            slot.Cycles = remaining < 0 ? -1 : slot.CyclesDone + Mathf.Max(remaining, 1);
        }

        /// <summary>
        /// Completes the tween the next time it reaches its end value (or its start value), for
        /// Yoyo and PingPong loops. Other modes always end on the end value: this cycle is the last.
        /// </summary>
        public static void SetRemainingCycles(int index, uint version, bool stopAtEndValue) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            bool backAndForth = slot.Mode == CycleMode.Yoyo || slot.Mode == CycleMode.PingPong;
            bool headingToEnd = (slot.CyclesDone & 1) == 0;
            int remaining = !backAndForth || headingToEnd == stopAtEndValue ? 1 : 2;
            SetRemainingCycles(index, version, remaining);
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
            public float TimeScale;
            public UpdatePhase Phase;
            public bool IsCallback;
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
                    Mode = slot.Mode,
                    TimeScale = slot.TimeScale,
                    Phase = EffectivePhase(slot),
                    IsCallback = slot.IsCallback
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
            public bool IsCallback;
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
                    IsSequence = child != null && child.IsSequence,
                    IsCallback = child != null && child.IsCallback
                });
            }
            return true;
        }

        /// <summary>
        /// Evaluates a root tween or sequence at <paramref name="time"/> seconds from its start
        /// (start delay included), from scratch: every child re-captures its start value, in
        /// timeline order. Used for editor scrubbing; the caller restores target values first.
        /// Tweens honor their cycles and yoyo; sequences cover their first cycle. Silent: no
        /// OnStart, OnComplete or timeline callback runs while scrubbing.
        /// </summary>
        public static void Seek(int index, uint version, float time) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            Debug.Assert(!slot.OwnedBySequence, "Seek the root, not a sequence child.");
            Debug.Assert(!float.IsNaN(time), "Seek time must be a number.");
            ResetForReplay(slot, true);
            float local = time - slot.StartDelay;
            if (local < 0f) { return; }
            EnsureStarted(slot, Quiet.Jump);
            if (!IsLive(slot, version)) { return; }
            if (slot.IsSequence) {
                ApplySequenceTime(slot, Mathf.Min(local, Mathf.Max(slot.SequenceDuration, 0f)), 0, Quiet.Jump);
                return;
            }
            float duration = Mathf.Max(slot.Duration, 1e-6f);
            int maxCycles = slot.Cycles < 0 ? int.MaxValue : Mathf.Max(slot.Cycles, 1);
            int cycle = (int)Math.Min(Math.Floor(local / duration), maxCycles - 1d);
            float progress = Mathf.Clamp01((local - cycle * duration) / duration);
            ApplyProgress(slot, progress, cycle);
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
