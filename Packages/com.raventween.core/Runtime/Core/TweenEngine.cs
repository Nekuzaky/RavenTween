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
    /// Central tween engine. Registers itself into the player loop once per domain and
    /// advances every live tween with zero steady-state allocation. Not thread-safe by
    /// design: all public entry points must be called from the main thread.
    /// </summary>
    static class TweenEngine {
        const int InitialCapacity = 64;

        static readonly List<TweenSlot> Slots = new List<TweenSlot>(InitialCapacity);
        static readonly Stack<int> FreeIndices = new Stack<int>(InitialCapacity);
        static bool _installed;
        static bool _processing;

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
            Install();
        }

        static void Install() {
            if (_installed) { return; }
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            InjectSystem(ref root, typeof(Update), OnUpdate);
            InjectSystem(ref root, typeof(PreLateUpdate), OnLateUpdate);
            PlayerLoop.SetPlayerLoop(root);
            _installed = true;
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
            // Guards against double-stepping if the system ever gets injected twice
            // (e.g. editor domain reload edge cases).
            if (Time.frameCount == _lastProcessedFrame) { return; }
            _lastProcessedFrame = Time.frameCount;
            Process(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>Advances every live root tween. Exposed internally for deterministic tests.</summary>
        public static void Process(float scaledDelta, float unscaledDelta) {
            Debug.Assert(scaledDelta >= 0f, "Delta time cannot be negative.");
            Debug.Assert(unscaledDelta >= 0f, "Unscaled delta time cannot be negative.");
            if (_processing) { return; }
            _processing = true;
            int count = Slots.Count; // Tweens created during callbacks start next frame.
            for (int i = 0; i < count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State != SlotState.Running || slot.OwnedBySequence) { continue; }
                float delta = (slot.UseUnscaledTime ? unscaledDelta : scaledDelta) * TimeScale;
                StepRoot(slot, i, delta);
            }
            _processing = false;
        }

        static void StepRoot(TweenSlot slot, int index, float delta) {
            if (slot.RequiresTarget && slot.UnityTarget == null) {
                HandleTargetDestroyed(slot, index);
                return;
            }
            slot.Elapsed += delta;
            float local = slot.Elapsed - slot.StartDelay;
            if (local < 0f) { return; }
            EnsureStarted(slot);
            if (slot.IsSequence) { StepSequence(slot, index, local); }
            else { StepTween(slot, index, local); }
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
            }
        }

        static void StepTween(TweenSlot slot, int index, float local) {
            float duration = Mathf.Max(slot.Duration, 0f);
            while (true) {
                float progress = duration <= 0f ? 1f : local / duration;
                if (progress < 1f) {
                    ApplyProgress(slot, progress, slot.CyclesDone);
                    return;
                }
                slot.CyclesDone++;
                bool finished = slot.Cycles >= 0 && slot.CyclesDone >= slot.Cycles;
                if (finished) {
                    ApplyProgress(slot, 1f, slot.CyclesDone - 1);
                    CompleteAndRelease(slot, index);
                    return;
                }
                if (duration <= 0f) { return; } // Zero-length infinite tween: nothing more to do.
                local -= duration;
                slot.Elapsed -= duration;
            }
        }

        static void ApplyProgress(TweenSlot slot, float cycleProgress, int cycleIndex) {
            float t = cycleProgress;
            if (slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1) { t = 1f - t; }
            float eased = EvaluateEase(slot, t);
            TweenValue value = TweenValue.Lerp(slot.StartValue, slot.EndValue, eased);
            WriteValue(slot, value);
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
            if (slot.OnUpdateFloat != null) { InvokeSafe(slot.OnUpdateFloat, value.Float); }
            if (slot.OnUpdateValue != null) { InvokeSafe(slot.OnUpdateValue, value); }
            InvokeSafe(slot.OnUpdate);
        }

        // ----- Sequences -----

        static void StepSequence(TweenSlot slot, int index, float local) {
            float duration = Mathf.Max(slot.SequenceDuration, 0f);
            while (true) {
                float progress = duration <= 0f ? 1f : local / duration;
                if (progress < 1f) {
                    ApplySequenceTime(slot, progress * duration, slot.CyclesDone);
                    return;
                }
                slot.CyclesDone++;
                bool finished = slot.Cycles >= 0 && slot.CyclesDone >= slot.Cycles;
                if (finished) {
                    ApplySequenceTime(slot, duration, slot.CyclesDone - 1);
                    CompleteAndRelease(slot, index);
                    return;
                }
                if (duration <= 0f) { return; }
                // Close out the finished cycle so children reach their end state and
                // fire their per-cycle callbacks before everything rewinds.
                ApplySequenceTime(slot, duration, slot.CyclesDone - 1);
                local -= duration;
                slot.Elapsed -= duration;
                ResetSequenceChildren(slot);
            }
        }

        static readonly Stack<TweenSlot> ResetWork = new Stack<TweenSlot>(16);

        // Iterative deep reset so nested sequences replay cleanly on every cycle wrap.
        static void ResetSequenceChildren(TweenSlot slot) {
            Debug.Assert(slot.IsSequence, "Only sequences carry child items.");
            Debug.Assert(ResetWork.Count == 0, "Reset work stack must start empty.");
            ResetWork.Push(slot);
            while (ResetWork.Count > 0) {
                TweenSlot current = ResetWork.Pop();
                if (current.Items == null) { continue; }
                for (int i = 0; i < current.Items.Count; i++) {
                    SequenceItem item = current.Items[i];
                    if (!TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                    child.StartFired = false;
                    child.CompleteNotified = false;
                    child.CyclesDone = 0;
                    if (child.IsSequence) { ResetWork.Push(child); }
                }
            }
        }

        static void ApplySequenceTime(TweenSlot slot, float time, int cycleIndex) {
            Debug.Assert(slot.IsSequence, "ApplySequenceTime requires a sequence slot.");
            Debug.Assert(slot.Items != null, "Sequence slot is missing its item list.");
            float t = time;
            if (slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1) {
                t = slot.SequenceDuration - t;
            }
            for (int i = 0; i < slot.Items.Count; i++) {
                SequenceItem item = slot.Items[i];
                if (!TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                if (child.RequiresTarget && child.UnityTarget == null) {
                    HandleTargetDestroyed(child, item.ChildIndex);
                    continue;
                }
                float childLocal = Mathf.Clamp(t - item.StartTime, 0f, item.Duration);
                EvaluateChildAtTime(child, childLocal);
            }
        }

        const int MaxSequenceDepth = 8;
        static int _sequenceDepth;

        static void EvaluateChildAtTime(TweenSlot child, float childLocal) {
            float local = childLocal - child.StartDelay;
            if (local < 0f) { return; }
            EnsureStarted(child);
            if (child.IsSequence) { EvaluateSequenceChild(child, local); return; }
            float duration = Mathf.Max(child.Duration, 0f);
            float cycleLength = duration <= 0f ? 0f : duration;
            if (cycleLength <= 0f) {
                child.CyclesDone = Mathf.Max(child.Cycles, 1);
                ApplyProgress(child, 1f, child.CyclesDone - 1);
                NotifyChildComplete(child);
                return;
            }
            int totalCycles = Mathf.Max(child.Cycles, 1);
            int cycleIndex = Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = local - cycleIndex * cycleLength;
            float progress = Mathf.Clamp01(inCycle / cycleLength);
            bool atEnd = local >= cycleLength * totalCycles;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            ApplyProgress(child, atEnd ? 1f : progress, atEnd ? totalCycles - 1 : cycleIndex);
            if (atEnd) { NotifyChildComplete(child); }
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
            int totalCycles = Mathf.Max(child.Cycles, 1);
            int cycleIndex = Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = local - cycleIndex * cycleLength;
            bool atEnd = local >= cycleLength * totalCycles;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            _sequenceDepth++;
            ApplySequenceTime(child, atEnd ? cycleLength : inCycle, atEnd ? totalCycles - 1 : cycleIndex);
            _sequenceDepth--;
            if (atEnd) { NotifyChildComplete(child); }
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

        static void HandleTargetDestroyed(TweenSlot slot, int index) {
            InvokeSafe(slot.OnTargetDestroyed);
            KillInternal(slot, index, false);
        }

        static void CompleteAndRelease(TweenSlot slot, int index) {
            InvokeSafe(slot.OnComplete);
            Action continuations = slot.AwaitContinuations;
            slot.AwaitContinuations = null;
            Release(slot, index);
            InvokeSafe(continuations);
        }

        /// <summary>Stops a tween. Optionally jumps to the final value and fires OnComplete.</summary>
        public static void Kill(int index, uint version, bool complete) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            if (complete) {
                ForceComplete(slot, index);
            } else {
                KillInternal(slot, index, true);
            }
        }

        static void ForceComplete(TweenSlot slot, int index) {
            bool targetAlive = !slot.RequiresTarget || slot.UnityTarget != null;
            if (targetAlive) {
                EnsureStarted(slot);
                int lastCycle = slot.Cycles > 0 ? slot.Cycles - 1 : Mathf.Max(slot.CyclesDone, 0);
                slot.CyclesDone = lastCycle + 1;
                if (slot.IsSequence) { ApplySequenceTime(slot, slot.SequenceDuration, lastCycle); }
                else { ApplyProgress(slot, 1f, lastCycle); }
            }
            CompleteAndRelease(slot, index);
        }

        static void KillInternal(TweenSlot slot, int index, bool fireOnKill) {
            if (fireOnKill) { InvokeSafe(slot.OnKill); }
            Action continuations = slot.AwaitContinuations;
            slot.AwaitContinuations = null;
            Release(slot, index);
            InvokeSafe(continuations);
        }

        static readonly Stack<int> ReleaseWork = new Stack<int>(16);

        // Iterative release: nested sequences are drained through an explicit work stack.
        static void Release(TweenSlot slot, int index) {
            Debug.Assert(slot.State != SlotState.Free, "Slot is already free.");
            Debug.Assert(ReleaseWork.Count == 0, "Release work stack must start empty.");
            ReleaseWork.Push(index);
            while (ReleaseWork.Count > 0) {
                int current = ReleaseWork.Pop();
                TweenSlot currentSlot = Slots[current];
                if (currentSlot.State == SlotState.Free) { continue; }
                if (currentSlot.IsSequence && currentSlot.Items != null) {
                    for (int i = 0; i < currentSlot.Items.Count; i++) {
                        SequenceItem item = currentSlot.Items[i];
                        if (TryGetSlotInternal(item.ChildIndex, item.ChildVersion, out _)) {
                            ReleaseWork.Push(item.ChildIndex);
                        }
                    }
                    currentSlot.Items.Clear();
                }
                currentSlot.State = SlotState.Free;
                currentSlot.Version++;
                FreeIndices.Push(current);
            }
        }

        /// <summary>Pauses or resumes a tween. Paused tweens keep their slot alive.</summary>
        public static void SetPaused(int index, uint version, bool paused) {
            if (!TryGetSlotInternal(index, version, out TweenSlot slot)) { return; }
            slot.State = paused ? SlotState.Paused : SlotState.Running;
        }

        /// <summary>Stops every live tween, optionally completing them first.</summary>
        public static void KillAll(bool complete) {
            for (int i = 0; i < Slots.Count; i++) {
                TweenSlot slot = Slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence) { continue; }
                Kill(i, slot.Version, complete);
            }
        }

        static void InvokeSafe(Action callback) {
            if (callback == null) { return; }
            try { callback(); }
            catch (Exception exception) { Debug.LogException(exception); }
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
