using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>
    /// Plays a ready-made <see cref="MotionPreset"/> without code: pick a motion, pick what
    /// starts it (enable, click, hover, a trigger zone, a collision, or a UnityEvent), done.
    /// Several Raven Motions can sit on one object, e.g. an entrance plus a hover pulse.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Motion")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    public sealed class RavenMotion : MonoBehaviour {
        [SerializeField] MotionPreset preset = MotionPreset.PopIn;
        [SerializeField] MotionTrigger trigger = MotionTrigger.OnEnable;
        [Tooltip("Object to animate. Empty = this object.")]
        [SerializeField] Transform target;
        [SerializeField] MotionSettings settings = MotionSettings.Default;
        [Tooltip("Trigger and collision starts: only objects with this tag count. Empty = any object.")]
        [SerializeField] string filterTag = "";
        [Tooltip("What happens to the animated object once the motion has finished.")]
        [SerializeField] MotionFinish whenFinished = MotionFinish.Nothing;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        static readonly List<RavenMotion> Siblings = new List<RavenMotion>(4);

        Sequence _current;
        MotionTarget _resolved;
        MotionRest _rest;
        bool _hasRest;
        MotionChannels _channels;
        Button _button;

        public MotionPreset Preset { get { return preset; } set { preset = value; } }
        public MotionTrigger Trigger { get { return trigger; } set { trigger = value; EnsureTriggerHooks(); } }
        public MotionSettings Settings { get { return settings; } set { settings = value; } }
        public MotionFinish WhenFinished { get { return whenFinished; } set { whenFinished = value; } }
        public UnityEvent OnComplete { get { return onComplete; } }

        /// <summary>Object being animated: <see cref="target"/>, or this one.</summary>
        public Transform AnimatedTransform { get { return target != null ? target : transform; } set { target = value; _hasRest = false; } }

        /// <summary>The running motion, if any.</summary>
        public Sequence Current { get { return _current; } }
        public bool IsPlaying { get { return _current.IsAlive; } }

        void Awake() {
            EnsureTriggerHooks();
        }

        void OnEnable() {
            if (trigger == MotionTrigger.OnEnable) { Play(); }
        }

        void Start() {
            if (trigger == MotionTrigger.OnStart) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        void OnDestroy() {
            if (_button != null) { _button.onClick.RemoveListener(Play); }
        }

        /// <summary>Plays the motion from the start, replacing a run that is still going.</summary>
        public void Play() {
            if (!isActiveAndEnabled || preset == MotionPreset.None) { return; }
            Resolve();
            if (!_resolved.IsValid) { return; }
            bool idle = !_current.IsAlive;
            if (!idle) { _current.Stop(); }
            PrepareRest(idle);
            _current = MotionBuilder.Build(_resolved, preset, settings, _rest, out _channels);
            if (_current.IsAlive) { _current.OnComplete(this, motion => motion.HandleComplete()); }
        }

        /// <summary>Stops the motion and puts the object back on its rest pose.</summary>
        public void Stop() {
            if (_current.IsAlive) { _current.Stop(); }
            _current = default;
            if (_hasRest && _resolved.IsValid) { _rest.Apply(_resolved, _channels); }
        }

        /// <summary>Jumps to the end of the motion at once (loops: back to the rest pose).</summary>
        public void Complete() {
            if (MotionLibrary.Category(preset) == MotionCategory.Loop) { Stop(); return; }
            if (_current.IsAlive) { _current.Complete(); }
        }

        /// <summary>Takes the object's current pose as its rest pose (after moving it from a script).</summary>
        public void CaptureRest() {
            Resolve();
            if (!_resolved.IsValid) { return; }
            _rest = MotionRest.Capture(_resolved);
            _hasRest = true;
        }

        /// <summary>Starts the motion if <paramref name="started"/> is this component's trigger.</summary>
        internal void HandleTrigger(MotionTrigger started) {
            if (started != trigger) { return; }
            if (started == MotionTrigger.OnClick && _button != null) { return; } // The Button listener plays it.
            Play();
        }

        void HandleComplete() {
            _current = default;
            onComplete.Invoke();
            if (!_resolved.IsValid) { return; }
            GameObject animated = _resolved.Transform.gameObject;
            if (whenFinished == MotionFinish.Deactivate) { animated.SetActive(false); }
            else if (whenFinished == MotionFinish.Destroy) { Destroy(animated); }
        }

        void Resolve() {
            Transform wanted = AnimatedTransform;
            if (_resolved.Transform == wanted) { return; }
            _resolved = MotionTarget.Resolve(wanted);
            _hasRest = false;
        }

        // The rest pose is taken once, shared with other Raven Motions animating the same
        // object, so an exit followed by an entrance never mistakes "hidden" for "at rest".
        // Attention and loop motions follow an object that scripts have moved meanwhile.
        void PrepareRest(bool idle) {
            if (!_hasRest) {
                if (!CopySiblingRest()) {
                    RebuildParentLayout();
                    _rest = MotionRest.Capture(_resolved);
                }
                _hasRest = true;
                return;
            }
            MotionCategory category = MotionLibrary.Category(preset);
            if (idle && (category == MotionCategory.Attention || category == MotionCategory.Loop)) {
                _rest.Position = _resolved.ReadPosition();
                _rest.Rotation = _resolved.Transform.localRotation;
            }
        }

        bool CopySiblingRest() {
            GetComponents(Siblings);
            bool found = false;
            for (int i = 0; i < Siblings.Count && !found; i++) {
                RavenMotion other = Siblings[i];
                if (other == this || !other._hasRest || other.AnimatedTransform != _resolved.Transform) { continue; }
                _rest = other._rest;
                found = true;
            }
            Siblings.Clear();
            return found;
        }

        // Inside a Layout Group, positions are only final after the layout pass.
        void RebuildParentLayout() {
            if (_resolved.Rect == null) { return; }
            var parent = _resolved.Rect.parent as RectTransform;
            if (parent != null && parent.GetComponent<LayoutGroup>() != null) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        void EnsureTriggerHooks() {
            if (trigger == MotionTrigger.OnClick && _button == null) {
                _button = GetComponent<Button>();
                if (_button != null) { _button.onClick.AddListener(Play); }
            }
            if (IsPointerTrigger(trigger) && GetComponent<RavenMotionPointerRelay>() == null) {
                gameObject.AddComponent<RavenMotionPointerRelay>();
            }
        }

        static bool IsPointerTrigger(MotionTrigger value) {
            return value >= MotionTrigger.OnClick && value <= MotionTrigger.OnPointerUp;
        }

        bool PassesFilter(GameObject other) {
            return string.IsNullOrEmpty(filterTag) || (other != null && other.CompareTag(filterTag));
        }

        void HandlePhysics(MotionTrigger started, GameObject other) {
            if (started == trigger && PassesFilter(other)) { Play(); }
        }

#if RAVEN_MODULE_PHYSICS
        void OnTriggerEnter(Collider other) { HandlePhysics(MotionTrigger.OnTriggerEnter, other.gameObject); }
        void OnTriggerExit(Collider other) { HandlePhysics(MotionTrigger.OnTriggerExit, other.gameObject); }
        void OnCollisionEnter(Collision collision) { HandlePhysics(MotionTrigger.OnCollisionEnter, collision.gameObject); }
#endif

#if RAVEN_MODULE_PHYSICS2D
        void OnTriggerEnter2D(Collider2D other) { HandlePhysics(MotionTrigger.OnTriggerEnter, other.gameObject); }
        void OnTriggerExit2D(Collider2D other) { HandlePhysics(MotionTrigger.OnTriggerExit, other.gameObject); }
        void OnCollisionEnter2D(Collision2D collision) { HandlePhysics(MotionTrigger.OnCollisionEnter, collision.gameObject); }
#endif
    }
}
