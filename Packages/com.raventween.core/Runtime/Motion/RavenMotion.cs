using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RavenTween {
    [AddComponentMenu("RavenTween/Raven Motion")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    public sealed class RavenMotion : MonoBehaviour, IMotionTriggerable {
        [Tooltip("A saved Motion Style. When set, it replaces the motion and feel settings below.")]
        [SerializeField] MotionStyle style;
        [SerializeField] MotionPreset preset = MotionPreset.PopIn;
        [SerializeField] MotionTrigger trigger = MotionTrigger.OnEnable;
        [Tooltip("Object to animate. Empty = this object.")]
        [SerializeField] Transform target;
        [SerializeField] MotionSettings settings = MotionSettings.Default;
        [Tooltip("Trigger and collision starts: only objects with this tag count. Empty = any object.")]
        [SerializeField] string filterTag = "";
        [Tooltip("What happens to the animated object once the motion has finished.")]
        [SerializeField] MotionFinish whenFinished = MotionFinish.Nothing;
        [Tooltip("Another Raven Motion to play when this one finishes (set its start to Manually).")]
        [SerializeField] RavenMotion thenPlay;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        static readonly List<RavenMotion> Siblings = new List<RavenMotion>(4);

        Sequence _current;
        MotionTarget _resolved;
        MotionRest _rest;
        bool _hasRest;
        MotionChannels _channels;
        MotionCategory _lastCategory;
        bool _leftHidden;
        Button _button;

        public MotionStyle Style { get { return style; } set { style = value; } }
        public MotionPreset Preset { get { return preset; } set { preset = value; } }
        public MotionTrigger Trigger { get { return trigger; } set { trigger = value; EnsureTriggerHooks(); } }
        public MotionSettings Settings { get { return settings; } set { settings = value; } }
        public MotionFinish WhenFinished { get { return whenFinished; } set { whenFinished = value; } }
        public RavenMotion ThenPlay { get { return thenPlay; } set { thenPlay = value; } }
        public UnityEvent OnComplete { get { return onComplete; } }

        public MotionPreset EffectivePreset { get { return style != null ? style.Preset : preset; } }

        public MotionSettings EffectiveSettings { get { return style != null ? style.Settings : settings; } }

        public Transform AnimatedTransform { get { return target != null ? target : transform; } set { target = value; _hasRest = false; } }

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

        void OnValidate() {
            if (Application.isPlaying && isActiveAndEnabled) { EnsureTriggerHooks(); }
        }

        public void Play() {
            MotionPreset playing = EffectivePreset;
            if (!isActiveAndEnabled || playing == MotionPreset.None) { return; }
            Resolve();
            if (!_resolved.IsValid) { return; }
            bool idle = !_current.IsAlive;
            if (!idle) { _current.Stop(); }
            PrepareRest(idle, playing);
            _lastCategory = MotionLibrary.Category(playing);
            if (_lastCategory == MotionCategory.Entrance) { ClearHidden(); }
            _current = MotionBuilder.Build(_resolved, playing, EffectiveSettings, _rest, out _channels);
            if (_current.IsAlive) { _current.OnComplete(this, motion => motion.HandleComplete()); }
        }

        public void Stop() {
            bool running = _current.IsAlive;
            if (running) { _current.Stop(); }
            _current = default;
            if (!_hasRest || !_resolved.IsValid || (!running && !_leftHidden)) { return; }
            _rest.Apply(_resolved, _channels);
            _leftHidden = false;
        }

        public void Complete() {
            if (MotionLibrary.Category(EffectivePreset) == MotionCategory.Loop) { Stop(); return; }
            if (_current.IsAlive) { _current.Complete(); }
        }

        public void CaptureRest() {
            Resolve();
            if (!_resolved.IsValid) { return; }
            _rest = MotionRest.Capture(_resolved);
            _hasRest = true;
        }

        public void HandleTrigger(MotionTrigger started) {
            if (started != trigger) { return; }
            if (started == MotionTrigger.OnClick && _button != null) { return; }
            Play();
        }

        void HandleComplete() {
            _current = default;
            _leftHidden = _lastCategory == MotionCategory.Exit;
            onComplete.Invoke();
            if (thenPlay != null && thenPlay != this) { thenPlay.Play(); }
            else if (thenPlay == this) { Play(); }
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

        void PrepareRest(bool idle, MotionPreset playing) {
            if (!_hasRest) {
                _hasRest = true;
                if (!CopySiblingRest()) {
                    MotionLayout.RebuildParent(_resolved);
                    _rest = MotionRest.Capture(_resolved);
                    return;
                }
            }
            if (!idle || SiblingBusy()) { return; }
            MotionCategory category = MotionLibrary.Category(playing);
            _rest.Position = _resolved.ReadPosition();
            _rest.Rotation = _resolved.Transform.localRotation;
            if (category == MotionCategory.Attention || category == MotionCategory.Loop) { _rest.Color = _resolved.ReadColor(); }
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

        bool SiblingBusy() {
            if (_leftHidden) { return true; }
            GetComponents(Siblings);
            bool busy = false;
            for (int i = 0; i < Siblings.Count && !busy; i++) {
                RavenMotion other = Siblings[i];
                if (other == this || other.AnimatedTransform != _resolved.Transform) { continue; }
                busy = other._current.IsAlive || other._leftHidden;
            }
            Siblings.Clear();
            return busy;
        }

        void ClearHidden() {
            _leftHidden = false;
            GetComponents(Siblings);
            for (int i = 0; i < Siblings.Count; i++) {
                if (Siblings[i].AnimatedTransform == _resolved.Transform) { Siblings[i]._leftHidden = false; }
            }
            Siblings.Clear();
        }

        internal static bool TryGetRestScale(GameObject owner, Transform animated, out Vector3 scale) {
            scale = Vector3.one;
            owner.GetComponents(Siblings);
            bool found = false;
            for (int i = 0; i < Siblings.Count && !found; i++) {
                RavenMotion other = Siblings[i];
                if (!other._hasRest || other.AnimatedTransform != animated) { continue; }
                scale = other._rest.Scale;
                found = true;
            }
            Siblings.Clear();
            return found;
        }

        void EnsureTriggerHooks() {
            _button = MotionTriggers.HookTrigger(this, trigger, _button, Play);
        }

        void HandlePhysics(MotionTrigger started, GameObject other) {
            if (started == trigger && MotionTriggers.PassesTag(filterTag, other)) { Play(); }
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

    static class MotionLayout {
        public static void RebuildParent(in MotionTarget target) {
            if (target.Rect == null) { return; }
            var parent = target.Rect.parent as RectTransform;
            if (parent != null && parent.GetComponent<LayoutGroup>() != null) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }
    }
}
