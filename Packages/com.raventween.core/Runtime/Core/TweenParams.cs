using System;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Serializable tween settings. Expose a field of this type in any MonoBehaviour to let
    /// designers tune duration, easing and cycles without recompiling, then apply it with
    /// <see cref="ApplyTo"/>.
    /// </summary>
    [Serializable]
    public struct TweenParams {
        [Min(0f)] public float duration;
        [Min(0f)] public float startDelay;
        public Ease ease;
        [Tooltip("Used when Ease is set to Custom.")]
        public AnimationCurve customCurve;
        [Tooltip("-1 loops forever.")]
        public int cycles;
        public CycleMode cycleMode;
        public bool useUnscaledTime;

        /// <summary>Sensible defaults: 0.3s, OutQuad, single cycle.</summary>
        public static TweenParams Default {
            get {
                return new TweenParams {
                    duration = 0.3f,
                    startDelay = 0f,
                    ease = Ease.OutQuad,
                    customCurve = null,
                    cycles = 1,
                    cycleMode = CycleMode.Restart,
                    useUnscaledTime = false
                };
            }
        }

        /// <summary>
        /// Applies delay, easing, cycles and time mode to a live tween and returns it.
        /// <see cref="duration"/> is not applied: a tween's duration is set when it is created,
        /// so pass it to the factory, e.g. <c>p.ApplyTo(Raven.Scale(t, 1.2f, p.duration))</c>.
        /// Call it before adding the tween to a sequence.
        /// </summary>
        public Tween ApplyTo(Tween tween) {
            Debug.Assert(duration >= 0f, "TweenParams duration cannot be negative.");
            Debug.Assert(cycles == -1 || cycles >= 0, "TweenParams cycles must be -1 or non-negative.");
            tween.Delay(startDelay);
            if (ease == Ease.Custom && customCurve != null) { tween.Ease(customCurve); }
            else if (ease != Ease.Custom) { tween.Ease(ease); }
            int cycleCount = cycles == 0 ? 1 : cycles;
            tween.Cycles(cycleCount, cycleMode);
            tween.UnscaledTime(useUnscaledTime);
            return tween;
        }
    }
}
