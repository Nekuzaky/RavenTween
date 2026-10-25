using System;
using UnityEngine;

namespace RavenTween {
    public enum MotionPreset : byte {
        None = 0,

        FadeIn = 10,
        PopIn = 11,
        ZoomIn = 12,
        SlideInLeft = 13,
        SlideInRight = 14,
        SlideInUp = 15,
        SlideInDown = 16,
        DropIn = 17,
        RiseIn = 18,
        SpinIn = 19,
        FlipIn = 20,
        ElasticIn = 21,

        FadeOut = 40,
        PopOut = 41,
        ZoomOut = 42,
        SlideOutLeft = 43,
        SlideOutRight = 44,
        SlideOutUp = 45,
        SlideOutDown = 46,
        FallOut = 47,
        SinkOut = 48,
        SpinOut = 49,
        FlipOut = 50,

        Pulse = 70,
        Bounce = 71,
        Shake = 72,
        Wobble = 73,
        Jello = 74,
        Heartbeat = 75,
        Tada = 76,
        Flash = 77,
        Swing = 78,
        Squash = 79,
        Nod = 80,
        HeadShake = 81,
        Tint = 82,

        Float = 100,
        Breathe = 101,
        Spin = 102,
        Sway = 103,
        Rock = 104,
        Blink = 105,
        Throb = 106,
        Wiggle = 107
    }

    public enum MotionCategory : byte {
        Entrance = 0,
        Exit = 1,
        Attention = 2,
        Loop = 3
    }

    public enum MotionTrigger : byte {
        OnEnable = 0,
        OnStart = 1,
        OnClick = 2,
        OnPointerEnter = 3,
        OnPointerExit = 4,
        OnPointerDown = 5,
        OnPointerUp = 6,
        OnTriggerEnter = 7,
        OnTriggerExit = 8,
        OnCollisionEnter = 9,
        Manual = 10
    }

    public enum MotionFinish : byte {
        Nothing = 0,
        Deactivate = 1,
        Destroy = 2
    }

    [Serializable]
    public struct MotionSettings {
        [Tooltip("Length in seconds. 0 = the preset's own timing.")]
        [Min(0f)] public float duration;
        [Tooltip("How strong the motion is. 1 = as designed, 0.5 = subtle, 2 = exaggerated.")]
        [Range(0.1f, 3f)] public float intensity;
        [Tooltip("Seconds to wait before starting.")]
        [Min(0f)] public float delay;
        [Tooltip("Travel distance for slides and drops: pixels for UI, units in a scene. 0 = automatic, from the object's size.")]
        [Min(0f)] public float distance;
        [Tooltip("How many times an attention motion plays in a row.")]
        [Min(1)] public int repeat;
        [Tooltip("Keep playing while the game is paused (Time.timeScale = 0).")]
        public bool useUnscaledTime;
        [Tooltip("Color of the Tint motion (damage red, warning yellow, highlight white).")]
        public Color color;

        public static readonly Color DefaultTint = new Color(1f, 0.22f, 0.2f, 1f);

        public static MotionSettings Default {
            get { return new MotionSettings { duration = 0f, intensity = 1f, delay = 0f, distance = 0f, repeat = 1, useUnscaledTime = false, color = DefaultTint }; }
        }
    }

    public static class MotionLibrary {
        public static readonly MotionPreset[] All = {
            MotionPreset.FadeIn, MotionPreset.PopIn, MotionPreset.ZoomIn, MotionPreset.SlideInLeft, MotionPreset.SlideInRight,
            MotionPreset.SlideInUp, MotionPreset.SlideInDown, MotionPreset.DropIn, MotionPreset.RiseIn, MotionPreset.SpinIn,
            MotionPreset.FlipIn, MotionPreset.ElasticIn,
            MotionPreset.FadeOut, MotionPreset.PopOut, MotionPreset.ZoomOut, MotionPreset.SlideOutLeft, MotionPreset.SlideOutRight,
            MotionPreset.SlideOutUp, MotionPreset.SlideOutDown, MotionPreset.FallOut, MotionPreset.SinkOut, MotionPreset.SpinOut,
            MotionPreset.FlipOut,
            MotionPreset.Pulse, MotionPreset.Bounce, MotionPreset.Shake, MotionPreset.Wobble, MotionPreset.Jello,
            MotionPreset.Heartbeat, MotionPreset.Tada, MotionPreset.Flash, MotionPreset.Swing, MotionPreset.Squash,
            MotionPreset.Nod, MotionPreset.HeadShake, MotionPreset.Tint,
            MotionPreset.Float, MotionPreset.Breathe, MotionPreset.Spin, MotionPreset.Sway, MotionPreset.Rock,
            MotionPreset.Blink, MotionPreset.Throb, MotionPreset.Wiggle
        };

        public static MotionCategory Category(MotionPreset preset) {
            int value = (int)preset;
            if (value >= 100) { return MotionCategory.Loop; }
            if (value >= 70) { return MotionCategory.Attention; }
            if (value >= 40) { return MotionCategory.Exit; }
            return MotionCategory.Entrance;
        }

        public static float DefaultDuration(MotionPreset preset) {
            switch (preset) {
                case MotionPreset.FadeIn: case MotionPreset.FadeOut: return 0.35f;
                case MotionPreset.PopIn: case MotionPreset.PopOut: return 0.4f;
                case MotionPreset.DropIn: return 0.7f;
                case MotionPreset.ElasticIn: return 0.8f;
                case MotionPreset.SpinIn: case MotionPreset.SpinOut: return 0.55f;
                case MotionPreset.Shake: case MotionPreset.HeadShake: return 0.5f;
                case MotionPreset.Heartbeat: return 0.8f;
                case MotionPreset.Tada: case MotionPreset.Wobble: case MotionPreset.Jello: return 0.8f;
                case MotionPreset.Flash: return 0.6f;
                case MotionPreset.Float: case MotionPreset.Sway: case MotionPreset.Rock: return 2.4f;
                case MotionPreset.Breathe: return 3f;
                case MotionPreset.Spin: return 2f;
                case MotionPreset.Blink: case MotionPreset.Throb: return 1.2f;
                case MotionPreset.Wiggle: return 0.5f;
                default: return 0.5f;
            }
        }

        public static bool NeedsAlpha(MotionPreset preset) {
            return preset == MotionPreset.FadeIn || preset == MotionPreset.FadeOut
                || preset == MotionPreset.Flash || preset == MotionPreset.Blink;
        }

        public static bool NeedsColor(MotionPreset preset) {
            return preset == MotionPreset.Tint;
        }

        public static bool MovesPosition(MotionPreset preset) {
            switch (preset) {
                case MotionPreset.SlideInLeft: case MotionPreset.SlideInRight: case MotionPreset.SlideInUp: case MotionPreset.SlideInDown:
                case MotionPreset.DropIn: case MotionPreset.RiseIn:
                case MotionPreset.SlideOutLeft: case MotionPreset.SlideOutRight: case MotionPreset.SlideOutUp: case MotionPreset.SlideOutDown:
                case MotionPreset.FallOut: case MotionPreset.SinkOut:
                case MotionPreset.Bounce: case MotionPreset.Shake: case MotionPreset.Wobble: case MotionPreset.Nod: case MotionPreset.HeadShake:
                case MotionPreset.Float: case MotionPreset.Sway:
                    return true;
                default:
                    return false;
            }
        }

        public static bool UsesDistance(MotionPreset preset) {
            switch (preset) {
                case MotionPreset.SlideInLeft: case MotionPreset.SlideInRight: case MotionPreset.SlideInUp: case MotionPreset.SlideInDown:
                case MotionPreset.DropIn: case MotionPreset.RiseIn:
                case MotionPreset.SlideOutLeft: case MotionPreset.SlideOutRight: case MotionPreset.SlideOutUp: case MotionPreset.SlideOutDown:
                case MotionPreset.FallOut: case MotionPreset.SinkOut:
                    return true;
                default:
                    return false;
            }
        }

        public static string DisplayName(MotionPreset preset) {
            string raw = preset.ToString();
            var builder = new System.Text.StringBuilder(raw.Length + 4);
            for (int i = 0; i < raw.Length; i++) {
                if (i > 0 && char.IsUpper(raw[i])) { builder.Append(' '); }
                builder.Append(raw[i]);
            }
            return builder.ToString();
        }

        public static string Description(MotionPreset preset) {
            switch (preset) {
                case MotionPreset.FadeIn: return "Fades in from fully transparent.";
                case MotionPreset.PopIn: return "Grows from nothing with a small overshoot. Great for buttons, icons and rewards.";
                case MotionPreset.ZoomIn: return "Scales up from half size while fading in. Calm and modern.";
                case MotionPreset.SlideInLeft: return "Slides in from the left while fading in.";
                case MotionPreset.SlideInRight: return "Slides in from the right while fading in.";
                case MotionPreset.SlideInUp: return "Slides up from below while fading in.";
                case MotionPreset.SlideInDown: return "Slides down from above while fading in.";
                case MotionPreset.DropIn: return "Falls from above and bounces on landing.";
                case MotionPreset.RiseIn: return "Rises a little while fading in. Subtle, good for text and lists.";
                case MotionPreset.SpinIn: return "Spins half a turn while growing from nothing.";
                case MotionPreset.FlipIn: return "Flips in like a card turning over.";
                case MotionPreset.ElasticIn: return "Springs into place with a wobbly, elastic overshoot.";
                case MotionPreset.FadeOut: return "Fades out to fully transparent.";
                case MotionPreset.PopOut: return "Pulls back slightly, then shrinks to nothing.";
                case MotionPreset.ZoomOut: return "Shrinks to half size while fading out.";
                case MotionPreset.SlideOutLeft: return "Slides out to the left while fading out.";
                case MotionPreset.SlideOutRight: return "Slides out to the right while fading out.";
                case MotionPreset.SlideOutUp: return "Slides out upwards while fading out.";
                case MotionPreset.SlideOutDown: return "Slides out downwards while fading out.";
                case MotionPreset.FallOut: return "Falls down and fades out, as if dropped.";
                case MotionPreset.SinkOut: return "Sinks a little while fading out. Subtle.";
                case MotionPreset.SpinOut: return "Spins half a turn while shrinking to nothing.";
                case MotionPreset.FlipOut: return "Flips out like a card turning away.";
                case MotionPreset.Pulse: return "Swells once and settles. A gentle 'look here'.";
                case MotionPreset.Bounce: return "Hops up and lands with a little squash.";
                case MotionPreset.Shake: return "Shakes sideways. Errors, wrong answers, locked doors.";
                case MotionPreset.Wobble: return "Sways and tilts from side to side, losing energy.";
                case MotionPreset.Jello: return "Squishes like jelly, stretching wide then tall.";
                case MotionPreset.Heartbeat: return "Two quick beats, like a heart.";
                case MotionPreset.Tada: return "Shrinks, then grows with a wiggle. Celebrations and rewards.";
                case MotionPreset.Flash: return "Blinks out and back twice.";
                case MotionPreset.Swing: return "Swings like a sign hanging from a nail.";
                case MotionPreset.Squash: return "Squashes flat and springs back. Impacts and presses.";
                case MotionPreset.Nod: return "Bobs down twice, like nodding 'yes'.";
                case MotionPreset.HeadShake: return "Turns side to side, like shaking 'no'.";
                case MotionPreset.Tint: return "Flashes a color and fades back. Damage, warnings, highlights.";
                case MotionPreset.Float: return "Drifts gently up and down. Pickups, idle objects, menus.";
                case MotionPreset.Breathe: return "Slowly grows and shrinks a little, as if breathing.";
                case MotionPreset.Spin: return "Turns round and round at a steady speed.";
                case MotionPreset.Sway: return "Drifts left and right.";
                case MotionPreset.Rock: return "Tilts left and right like a boat.";
                case MotionPreset.Blink: return "Fades down and back up, over and over.";
                case MotionPreset.Throb: return "Pulses in size, over and over. 'Press me!'";
                case MotionPreset.Wiggle: return "A quick, nervous little wiggle, over and over.";
                default: return "";
            }
        }
    }
}
