using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenTween {
    /// <summary>
    /// Forwards pointer events to the Raven Motions of its GameObject. Added on demand, only to
    /// objects that use a pointer trigger: an event handler stops pointer events from reaching
    /// parent objects, so it must not sit on objects that don't need it.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenMotionPointerRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
        static readonly List<RavenMotion> Motions = new List<RavenMotion>(4);

        void Awake() {
            hideFlags = HideFlags.HideInInspector;
        }

        public void OnPointerEnter(PointerEventData eventData) { Forward(MotionTrigger.OnPointerEnter); }
        public void OnPointerExit(PointerEventData eventData) { Forward(MotionTrigger.OnPointerExit); }
        public void OnPointerDown(PointerEventData eventData) { Forward(MotionTrigger.OnPointerDown); }
        public void OnPointerUp(PointerEventData eventData) { Forward(MotionTrigger.OnPointerUp); }
        public void OnPointerClick(PointerEventData eventData) { Forward(MotionTrigger.OnClick); }

        void Forward(MotionTrigger trigger) {
            GetComponents(Motions);
            // Play runs no user code (OnComplete fires later), so the shared list cannot be re-entered.
            int count = Motions.Count;
            for (int i = 0; i < count; i++) {
                RavenMotion motion = Motions[i];
                if (motion != null && motion.isActiveAndEnabled) { motion.HandleTrigger(trigger); }
            }
            Motions.Clear();
        }
    }
}
