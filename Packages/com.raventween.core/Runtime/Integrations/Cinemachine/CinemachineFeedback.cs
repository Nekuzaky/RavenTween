using UnityEngine;
#if RAVEN_CINEMACHINE3
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

namespace RavenTween {
    /// <summary>
    /// Serves the Camera Impulse action of Raven Feedback: the impulse source fires with the
    /// action's strength, and every Cinemachine camera with an Impulse Listener reacts (works
    /// with Cinemachine 2 and 3).
    /// </summary>
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
