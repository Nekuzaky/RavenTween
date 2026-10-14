using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// Shows and hides a panel, a popup or any object with an entrance and an exit motion.
    /// Wire Show, Hide or Toggle to buttons; the object is deactivated (or made non-interactive)
    /// while hidden and always comes back exactly where it was designed.
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
        [Tooltip("Deactivate the object while hidden. Off: it stays active but cannot be clicked (needs a Canvas Group for UI).")]
        [SerializeField] bool deactivateWhenHidden = true;
        [SerializeField] UnityEvent onShown = new UnityEvent();
        [SerializeField] UnityEvent onHidden = new UnityEvent();

        Sequence _current;
        MotionTarget _resolved;
        MotionRest _rest;
        MotionChannels _channels;
        bool _hasRest;
        bool _shown = true;
        bool _initialized;

        /// <summary>True when shown or showing; false when hidden or hiding.</summary>
        public bool IsShown { get { return _shown; } }
        public bool IsAnimating { get { return _current.IsAlive; } }
        public UnityEvent OnShown { get { return onShown; } }
        public UnityEvent OnHidden { get { return onHidden; } }
        public MotionPreset ShowMotion { get { return showMotion; } set { showMotion = value; } }
        public MotionPreset HideMotion { get { return hideMotion; } set { hideMotion = value; } }

        void Awake() {
            Initialize();
        }

        void OnDisable() {
            if (_current.IsAlive) { _current.Stop(); }
            _current = default;
            // Deactivated from outside while visible: come back at rest next time.
            if (_hasRest && _shown) { _rest.Apply(_resolved, _channels); }
        }

        /// <summary>Plays the entrance, activating the object first if needed.</summary>
        public void Show() {
            Initialize();
            _shown = true;
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (!isActiveAndEnabled) { return; } // A parent is inactive: it shows when the parent does.
            SetInteractable(true);
            Play(ShowPreset(), showStyle != null ? showStyle.Settings : showSettings, true);
        }

        /// <summary>Plays the exit, then deactivates the object (or blocks its input).</summary>
        public void Hide() {
            Initialize();
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
            _shown = visible;
            if (visible) {
                if (!gameObject.activeSelf) { gameObject.SetActive(true); }
                if (!_hasRest && gameObject.activeInHierarchy) { CaptureRest(); }
                if (_hasRest) { _rest.Apply(_resolved, MotionChannels.All); }
                SetInteractable(true);
            } else {
                ApplyHidden();
            }
        }

        // The rest pose is taken while the object is still exactly as designed.
        void Initialize() {
            if (_initialized) { return; }
            _initialized = true;
            _resolved = MotionTarget.Resolve(transform);
            if (gameObject.activeInHierarchy) { CaptureRest(); }
            if (startHidden) { SetVisible(false); }
        }

        void CaptureRest() {
            MotionLayout.RebuildParent(_resolved);
            _rest = MotionRest.Capture(_resolved);
            _hasRest = true;
        }

        void Play(MotionPreset preset, MotionSettings settings, bool showing) {
            if (!_hasRest) { CaptureRest(); }
            if (_current.IsAlive) { _current.Stop(); }
            // Undo whatever hid it (a half-played exit, a shrink); the entrance then sets its own start.
            if (showing) { _rest.Apply(_resolved, MotionChannels.All); }
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
        // invisible and unclickable: transparent with a Canvas Group, shrunk to nothing otherwise.
        void ApplyHidden() {
            if (deactivateWhenHidden) {
                if (_hasRest) { _rest.Apply(_resolved, _channels | MotionChannels.All); }
                gameObject.SetActive(false);
                return;
            }
            SetInteractable(false);
            if (_current.IsAlive) { return; }
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
    }
}
