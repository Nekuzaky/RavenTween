using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenTween {
    public abstract class RavenPointerRelay : MonoBehaviour {
        static readonly List<IMotionTriggerable> Listeners = new List<IMotionTriggerable>(4);

        void Awake() {
            hideFlags = HideFlags.HideInInspector;
        }

        protected void Forward(MotionTrigger trigger) {
            GetComponents(Listeners);
            IMotionTriggerable[] copy = Listeners.ToArray();
            Listeners.Clear();
            for (int i = 0; i < copy.Length; i++) {
                var behaviour = copy[i] as Behaviour;
                if (behaviour != null && behaviour.isActiveAndEnabled) { copy[i].HandleTrigger(trigger); }
            }
        }
    }

    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenHoverRelay : RavenPointerRelay, IPointerEnterHandler, IPointerExitHandler {
        public void OnPointerEnter(PointerEventData eventData) { Forward(MotionTrigger.OnPointerEnter); }
        public void OnPointerExit(PointerEventData eventData) { Forward(MotionTrigger.OnPointerExit); }
    }

    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenPressRelay : RavenPointerRelay, IPointerDownHandler, IPointerUpHandler {
        public void OnPointerDown(PointerEventData eventData) { Forward(MotionTrigger.OnPointerDown); }
        public void OnPointerUp(PointerEventData eventData) { Forward(MotionTrigger.OnPointerUp); }
    }

    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenClickRelay : RavenPointerRelay, IPointerClickHandler {
        public void OnPointerClick(PointerEventData eventData) { Forward(MotionTrigger.OnClick); }
    }
}
