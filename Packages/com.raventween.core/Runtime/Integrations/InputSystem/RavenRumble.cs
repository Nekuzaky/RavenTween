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
        static readonly Action TimerEndAction = TimerEnd;
        static Tween _timer;
        static Gamepad _pad;
        static float _low;
        static float _high;
        static bool _replacing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            _timer = default;
            _pad = null;
            _low = 0f;
            _high = 0f;
            _replacing = false;
            FeedbackHooks.Rumble = (action, power) => Play(action.lowMotor * power, action.highMotor * power, action.duration);
        }

        /// <summary>Vibrates the current gamepad, if any, for <paramref name="duration"/> real seconds.</summary>
        public static void Play(float lowFrequency, float highFrequency, float duration) {
            Gamepad pad = Gamepad.current;
            if (pad == null || duration <= 0f) { return; }
            bool running = _timer.IsAlive && _pad == pad;
            float remaining = duration;
            if (_timer.IsAlive) {
                if (running) { remaining = Mathf.Max(_timer.DurationTotal - _timer.ElapsedTimeTotal, duration); }
                // Replacing the timer must not stop the vibration it is about to extend.
                _replacing = true;
                _timer.Stop();
                _replacing = false;
            }
            // The player switched controllers: the previous one must not keep vibrating.
            if (_pad != null && _pad != pad) { _pad.ResetHaptics(); }
            _pad = pad;
            _low = Mathf.Clamp01(running ? Mathf.Max(_low, lowFrequency) : lowFrequency);
            _high = Mathf.Clamp01(running ? Mathf.Max(_high, highFrequency) : highFrequency);
            pad.SetMotorSpeeds(_low, _high);
            _timer = Raven.Delay(remaining).UnscaledTime().OnComplete(TimerEndAction).OnKill(TimerEndAction);
        }

        /// <summary>Stops any vibration at once.</summary>
        public static void Stop() {
            if (_timer.IsAlive) {
                _replacing = true;
                _timer.Stop();
                _replacing = false;
            }
            Silence();
        }

        static void TimerEnd() {
            if (!_replacing) { Silence(); }
        }

        static void Silence() {
            _low = 0f;
            _high = 0f;
            if (_pad != null) { _pad.ResetHaptics(); }
            _pad = null;
        }
    }
}
