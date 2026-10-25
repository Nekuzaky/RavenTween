using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RavenTween {
    public static class AnimationRiggingTweens {
        public static Tween TweenWeight<T>(this T constraint, float to, float duration)
            where T : MonoBehaviour, IRigConstraint {
            Debug.Assert(constraint != null, "TweenWeight needs a live rig constraint.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (constraint == null) { return default; }
            return Raven.CustomTo(constraint, c => c.weight, Mathf.Clamp01(to), duration, (c, w) => c.weight = w);
        }

        public static Tween TweenWeight(this Rig rig, float to, float duration) {
            Debug.Assert(rig != null, "TweenWeight needs a live Rig.");
            Debug.Assert(to >= 0f && to <= 1f, "Weight must be in [0, 1].");
            if (rig == null) { return default; }
            return Raven.CustomTo(rig, r => r.weight, Mathf.Clamp01(to), duration, (r, w) => r.weight = w);
        }

        public static Sequence TweenReach(this TwoBoneIKConstraint ik, Vector3 worldPosition, float duration) {
            Debug.Assert(ik != null, "TweenReach needs a live TwoBoneIKConstraint.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            Transform goal = ik != null ? ik.data.target : null;
            if (goal == null) {
                Debug.LogError("RavenTween: TweenReach needs a TwoBoneIKConstraint with a Target transform assigned.", ik);
                return default;
            }
            return Raven.Sequence()
                .Chain(Raven.Position(goal, worldPosition, duration).Ease(Ease.InOutSine))
                .Group(ik.TweenWeight(1f, duration).Ease(Ease.OutSine));
        }

        public static Tween TweenRelease(this TwoBoneIKConstraint ik, float duration) {
            Debug.Assert(ik != null, "TweenRelease needs a live TwoBoneIKConstraint.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (ik == null) { return default; }
            return ik.TweenWeight(0f, duration).Ease(Ease.InOutSine);
        }
    }
}
