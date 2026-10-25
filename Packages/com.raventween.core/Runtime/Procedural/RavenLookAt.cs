using UnityEngine;

namespace RavenTween {
    [AddComponentMenu("RavenTween/Raven Look At")]
    [DisallowMultipleComponent]
    public sealed class RavenLookAt : MonoBehaviour {
        const float MinDirectionSqr = 1e-8f;

        [Tooltip("What to look at. When empty, the transform eases back to its base pose.")]
        [SerializeField] Transform target;
        [Tooltip("0 = base pose only, 1 = fully looking at the target. Tween it with TweenWeight.")]
        [SerializeField, Range(0f, 1f)] float weight = 1f;
        [Tooltip("The local axis that should point at the target.")]
        [SerializeField] Vector3 forwardAxis = Vector3.forward;
        [Tooltip("Maximum turn away from the base pose, in degrees.")]
        [SerializeField, Range(0f, 180f)] float maxAngle = 70f;
        [Tooltip("Seconds to catch up with the target. 0 snaps instantly.")]
        [SerializeField, Min(0f)] float smoothTime = 0.12f;
        [SerializeField] bool useUnscaledTime;

        Quaternion _baseLocal = Quaternion.identity;
        Quaternion _writtenLocal = Quaternion.identity;
        bool _hasWritten;
        Quaternion _offset = Quaternion.identity;

        public Transform Target {
            get { return target; }
            set { target = value; }
        }

        public float Weight {
            get { return weight; }
            set { weight = Mathf.Clamp01(value); }
        }

        public float MaxAngle {
            get { return maxAngle; }
            set { maxAngle = Mathf.Clamp(value, 0f, 180f); }
        }

        public float SmoothTime {
            get { return smoothTime; }
            set { smoothTime = Mathf.Max(value, 0f); }
        }

        void OnEnable() {
            _hasWritten = false;
            _offset = Quaternion.identity;
        }

        void OnDisable() {
            if (_hasWritten && SameRotation(transform.localRotation, _writtenLocal)) {
                transform.localRotation = _baseLocal;
            }
            _hasWritten = false;
        }

        void LateUpdate() {
            Evaluate(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        public void ResetSmoothing() {
            _offset = Quaternion.identity;
        }

        internal void Evaluate(float deltaTime) {
            Debug.Assert(deltaTime >= 0f, "Delta time cannot be negative.");
            Debug.Assert(!float.IsNaN(weight), "Look-at weight must be a number.");
            Quaternion current = transform.localRotation;
            bool untouched = _hasWritten && SameRotation(current, _writtenLocal);
            _baseLocal = untouched ? _baseLocal : current;
            transform.localRotation = _baseLocal;
            Quaternion desired = ComputeDesiredOffset();
            float follow = smoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Mathf.Max(deltaTime, 0f) / smoothTime);
            _offset = Quaternion.Slerp(_offset, desired, follow);
            Quaternion weighted = Quaternion.Slerp(Quaternion.identity, _offset, weight);
            transform.rotation = weighted * transform.rotation;
            _writtenLocal = transform.localRotation;
            _hasWritten = true;
        }

        static bool SameRotation(Quaternion a, Quaternion b) {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        Quaternion ComputeDesiredOffset() {
            if (target == null || forwardAxis.sqrMagnitude < MinDirectionSqr) { return Quaternion.identity; }
            Vector3 toTarget = target.position - transform.position;
            if (toTarget.sqrMagnitude < MinDirectionSqr) { return Quaternion.identity; }
            Vector3 forward = transform.rotation * forwardAxis.normalized;
            Quaternion offset = Quaternion.FromToRotation(forward, toTarget);
            float angle = Quaternion.Angle(Quaternion.identity, offset);
            if (angle > maxAngle && angle > 0f) {
                offset = Quaternion.Slerp(Quaternion.identity, offset, maxAngle / angle);
            }
            return offset;
        }

        void OnValidate() {
            weight = Mathf.Clamp01(weight);
            if (forwardAxis.sqrMagnitude < MinDirectionSqr) { forwardAxis = Vector3.forward; }
        }

        void OnDrawGizmosSelected() {
            if (target == null) { return; }
            Gizmos.color = new Color(0.43f, 0.69f, 1f, 0.9f);
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position, 0.05f);
        }
    }
}
