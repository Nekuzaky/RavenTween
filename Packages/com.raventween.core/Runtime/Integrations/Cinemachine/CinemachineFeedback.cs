using UnityEngine;
#if RAVEN_CINEMACHINE3
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

namespace RavenTween {
    public static class CinemachineFeedback {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            FeedbackHooks.CameraImpulse = Impulse;
        }

        static void Impulse(FeedbackAction action, float power) {
            var source = action.effect as CinemachineImpulseSource;
            if (source == null && action.effect != null) { source = action.effect.GetComponent<CinemachineImpulseSource>(); }
            if (source == null) {
                Debug.LogWarning("RavenTween: Camera Impulse needs a Cinemachine Impulse Source in its Impulse Source field.");
                return;
            }
            source.GenerateImpulseWithForce(power);
        }
    }
}
