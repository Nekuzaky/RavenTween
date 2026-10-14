using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenTween {
    /// <summary>
    /// Forwards pointer events to the no-code components of its GameObject (Raven Motion, Raven
    /// Feedback…). The input module sends each event to the first object up the hierarchy that
    /// handles it, so a handler would steal it from parent objects: there is one relay per kind
    /// of event, added on demand, so hovering an icon never stops its parent Button's clicks.
    /// </summary>
    public abstract class RavenPointerRelay : MonoBehaviour {
        static readonly List<IMotionTriggerable> Listeners = new List<IMotionTriggerable>(4);

        void Awake() {
            hideFlags = HideFlags.HideInInspector;
        }

        // A listener may run user code that raises another pointer event, so it works on a copy.
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

    /// <summary>Hover events (enter, leave).</summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenHoverRelay : RavenPointerRelay, IPointerEnterHandler, IPointerExitHandler {
        public void OnPointerEnter(PointerEventData eventData) { Forward(MotionTrigger.OnPointerEnter); }
        public void OnPointerExit(PointerEventData eventData) { Forward(MotionTrigger.OnPointerExit); }
    }

    /// <summary>Press events (down, up).</summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenPressRelay : RavenPointerRelay, IPointerDownHandler, IPointerUpHandler {
        public void OnPointerDown(PointerEventData eventData) { Forward(MotionTrigger.OnPointerDown); }
        public void OnPointerUp(PointerEventData eventData) { Forward(MotionTrigger.OnPointerUp); }
    }

    /// <summary>Click events.</summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class RavenClickRelay : RavenPointerRelay, IPointerClickHandler {
        public void OnPointerClick(PointerEventData eventData) { Forward(MotionTrigger.OnClick); }
    }
}
