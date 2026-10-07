using System;
using System.Collections.Generic;
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
            if (child.Cycles < 0) {
                Debug.LogError("RavenTween: infinite tweens cannot be nested in a sequence; clamped to 1 cycle.");
                child.Cycles = 1;
            }
            child.OwnedBySequence = true;
            float childLength = ComputeChildLength(child);
            if (sequenceSlot.Items == null) { sequenceSlot.Items = new List<SequenceItem>(4); }
            sequenceSlot.Items.Add(new SequenceItem {
                StartTime = startTime,
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

        static float ComputeChildLength(TweenSlot child) {
            int cycles = Mathf.Max(child.Cycles, 1);
            float body = child.IsSequence ? child.SequenceDuration : child.Duration;
            return child.StartDelay + Mathf.Max(body, 0f) * cycles;
        }

        // ----- Configuration -----

        /// <summary>Repeats the whole sequence. Use -1 for an infinite loop.</summary>
        public Sequence Cycles(int count, CycleMode mode = CycleMode.Restart) {
            Debug.Assert(count == -1 || count >= 1, "Cycle count must be -1 (infinite) or at least 1.");
            if (TryGetBuildable(out TweenSlot slot)) {
                slot.Cycles = count < 0 ? -1 : Mathf.Max(count, 1);
                slot.Mode = mode;
            }
            return this;
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
