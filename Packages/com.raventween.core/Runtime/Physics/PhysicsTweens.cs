using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Rigidbody tweens. They move the body through MovePosition / MoveRotation, so collisions
    /// and interpolation keep working, and run in FixedUpdate by default. Compiled only when the
    /// Physics module is enabled.
    /// </summary>
    public static class PhysicsTweens {
        /// <summary>Moves a Rigidbody to <paramref name="to"/> (world space) with MovePosition, in FixedUpdate.</summary>
        public static Tween TweenMovePosition(this Rigidbody target, Vector3 to, float duration) {
            return Raven.CustomTo(target, rb => rb.position, to, duration, (rb, p) => rb.MovePosition(p))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        /// <summary>Turns a Rigidbody to <paramref name="to"/> (world space) with MoveRotation, in FixedUpdate.</summary>
        public static Tween TweenMoveRotation(this Rigidbody target, Quaternion to, float duration) {
            return Raven.CustomTo(target, rb => rb.rotation, to, duration, (rb, r) => rb.MoveRotation(r))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        /// <summary>Moves a Rigidbody at <paramref name="speed"/> units per second.</summary>
        public static Tween TweenMovePositionAtSpeed(this Rigidbody target, Vector3 to, float speed) {
            float distance = target != null ? Vector3.Distance(target.position, to) : 0f;
            return TweenMovePosition(target, to, speed > 0f ? distance / speed : 0f);
        }
    }
}
