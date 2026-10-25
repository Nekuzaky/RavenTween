using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    public readonly struct Sequence : IEquatable<Sequence> {
        internal readonly int Index;
        internal readonly uint Version;

        internal Sequence(int index, uint version) {
            Index = index;
            Version = version;
        }

        public bool IsAlive {
            get { return TweenEngine.TryGetSlot(Index, Version, out _); }
        }

        public float Duration {
            get { return TweenEngine.TryGetSlot(Index, Version, out TweenSlot slot) ? slot.SequenceDuration : 0f; }
        }

        public Sequence Chain(Tween tween) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.ChainCursor, tween.Index, tween.Version);
            }
            return this;
        }

        public Sequence Group(Tween tween) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.LastInsertTime, tween.Index, tween.Version);
            }
            return this;
        }

        public Sequence Insert(float time, Tween tween) {
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, Mathf.Max(time, 0f), tween.Index, tween.Version);
            }
            return this;
        }

        public Sequence Chain(Sequence sequence) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.ChainCursor, sequence.Index, sequence.Version);
            }
            return this;
        }

        public Sequence Group(Sequence sequence) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.LastInsertTime, sequence.Index, sequence.Version);
            }
            return this;
        }

        public Sequence Insert(float time, Sequence sequence) {
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, Mathf.Max(time, 0f), sequence.Index, sequence.Version);
            }
            return this;
        }

        public Sequence ChainDelay(float seconds) {
            Debug.Assert(seconds >= 0f, "Chain delay cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                slot.LastInsertTime = slot.ChainCursor;
                slot.ChainCursor += Mathf.Max(seconds, 0f);
                if (slot.ChainCursor > slot.SequenceDuration) { slot.SequenceDuration = slot.ChainCursor; }
            }
            return this;
        }

        public Sequence ChainCallback(Action callback) {
            Debug.Assert(callback != null, "Chain callback cannot be null.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, slot.ChainCursor, callback, null, null, null);
            }
            return this;
        }

        public Sequence InsertCallback(float time, Action callback) {
            Debug.Assert(callback != null, "Insert callback cannot be null.");
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, Mathf.Max(time, 0f), callback, null, null, null);
            }
            return this;
        }

        public Sequence ChainCallback<T>(T target, Action<T> callback) where T : class {
            Debug.Assert(target != null && callback != null, "Chain callback needs a target and a callback.");
            if (target != null && callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, slot.ChainCursor, null, target, callback, TargetCallbacks<T>.Complete);
            }
            return this;
        }

        public Sequence InsertCallback<T>(float time, T target, Action<T> callback) where T : class {
            Debug.Assert(target != null && callback != null, "Insert callback needs a target and a callback.");
            if (target != null && callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, Mathf.Max(time, 0f), null, target, callback, TargetCallbacks<T>.Complete);
            }
            return this;
        }

        static void AddCallback(TweenSlot sequenceSlot, float time, Action callback, object target,
                                Delegate targetCallback, Action<object, Delegate> invoker) {
            if (sequenceSlot.OwnedBySequence) {
                Debug.LogError("RavenTween: this sequence is already nested in another one; build it completely before adding it.");
                return;
            }
            int index = TweenEngine.RentCallback(out TweenSlot holder);
            holder.OwnedBySequence = true;
            holder.OnComplete = callback;
            holder.CompleteTarget = target;
            holder.CompleteDelegate = targetCallback;
            holder.CompleteInvoker = invoker;
            InsertSorted(sequenceSlot.Items, new SequenceItem {
                StartTime = time, ActiveStart = time, Duration = 0f, ChildIndex = index, ChildVersion = holder.Version
            });
            sequenceSlot.HasCallbacks = true;
            sequenceSlot.LastInsertTime = time;
            if (time > sequenceSlot.SequenceDuration) { sequenceSlot.SequenceDuration = time; }
        }

        static void AddItem(TweenSlot sequenceSlot, float startTime, int childIndex, uint childVersion) {
            Debug.Assert(sequenceSlot.IsSequence, "AddItem requires a sequence slot.");
            if (!TweenEngine.TryGetSlot(childIndex, childVersion, out TweenSlot child)) {
                Debug.LogError("RavenTween: cannot add a dead tween to a sequence.");
                return;
            }
            if (child.OwnedBySequence) {
                Debug.LogError("RavenTween: this tween already belongs to a sequence.");
                return;
            }
            if (sequenceSlot.OwnedBySequence) {
                Debug.LogError("RavenTween: this sequence is already nested in another one; build it completely before adding it.");
                return;
            }
            if (child.IsSequence && TweenEngine.IsInSubtree(child, sequenceSlot)) {
                Debug.LogError("RavenTween: a sequence cannot contain itself.");
                return;
            }
            if (child.Cycles < 0) {
                Debug.LogError("RavenTween: infinite tweens cannot be nested in a sequence; clamped to 1 cycle.");
                child.Cycles = 1;
            }
            child.OwnedBySequence = true;
            float childLength = ComputeChildLength(child);
            InsertSorted(sequenceSlot.Items, new SequenceItem {
                StartTime = startTime,
                ActiveStart = startTime + child.StartDelay,
                Duration = childLength,
                ChildIndex = childIndex,
                ChildVersion = childVersion
            });
            sequenceSlot.LastInsertTime = startTime;
            float end = startTime + childLength;
            if (end > sequenceSlot.ChainCursor) { sequenceSlot.ChainCursor = end; }
            TweenEngine.RecalculateSequenceDuration(sequenceSlot);
            if (sequenceSlot.SequenceDuration < sequenceSlot.ChainCursor) {
                sequenceSlot.SequenceDuration = sequenceSlot.ChainCursor;
            }
        }

        static void InsertSorted(List<SequenceItem> items, SequenceItem item) {
            int at = items.Count;
            while (at > 0 && items[at - 1].ActiveStart > item.ActiveStart) { at--; }
            items.Insert(at, item);
        }

        static float ComputeChildLength(TweenSlot child) {
            int cycles = Mathf.Max(child.Cycles, 1);
            float body = child.IsSequence ? child.SequenceDuration : child.Duration;
            return child.StartDelay + Mathf.Max(body, 0f) * cycles;
        }

        public Sequence Cycles(int count, CycleMode mode = CycleMode.Restart) {
            Debug.Assert(count == -1 || count >= 1, "Cycle count must be -1 (infinite) or at least 1.");
            if (TryGetBuildable(out TweenSlot slot)) {
                slot.Cycles = count < 0 ? -1 : Mathf.Max(count, 1);
                slot.Mode = SequenceMode(mode);
            }
            return this;
        }

        static CycleMode SequenceMode(CycleMode mode) {
            if (mode == CycleMode.PingPong) { return CycleMode.Yoyo; }
            if (mode == CycleMode.Incremental) {
                Debug.LogWarning("RavenTween: sequences cannot cycle in Incremental mode; Restart is used.");
                return CycleMode.Restart;
            }
            return mode;
        }

        public Sequence Infinite(CycleMode mode = CycleMode.Restart) { return Cycles(-1, mode); }

        public Sequence Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) { slot.StartDelay = Mathf.Max(seconds, 0f); }
            return this;
        }

        public Sequence UnscaledTime(bool unscaled = true) {
            if (TryGetBuildable(out TweenSlot slot)) { slot.UseUnscaledTime = unscaled; }
            return this;
        }

        public Sequence UpdateIn(UpdatePhase phase) {
            HandleState.SetPhase(Index, Version, phase);
            return this;
        }

        public Sequence WithCancellation(CancellationToken token) {
            HandleState.SetCancellation(Index, Version, token);
            return this;
        }

        public bool IsPaused { get { return HandleState.IsPaused(Index, Version); } }

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

        public float TimeScale {
            get { return HandleState.TimeScale(Index, Version); }
            set { HandleState.SetTimeScale(Index, Version, value); }
        }

        public Sequence SetRemainingCycles(int cycles) {
            TweenEngine.SetRemainingCycles(Index, Version, cycles);
            return this;
        }

        public Sequence SetRemainingCycles(bool stopAtEnd) {
            TweenEngine.SetRemainingCycles(Index, Version, stopAtEnd);
            return this;
        }

        public Sequence OnStart(Action callback) {
            Debug.Assert(callback != null, "OnStart callback cannot be null.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) { slot.OnStart += callback; }
            return this;
        }

        public Sequence OnComplete(Action callback) {
            Debug.Assert(callback != null, "OnComplete callback cannot be null.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) { slot.OnComplete += callback; }
            return this;
        }

        public Sequence OnKill(Action callback) {
            Debug.Assert(callback != null, "OnKill callback cannot be null.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) { slot.OnKill += callback; }
            return this;
        }

        public Sequence OnComplete<T>(T target, Action<T> callback) where T : class {
            HandleState.SetOnComplete(Index, Version, target, callback);
            return this;
        }

        public Sequence Start() {
            TweenEngine.SetPaused(Index, Version, false);
            return this;
        }

        public void Stop() { TweenEngine.Kill(Index, Version, false); }

        public void Complete() { TweenEngine.Kill(Index, Version, true); }

        public void Pause() { TweenEngine.SetPaused(Index, Version, true); }
        public void Resume() { TweenEngine.SetPaused(Index, Version, false); }

        public TweenAwaiter GetAwaiter() { return new TweenAwaiter(new Tween(Index, Version)); }

        public TweenAwaiter ToCompletion() { return GetAwaiter(); }

        public CustomYieldInstruction ToYieldInstruction() {
            return new TweenYieldInstruction(new Tween(Index, Version));
        }

        bool TryGetBuildable(out TweenSlot slot) {
            bool alive = TweenEngine.TryGetSlot(Index, Version, out slot);
            Debug.Assert(alive, "Configuring a dead sequence handle has no effect.");
            Debug.Assert(!alive || slot.IsSequence, "Handle does not reference a sequence.");
            return alive && slot.IsSequence;
        }

        public bool Equals(Sequence other) { return Index == other.Index && Version == other.Version; }
        public override bool Equals(object obj) { return obj is Sequence other && Equals(other); }
        public override int GetHashCode() { return (Index * 397) ^ (int)Version; }
    }
}
