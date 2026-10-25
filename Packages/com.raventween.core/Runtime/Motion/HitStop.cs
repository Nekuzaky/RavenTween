using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        public static void HitStop(float timeScale = 0.05f, float duration = 0.08f) {
            TweenEngine.BeginHitStop(timeScale, duration);
        }

        public static bool IsHitStopping { get { return TweenEngine.HitStopActive; } }

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

        public static bool IsInSlowMotion { get { return _slowDown.IsAlive || _slowTimer.IsAlive || _slowBack.IsAlive; } }

        public static void SlowMotion(float timeScale = 0.3f, float duration = 1f) {
            Debug.Assert(timeScale >= 0f && duration >= 0f, "Slow motion needs a non-negative time scale and duration.");
            float scale = Mathf.Max(timeScale, 0.0001f);
            if (!IsHitStopping && !IsInSlowMotion && Time.timeScale < scale) { return; }
            bool running = IsInSlowMotion;
            _slowDown.Stop();
            _slowTimer.Stop();
            _slowBack.Stop();
            if (!running) { _slowMotionRestore = IsHitStopping ? TweenEngine.TimeScaleBeforeHitStop : Time.timeScale; }
            _slowMotionScale = scale;
            float down = duration * 0.2f;
            _slowMotionBack = Mathf.Max(duration * 0.3f, 0.01f);
            _slowDown = GlobalTimeScale(_slowMotionScale, down);
            _slowTimer = Delay(Mathf.Max(duration - _slowMotionBack, 0f)).UnscaledTime().OnComplete(SpeedBackUpAction);
        }

        static void SpeedBackUp() {
            if (!Mathf.Approximately(Time.timeScale, _slowMotionScale)) { return; }
            _slowBack = GlobalTimeScale(_slowMotionRestore, _slowMotionBack);
        }
    }
}
