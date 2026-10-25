using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenTween {
    [AddComponentMenu("RavenTween/Raven Button Feel")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    [DisallowMultipleComponent]
    public sealed class RavenButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
        public enum Style : byte {
            Subtle = 0,
            Bouncy = 1,
            Juicy = 2,
            Custom = 3
        }

        [SerializeField] Style style = Style.Bouncy;
        [Tooltip("Scale under the pointer, relative to the rest size.")]
        [SerializeField, Range(0.8f, 1.5f)] float hoverScale = 1.06f;
        [Tooltip("Scale while pressed, relative to the rest size.")]
        [SerializeField, Range(0.6f, 1.2f)] float pressScale = 0.94f;
        [Tooltip("Seconds each change takes.")]
        [SerializeField, Min(0.01f)] float duration = 0.14f;
        [Tooltip("Keep reacting while the game is paused (pause menus).")]
        [SerializeField] bool playWhilePaused = true;

        Vector3 _rest;
        bool _hasRest;
        bool _hovered;
        bool _pressed;
        Tween _tween;
        Selectable _selectable;

        public Style FeelStyle { get { return style; } set { style = value; } }

        void Awake() {
            _selectable = GetComponent<Selectable>();
        }

        void OnDisable() {
            _hovered = false;
            _pressed = false;
            if (_tween.IsAlive) { _tween.Stop(); }
            if (_hasRest) { transform.localScale = _rest; }
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { _pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { _pressed = false; Refresh(); }

        void Refresh() {
            if (!_hasRest) { CaptureRest(); }
            bool interactable = _selectable == null || _selectable.IsInteractable();
            float factor = 1f;
            if (interactable && _pressed) { factor = PressScale(); }
            else if (interactable && _hovered) { factor = HoverScale(); }
            if (_tween.IsAlive) { _tween.Stop(); }
            Ease ease = _pressed ? Ease.OutQuad : Ease.OutBack;
            _tween = Raven.Scale(transform, _rest * factor, Duration()).Ease(ease).UnscaledTime(playWhilePaused);
        }

        void CaptureRest() {
            if (!RavenMotion.TryGetRestScale(gameObject, transform, out _rest)
                && !RavenShowHide.TryGetRestScale(gameObject, transform, out _rest)) {
                _rest = transform.localScale;
            }
            _hasRest = true;
        }

        float HoverScale() {
            switch (style) {
                case Style.Subtle: return 1.03f;
                case Style.Bouncy: return 1.06f;
                case Style.Juicy: return 1.12f;
                default: return hoverScale;
            }
        }

        float PressScale() {
            switch (style) {
                case Style.Subtle: return 0.97f;
                case Style.Bouncy: return 0.94f;
                case Style.Juicy: return 0.88f;
                default: return pressScale;
            }
        }

        float Duration() {
            switch (style) {
                case Style.Subtle: return 0.1f;
                case Style.Bouncy: return 0.14f;
                case Style.Juicy: return 0.2f;
                default: return duration;
            }
        }
    }
}
