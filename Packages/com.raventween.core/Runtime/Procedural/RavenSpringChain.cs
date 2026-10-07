using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Secondary motion for a chain of bones — ponytails, antennae, tails, cables, ears. Each
    /// bone lags behind, swings and settles back toward its base pose (from an Animator, a tween
    /// or a script). Runs after animation, allocates nothing per frame, blends by <see cref="Weight"/>.
    /// </summary>
    /// <remarks>
    /// Like <see cref="RavenLookAt"/>, the base pose is tracked rather than reset: a bone whose
    /// rotation is still the one written last frame keeps its previous base pose; any other
    /// rotation was written by someone else and becomes the new base pose.
    /// </remarks>
    [AddComponentMenu("RavenTween/Raven Spring Chain")]
    [DisallowMultipleComponent]
    public sealed class RavenSpringChain : MonoBehaviour {
        const int MaxBones = 64;
        const int MaxSubSteps = 4;
        const float StepTime = 1f / 60f;
        const float MinLengthSqr = 1e-10f;

        [Tooltip("First bone of the chain. It stays attached; every child below it swings. Defaults to this transform.")]
        [SerializeField] Transform root;
        [Tooltip("0 = base pose only, 1 = full spring motion. Tween it with TweenWeight.")]
        [SerializeField, Range(0f, 1f)] float weight = 1f;
        [Tooltip("How strongly each bone is pulled back to its base pose. Higher = stiffer.")]
        [SerializeField, Range(0f, 1f)] float stiffness = 0.08f;
        [Tooltip("How quickly the swinging dies out. Higher = settles faster.")]
        [SerializeField, Range(0f, 1f)] float damping = 0.12f;
        [Tooltip("Constant force in world space, e.g. (0, -2, 0) to make the chain droop.")]
        [SerializeField] Vector3 gravity = Vector3.zero;
        [Tooltip("Adds a virtual point past the last bone so the last bone swings too. 0 = off.")]
        [SerializeField, Min(0f)] float tipLength;
        [SerializeField] bool useUnscaledTime;

        Transform[] _bones;
        Quaternion[] _baseLocal;
        Quaternion[] _writtenLocal;
        Vector3[] _animated;
        Vector3[] _positions;
        Vector3[] _previous;
        Vector3 _tipLocalDirection;
        int _boneCount;
        int _particleCount;
        float _accumulator;
        bool _hasWritten;

        /// <summary>Blend between the base pose (0) and full spring motion (1).</summary>
        public float Weight {
            get { return weight; }
            set { weight = Mathf.Clamp01(value); }
        }

        public float Stiffness {
            get { return stiffness; }
            set { stiffness = Mathf.Clamp01(value); }
        }

        public float Damping {
            get { return damping; }
            set { damping = Mathf.Clamp01(value); }
        }

        public Vector3 Gravity {
            get { return gravity; }
            set { gravity = value; }
        }

        /// <summary>Number of bones driven by this chain, root included.</summary>
        public int BoneCount { get { return _boneCount; } }

        void OnEnable() {
            Build();
        }

        void OnDisable() {
            RestoreBasePose();
            _hasWritten = false;
        }

        void LateUpdate() {
            Evaluate(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        /// Re-reads the chain from the root and snaps the simulation to the current base pose.
        /// Call it after changing the bone hierarchy. After a teleport, <see cref="ResetPhysics"/> is enough.
        /// </summary>
        public void Build() {
            RestoreBasePose(); // Never capture a swung pose as the new base pose.
            Transform start = root != null ? root : transform;
            _boneCount = CountChain(start);
            _particleCount = _boneCount + (tipLength > 0f ? 1 : 0);
            AllocateBuffers();
            Transform bone = start;
            for (int i = 0; i < _boneCount; i++) {
                _bones[i] = bone;
                _baseLocal[i] = bone.localRotation;
                bone = bone.childCount > 0 ? bone.GetChild(0) : null;
            }
            _hasWritten = false;
            _tipLocalDirection = ComputeTipDirection();
            ResetPhysics();
        }

        /// <summary>Puts every simulated point back on the base pose, with no velocity (e.g. after a teleport).</summary>
        public void ResetPhysics() {
            if (_bones == null || HasMissingBone()) { return; }
            ReadAnimatedPose();
            for (int i = 0; i < _particleCount; i++) {
                _positions[i] = _animated[i];
                _previous[i] = _animated[i];
            }
            _accumulator = 0f;
        }

        static int CountChain(Transform start) {
            Debug.Assert(start != null, "Spring chain needs a root transform.");
            int count = 0;
            Transform bone = start;
            while (bone != null && count < MaxBones) {
                count++;
                bone = bone.childCount > 0 ? bone.GetChild(0) : null;
            }
            return count;
        }

        void AllocateBuffers() {
            Debug.Assert(_boneCount > 0, "Chain must contain at least its root.");
            if (_bones == null || _bones.Length != _boneCount) {
                _bones = new Transform[_boneCount];
                _baseLocal = new Quaternion[_boneCount];
                _writtenLocal = new Quaternion[_boneCount];
            }
            if (_positions == null || _positions.Length != _particleCount) {
                _animated = new Vector3[_particleCount];
                _positions = new Vector3[_particleCount];
                _previous = new Vector3[_particleCount];
            }
        }

        // Tip continues the last segment's direction, expressed in the last bone's local space.
        Vector3 ComputeTipDirection() {
            Transform last = _bones[_boneCount - 1];
            Vector3 worldDirection = _boneCount > 1
                ? last.position - _bones[_boneCount - 2].position
                : last.up;
            if (worldDirection.sqrMagnitude < MinLengthSqr) { worldDirection = last.up; }
            return Quaternion.Inverse(last.rotation) * worldDirection.normalized;
        }

        bool HasMissingBone() {
            for (int i = 0; i < _boneCount; i++) {
                if (_bones[i] == null) { return true; }
            }
            return false;
        }

        // Puts back the base pose on bones that still hold what this component wrote.
        void RestoreBasePose() {
            if (_bones == null || !_hasWritten) { return; }
            for (int i = 0; i < _boneCount; i++) {
                Transform bone = _bones[i];
                if (bone != null && SameRotation(bone.localRotation, _writtenLocal[i])) { bone.localRotation = _baseLocal[i]; }
            }
        }

        // Adopts rotations written by others as the new base pose, restores ours otherwise.
        void TrackBasePose() {
            for (int i = 0; i < _boneCount; i++) {
                Transform bone = _bones[i];
                Quaternion current = bone.localRotation;
                if (_hasWritten && SameRotation(current, _writtenLocal[i])) { bone.localRotation = _baseLocal[i]; }
                else { _baseLocal[i] = current; }
            }
        }

        // Exact on purpose: the value compared is read back from the transform, so an untouched
        // bone matches bit for bit, while a dot-product threshold can reject a slightly
        // denormalized quaternion and wrongly adopt the swung pose as the base pose.
        static bool SameRotation(Quaternion a, Quaternion b) {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        /// <summary>One frame: track the base pose, simulate in fixed steps, write rotations.</summary>
        internal void Evaluate(float deltaTime) {
            Debug.Assert(deltaTime >= 0f, "Delta time cannot be negative.");
            if (_bones == null || _particleCount < 2) { return; }
            if (HasMissingBone()) {
                Build(); // A bone was destroyed: re-read whatever chain is left.
                return;
            }
            TrackBasePose();
            ReadAnimatedPose();
            _accumulator = Mathf.Min(_accumulator + Mathf.Max(deltaTime, 0f), StepTime * MaxSubSteps);
            while (_accumulator >= StepTime) {
                SimulateStep();
                _accumulator -= StepTime;
            }
            ApplyRotations(_accumulator / StepTime);
            for (int i = 0; i < _boneCount; i++) { _writtenLocal[i] = _bones[i].localRotation; }
            _hasWritten = true;
        }

        void ReadAnimatedPose() {
            for (int i = 0; i < _boneCount; i++) { _animated[i] = _bones[i].position; }
            if (_particleCount > _boneCount) {
                Transform last = _bones[_boneCount - 1];
                _animated[_boneCount] = last.position + last.rotation * _tipLocalDirection * tipLength;
            }
        }

        // Verlet integration, a pull toward the base pose, then a length constraint per segment.
        void SimulateStep() {
            Debug.Assert(_particleCount <= MaxBones + 1, "Particle count exceeds the chain bound.");
            Vector3 gravityStep = gravity * (StepTime * StepTime);
            float keep = 1f - damping;
            _positions[0] = _animated[0];
            _previous[0] = _animated[0];
            for (int i = 1; i < _particleCount; i++) {
                Vector3 current = _positions[i];
                Vector3 velocity = (current - _previous[i]) * keep;
                _previous[i] = current;
                Vector3 next = current + velocity + gravityStep;
                next = Vector3.Lerp(next, _animated[i], stiffness);
                float restLength = (_animated[i] - _animated[i - 1]).magnitude;
                Vector3 fromParent = next - _positions[i - 1];
                if (fromParent.sqrMagnitude > MinLengthSqr) {
                    next = _positions[i - 1] + fromParent.normalized * restLength;
                }
                _positions[i] = next;
            }
        }

        // Each bone rotates so its child lands on the simulated point; parents first. Points are
        // interpolated between the last two fixed steps, so motion stays smooth above 60 FPS.
        void ApplyRotations(float alpha) {
            int segments = _particleCount - 1;
            for (int i = 0; i < segments; i++) {
                Transform bone = _bones[i];
                Vector3 childNow = i + 1 < _boneCount
                    ? _bones[i + 1].position
                    : bone.position + bone.rotation * _tipLocalDirection * tipLength;
                Vector3 simulated = Vector3.Lerp(_previous[i + 1], _positions[i + 1], alpha);
                Vector3 from = childNow - bone.position;
                Vector3 to = simulated - bone.position;
                if (from.sqrMagnitude < MinLengthSqr || to.sqrMagnitude < MinLengthSqr) { continue; }
                Quaternion delta = Quaternion.FromToRotation(from, to);
                bone.rotation = Quaternion.Slerp(Quaternion.identity, delta, weight) * bone.rotation;
            }
        }

        void OnValidate() {
            weight = Mathf.Clamp01(weight);
            if (isActiveAndEnabled && Application.isPlaying) { Build(); }
        }

        void OnDrawGizmosSelected() {
            if (_positions == null || !Application.isPlaying) { return; }
            Gizmos.color = new Color(0.43f, 0.69f, 1f, 0.9f);
            for (int i = 1; i < _particleCount; i++) {
                Gizmos.DrawLine(_positions[i - 1], _positions[i]);
                Gizmos.DrawWireSphere(_positions[i], 0.02f);
            }
        }
    }
}
