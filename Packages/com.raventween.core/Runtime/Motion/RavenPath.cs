using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// Moves its object along a path of points drawn in the Scene view: moving platforms, patrols,
    /// flying pickups, camera fly-bys. The path starts where the object stands and is stored
    /// relative to it, so moving the object moves the whole path. Constant speed along curves.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Path")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    public sealed class RavenPath : MonoBehaviour, IMotionTriggerable {
        public enum PathLoop : byte {
            /// <summary>Travel once and stop at the end.</summary>
            Once = 0,
            /// <summary>Start over from the beginning forever (close the path to make a circuit).</summary>
            Loop = 1,
            /// <summary>Go to the end and back, forever.</summary>
            BackAndForth = 2
        }

        public enum Orientation : byte {
            /// <summary>Keep the object's rotation.</summary>
            None = 0,
            /// <summary>Face the direction of travel (3D: forward along the path, up stays up).</summary>
            FacePath = 1,
            /// <summary>Face the direction of travel in the screen plane (2D: the object's right side leads).</summary>
            FacePath2D = 2
        }

        public enum Timing : byte {
            /// <summary>The whole trip takes Duration seconds.</summary>
            Duration = 0,
            /// <summary>Travel at Speed units per second, whatever the length.</summary>
            Speed = 1
        }

        const int SamplesPerSegment = 24;

        [Tooltip("Points after the start, relative to where the object stands (in its parent's space). Drag them in the Scene view.")]
        [SerializeField] List<Vector3> points = new List<Vector3> { new Vector3(3f, 0f, 0f), new Vector3(3f, 2f, 0f) };
        [Tooltip("Come back to the start at the end, making a circuit.")]
        [SerializeField] bool closed;
        [Tooltip("Round the corners into a smooth curve.")]
        [SerializeField] bool smooth = true;
        [SerializeField] Timing timing = Timing.Duration;
        [SerializeField, Min(0.01f)] float duration = 3f;
        [Tooltip("Units per second, when Timing is Speed.")]
        [SerializeField, Min(0.01f)] float speed = 2f;
        [SerializeField] Ease ease = Ease.InOutSine;
        [SerializeField] PathLoop loop = PathLoop.Once;
        [SerializeField] Orientation orientation = Orientation.None;
        [SerializeField] MotionTrigger trigger = MotionTrigger.OnEnable;
        [Tooltip("Keep moving while the game is paused (Time.timeScale = 0).")]
        [SerializeField] bool useUnscaledTime;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        readonly List<Vector3> _samples = new List<Vector3>(64);
        readonly List<float> _lengths = new List<float>(64);
        float _total;
        Vector3 _origin;
        Quaternion _restRotation;
        bool _hasOrigin;
        Tween _tween;
        float _lastProgress = -1f;
        bool _reversed;

        public List<Vector3> Points { get { return points; } }
        public bool Closed { get { return closed; } set { closed = value; } }
        public bool Smooth { get { return smooth; } set { smooth = value; } }
        public PathLoop Looping { get { return loop; } set { loop = value; } }
        public UnityEvent OnComplete { get { return onComplete; } }
        public bool IsPlaying { get { return _tween.IsAlive; } }

        /// <summary>Where the path starts, in the parent's space: the object's position when it first played.</summary>
        public Vector3 Origin { get { return _hasOrigin ? _origin : transform.localPosition; } }

        /// <summary>Length of the path in the parent's units.</summary>
        public float Length { get { Rebuild(); return _total; } }

        /// <summary>Seconds one trip takes.</summary>
        public float TripDuration { get { return timing == Timing.Speed ? Mathf.Max(Length, 0.0001f) / speed : duration; } }

        void Awake() {
            MotionTriggers.EnsurePointerRelay(gameObject, trigger);
        }

        void OnEnable() {
            if (trigger == MotionTrigger.OnEnable) { Play(); }
        }

        void Start() {
            if (trigger == MotionTrigger.OnStart) { Play(); }
        }

        void OnDisable() {
            if (_tween.IsAlive) { _tween.Stop(); }
        }

        /// <summary>Travels the path from its start.</summary>
        public void Play() {
            if (!isActiveAndEnabled) { return; }
            if (_tween.IsAlive) { _tween.Stop(); }
            _tween = CreateTween();
            if (!_tween.IsAlive) { return; }
            if (loop == PathLoop.Loop) { _tween.Infinite(CycleMode.Restart); }
            else if (loop == PathLoop.BackAndForth) { _tween.Infinite(CycleMode.Yoyo); }
            _tween.UnscaledTime(useUnscaledTime).OnComplete(this, path => path.onComplete.Invoke());
        }

        /// <summary>Stops where the object is.</summary>
        public void Stop() {
            if (_tween.IsAlive) { _tween.Stop(); }
        }

        /// <summary>Stops and puts the object back at the start of the path.</summary>
        public void ResetToStart() {
            Stop();
            if (!_hasOrigin) { return; }
            transform.localPosition = _origin;
            transform.localRotation = _restRotation;
        }

        public void HandleTrigger(MotionTrigger started) {
            if (started == trigger) { Play(); }
        }

        /// <summary>One trip as a tween (no loop), for previews and custom sequences.</summary>
        public Tween CreateTween() {
            if (!_hasOrigin) {
                _origin = transform.localPosition;
                _restRotation = transform.localRotation;
                _hasOrigin = true;
            }
            Rebuild();
            if (_total <= 0f) { return default; }
            _lastProgress = -1f;
            _reversed = false;
            return Raven.Custom(this, 0f, 1f, TripDuration, (path, t) => path.Apply(t)).Ease(ease);
        }

        /// <summary>Forgets the start point: the next play starts from where the object stands now.</summary>
        public void ForgetOrigin() {
            _hasOrigin = false;
        }

        /// <summary>Point at <paramref name="t"/> (0 = start, 1 = end) along the path, at constant speed, in the parent's space.</summary>
        public Vector3 Evaluate(float t) {
            Rebuild();
            return Origin + SampleAt(Mathf.Clamp01(t) * _total);
        }

        /// <summary>Fills <paramref name="world"/> with points along the path in world space, for drawing.</summary>
        public void GetWorldPolyline(List<Vector3> world) {
            Debug.Assert(world != null, "A list is needed to receive the path.");
            world.Clear();
            Rebuild();
            for (int i = 0; i < _samples.Count; i++) { world.Add(ToWorld(Origin + _samples[i])); }
        }

        /// <summary>World position of control point <paramref name="index"/> (-1 = the start).</summary>
        public Vector3 GetWorldPoint(int index) {
            Vector3 offset = index < 0 ? Vector3.zero : points[index];
            return ToWorld(Origin + offset);
        }

        /// <summary>Moves control point <paramref name="index"/> to a world position.</summary>
        public void SetWorldPoint(int index, Vector3 world) {
            Transform parent = transform.parent;
            Vector3 local = parent != null ? parent.InverseTransformPoint(world) : world;
            points[index] = local - Origin;
        }

        Vector3 ToWorld(Vector3 local) {
            Transform parent = transform.parent;
            return parent != null ? parent.TransformPoint(local) : local;
        }

        void Apply(float t) {
            float clamped = Mathf.Clamp01(t);
            Vector3 position = Origin + SampleAt(clamped * _total);
            transform.localPosition = position;
            // On the way back (Back And Forth, or an ease that turns around) face the other way.
            if (_lastProgress >= 0f && !Mathf.Approximately(clamped, _lastProgress)) { _reversed = clamped < _lastProgress; }
            _lastProgress = clamped;
            if (orientation == Orientation.None) { return; }
            Vector3 ahead = SampleAt(Mathf.Min(clamped * _total + 0.05f, _total)) - SampleAt(Mathf.Max(clamped * _total - 0.05f, 0f));
            if (_reversed) { ahead = -ahead; }
            if (ahead.sqrMagnitude < 1e-10f) { return; }
            if (orientation == Orientation.FacePath) { transform.localRotation = Quaternion.LookRotation(ahead.normalized, Vector3.up); }
            else { transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(ahead.y, ahead.x) * Mathf.Rad2Deg); }
        }

        // Offset from the origin at a distance along the path (binary search in the length table).
        Vector3 SampleAt(float distance) {
            int count = _samples.Count;
            if (count == 0) { return Vector3.zero; }
            if (distance <= 0f || count == 1) { return _samples[0]; }
            if (distance >= _total) { return _samples[count - 1]; }
            int low = 0;
            int high = count - 1;
            while (high - low > 1) {
                int mid = (low + high) >> 1;
                if (_lengths[mid] < distance) { low = mid; } else { high = mid; }
            }
            float span = _lengths[high] - _lengths[low];
            float k = span > 0f ? (distance - _lengths[low]) / span : 0f;
            return Vector3.LerpUnclamped(_samples[low], _samples[high], k);
        }

        // Samples the path (start, points, back to the start when closed) into a length table.
        void Rebuild() {
            _samples.Clear();
            _lengths.Clear();
            _total = 0f;
            int controls = points.Count + 1 + (closed ? 1 : 0);
            if (controls < 2) { return; }
            for (int segment = 0; segment < controls - 1; segment++) {
                int first = segment == 0 ? 0 : 1;
                for (int s = first; s <= SamplesPerSegment; s++) {
                    AddSample(SegmentPoint(segment, controls, s / (float)SamplesPerSegment));
                }
            }
        }

        void AddSample(Vector3 point) {
            if (_samples.Count > 0) { _total += Vector3.Distance(_samples[_samples.Count - 1], point); }
            _samples.Add(point);
            _lengths.Add(_total);
        }

        Vector3 SegmentPoint(int segment, int controls, float t) {
            Vector3 p1 = Control(segment, controls);
            Vector3 p2 = Control(segment + 1, controls);
            if (!smooth) { return Vector3.LerpUnclamped(p1, p2, t); }
            Vector3 p0 = Control(segment - 1, controls);
            Vector3 p3 = Control(segment + 2, controls);
            return CatmullRom(p0, p1, p2, p3, t);
        }

        // Control point i: 0 is the start, then each point, then the start again when closed.
        // Out-of-range neighbours wrap on a closed path and repeat the end point on an open one.
        Vector3 Control(int i, int controls) {
            if (closed) {
                int ring = controls - 1;
                i = ((i % ring) + ring) % ring;
            } else {
                i = Mathf.Clamp(i, 0, controls - 1);
            }
            return i == 0 ? Vector3.zero : points[i - 1];
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
