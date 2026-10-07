using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Handle to a live sequence. A sequence owns its child tweens and drives them on a
    /// shared timeline through <see cref="Chain"/>, <see cref="Group"/> and <see cref="Insert"/>.
    /// </summary>
    public readonly struct Sequence : IEquatable<Sequence> {
        internal readonly int Index;
        internal readonly uint Version;

        internal Sequence(int index, uint version) {
            Index = index;
            Version = version;
        }

        /// <summary>True while the sequence is running or paused.</summary>
        public bool IsAlive {
            get { return TweenEngine.TryGetSlot(Index, Version, out _); }
        }

        /// <summary>Total length of one sequence cycle, in seconds.</summary>
        public float Duration {
            get { return TweenEngine.TryGetSlot(Index, Version, out TweenSlot slot) ? slot.SequenceDuration : 0f; }
        }

        // ----- Building -----

        /// <summary>Appends a tween after everything already in the sequence.</summary>
        public Sequence Chain(Tween tween) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.ChainCursor, tween.Index, tween.Version);
            }
            return this;
        }

        /// <summary>Plays a tween alongside the previously added item.</summary>
        public Sequence Group(Tween tween) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.LastInsertTime, tween.Index, tween.Version);
            }
            return this;
        }

        /// <summary>Places a tween at an absolute time on the sequence timeline.</summary>
        public Sequence Insert(float time, Tween tween) {
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, Mathf.Max(time, 0f), tween.Index, tween.Version);
            }
            return this;
        }

        /// <summary>Appends a nested sequence after everything already in this sequence.</summary>
        public Sequence Chain(Sequence sequence) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.ChainCursor, sequence.Index, sequence.Version);
            }
            return this;
        }

        /// <summary>Plays a nested sequence alongside the previously added item.</summary>
        public Sequence Group(Sequence sequence) {
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, slot.LastInsertTime, sequence.Index, sequence.Version);
            }
            return this;
        }

        /// <summary>Places a nested sequence at an absolute time on the timeline.</summary>
        public Sequence Insert(float time, Sequence sequence) {
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                AddItem(slot, Mathf.Max(time, 0f), sequence.Index, sequence.Version);
            }
            return this;
        }

        /// <summary>Appends a pause of <paramref name="seconds"/> after the current chain end.</summary>
        public Sequence ChainDelay(float seconds) {
            Debug.Assert(seconds >= 0f, "Chain delay cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) {
                slot.LastInsertTime = slot.ChainCursor;
                slot.ChainCursor += Mathf.Max(seconds, 0f);
                if (slot.ChainCursor > slot.SequenceDuration) { slot.SequenceDuration = slot.ChainCursor; }
            }
            return this;
        }

        /// <summary>Runs <paramref name="callback"/> when the timeline reaches the current chain end.</summary>
        public Sequence ChainCallback(Action callback) {
            Debug.Assert(callback != null, "Chain callback cannot be null.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, slot.ChainCursor, callback, null, null, null);
            }
            return this;
        }

        /// <summary>Runs <paramref name="callback"/> when the timeline reaches <paramref name="time"/> seconds.</summary>
        public Sequence InsertCallback(float time, Action callback) {
            Debug.Assert(callback != null, "Insert callback cannot be null.");
            Debug.Assert(time >= 0f, "Insert time cannot be negative.");
            if (callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, Mathf.Max(time, 0f), callback, null, null, null);
            }
            return this;
        }

        /// <summary>
        /// Allocation-free <see cref="ChainCallback(Action)"/>: <paramref name="target"/> is passed
        /// back to a non-capturing lambda, e.g. <c>ChainCallback(this, self =&gt; self.Spawn())</c>.
        /// Skipped if the target is a destroyed Unity object.
        /// </summary>
        public Sequence ChainCallback<T>(T target, Action<T> callback) where T : class {
            Debug.Assert(target != null && callback != null, "Chain callback needs a target and a callback.");
            if (target != null && callback != null && TryGetBuildable(out TweenSlot slot)) {
                AddCallback(slot, slot.ChainCursor, null, target, callback, TargetCallbacks<T>.Complete);
            }
            return this;
        }

        /// <summary>Allocation-free <see cref="InsertCallback(float, Action)"/>; see <see cref="ChainCallback{T}"/>.</summary>
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
            int index = TweenEngine.Rent(out TweenSlot holder);
            holder.IsCallback = true;
            holder.OwnedBySequence = true;
            holder.OnComplete = callback;
            holder.CompleteTarget = target;
            holder.CompleteDelegate = targetCallback;
            holder.CompleteInvoker = invoker;
            if (sequenceSlot.Items == null) { sequenceSlot.Items = new List<SequenceItem>(4); }
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
            if (sequenceSlot.Items == null) { sequenceSlot.Items = new List<SequenceItem>(4); }
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
            // A trailing ChainDelay can extend the timeline beyond the last item.
            if (sequenceSlot.SequenceDuration < sequenceSlot.ChainCursor) {
                sequenceSlot.SequenceDuration = sequenceSlot.ChainCursor;
            }
        }

        // The engine evaluates items in the order they start writing (rewinds in reverse), so the
        // list is kept sorted by ActiveStart — a child's own delay counts. Equal times keep
        // insertion order: the later-added item wins the property.
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

        // ----- Configuration -----

        /// <summary>
        /// Repeats the whole sequence. Use -1 for an infinite loop. Sequences support Restart and
        /// Yoyo; PingPong plays as Yoyo and Incremental as Restart.
        /// </summary>
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

        /// <summary>Loops the sequence forever.</summary>
        public Sequence Infinite(CycleMode mode = CycleMode.Restart) { return Cycles(-1, mode); }

        /// <summary>Delays the start of the whole sequence.</summary>
        public Sequence Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay cannot be negative.");
            if (TryGetBuildable(out TweenSlot slot)) { slot.StartDelay = Mathf.Max(seconds, 0f); }
            return this;
        }

        /// <summary>Uses unscaled time so the sequence ignores Time.timeScale.</summary>
        public Sequence UnscaledTime(bool unscaled = true) {
            if (TryGetBuildable(out TweenSlot slot)) { slot.UseUnscaledTime = unscaled; }
            return this;
        }

        /// <summary>Runs the sequence in another player loop phase (FixedUpdate for physics). Children follow it.</summary>
        public Sequence UpdateIn(UpdatePhase phase) {
            HandleState.SetPhase(Index, Version, phase);
            return this;
        }

        /// <summary>Stops the sequence (firing OnKill) as soon as <paramref name="token"/> is cancelled.</summary>
        public Sequence WithCancellation(CancellationToken token) {
            HandleState.SetCancellation(Index, Version, token);
            return this;
        }

        // ----- State -----

        /// <summary>True when the sequence is alive and paused.</summary>
        public bool IsPaused { get { return HandleState.IsPaused(Index, Version); } }

        /// <summary>Delay + every cycle, in seconds. Infinity when the sequence loops forever.</summary>
        public float DurationTotal { get { return HandleState.DurationTotal(Index, Version); } }

        /// <summary>Cycles completed so far.</summary>
        public int CyclesDone { get { return HandleState.CyclesDone(Index, Version); } }

        /// <summary>Number of cycles; -1 when infinite.</summary>
        public int CyclesTotal { get { return HandleState.CyclesTotal(Index, Version); } }

        /// <summary>Seconds into the current cycle. Setting it jumps the whole timeline there.</summary>
        public float ElapsedTime {
            get { return HandleState.ElapsedTime(Index, Version); }
            set { HandleState.SetElapsedTime(Index, Version, value); }
        }

        /// <summary>Seconds since the sequence was created, delay included. Setting it jumps there (past the end completes it).</summary>
        public float ElapsedTimeTotal {
            get { return TweenEngine.ElapsedTotal(Index, Version); }
            set { TweenEngine.SetElapsedTotal(Index, Version, value); }
        }

        /// <summary>0–1 through the current cycle. Settable.</summary>
        public float Progress {
            get { return HandleState.Progress(Index, Version); }
            set { HandleState.SetProgress(Index, Version, value); }
        }

        /// <summary>0–1 through the whole sequence (0 when infinite). Settable.</summary>
        public float ProgressTotal {
            get { return HandleState.ProgressTotal(Index, Version); }
            set { HandleState.SetProgressTotal(Index, Version, value); }
        }

        /// <summary>Speed multiplier for this sequence only (1 = normal, 0 = frozen). Tween it with Raven.TweenTimeScale.</summary>
        public float TimeScale {
            get { return HandleState.TimeScale(Index, Version); }
            set { HandleState.SetTimeScale(Index, Version, value); }
        }

        /// <summary>Changes how many cycles remain, the current one included (1 = this is the last, -1 = forever).</summary>
        public Sequence SetRemainingCycles(int cycles) {
            TweenEngine.SetRemainingCycles(Index, Version, cycles);
            return this;
        }

        /// <summary>For Yoyo loops: completes the next time the timeline reaches its end (true) or its start (false).</summary>
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

        /// <summary>
        /// Allocation-free OnComplete: <paramref name="target"/> is passed back to a non-capturing
        /// lambda. Skipped if the target is a destroyed Unity object. One per sequence.
        /// </summary>
        public Sequence OnComplete<T>(T target, Action<T> callback) where T : class {
            HandleState.SetOnComplete(Index, Version, target, callback);
            return this;
        }

        // ----- Control -----

        /// <summary>Sequences play automatically; Start() documents intent and resumes a paused sequence.</summary>
        public Sequence Start() {
            TweenEngine.SetPaused(Index, Version, false);
            return this;
        }

        /// <summary>Stops the sequence and its children where they are and fires OnKill.</summary>
        public void Stop() { TweenEngine.Kill(Index, Version, false); }

        /// <summary>Jumps everything to its final value and fires OnComplete.</summary>
        public void Complete() { TweenEngine.Kill(Index, Version, true); }

        public void Pause() { TweenEngine.SetPaused(Index, Version, true); }
        public void Resume() { TweenEngine.SetPaused(Index, Version, false); }

        // ----- Async & coroutines -----

        /// <summary>Awaits sequence completion (or kill). Usage: await sequence;</summary>
        public TweenAwaiter GetAwaiter() { return new TweenAwaiter(new Tween(Index, Version)); }

        /// <summary>Awaits sequence completion.</summary>
        public TweenAwaiter ToCompletion() { return GetAwaiter(); }

        /// <summary>Returns a coroutine instruction that waits until the sequence dies.</summary>
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
