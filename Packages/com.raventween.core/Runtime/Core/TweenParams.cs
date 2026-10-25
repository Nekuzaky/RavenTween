using System;
using UnityEngine;

namespace RavenTween {
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
