using System.Collections.Generic;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Places Raven Sequence Player steps on a timeline with exactly the rules the runtime
    /// Sequence uses (Chain after everything, Group with the previous item, Insert at a time).
    /// Pure; covered by tests that compare it with real sequences.
    /// </summary>
    static class SequenceLayout {
        public struct Block {
            public float Start;
            public float Length;
            public bool Valid;
            public bool LoopClamped;
        }

        /// <summary>Length of one step on the timeline; 0 for incomplete steps.</summary>
        public static float StepLength(TweenTemplate template, out bool loopClamped) {
            loopClamped = false;
            if (template == null) { return 0f; }
            TweenParams s = template.settings;
            int cycles = s.cycles == 0 ? 1 : s.cycles;
            if (cycles < 0) {
                loopClamped = true;
                cycles = template.CyclesInsideSequence;
            }
            return Mathf.Max(s.startDelay, 0f) + Mathf.Max(s.duration, 0f) * cycles;
        }

        /// <summary>Fills <paramref name="blocks"/> (one per step) and returns the total duration.</summary>
        public static float Compute(IList<RavenSequencePlayer.Step> steps, List<Block> blocks) {
            Debug.Assert(steps != null, "Layout needs a step list.");
            Debug.Assert(blocks != null, "Layout needs an output list.");
            blocks.Clear();
            float cursor = 0f, lastStart = 0f, total = 0f;
            for (int i = 0; i < steps.Count; i++) {
                RavenSequencePlayer.Step step = steps[i];
                bool valid = step.template != null &&
                             PropertyAccessor.ResolveTarget(step.target, step.template.property, false) != null;
                float length = StepLength(step.template, out bool clamped);
                float start = PlaceStart(step, cursor, lastStart);
                blocks.Add(new Block { Start = start, Length = length, Valid = valid, LoopClamped = clamped });
                if (!valid) { continue; } // The runtime skips incomplete steps entirely.
                lastStart = start;
                float end = start + length;
                if (end > cursor) { cursor = end; }
                if (end > total) { total = end; }
            }
            return total;
        }

        static float PlaceStart(RavenSequencePlayer.Step step, float cursor, float lastStart) {
            switch (step.mode) {
                case RavenSequencePlayer.StepMode.Group: return lastStart;
                case RavenSequencePlayer.StepMode.Insert: return Mathf.Max(step.insertTime, 0f);
                default: return cursor;
            }
        }
    }
}
