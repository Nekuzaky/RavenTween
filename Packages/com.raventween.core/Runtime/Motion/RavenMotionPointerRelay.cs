using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenTween {
    /// <summary>
    /// Forwards pointer events to the no-code components of its GameObject (Raven Motion, Raven
    /// Feedback…). Added on demand, only to objects that use a pointer trigger: an event handler
    /// stops pointer events from reaching parent objects, so it must not sit where it isn't needed.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenMotionPointerRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
        static readonly List<IMotionTriggerable> Listeners = new List<IMotionTriggerable>(4);

        void Awake() {
            hideFlags = HideFlags.HideInInspector;
        }

        public void OnPointerEnter(PointerEventData eventData) { Forward(MotionTrigger.OnPointerEnter); }
        public void OnPointerExit(PointerEventData eventData) { Forward(MotionTrigger.OnPointerExit); }
        public void OnPointerDown(PointerEventData eventData) { Forward(MotionTrigger.OnPointerDown); }
        public void OnPointerUp(PointerEventData eventData) { Forward(MotionTrigger.OnPointerUp); }
        public void OnPointerClick(PointerEventData eventData) { Forward(MotionTrigger.OnClick); }

        // A listener may run user code (a feedback's UnityEvent can trigger another pointer
        // event), so it works on a copy rather than the shared list.
        void Forward(MotionTrigger trigger) {
            GetComponents(Listeners);
            IMotionTriggerable[] copy = Listeners.ToArray();
            Listeners.Clear();
            for (int i = 0; i < copy.Length; i++) {
                var behaviour = copy[i] as Behaviour;
                if (behaviour != null && behaviour.isActiveAndEnabled) { copy[i].HandleTrigger(trigger); }
            }
        }
    }
}
