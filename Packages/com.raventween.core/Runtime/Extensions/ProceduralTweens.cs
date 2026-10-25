using UnityEngine;

namespace RavenTween {
    public static class ProceduralTweens {
        public static Tween TweenWeight(this RavenLookAt lookAt, float to, float duration) {
            Debug.Assert(lookAt != null, "TweenWeight needs a live RavenLookAt.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (lookAt == null) { return default; }
            return Raven.CustomTo(lookAt, c => c.Weight, Mathf.Clamp01(to), duration, (c, w) => c.Weight = w);
        }

        public static Tween TweenWeight(this RavenSpringChain chain, float to, float duration) {
            Debug.Assert(chain != null, "TweenWeight needs a live RavenSpringChain.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (chain == null) { return default; }
            return Raven.CustomTo(chain, c => c.Weight, Mathf.Clamp01(to), duration, (c, w) => c.Weight = w);
        }

        public static Tween LookAtTarget(this RavenLookAt lookAt, Transform newTarget, float blendDuration) {
            Debug.Assert(lookAt != null, "LookAtTarget needs a live RavenLookAt.");
            Debug.Assert(blendDuration >= 0f, "Blend duration cannot be negative.");
            if (lookAt == null) { return default; }
            lookAt.Target = newTarget;
            return lookAt.TweenWeight(1f, blendDuration);
        }
    }
}
