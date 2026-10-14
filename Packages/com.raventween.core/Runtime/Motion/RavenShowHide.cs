using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// Shows and hides a panel, a popup or any object with an entrance and an exit motion.
    /// Wire Show, Hide or Toggle to buttons; the object is deactivated (or made invisible and
    /// non-interactive) while hidden and always comes back exactly where it was designed.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Show Hide")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    [DisallowMultipleComponent]
    public sealed class RavenShowHide : MonoBehaviour {
        [Header("Show")]
        [SerializeField] MotionPreset showMotion = MotionPreset.PopIn;
        [Tooltip("A saved Motion Style for showing. When set, it replaces the show motion and settings.")]
        [SerializeField] MotionStyle showStyle;
        [SerializeField] MotionSettings showSettings = MotionSettings.Default;

        [Header("Hide")]
        [SerializeField] MotionPreset hideMotion = MotionPreset.PopOut;
        [Tooltip("A saved Motion Style for hiding. When set, it replaces the hide motion and settings.")]
        [SerializeField] MotionStyle hideStyle;
        [SerializeField] MotionSettings hideSettings = MotionSettings.Default;

        [Header("Behavior")]
        [Tooltip("Hidden when the scene starts; call Show to reveal it.")]
        [SerializeField] bool startHidden;
        [Tooltip("Deactivate the object while hidden. Off: it stays active but invisible and unclickable (transparent with a Canvas Group, shrunk to nothing without one).")]
        [SerializeField] bool deactivateWhenHidden = true;
        [SerializeField] UnityEvent onShown = new UnityEvent();
        [SerializeField] UnityEvent onHidden = new UnityEvent();

        Sequence _current;
        MotionTarget _resolved;
        MotionRest _rest;
        MotionChannels _channels;
        bool _shown = true;
        bool _initialized;
        // Show() was called while a parent was inactive: the entrance plays once it is enabled.
        bool _showPending;
        // A parent switched it off halfway through hiding: deactivate it as soon as it is back.
        bool _deactivatePending;

        // Show and hide motions never animate color: a color set by a script (or a material
        // left without a property block) is left alone.
        MotionChannels RestoreMask { get { return (MotionChannels.All & ~MotionChannels.Color) | (_channels & MotionChannels.Color); } }

        /// <summary>True when shown or showing; false when hidden or hiding.</summary>
        public bool IsShown { get { return _shown; } }
        public bool IsAnimating { get { return _current.IsAlive; } }
        public UnityEvent OnShown { get { return onShown; } }
        public UnityEvent OnHidden { get { return onHidden; } }
        public MotionPreset ShowMotion { get { return showMotion; } set { showMotion = value; } }
        public MotionPreset HideMotion { get { return hideMotion; } set { hideMotion = value; } }
        /// <summary>Deactivate the object while hidden (otherwise: invisible and unclickable).</summary>
        public bool DeactivateWhenHidden { get { return deactivateWhenHidden; } set { deactivateWhenHidden = value; } }

        void Awake() {
            Initialize();
        }

        void OnEnable() {
            if (_deactivatePending) {
                _deactivatePending = false;
                if (!_shown) { Raven.Delay(0f).UnscaledTime().OnComplete(this, panel => panel.DeactivateIfHidden()); }
                return;
            }
            if (!_showPending) { return; }
            _showPending = false;
            PlayShow();
        }

        void DeactivateIfHidden() {
            if (!_shown && deactivateWhenHidden) { ApplyHidden(); }
        }

        void OnDisable() {
            bool hiding = _current.IsAlive && !_shown;
            if (_current.IsAlive) { _current.Stop(); }
            _current = default;
            // Switched off by a parent halfway through hiding: it counts as hidden.
            if (hiding) {
                // It can't be deactivated from here: it stays invisible and unclickable, and is
                // deactivated as soon as the parent comes back.
                _rest.Apply(_resolved, RestoreMask);
                SetInteractable(false);
                ApplyInvisible();
                _deactivatePending = deactivateWhenHidden;
                onHidden.Invoke();
                return;
            }
            // Switched off from outside while visible: come back at rest next time.
            if (_shown) { _rest.Apply(_resolved, _channels); }
        }

        /// <summary>Plays the entrance, activating the object first if needed.</summary>
        public void Show() {
            Initialize();
            if (_shown && !_current.IsAlive && gameObject.activeInHierarchy && !NeedsReveal()) { return; }
            _shown = true;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (!isActiveAndEnabled) {
                // A parent is inactive: be ready now, animate when it is shown.
                _rest.Apply(_resolved, RestoreMask);
                SetInteractable(true);
                _showPending = true;
                return;
            }
            PlayShow();
        }

        /// <summary>Plays the exit, then deactivates the object (or hides it and blocks its input).</summary>
        public void Hide() {
            Initialize();
            _showPending = false;
            if (!_shown && !_current.IsAlive) { return; }
            _shown = false;
            if (!isActiveAndEnabled) { FinishHidden(); return; }
            SetInteractable(false);
            Play(HidePreset(), hideStyle != null ? hideStyle.Settings : hideSettings, false);
        }

        public void Toggle() {
            if (_shown) { Hide(); }
            else { Show(); }
        }

        /// <summary>Shows or hides at once, without animation and without raising the events.</summary>
        public void SetVisible(bool visible) {
            Initialize();
            if (_current.IsAlive) { _current.Stop(); }
            _current = default;
            _showPending = false;
            _shown = visible;
            if (visible) {
                if (!gameObject.activeSelf) { gameObject.SetActive(true); }
                _rest.Apply(_resolved, RestoreMask);
                SetInteractable(true);
            } else {
                ApplyHidden();
            }
        }

        // The rest pose is taken while the object is exactly as designed, active or not.
        void Initialize() {
            if (_initialized) { return; }
            _initialized = true;
            _resolved = MotionTarget.Resolve(transform);
            if (gameObject.activeInHierarchy) { MotionLayout.RebuildParent(_resolved); }
            _rest = MotionRest.Capture(_resolved);
            if (startHidden) { SetVisible(false); }
        }

        // Kept active but made invisible by SetVisible(false) or an earlier hide.
        bool NeedsReveal() {
            return !deactivateWhenHidden && _resolved.IsValid
                && ((_resolved.Group != null && _resolved.Group.alpha <= 0f) || transform.localScale == Vector3.zero);
        }

        void PlayShow() {
            SetInteractable(true);
            Play(ShowPreset(), showStyle != null ? showStyle.Settings : showSettings, true);
        }

        void Play(MotionPreset preset, MotionSettings settings, bool showing) {
            if (_current.IsAlive) { _current.Stop(); }
            // Undo whatever hid it (a half-played exit, a shrink); the entrance then sets its own start.
            if (showing) { _rest.Apply(_resolved, RestoreMask); }
            _current = MotionBuilder.Build(_resolved, preset, settings, _rest, out MotionChannels used);
            _channels |= used;
            if (!_current.IsAlive) {
                if (showing) { onShown.Invoke(); } else { FinishHidden(); }
                return;
            }
            if (showing) { _current.OnComplete(this, panel => panel.onShown.Invoke()); }
            else { _current.OnComplete(this, panel => panel.FinishHidden()); }
        }

        void FinishHidden() {
            _current = default;
            ApplyHidden();
            onHidden.Invoke();
        }

        // Deactivated (back on its rest pose, ready for the next Show), or kept active but
        // invisible and unclickable.
        void ApplyHidden() {
            if (deactivateWhenHidden) {
                _rest.Apply(_resolved, RestoreMask);
                gameObject.SetActive(false);
                return;
            }
            SetInteractable(false);
            ApplyInvisible();
        }

        // Transparent with a Canvas Group, shrunk to nothing without one.
        void ApplyInvisible() {
            if (_resolved.Group != null) { _resolved.Group.alpha = 0f; }
            else { transform.localScale = Vector3.zero; }
            _channels |= MotionChannels.Alpha | MotionChannels.Scale;
        }

        void SetInteractable(bool interactable) {
            if (_resolved.Group == null) { return; }
            _resolved.Group.interactable = interactable;
            _resolved.Group.blocksRaycasts = interactable;
        }

        MotionPreset ShowPreset() { return showStyle != null ? showStyle.Preset : showMotion; }
        MotionPreset HidePreset() { return hideStyle != null ? hideStyle.Preset : hideMotion; }

        /// <summary>The designed scale of <paramref name="animated"/> if a Raven Show Hide on this object knows it.</summary>
        internal static bool TryGetRestScale(GameObject owner, Transform animated, out Vector3 scale) {
            scale = Vector3.one;
            var panel = owner.GetComponent<RavenShowHide>();
            if (panel == null || panel.transform != animated) { return false; }
            panel.Initialize();
            scale = panel._rest.Scale;
            return true;
        }
    }
}
