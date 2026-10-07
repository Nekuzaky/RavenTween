using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Weight tweens for the procedural components. Blending a look-at or a spring chain in and
    /// out over time is what turns them from always-on effects into animation. Every tween
    /// starts from the weight the component has <b>when the tween starts</b>, so they can be
    /// chained in sequences or delayed.
    /// </summary>
    public static class ProceduralTweens {
        /// <summary>Blends the look-at in (1) or out (0) from its current weight.</summary>
        public static Tween TweenWeight(this RavenLookAt lookAt, float to, float duration) {
            Debug.Assert(lookAt != null, "TweenWeight needs a live RavenLookAt.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (lookAt == null) { return default; }
            return Raven.CustomTo(lookAt, c => c.Weight, Mathf.Clamp01(to), duration, (c, w) => c.Weight = w);
        }

        /// <summary>Blends the spring motion in (1) or out (0) from its current weight.</summary>
        public static Tween TweenWeight(this RavenSpringChain chain, float to, float duration) {
            Debug.Assert(chain != null, "TweenWeight needs a live RavenSpringChain.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (chain == null) { return default; }
            return Raven.CustomTo(chain, c => c.Weight, Mathf.Clamp01(to), duration, (c, w) => c.Weight = w);
        }

        /// <summary>
        /// Switches the look-at to a new target and blends it in. With a weight already at 1 the
        /// smoothing handles the turn; from 0 the head eases from its animated pose.
        /// </summary>
        public static Tween LookAtTarget(this RavenLookAt lookAt, Transform newTarget, float blendDuration) {
            Debug.Assert(lookAt != null, "LookAtTarget needs a live RavenLookAt.");
            Debug.Assert(blendDuration >= 0f, "Blend duration cannot be negative.");
            if (lookAt == null) { return default; }
            lookAt.Target = newTarget;
            return lookAt.TweenWeight(1f, blendDuration);
        }
    }
}
