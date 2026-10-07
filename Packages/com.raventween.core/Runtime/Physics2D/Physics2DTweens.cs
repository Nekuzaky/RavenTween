using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Rigidbody2D tweens through MovePosition / MoveRotation, run in FixedUpdate by default.
    /// Compiled only when the Physics 2D module is enabled.
    /// </summary>
    public static class Physics2DTweens {
        /// <summary>Moves a Rigidbody2D to <paramref name="to"/> with MovePosition, in FixedUpdate.</summary>
        public static Tween TweenMovePosition(this Rigidbody2D target, Vector2 to, float duration) {
            return Raven.CustomTo(target, rb => rb.position, to, duration, (rb, p) => rb.MovePosition(p))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        /// <summary>Turns a Rigidbody2D to <paramref name="angle"/> degrees with MoveRotation, in FixedUpdate.</summary>
        public static Tween TweenMoveRotation(this Rigidbody2D target, float angle, float duration) {
            return Raven.CustomTo(target, rb => rb.rotation, angle, duration, (rb, a) => rb.MoveRotation(a))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        /// <summary>Moves a Rigidbody2D at <paramref name="speed"/> units per second.</summary>
        public static Tween TweenMovePositionAtSpeed(this Rigidbody2D target, Vector2 to, float speed) {
            float distance = target != null ? Vector2.Distance(target.position, to) : 0f;
            return TweenMovePosition(target, to, speed > 0f ? distance / speed : 0f);
        }
    }
}
