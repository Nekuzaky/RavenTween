using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
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
        bool _showPending;
        bool _deactivatePending;

        MotionChannels RestoreMask { get { return (MotionChannels.All & ~MotionChannels.Color) | (_channels & MotionChannels.Color); } }

        public bool IsShown { get { return _shown; } }
        public bool IsAnimating { get { return _current.IsAlive; } }
        public UnityEvent OnShown { get { return onShown; } }
        public UnityEvent OnHidden { get { return onHidden; } }
        public MotionPreset ShowMotion { get { return showMotion; } set { showMotion = value; } }
        public MotionPreset HideMotion { get { return hideMotion; } set { hideMotion = value; } }
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
            if (hiding) {
                _rest.Apply(_resolved, RestoreMask);
                SetInteractable(false);
                ApplyInvisible();
                _deactivatePending = deactivateWhenHidden;
                onHidden.Invoke();
                return;
            }
            if (_shown) { _rest.Apply(_resolved, _channels); }
        }

        public void Show() {
            Initialize();
            if (_shown && !_current.IsAlive && gameObject.activeInHierarchy && !NeedsReveal()) { return; }
            _shown = true;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (!isActiveAndEnabled) {
                _rest.Apply(_resolved, RestoreMask);
                SetInteractable(true);
                _showPending = true;
                return;
            }
            PlayShow();
        }

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

        void Initialize() {
            if (_initialized) { return; }
            _initialized = true;
            _resolved = MotionTarget.Resolve(transform);
            if (gameObject.activeInHierarchy) { MotionLayout.RebuildParent(_resolved); }
            _rest = MotionRest.Capture(_resolved);
            if (startHidden) { SetVisible(false); }
        }

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

        void ApplyHidden() {
            if (deactivateWhenHidden) {
                _rest.Apply(_resolved, RestoreMask);
                gameObject.SetActive(false);
                return;
            }
            SetInteractable(false);
            ApplyInvisible();
        }

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
