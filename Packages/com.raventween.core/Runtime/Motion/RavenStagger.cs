using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>
    /// Plays one <see cref="MotionPreset"/> on every child, one after another: menus that cascade
    /// in, list rows, inventory slots, a row of coins. No code needed.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Stagger")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    public sealed class RavenStagger : MonoBehaviour {
        public enum Order : byte {
            /// <summary>First child first.</summary>
            FirstToLast = 0,
            /// <summary>Last child first.</summary>
            LastToFirst = 1,
            /// <summary>From the middle child outwards.</summary>
            CenterOut = 2
        }

        [Tooltip("A saved Motion Style. When set, it replaces the motion and settings below.")]
        [SerializeField] MotionStyle style;
        [SerializeField] MotionPreset preset = MotionPreset.RiseIn;
        [Tooltip("OnEnable, OnStart or Manual. Pointer and physics triggers belong on a Raven Motion.")]
        [SerializeField] MotionTrigger trigger = MotionTrigger.OnEnable;
        [Tooltip("Seconds between one child and the next.")]
        [SerializeField, Min(0f)] float interval = 0.06f;
        [SerializeField] Order order = Order.FirstToLast;
        [SerializeField] MotionSettings settings = MotionSettings.Default;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        readonly List<Transform> _children = new List<Transform>(16);
        readonly List<MotionTarget> _targets = new List<MotionTarget>(16);
        readonly List<MotionRest> _rests = new List<MotionRest>(16);
        readonly List<MotionChannels> _channels = new List<MotionChannels>(16);
        readonly List<Sequence> _live = new List<Sequence>(16);
        int _pending;
        // The last run was an exit that finished: the children are hidden, not at rest.
        bool _leftHidden;
        bool _playingExit;

        public MotionStyle Style { get { return style; } set { style = value; } }
        public MotionPreset Preset { get { return preset; } set { preset = value; } }
        public MotionPreset EffectivePreset { get { return style != null ? style.Preset : preset; } }
        public MotionSettings EffectiveSettings { get { return style != null ? style.Settings : settings; } }
        public float Interval { get { return interval; } set { interval = Mathf.Max(value, 0f); } }
        public MotionSettings Settings { get { return settings; } set { settings = value; } }
        public UnityEvent OnComplete { get { return onComplete; } }
        public bool IsPlaying { get { return _pending > 0; } }

        void OnEnable() {
            if (trigger == MotionTrigger.OnEnable) { Play(); }
        }

        void Start() {
            if (trigger == MotionTrigger.OnStart) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        /// <summary>Plays the motion on every active child, one after another.</summary>
        public void Play() {
            MotionPreset playing = EffectivePreset;
            if (!isActiveAndEnabled || playing == MotionPreset.None) { return; }
            Stop();
            CollectChildren();
            _playingExit = MotionLibrary.Category(playing) == MotionCategory.Exit;
            _leftHidden = false;
            MotionSettings shared = EffectiveSettings;
            int count = _targets.Count;
            for (int i = 0; i < count; i++) {
                MotionSettings child = shared;
                child.delay = shared.delay + Slot(i, count) * interval;
                MotionRest rest = _rests[i];
                Sequence sequence = MotionBuilder.Build(_targets[i], playing, child, rest, out MotionChannels used);
                _channels[i] = used;
                if (!sequence.IsAlive) { continue; }
                _pending++;
                sequence.OnComplete(this, stagger => stagger.HandleChildComplete());
                _live.Add(sequence);
            }
            if (_pending == 0) { onComplete.Invoke(); }
        }

        /// <summary>
        /// Stops every child's motion and puts back what it animated. Children at rest are left
        /// alone, unless an exit hid them: then they are shown again on their rest poses.
        /// </summary>
        public void Stop() {
            bool running = _pending > 0;
            for (int i = 0; i < _live.Count; i++) { _live[i].Stop(); }
            _live.Clear();
            _pending = 0;
            if (!running && !_leftHidden) { return; }
            _leftHidden = false;
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].IsValid) { _rests[i].Apply(_targets[i], _channels[i]); }
            }
        }

        // Rest poses are read again on every play, after the layout pass, so children follow
        // layout changes and moves made by scripts. Only children hidden by an exit keep their
        // previous rest poses: hidden is not "at rest".
        void CollectChildren() {
            RebuildLayout();
            Transform root = transform;
            int count = root.childCount;
            var previous = _children.Count == 0 ? null : _children.ToArray();
            _children.Clear();
            for (int i = 0; i < count; i++) {
                Transform child = root.GetChild(i);
                if (child.gameObject.activeInHierarchy) { _children.Add(child); }
            }
            if (previous != null && SameChildren(previous) && _leftHidden) { return; }
            _targets.Clear();
            _rests.Clear();
            _channels.Clear();
            for (int i = 0; i < _children.Count; i++) {
                MotionTarget target = MotionTarget.Resolve(_children[i]);
                _targets.Add(target);
                _rests.Add(MotionRest.Capture(target));
                _channels.Add(MotionChannels.None);
            }
        }

        bool SameChildren(Transform[] previous) {
            if (previous.Length != _children.Count) { return false; }
            for (int i = 0; i < previous.Length; i++) {
                if (previous[i] != _children[i]) { return false; }
            }
            return true;
        }

        void RebuildLayout() {
            var rect = transform as RectTransform;
            if (rect != null && GetComponent<LayoutGroup>() != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(rect); }
        }

        // Position of child i in the cascade.
        int Slot(int i, int count) {
            if (order == Order.LastToFirst) { return count - 1 - i; }
            if (order == Order.CenterOut) {
                float center = (count - 1) * 0.5f;
                return Mathf.RoundToInt(Mathf.Abs(i - center) * 2f) / 2;
            }
            return i;
        }

        void HandleChildComplete() {
            if (_pending <= 0) { return; }
            _pending--;
            if (_pending > 0) { return; }
            _live.Clear();
            _leftHidden = _playingExit;
            onComplete.Invoke();
        }
    }
}
