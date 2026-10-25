using UnityEngine;

namespace RavenTween {
    public static class Physics2DTweens {
        public static Tween TweenMovePosition(this Rigidbody2D target, Vector2 to, float duration) {
            return Raven.CustomTo(target, rb => rb.position, to, duration, (rb, p) => rb.MovePosition(p))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        public static Tween TweenMoveRotation(this Rigidbody2D target, float angle, float duration) {
            return Raven.CustomTo(target, rb => rb.rotation, angle, duration, (rb, a) => rb.MoveRotation(a))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        public static Tween TweenMovePositionAtSpeed(this Rigidbody2D target, Vector2 to, float speed) {
            float distance = target != null ? Vector2.Distance(target.position, to) : 0f;
            return TweenMovePosition(target, to, speed > 0f ? distance / speed : 0f);
        }
    }
}
