using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// Builds and plays a sequence from serialized steps without writing code.
    /// Each step chains, groups or inserts a template on a scene target.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Sequence Player")]
    public sealed class RavenSequencePlayer : MonoBehaviour {
        public enum StepMode : byte {
            /// <summary>Plays after everything before it.</summary>
            Chain = 0,
            /// <summary>Plays alongside the previous step.</summary>
            Group = 1,
            /// <summary>Plays at an absolute time on the sequence timeline.</summary>
            Insert = 2
        }

        [Serializable]
        public struct Step {
            public StepMode mode;
            [Tooltip("Timeline position in seconds, used only by Insert.")]
            [Min(0f)] public float insertTime;
            [Tooltip("Component or asset the template animates.")]
            public UnityEngine.Object target;
            public TweenTemplate template;
        }

        [SerializeField] List<Step> steps = new List<Step>();
        [SerializeField, Min(-1)] int cycles = 1;
        [SerializeField] CycleMode cycleMode = CycleMode.Restart;
        [SerializeField] bool useUnscaledTime;
        [SerializeField] bool playOnEnable;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        Sequence _sequence;

        /// <summary>Steps played by this component. Edit at runtime before calling Play.</summary>
        public List<Step> Steps { get { return steps; } }

        /// <summary>Raised when the built sequence completes.</summary>
        public UnityEvent OnComplete { get { return onComplete; } }

        /// <summary>Handle to the sequence of the current run; dead when nothing plays.</summary>
        public Sequence Current { get { return _sequence; } }

        void OnEnable() {
            if (playOnEnable) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        /// <summary>Stops any previous run, rebuilds the sequence from steps, and plays it.</summary>
        public void Play() {
            Stop();
            Sequence sequence = Raven.Sequence();
            int added = 0;
            for (int i = 0; i < steps.Count; i++) {
                added += TryAddStep(sequence, steps[i], i) ? 1 : 0;
            }
            if (added == 0) {
                sequence.Stop();
                onComplete.Invoke();
                return;
            }
            sequence.Cycles(cycles == 0 ? 1 : cycles, cycleMode);
            sequence.UnscaledTime(useUnscaledTime);
            sequence.OnComplete(onComplete.Invoke);
            _sequence = sequence;
        }

        bool TryAddStep(Sequence sequence, Step step, int index) {
            if (step.template == null || step.target == null) {
                Debug.LogWarning("RavenTween: sequence step " + index + " is incomplete and was skipped.", this);
                return false;
            }
            Tween tween = step.template.Play(step.target);
            if (!tween.IsAlive) { return false; }
            switch (step.mode) {
                case StepMode.Chain: sequence.Chain(tween); break;
                case StepMode.Group: sequence.Group(tween); break;
                case StepMode.Insert: sequence.Insert(step.insertTime, tween); break;
                default: sequence.Chain(tween); break;
            }
            return true;
        }

        /// <summary>Stops the running sequence, if any.</summary>
        public void Stop() {
            if (_sequence.IsAlive) { _sequence.Stop(); }
            _sequence = default;
        }

        /// <summary>Jumps the running sequence to its end.</summary>
        public void CompleteNow() {
            if (_sequence.IsAlive) { _sequence.Complete(); }
            _sequence = default;
        }
    }
}
