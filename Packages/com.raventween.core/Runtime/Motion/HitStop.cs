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

        static Tween _slowDown;
        static Tween _slowTimer;
        static Tween _slowBack;
        static float _slowMotionRestore = 1f;
        static float _slowMotionScale;
        static float _slowMotionBack;
        static readonly System.Action SpeedBackUpAction = SpeedBackUp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSlowMotion() {
            _slowDown = default;
            _slowTimer = default;
            _slowBack = default;
            _slowMotionRestore = 1f;
            _slowMotionScale = 0f;
        }

        /// <summary>True while a slow motion holds the time scale down.</summary>
        public static bool IsInSlowMotion { get { return _slowDown.IsAlive || _slowTimer.IsAlive || _slowBack.IsAlive; } }

        /// <summary>
        /// Slows the game down smoothly to <paramref name="timeScale"/>, holds, then speeds back up,
        /// in <paramref name="duration"/> real seconds. A new slow motion replaces a running one and
        /// keeps the original speed; if the game changes the time scale meanwhile (a pause menu),
        /// the speed is not restored over it.
        /// </summary>
        public static void SlowMotion(float timeScale = 0.3f, float duration = 1f) {
            Debug.Assert(timeScale >= 0f && duration >= 0f, "Slow motion needs a non-negative time scale and duration.");
            bool running = IsInSlowMotion;
            _slowDown.Stop();
            _slowTimer.Stop();
            _slowBack.Stop();
            if (!running) { _slowMotionRestore = IsHitStopping ? 1f : Time.timeScale; }
            _slowMotionScale = Mathf.Max(timeScale, 0.0001f);
            float down = duration * 0.2f;
            _slowMotionBack = Mathf.Max(duration * 0.3f, 0.01f);
            // Separate root tweens: a sequence would write its children's end values again when it ends.
            _slowDown = GlobalTimeScale(_slowMotionScale, down);
            _slowTimer = Delay(Mathf.Max(duration - _slowMotionBack, 0f)).UnscaledTime().OnComplete(SpeedBackUpAction);
        }

        // Only if the time scale is still the one slow motion set: a pause keeps its pause.
        static void SpeedBackUp() {
            if (!Mathf.Approximately(Time.timeScale, _slowMotionScale)) { return; }
            _slowBack = GlobalTimeScale(_slowMotionRestore, _slowMotionBack);
        }
    }
}
