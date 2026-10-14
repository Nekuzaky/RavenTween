using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>
        /// Freezes the game for an instant, the classic heavy-hit effect: <c>Time.timeScale</c>
        /// drops to <paramref name="timeScale"/> for <paramref name="duration"/> real seconds, then
        /// comes back. Overlapping hit stops extend the freeze and restore the original time scale
        /// once. The countdown runs in real time inside the engine, whatever the update phase,
        /// <see cref="TimeScale"/> or stopped tweens, so the game can never stay frozen.
        /// </summary>
        public static void HitStop(float timeScale = 0.05f, float duration = 0.08f) {
            TweenEngine.BeginHitStop(timeScale, duration);
        }

        /// <summary>True while a hit stop holds the time scale.</summary>
        public static bool IsHitStopping { get { return TweenEngine.HitStopActive; } }

        /// <summary>Ends a running hit stop at once and restores the time scale.</summary>
        public static void EndHitStop() {
            TweenEngine.EndHitStop();
        }
    }
}
