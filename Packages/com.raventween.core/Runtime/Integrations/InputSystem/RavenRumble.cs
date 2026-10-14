using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenTween {
    /// <summary>
    /// Gamepad vibration for a moment: <c>RavenRumble.Play(0.6f, 0.3f, 0.2f)</c>. Overlapping
    /// rumbles use the strongest motors and stop when the last one ends. Timed in real time, so a
    /// hit stop does not stretch it. Also serves the Rumble action of Raven Feedback.
    /// </summary>
    public static class RavenRumble {
        static readonly Action StopAction = Stop;
        static Tween _timer;
        static float _low;
        static float _high;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            _timer = default;
            _low = 0f;
            _high = 0f;
            FeedbackHooks.Rumble = (action, power) => Play(action.lowMotor * power, action.highMotor * power, action.duration);
        }

        /// <summary>Vibrates the current gamepad, if any, for <paramref name="duration"/> real seconds.</summary>
        public static void Play(float lowFrequency, float highFrequency, float duration) {
            Gamepad pad = Gamepad.current;
            if (pad == null || duration <= 0f) { return; }
            bool running = _timer.IsAlive;
            _low = Mathf.Clamp01(running ? Mathf.Max(_low, lowFrequency) : lowFrequency);
            _high = Mathf.Clamp01(running ? Mathf.Max(_high, highFrequency) : highFrequency);
            pad.SetMotorSpeeds(_low, _high);
            float remaining = running ? Mathf.Max(_timer.DurationTotal - _timer.ElapsedTimeTotal, duration) : duration;
            _timer.Stop();
            _timer = Raven.Delay(remaining).UnscaledTime().OnComplete(StopAction).OnKill(StopAction);
        }

        /// <summary>Stops any vibration at once.</summary>
        public static void Stop() {
            _low = 0f;
            _high = 0f;
            Gamepad pad = Gamepad.current;
            if (pad != null) { pad.ResetHaptics(); }
        }
    }
}
