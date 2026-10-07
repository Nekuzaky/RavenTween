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

        // ----- Single axis: the other axes stay free for other tweens or scripts -----

        /// <summary>Tweens the world X position only.</summary>
        public static Tween PositionX(Transform target, float to, float duration) { return Axis(target, PropertyKind.PositionX, to, duration); }

        /// <summary>Tweens the world Y position only.</summary>
        public static Tween PositionY(Transform target, float to, float duration) { return Axis(target, PropertyKind.PositionY, to, duration); }

        /// <summary>Tweens the world Z position only.</summary>
        public static Tween PositionZ(Transform target, float to, float duration) { return Axis(target, PropertyKind.PositionZ, to, duration); }

        /// <summary>Tweens the local X position only.</summary>
        public static Tween LocalPositionX(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalPositionX, to, duration); }

        /// <summary>Tweens the local Y position only.</summary>
        public static Tween LocalPositionY(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalPositionY, to, duration); }

        /// <summary>Tweens the local Z position only.</summary>
        public static Tween LocalPositionZ(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalPositionZ, to, duration); }

        /// <summary>Tweens the local X scale only.</summary>
        public static Tween ScaleX(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalScaleX, to, duration); }

        /// <summary>Tweens the local Y scale only.</summary>
        public static Tween ScaleY(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalScaleY, to, duration); }

        /// <summary>Tweens the local Z scale only.</summary>
        public static Tween ScaleZ(Transform target, float to, float duration) { return Axis(target, PropertyKind.LocalScaleZ, to, duration); }

        static Tween Axis(Object target, PropertyKind kind, float to, float duration) {
            Debug.Assert(target != null, kind + " tween needs a live target.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, kind, 0, new TweenValue(to), duration);
        }

        // ----- At a speed: the duration comes from the distance to travel right now -----

        /// <summary>Moves to <paramref name="to"/> at <paramref name="speed"/> units per second (world space).</summary>
        public static Tween PositionAtSpeed(Transform target, Vector3 to, float speed) {
            float distance = target != null ? Vector3.Distance(target.position, to) : 0f;
            return Position(target, to, DurationFor(distance, speed));
        }

        /// <summary>Moves to <paramref name="to"/> at <paramref name="speed"/> units per second (local space).</summary>
        public static Tween LocalPositionAtSpeed(Transform target, Vector3 to, float speed) {
            float distance = target != null ? Vector3.Distance(target.localPosition, to) : 0f;
            return LocalPosition(target, to, DurationFor(distance, speed));
        }

        /// <summary>Turns to <paramref name="to"/> at <paramref name="degreesPerSecond"/> (world space, shortest path).</summary>
        public static Tween RotationAtSpeed(Transform target, Quaternion to, float degreesPerSecond) {
            float angle = target != null ? Quaternion.Angle(target.rotation, to) : 0f;
            return Rotation(target, to, DurationFor(angle, degreesPerSecond));
        }

        /// <summary>Turns to <paramref name="to"/> at <paramref name="degreesPerSecond"/> (local space, shortest path).</summary>
        public static Tween LocalRotationAtSpeed(Transform target, Quaternion to, float degreesPerSecond) {
            float angle = target != null ? Quaternion.Angle(target.localRotation, to) : 0f;
            return LocalRotation(target, to, DurationFor(angle, degreesPerSecond));
        }

        internal static float DurationFor(float distance, float speed) {
            Debug.Assert(speed > 0f, "Speed must be positive.");
            if (!(speed > 0f) || float.IsInfinity(speed)) {
                Debug.LogError("RavenTween: speed must be a positive number; the tween plays instantly.");
                return 0f;
            }
            return distance / speed;
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

        public static Tween TweenPositionX(this Transform target, float to, float duration) { return Raven.PositionX(target, to, duration); }
        public static Tween TweenPositionY(this Transform target, float to, float duration) { return Raven.PositionY(target, to, duration); }
        public static Tween TweenPositionZ(this Transform target, float to, float duration) { return Raven.PositionZ(target, to, duration); }
        public static Tween TweenLocalPositionX(this Transform target, float to, float duration) { return Raven.LocalPositionX(target, to, duration); }
        public static Tween TweenLocalPositionY(this Transform target, float to, float duration) { return Raven.LocalPositionY(target, to, duration); }
        public static Tween TweenLocalPositionZ(this Transform target, float to, float duration) { return Raven.LocalPositionZ(target, to, duration); }
        public static Tween TweenScaleX(this Transform target, float to, float duration) { return Raven.ScaleX(target, to, duration); }
        public static Tween TweenScaleY(this Transform target, float to, float duration) { return Raven.ScaleY(target, to, duration); }
        public static Tween TweenScaleZ(this Transform target, float to, float duration) { return Raven.ScaleZ(target, to, duration); }

        public static Tween TweenPositionAtSpeed(this Transform target, Vector3 to, float speed) { return Raven.PositionAtSpeed(target, to, speed); }
        public static Tween TweenLocalPositionAtSpeed(this Transform target, Vector3 to, float speed) { return Raven.LocalPositionAtSpeed(target, to, speed); }
        public static Tween TweenRotationAtSpeed(this Transform target, Quaternion to, float degreesPerSecond) { return Raven.RotationAtSpeed(target, to, degreesPerSecond); }
        public static Tween TweenLocalRotationAtSpeed(this Transform target, Quaternion to, float degreesPerSecond) { return Raven.LocalRotationAtSpeed(target, to, degreesPerSecond); }
    }
}
