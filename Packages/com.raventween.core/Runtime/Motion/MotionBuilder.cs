using System.Collections.Generic;
using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        public static Sequence Motion(Component target, MotionPreset preset) {
            return Motion(target, preset, MotionSettings.Default);
        }

        public static Sequence Motion(Component target, MotionPreset preset, MotionSettings settings) {
            Debug.Assert(target != null, "Motion needs a live target.");
            if (target == null) {
                Debug.LogError("RavenTween: cannot play a motion on a null or destroyed object.");
                return default;
            }
            return MotionBuilder.PlayOn(target.transform, preset, settings);
        }

        public static Sequence Motion(in MotionTarget target, MotionPreset preset, MotionSettings settings, in MotionRest rest) {
            return MotionBuilder.Build(target, preset, settings, rest, out _);
        }
    }

    public static class MotionTweens {
        public static Sequence PlayMotion(this Component target, MotionPreset preset) {
            return Raven.Motion(target, preset);
        }

        public static Sequence PlayMotion(this Component target, MotionPreset preset, MotionSettings settings) {
            return Raven.Motion(target, preset, settings);
        }
    }

    static class MotionBuilder {
        struct Running {
            public Sequence Motion;
            public MotionRest Rest;
            public MotionChannels Channels;
            public bool Exit;
        }

        static readonly Dictionary<Transform, Running> ByObject = new Dictionary<Transform, Running>(32);
        static readonly List<Transform> Finished = new List<Transform>(32);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRunning() {
            ByObject.Clear();
            Finished.Clear();
        }

        public static Sequence PlayOn(Transform transform, MotionPreset preset, MotionSettings settings) {
            MotionTarget resolved = MotionTarget.Resolve(transform);
            MotionRest rest;
            bool known = ByObject.TryGetValue(transform, out Running running);
            if (known && running.Motion.IsAlive) {
                running.Motion.Stop();
                rest = running.Rest;
            } else if (known && running.Exit) {
                rest = running.Rest;
            } else {
                rest = MotionRest.Capture(resolved);
            }
            Sequence motion = Build(resolved, preset, settings, rest, out MotionChannels channels);
            if (ByObject.Count >= 64) { DropFinished(); }
            ByObject[transform] = new Running {
                Motion = motion, Rest = rest, Channels = channels,
                Exit = MotionLibrary.Category(preset) == MotionCategory.Exit
            };
            return motion;
        }

        static void DropFinished() {
            foreach (KeyValuePair<Transform, Running> entry in ByObject) {
                bool keep = entry.Value.Motion.IsAlive || (entry.Value.Exit && entry.Key != null);
                if (!keep) { Finished.Add(entry.Key); }
            }
            for (int i = 0; i < Finished.Count; i++) { ByObject.Remove(Finished[i]); }
            Finished.Clear();
        }

        static readonly float[] Even5 = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        static readonly float[] Half3 = { 0f, 0.5f, 1f };
        static readonly Vector3[] UpDownLoop = { Vector3.zero, Vector3.up, Vector3.zero, Vector3.down, Vector3.zero };
        static readonly Vector3[] SideLoop = { Vector3.zero, Vector3.right, Vector3.zero, Vector3.left, Vector3.zero };
        static readonly float[] ShakeTimes = { 0f, 0.1f, 0.25f, 0.4f, 0.55f, 0.7f, 0.85f, 1f };
        static readonly Vector3[] ShakeKeys = {
            Vector3.zero, new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f),
            new Vector3(0.8f, 0f, 0f), new Vector3(-0.5f, 0f, 0f), new Vector3(0.3f, 0f, 0f), Vector3.zero
        };
        static readonly float[] WobbleTimes = { 0f, 0.15f, 0.3f, 0.45f, 0.6f, 0.75f, 1f };
        static readonly Vector3[] WobbleMove = {
            Vector3.zero, new Vector3(-0.25f, 0f, 0f), new Vector3(0.2f, 0f, 0f), new Vector3(-0.15f, 0f, 0f),
            new Vector3(0.1f, 0f, 0f), new Vector3(-0.05f, 0f, 0f), Vector3.zero
        };
        static readonly Vector3[] WobbleTilt = {
            Vector3.zero, new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, 3f),
            new Vector3(0f, 0f, -2f), new Vector3(0f, 0f, 1f), Vector3.zero
        };
        static readonly float[] JelloTimes = { 0f, 0.3f, 0.4f, 0.5f, 0.65f, 0.75f, 1f };
        static readonly Vector3[] JelloScale = {
            Vector3.one, new Vector3(1.25f, 0.75f, 1f), new Vector3(0.75f, 1.25f, 1f), new Vector3(1.15f, 0.85f, 1f),
            new Vector3(0.95f, 1.05f, 1f), new Vector3(1.05f, 0.95f, 1f), Vector3.one
        };
        static readonly float[] HeartTimes = { 0f, 0.14f, 0.28f, 0.42f, 0.7f, 1f };
        static readonly Vector3[] HeartScale = {
            Vector3.one, Vector3.one * 1.2f, Vector3.one, Vector3.one * 1.2f, Vector3.one, Vector3.one
        };
        static readonly float[] TadaTimes = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
        static readonly Vector3[] TadaScale = {
            Vector3.one, Vector3.one * 0.9f, Vector3.one * 0.9f, Vector3.one * 1.1f, Vector3.one * 1.1f, Vector3.one * 1.1f,
            Vector3.one * 1.1f, Vector3.one * 1.1f, Vector3.one * 1.1f, Vector3.one * 1.1f, Vector3.one
        };
        static readonly Vector3[] TadaTilt = {
            Vector3.zero, new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f),
            new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f),
            new Vector3(0f, 0f, 3f), Vector3.zero
        };
        static readonly float[] SwingTimes = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        static readonly Vector3[] SwingTilt = {
            Vector3.zero, new Vector3(0f, 0f, 15f), new Vector3(0f, 0f, -10f), new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, -5f), Vector3.zero
        };
        static readonly float[] HeadShakeTimes = { 0f, 0.13f, 0.37f, 0.63f, 0.87f, 1f };
        static readonly Vector3[] HeadShakeTurn = {
            Vector3.zero, new Vector3(0f, -9f, 0f), new Vector3(0f, 7f, 0f), new Vector3(0f, -5f, 0f), new Vector3(0f, 3f, 0f), Vector3.zero
        };
        static readonly Vector3[] HeadShakeMove = {
            Vector3.zero, new Vector3(-0.06f, 0f, 0f), new Vector3(0.05f, 0f, 0f), new Vector3(-0.03f, 0f, 0f), new Vector3(0.02f, 0f, 0f), Vector3.zero
        };
        static readonly Vector3[] NodMove = { Vector3.zero, Vector3.down, Vector3.zero, Vector3.down, Vector3.zero };
        static readonly float[] WiggleTimes = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        static readonly Vector3[] WiggleTilt = {
            Vector3.zero, new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, -4f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f), Vector3.zero
        };
        static readonly float[] FlashTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        static readonly Vector3[] FlashAlpha = { Vector3.one, Vector3.zero, Vector3.one, Vector3.zero, Vector3.one };

        enum Channel : byte { Position, Rotation, Scale, Alpha }

        struct Context {
            public MotionTarget Target;
            public MotionRest Rest;
            public Sequence Sequence;
            public float Duration;
            public float Intensity;
            public float Distance;
            public Color Tint;
            public MotionChannels Channels;
            public MotionChannels StartChannels;
            public MotionRest Start;
            public bool MissedFade;
            public float MissedFrom;
            public float MissedTo;
            public float MissedStart;
            public float MissedLength;
            public Ease MissedEase;
        }

        public static Sequence Build(in MotionTarget target, MotionPreset preset, MotionSettings settings, in MotionRest rest, out MotionChannels channels) {
            channels = MotionChannels.None;
            if (!target.IsValid || preset == MotionPreset.None) { return default; }
            var c = new Context {
                Target = target,
                Rest = rest,
                Sequence = Raven.Sequence(),
                Duration = settings.duration > 0f ? settings.duration : MotionLibrary.DefaultDuration(preset),
                Intensity = Mathf.Clamp(settings.intensity <= 0f ? 1f : settings.intensity, 0.05f, 5f),
                Start = rest
            };
            c.Distance = (settings.distance > 0f ? settings.distance : target.AutoDistance(rest)) * c.Intensity;
            c.Tint = settings.color.a > 0f ? settings.color : MotionSettings.DefaultTint;
            MotionCategory category = MotionLibrary.Category(preset);
            Compose(ref c, preset);
            ReplaceMissedFade(ref c, category);
            if (category == MotionCategory.Entrance) { c.Start.Apply(target, c.StartChannels); }
            else if (category == MotionCategory.Exit) { c.Rest.Apply(target, c.Channels); }
            ApplyTiming(c.Sequence, category, settings);
            channels = c.Channels;
            return c.Sequence;
        }

        static void ApplyTiming(Sequence sequence, MotionCategory category, in MotionSettings settings) {
            if (settings.delay > 0f) { sequence.Delay(settings.delay); }
            if (settings.useUnscaledTime) { sequence.UnscaledTime(); }
            if (category == MotionCategory.Loop) { sequence.Infinite(CycleMode.Restart); }
            else if (category == MotionCategory.Attention && settings.repeat > 1) { sequence.Cycles(settings.repeat, CycleMode.Restart); }
        }

        static void Compose(ref Context c, MotionPreset preset) {
            switch (MotionLibrary.Category(preset)) {
                case MotionCategory.Entrance: ComposeEntrance(ref c, preset); break;
                case MotionCategory.Exit: ComposeExit(ref c, preset); break;
                case MotionCategory.Attention: ComposeAttention(ref c, preset); break;
                default: ComposeLoop(ref c, preset); break;
            }
        }

        static void ReplaceMissedFade(ref Context c, MotionCategory category) {
            if (!c.MissedFade || (c.Channels & MotionChannels.Scale) != 0) { return; }
            if (category != MotionCategory.Entrance && category != MotionCategory.Exit) { return; }
            Scale(ref c, Vector3.one * c.MissedFrom, Vector3.one * c.MissedTo, c.MissedStart, c.MissedLength, c.MissedEase);
        }

        static void ComposeEntrance(ref Context c, MotionPreset preset) {
            float d = c.Duration;
            float hide = Mathf.Clamp01(c.Intensity);
            switch (preset) {
                case MotionPreset.FadeIn:
                    if (c.Target.HasAlpha) { Fade(ref c, 0f, 1f, 0f, d, Ease.OutQuad); }
                    else { Scale(ref c, Vector3.one * (1f - 0.5f * hide), Vector3.one, 0f, d, Ease.OutCubic); }
                    break;
                case MotionPreset.PopIn:
                    Scale(ref c, Vector3.one * (1f - hide), Vector3.one, 0f, d, Ease.OutBack);
                    Fade(ref c, 0f, 1f, 0f, d * 0.5f, Ease.OutQuad);
                    break;
                case MotionPreset.ZoomIn:
                    Scale(ref c, Vector3.one * (1f - 0.5f * hide), Vector3.one, 0f, d, Ease.OutCubic);
                    Fade(ref c, 0f, 1f, 0f, d * 0.7f, Ease.OutQuad);
                    break;
                case MotionPreset.SlideInLeft: SlideIn(ref c, Vector3.left); break;
                case MotionPreset.SlideInRight: SlideIn(ref c, Vector3.right); break;
                case MotionPreset.SlideInUp: SlideIn(ref c, Vector3.down); break;
                case MotionPreset.SlideInDown: SlideIn(ref c, Vector3.up); break;
                case MotionPreset.DropIn:
                    Move(ref c, Vector3.up * (c.Distance * 2f), Vector3.zero, 0f, d, Ease.OutBounce);
                    Fade(ref c, 0f, 1f, 0f, d * 0.25f, Ease.OutQuad);
                    break;
                case MotionPreset.RiseIn:
                    Move(ref c, Vector3.down * (c.Distance * 0.4f), Vector3.zero, 0f, d, Ease.OutQuad);
                    Fade(ref c, 0f, 1f, 0f, d, Ease.OutQuad);
                    break;
                case MotionPreset.SpinIn:
                    Scale(ref c, Vector3.one * (1f - hide), Vector3.one, 0f, d, Ease.OutBack);
                    Rotate(ref c, SpinAxis(c) * (-180f * c.Intensity), Vector3.zero, 0f, d, Ease.OutCubic);
                    Fade(ref c, 0f, 1f, 0f, d * 0.3f, Ease.OutQuad);
                    break;
                case MotionPreset.FlipIn:
                    Rotate(ref c, new Vector3(0f, 90f * hide, 0f), Vector3.zero, 0f, d, Ease.OutBack);
                    Fade(ref c, 0f, 1f, 0f, d * 0.4f, Ease.OutQuad);
                    break;
                default:
                    Scale(ref c, Vector3.one * (1f - hide), Vector3.one, 0f, d, Ease.OutElastic);
                    Fade(ref c, 0f, 1f, 0f, d * 0.3f, Ease.OutQuad);
                    break;
            }
        }

        static void SlideIn(ref Context c, Vector3 from) {
            Move(ref c, from * c.Distance, Vector3.zero, 0f, c.Duration, Ease.OutCubic);
            Fade(ref c, 0f, 1f, 0f, c.Duration * 0.6f, Ease.OutQuad);
        }

        static void ComposeExit(ref Context c, MotionPreset preset) {
            float d = c.Duration;
            float hide = Mathf.Clamp01(c.Intensity);
            switch (preset) {
                case MotionPreset.FadeOut:
                    if (c.Target.HasAlpha) { Fade(ref c, 1f, 0f, 0f, d, Ease.InQuad); }
                    else { Scale(ref c, Vector3.one, Vector3.zero, 0f, d, Ease.InBack); }
                    break;
                case MotionPreset.PopOut:
                    Scale(ref c, Vector3.one, Vector3.one * (1f - hide), 0f, d, Ease.InBack);
                    Fade(ref c, 1f, 0f, d * 0.5f, d * 0.5f, Ease.InQuad);
                    break;
                case MotionPreset.ZoomOut:
                    Scale(ref c, Vector3.one, Vector3.one * (1f - 0.5f * hide), 0f, d, Ease.InCubic);
                    Fade(ref c, 1f, 0f, 0f, d, Ease.InQuad);
                    break;
                case MotionPreset.SlideOutLeft: SlideOut(ref c, Vector3.left); break;
                case MotionPreset.SlideOutRight: SlideOut(ref c, Vector3.right); break;
                case MotionPreset.SlideOutUp: SlideOut(ref c, Vector3.up); break;
                case MotionPreset.SlideOutDown: SlideOut(ref c, Vector3.down); break;
                case MotionPreset.FallOut:
                    Move(ref c, Vector3.zero, Vector3.down * (c.Distance * 2f), 0f, d, Ease.InQuad);
                    Rotate(ref c, Vector3.zero, SpinAxis(c) * (-20f * c.Intensity), 0f, d, Ease.InQuad);
                    Fade(ref c, 1f, 0f, d * 0.5f, d * 0.5f, Ease.InQuad);
                    break;
                case MotionPreset.SinkOut:
                    Move(ref c, Vector3.zero, Vector3.down * (c.Distance * 0.4f), 0f, d, Ease.InQuad);
                    Fade(ref c, 1f, 0f, 0f, d, Ease.InQuad);
                    break;
                case MotionPreset.SpinOut:
                    Scale(ref c, Vector3.one, Vector3.one * (1f - hide), 0f, d, Ease.InBack);
                    Rotate(ref c, Vector3.zero, SpinAxis(c) * (180f * c.Intensity), 0f, d, Ease.InCubic);
                    Fade(ref c, 1f, 0f, d * 0.7f, d * 0.3f, Ease.InQuad);
                    break;
                default:
                    Rotate(ref c, Vector3.zero, new Vector3(0f, 90f * hide, 0f), 0f, d, Ease.InBack);
                    Fade(ref c, 1f, 0f, d * 0.6f, d * 0.4f, Ease.InQuad);
                    break;
            }
        }

        static void SlideOut(ref Context c, Vector3 to) {
            Move(ref c, Vector3.zero, to * c.Distance, 0f, c.Duration, Ease.InCubic);
            Fade(ref c, 1f, 0f, c.Duration * 0.4f, c.Duration * 0.6f, Ease.InQuad);
        }

        static void ComposeAttention(ref Context c, MotionPreset preset) {
            float k = c.Intensity;
            float small = c.Distance * 0.1f;
            switch (preset) {
                case MotionPreset.Pulse: Keys(ref c, Channel.Scale, Half3, PulseScale, 0.12f * k, Ease.InOutQuad); break;
                case MotionPreset.Bounce: Bounce(ref c); break;
                case MotionPreset.Shake: Keys(ref c, Channel.Position, ShakeTimes, ShakeKeys, small, Ease.InOutSine); break;
                case MotionPreset.Wobble:
                    Keys(ref c, Channel.Position, WobbleTimes, WobbleMove, c.Distance, Ease.InOutSine);
                    Keys(ref c, Channel.Rotation, WobbleTimes, WobbleTilt, k, Ease.InOutSine);
                    break;
                case MotionPreset.Jello: Keys(ref c, Channel.Scale, JelloTimes, JelloScale, k, Ease.InOutSine); break;
                case MotionPreset.Heartbeat: Keys(ref c, Channel.Scale, HeartTimes, HeartScale, k, Ease.InOutQuad); break;
                case MotionPreset.Tada:
                    Keys(ref c, Channel.Scale, TadaTimes, TadaScale, k, Ease.InOutSine);
                    Keys(ref c, Channel.Rotation, TadaTimes, TadaTilt, k, Ease.InOutSine);
                    break;
                case MotionPreset.Flash:
                    if (c.Target.HasAlpha) { Keys(ref c, Channel.Alpha, FlashTimes, FlashAlpha, k, Ease.InOutSine); }
                    else { Keys(ref c, Channel.Scale, Half3, PulseScale, 0.12f * k, Ease.InOutQuad); }
                    break;
                case MotionPreset.Swing: Keys(ref c, Channel.Rotation, SwingTimes, SwingTilt, k, Ease.InOutSine); break;
                case MotionPreset.Squash: Squash(ref c); break;
                case MotionPreset.Nod: Keys(ref c, Channel.Position, Even5, NodMove, small, Ease.InOutSine); break;
                case MotionPreset.Tint: Tint(ref c); break;
                default:
                    Keys(ref c, Channel.Rotation, HeadShakeTimes, HeadShakeTurn, k, Ease.InOutSine);
                    Keys(ref c, Channel.Position, HeadShakeTimes, HeadShakeMove, c.Distance, Ease.InOutSine);
                    break;
            }
        }

        static readonly Vector3[] PulseScale = { Vector3.zero, Vector3.one, Vector3.zero };
        static readonly float[] BounceTimes = { 0f, 0.35f, 0.7f, 1f };
        static readonly Vector3[] BounceMove = { Vector3.zero, Vector3.up, Vector3.zero, Vector3.zero };
        static readonly float[] LandTimes = { 0f, 0.7f, 0.82f, 1f };
        static readonly Vector3[] LandScale = { Vector3.one, Vector3.one, new Vector3(1.15f, 0.85f, 1.15f), Vector3.one };

        static void Bounce(ref Context c) {
            Keys(ref c, Channel.Position, BounceTimes, BounceMove, c.Distance * 0.35f, Ease.OutQuad, Ease.InQuad);
            Keys(ref c, Channel.Scale, LandTimes, LandScale, c.Intensity, Ease.OutQuad);
        }

        static void Tint(ref Context c) {
            if (!c.Target.HasColor) {
                Keys(ref c, Channel.Scale, Half3, PulseScale, 0.12f * c.Intensity, Ease.InOutQuad);
                return;
            }
            Color rest = c.Rest.Color;
            Color peak = Color.Lerp(rest, c.Tint, Mathf.Clamp01(c.Intensity));
            peak.a = rest.a;
            float rise = c.Duration * 0.15f;
            Recolor(ref c, rest, peak, 0f, rise, Ease.OutQuad);
            Recolor(ref c, peak, rest, rise, c.Duration - rise, Ease.InOutSine);
        }

        static void Squash(ref Context c) {
            float k = 0.25f * c.Intensity;
            float first = c.Duration * 0.25f;
            Scale(ref c, Vector3.one, new Vector3(1f + k, 1f - k, 1f + k), 0f, first, Ease.OutQuad);
            Scale(ref c, new Vector3(1f + k, 1f - k, 1f + k), Vector3.one, first, c.Duration - first, Ease.OutElastic);
        }

        static void ComposeLoop(ref Context c, MotionPreset preset) {
            float k = c.Intensity;
            switch (preset) {
                case MotionPreset.Float: Keys(ref c, Channel.Position, Even5, UpDownLoop, c.Distance * 0.12f, Ease.InOutSine); break;
                case MotionPreset.Breathe: Keys(ref c, Channel.Scale, Half3, PulseScale, 0.04f * k, Ease.InOutSine); break;
                case MotionPreset.Spin: Spin(ref c); break;
                case MotionPreset.Sway: Keys(ref c, Channel.Position, Even5, SideLoop, c.Distance * 0.15f, Ease.InOutSine); break;
                case MotionPreset.Rock: Keys(ref c, Channel.Rotation, Even5, RockTilt, k, Ease.InOutSine); break;
                case MotionPreset.Blink:
                    if (c.Target.HasAlpha) { Keys(ref c, Channel.Alpha, Half3, BlinkAlpha, Mathf.Clamp01(0.75f * k), Ease.InOutSine); }
                    else { Keys(ref c, Channel.Scale, Half3, PulseScale, 0.04f * k, Ease.InOutSine); }
                    break;
                case MotionPreset.Throb: Keys(ref c, Channel.Scale, Half3, PulseScale, 0.1f * k, Ease.InOutQuad); break;
                default: Keys(ref c, Channel.Rotation, WiggleTimes, WiggleTilt, k, Ease.InOutSine); break;
            }
        }

        static readonly Vector3[] RockTilt = {
            Vector3.zero, new Vector3(0f, 0f, 8f), Vector3.zero, new Vector3(0f, 0f, -8f), Vector3.zero
        };
        static readonly Vector3[] BlinkAlpha = { Vector3.one, Vector3.zero, Vector3.one };

        static void Spin(ref Context c) {
            Vector3 start = c.Rest.Rotation.eulerAngles;
            Vector3 turn = c.Target.IsFlat ? new Vector3(0f, 0f, -360f) : new Vector3(0f, 360f, 0f);
            Tween tween = Raven.LocalEulerAngles(c.Target.Transform, start + turn, c.Duration).From(start).Ease(Ease.Linear);
            c.Sequence.Insert(0f, tween);
            c.Channels |= MotionChannels.Rotation;
        }

        static Vector3 SpinAxis(in Context c) {
            return c.Target.IsFlat ? Vector3.forward : Vector3.up;
        }

        static void Keys(ref Context c, Channel channel, float[] times, Vector3[] values, float amount, Ease ease) {
            Keys(ref c, channel, times, values, amount, ease, ease);
        }

        static void Keys(ref Context c, Channel channel, float[] times, Vector3[] values, float amount, Ease odd, Ease even) {
            Debug.Assert(times.Length == values.Length && times.Length >= 2, "Keyframes need matching times and values.");
            for (int i = 0; i + 1 < times.Length; i++) {
                float start = times[i] * c.Duration;
                float length = (times[i + 1] - times[i]) * c.Duration;
                if (length <= 0f) { continue; }
                Ease ease = (i & 1) == 0 ? odd : even;
                Segment(ref c, channel, values[i], values[i + 1], amount, values == PulseScale, start, length, ease);
            }
        }

        static void Segment(ref Context c, Channel channel, Vector3 from, Vector3 to, float amount, bool pulse, float start, float length, Ease ease) {
            switch (channel) {
                case Channel.Position: Move(ref c, from * amount, to * amount, start, length, ease); break;
                case Channel.Rotation: Rotate(ref c, from * amount, to * amount, start, length, ease); break;
                case Channel.Scale:
                    Scale(ref c, Factor(from, amount, pulse), Factor(to, amount, pulse), start, length, ease);
                    break;
                default:
                    Fade(ref c, Mathf.Clamp01(1f - (1f - from.x) * amount), Mathf.Clamp01(1f - (1f - to.x) * amount), start, length, ease);
                    break;
            }
        }

        static Vector3 Factor(Vector3 value, float amount, bool pulse) {
            if (pulse) { return Vector3.one + value * amount; }
            return Vector3.one + (value - Vector3.one) * amount;
        }

        static void Move(ref Context c, Vector3 fromOffset, Vector3 toOffset, float start, float length, Ease ease) {
            Vector3 from = c.Rest.Position + fromOffset;
            Vector3 to = c.Rest.Position + toOffset;
            Tween tween = c.Target.Rect != null
                ? Raven.AnchoredPosition(c.Target.Rect, to, length).From((Vector2)from)
                : Raven.LocalPosition(c.Target.Transform, to, length).From(from);
            c.Sequence.Insert(start, tween.Ease(ease));
            c.Channels |= MotionChannels.Position;
            if (start <= 0f) { c.Start.Position = from; c.StartChannels |= MotionChannels.Position; }
        }

        static void Rotate(ref Context c, Vector3 fromEuler, Vector3 toEuler, float start, float length, Ease ease) {
            Quaternion from = c.Rest.Rotation * Quaternion.Euler(fromEuler);
            Quaternion to = c.Rest.Rotation * Quaternion.Euler(toEuler);
            c.Sequence.Insert(start, Raven.LocalRotation(c.Target.Transform, to, length).From(from).Ease(ease));
            c.Channels |= MotionChannels.Rotation;
            if (start <= 0f) { c.Start.Rotation = from; c.StartChannels |= MotionChannels.Rotation; }
        }

        static void Scale(ref Context c, Vector3 fromFactor, Vector3 toFactor, float start, float length, Ease ease) {
            Vector3 from = Vector3.Scale(c.Rest.Scale, fromFactor);
            Vector3 to = Vector3.Scale(c.Rest.Scale, toFactor);
            c.Sequence.Insert(start, Raven.Scale(c.Target.Transform, to, length).From(from).Ease(ease));
            c.Channels |= MotionChannels.Scale;
            if (start <= 0f) { c.Start.Scale = from; c.StartChannels |= MotionChannels.Scale; }
        }

        static void Recolor(ref Context c, Color from, Color to, float start, float length, Ease ease) {
            Tween tween;
            if (c.Target.Graphic != null) { tween = Raven.Color(c.Target.Graphic, to, length); }
            else if (c.Target.Sprite != null) { tween = Raven.Color(c.Target.Sprite, to, length); }
            else { tween = Raven.PropertyBlockColor(c.Target.Renderer, c.Target.ColorId, to, length); }
            c.Sequence.Insert(start, tween.From(from).Ease(ease));
            c.Channels |= MotionChannels.Color;
            if (start <= 0f) { c.Start.Color = from; c.StartChannels |= MotionChannels.Color; }
        }

        static void Fade(ref Context c, float fromFactor, float toFactor, float start, float length, Ease ease) {
            if (!c.Target.HasAlpha) {
                if (!c.MissedFade) {
                    c.MissedFade = true;
                    c.MissedFrom = fromFactor;
                    c.MissedTo = toFactor;
                    c.MissedStart = start;
                    c.MissedLength = length;
                    c.MissedEase = ease;
                }
                return;
            }
            float from = c.Rest.Alpha * fromFactor;
            float to = c.Rest.Alpha * toFactor;
            Tween tween;
            if (c.Target.Group != null) { tween = Raven.Alpha(c.Target.Group, to, length); }
            else if (c.Target.Graphic != null) { tween = Raven.Alpha(c.Target.Graphic, to, length); }
            else { tween = Raven.Alpha(c.Target.Sprite, to, length); }
            c.Sequence.Insert(start, tween.From(from).Ease(ease));
            c.Channels |= MotionChannels.Alpha;
            if (start <= 0f) { c.Start.Alpha = from; c.StartChannels |= MotionChannels.Alpha; }
        }
    }
}
