using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>
    /// Plays a ready-made <see cref="MotionPreset"/> without code: pick a motion, pick what
    /// starts it (enable, click, hover, a trigger zone, a collision, or a UnityEvent), done.
    /// Several Raven Motions can sit on one object, e.g. an entrance plus a hover pulse, and
    /// one can start the next ("Then Play"), e.g. Pop In then Float. Added from a script, it
    /// plays as soon as it is enabled: add it to an inactive object to configure it first.
    /// </summary>
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
        // An exit finished and left the object hidden (it is not at rest until shown again).
        bool _leftHidden;
        Button _button;

        public MotionStyle Style { get { return style; } set { style = value; } }
        public MotionPreset Preset { get { return preset; } set { preset = value; } }
        public MotionTrigger Trigger { get { return trigger; } set { trigger = value; EnsureTriggerHooks(); } }
        public MotionSettings Settings { get { return settings; } set { settings = value; } }
        public MotionFinish WhenFinished { get { return whenFinished; } set { whenFinished = value; } }
        public RavenMotion ThenPlay { get { return thenPlay; } set { thenPlay = value; } }
        public UnityEvent OnComplete { get { return onComplete; } }

        /// <summary>The motion that plays: the style's when a style is set, otherwise <see cref="Preset"/>.</summary>
        public MotionPreset EffectivePreset { get { return style != null ? style.Preset : preset; } }

        /// <summary>The settings in use: the style's when a style is set, otherwise <see cref="Settings"/>.</summary>
        public MotionSettings EffectiveSettings { get { return style != null ? style.Settings : settings; } }

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

        // The inspector writes the trigger directly: apply its hooks when it changes in Play Mode.
        void OnValidate() {
            if (Application.isPlaying && isActiveAndEnabled) { EnsureTriggerHooks(); }
        }

        /// <summary>Plays the motion from the start, replacing a run that is still going.</summary>
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

        /// <summary>
        /// Stops the motion and puts back what it animated. An idle object is left alone, unless an
        /// exit hid it: then it is shown again on its rest pose.
        /// </summary>
        public void Stop() {
            bool running = _current.IsAlive;
            if (running) { _current.Stop(); }
            _current = default;
            if (!_hasRest || !_resolved.IsValid || (!running && !_leftHidden)) { return; }
            _rest.Apply(_resolved, _channels);
            _leftHidden = false;
        }

        /// <summary>Jumps to the end of the motion at once (loops: back to the rest pose).</summary>
        public void Complete() {
            if (MotionLibrary.Category(EffectivePreset) == MotionCategory.Loop) { Stop(); return; }
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
        public void HandleTrigger(MotionTrigger started) {
            if (started != trigger) { return; }
            if (started == MotionTrigger.OnClick && _button != null) { return; } // The Button listener plays it.
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

        // The rest pose is taken once, shared with other Raven Motions animating the same object,
        // so an exit followed by an entrance never mistakes "hidden" for "at rest". When nothing
        // animates the object and no exit left it hidden, it follows what scripts did meanwhile:
        // a pooled enemy re-enabled elsewhere, a text recolored, a panel moved.
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

        // True when this or another Raven Motion on the same object is playing, or an exit left
        // it hidden: its current pose is then not the rest pose.
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

        // An entrance shows the object again: no Raven Motion on it counts it as hidden any more.
        void ClearHidden() {
            _leftHidden = false;
            GetComponents(Siblings);
            for (int i = 0; i < Siblings.Count; i++) {
                if (Siblings[i].AnimatedTransform == _resolved.Transform) { Siblings[i]._leftHidden = false; }
            }
            Siblings.Clear();
        }

        /// <summary>The rest scale of <paramref name="animated"/> if a Raven Motion on this object knows it.</summary>
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
            if (trigger != MotionTrigger.OnClick && _button != null) {
                _button.onClick.RemoveListener(Play);
                _button = null;
            }
            if (trigger == MotionTrigger.OnClick && _button == null) {
                // A click on a Button (or on anything inside one, like its icon) is the Button's:
                // listen to it rather than adding a handler that would take the click away.
                _button = GetComponentInParent<Button>();
                if (_button != null) { _button.onClick.AddListener(Play); }
            }
            // A Button already reports its clicks; other pointer starts need a relay.
            if (!(trigger == MotionTrigger.OnClick && _button != null)) { MotionTriggers.EnsurePointerRelay(gameObject, trigger); }
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

    /// <summary>Layout helpers shared by the no-code components.</summary>
    static class MotionLayout {
        /// <summary>Inside a Layout Group, positions are only final after the layout pass: run it now.</summary>
        public static void RebuildParent(in MotionTarget target) {
            if (target.Rect == null) { return; }
            var parent = target.Rect.parent as RectTransform;
            if (parent != null && parent.GetComponent<LayoutGroup>() != null) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }
    }
}
