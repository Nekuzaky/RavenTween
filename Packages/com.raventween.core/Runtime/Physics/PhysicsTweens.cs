using UnityEngine;

namespace RavenTween {
    public static class PhysicsTweens {
        public static Tween TweenMovePosition(this Rigidbody target, Vector3 to, float duration) {
            return Raven.CustomTo(target, rb => rb.position, to, duration, (rb, p) => rb.MovePosition(p))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        public static Tween TweenMoveRotation(this Rigidbody target, Quaternion to, float duration) {
            return Raven.CustomTo(target, rb => rb.rotation, to, duration, (rb, r) => rb.MoveRotation(r))
                        .UpdateIn(UpdatePhase.FixedUpdate);
        }

        public static Tween TweenMovePositionAtSpeed(this Rigidbody target, Vector3 to, float speed) {
            float distance = target != null ? Vector3.Distance(target.position, to) : 0f;
            return TweenMovePosition(target, to, speed > 0f ? distance / speed : 0f);
        }
    }
}
