using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens world position.</summary>
        public static Tween Position(Transform target, Vector3 to, float duration) {
            Debug.Assert(target != null, "Position tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.Position, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens local position.</summary>
        public static Tween LocalPosition(Transform target, Vector3 to, float duration) {
            Debug.Assert(target != null, "LocalPosition tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LocalPosition, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens world rotation (slerp).</summary>
        public static Tween Rotation(Transform target, Quaternion to, float duration) {
            Debug.Assert(target != null, "Rotation tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.Rotation, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens local rotation (slerp).</summary>
        public static Tween LocalRotation(Transform target, Quaternion to, float duration) {
            Debug.Assert(target != null, "LocalRotation tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LocalRotation, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens world euler angles (lerped component-wise).</summary>
        public static Tween EulerAngles(Transform target, Vector3 to, float duration) {
            Debug.Assert(target != null, "EulerAngles tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.EulerAngles, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens local euler angles (lerped component-wise).</summary>
        public static Tween LocalEulerAngles(Transform target, Vector3 to, float duration) {
            Debug.Assert(target != null, "LocalEulerAngles tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LocalEulerAngles, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens local scale.</summary>
        public static Tween Scale(Transform target, Vector3 to, float duration) {
            Debug.Assert(target != null, "Scale tween needs a live Transform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LocalScale, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens local scale uniformly.</summary>
        public static Tween Scale(Transform target, float to, float duration) {
            return Scale(target, new Vector3(to, to, to), duration);
        }
    }

    /// <summary>Extension-method style access to Transform tweens.</summary>
    public static class TransformTweens {
        public static Tween TweenPosition(this Transform target, Vector3 to, float duration) {
            return Raven.Position(target, to, duration);
        }

        public static Tween TweenLocalPosition(this Transform target, Vector3 to, float duration) {
            return Raven.LocalPosition(target, to, duration);
        }

        public static Tween TweenRotation(this Transform target, Quaternion to, float duration) {
            return Raven.Rotation(target, to, duration);
        }

        public static Tween TweenLocalRotation(this Transform target, Quaternion to, float duration) {
            return Raven.LocalRotation(target, to, duration);
        }

        public static Tween TweenEulerAngles(this Transform target, Vector3 to, float duration) {
            return Raven.EulerAngles(target, to, duration);
        }

        public static Tween TweenLocalEulerAngles(this Transform target, Vector3 to, float duration) {
            return Raven.LocalEulerAngles(target, to, duration);
        }

        public static Tween TweenScale(this Transform target, Vector3 to, float duration) {
            return Raven.Scale(target, to, duration);
        }

        public static Tween TweenScale(this Transform target, float to, float duration) {
            return Raven.Scale(target, to, duration);
        }
    }
}
