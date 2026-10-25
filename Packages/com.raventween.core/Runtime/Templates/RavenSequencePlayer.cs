using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    [AddComponentMenu("RavenTween/Raven Sequence Player")]
    public sealed class RavenSequencePlayer : MonoBehaviour {
        public enum StepMode : byte {
            Chain = 0,
            Group = 1,
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

        public List<Step> Steps { get { return steps; } }

        public UnityEvent OnComplete { get { return onComplete; } }

        public Sequence Current { get { return _sequence; } }

        void OnEnable() {
            if (playOnEnable) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        public void Play() {
            Stop();
            Sequence sequence = BuildSequence();
            if (!sequence.IsAlive) {
                onComplete.Invoke();
                return;
            }
            sequence.Cycles(cycles == 0 ? 1 : cycles, cycleMode);
            sequence.UnscaledTime(useUnscaledTime);
            sequence.OnComplete(onComplete.Invoke);
            _sequence = sequence;
        }

        internal Sequence BuildSequence() {
            Sequence sequence = Raven.Sequence();
            int added = 0;
            for (int i = 0; i < steps.Count; i++) {
                added += TryAddStep(sequence, steps[i], i) ? 1 : 0;
            }
            if (added > 0) { return sequence; }
            sequence.Stop();
            return default;
        }

        bool TryAddStep(Sequence sequence, Step step, int index) {
            if (step.template == null || step.target == null) {
                if (Application.isPlaying) {
                    Debug.LogWarning("RavenTween: sequence step " + index + " is incomplete and was skipped.", this);
                }
                return false;
            }
            Tween tween = step.template.Play(step.target);
            if (!tween.IsAlive) { return false; }
            if (step.template.LoopsForever) {
                tween.Cycles(step.template.CyclesInsideSequence, step.template.settings.cycleMode);
            }
            switch (step.mode) {
                case StepMode.Chain: sequence.Chain(tween); break;
                case StepMode.Group: sequence.Group(tween); break;
                case StepMode.Insert: sequence.Insert(step.insertTime, tween); break;
                default: sequence.Chain(tween); break;
            }
            return true;
        }

        public void Stop() {
            Sequence running = _sequence;
            _sequence = default;
            if (running.IsAlive) { running.Stop(); }
        }

        public void CompleteNow() {
            Sequence running = _sequence;
            _sequence = default;
            if (running.IsAlive) { running.Complete(); }
        }
    }
}
