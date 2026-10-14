using System;
using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        static readonly Action EndHitStopAction = EndHitStop;
        static int _hitStops;
        static float _hitStopRestore = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHitStop() {
            _hitStops = 0;
            _hitStopRestore = 1f;
        }

        /// <summary>
        /// Freezes the game for an instant, the classic heavy-hit effect: <c>Time.timeScale</c>
        /// drops to <paramref name="timeScale"/> for <paramref name="duration"/> real seconds, then
        /// comes back. Overlapping hit stops restore the original time scale only once the last
        /// one ends, and it is restored even if the tweens are stopped meanwhile.
        /// </summary>
        public static Tween HitStop(float timeScale = 0.05f, float duration = 0.08f) {
            Debug.Assert(timeScale >= 0f, "A hit stop's time scale cannot be negative.");
            Debug.Assert(duration >= 0f, "A hit stop's duration cannot be negative.");
            if (_hitStops == 0) { _hitStopRestore = Time.timeScale; }
            _hitStops++;
            Time.timeScale = Mathf.Max(timeScale, 0f);
            return Delay(Mathf.Max(duration, 0f)).UnscaledTime().OnComplete(EndHitStopAction).OnKill(EndHitStopAction);
        }

        static void EndHitStop() {
            if (_hitStops <= 0) { return; }
            _hitStops--;
            if (_hitStops == 0) { Time.timeScale = _hitStopRestore; }
        }
    }
}
