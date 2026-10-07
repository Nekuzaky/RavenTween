using UnityEngine;

namespace RavenTween {
    /// <summary>Standard easing functions. Use <see cref="Ease.Custom"/> with an AnimationCurve or delegate.</summary>
    public enum Ease {
        Linear = 0,
        InSine, OutSine, InOutSine,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint,
        InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc,
        InBack, OutBack, InOutBack,
        InElastic, OutElastic, InOutElastic,
        InBounce, OutBounce, InOutBounce,
        Custom = 100
    }

    /// <summary>What an <see cref="Easing"/> evaluates.</summary>
    public enum EasingKind : byte {
        Standard = 0,
        Curve = 1,
        Overshoot = 2,
        Bounce = 3,
        BounceExact = 4,
        Elastic = 5
    }

    /// <summary>
    /// An ease with parameters: a standard <see cref="Ease"/>, an AnimationCurve, or a tunable
    /// Overshoot / Bounce / Elastic. Pass it to <c>tween.Ease(...)</c>. A plain struct: no allocation.
    /// </summary>
    public readonly struct Easing {
        public readonly EasingKind Kind;
        public readonly Ease Standard;
        public readonly AnimationCurve Curve;
        public readonly float A;
        public readonly float B;

        Easing(EasingKind kind, Ease standard, AnimationCurve curve, float a, float b) {
            Kind = kind;
            Standard = standard;
            Curve = curve;
            A = a;
            B = b;
        }

        /// <summary>A standard ease.</summary>
        public static Easing Of(Ease ease) { return new Easing(EasingKind.Standard, ease, null, 0f, 0f); }

        /// <summary>An AnimationCurve evaluated over [0, 1].</summary>
        public static Easing FromCurve(AnimationCurve curve) { return new Easing(EasingKind.Curve, Ease.Custom, curve, 0f, 0f); }

        /// <summary>Goes past the end and settles back (OutBack). 1 = the classic overshoot, 0 = none, 2 = twice as far.</summary>
        public static Easing Overshoot(float strength = 1f) {
            return new Easing(EasingKind.Overshoot, Ease.Custom, null, Mathf.Max(strength, 0f), 0f);
        }

        /// <summary>Bounces on the end value (OutBounce). 1 = the classic bounce, 0.5 = bounces half as high.</summary>
        public static Easing Bounce(float strength = 1f) {
            return new Easing(EasingKind.Bounce, Ease.Custom, null, Mathf.Max(strength, 0f), 0f);
        }

        /// <summary>
        /// Bounces exactly <paramref name="amplitude"/> back from the end value, in the tween's own
        /// units (meters, degrees…), whatever the distance travelled.
        /// </summary>
        public static Easing BounceExact(float amplitude) {
            return new Easing(EasingKind.BounceExact, Ease.Custom, null, Mathf.Max(amplitude, 0f), 0f);
        }

        /// <summary>
        /// Springs around the end value (OutElastic). Higher <paramref name="strength"/> keeps
        /// oscillating longer; <paramref name="period"/> is the length of one oscillation, as a
        /// fraction of the duration.
        /// </summary>
        public static Easing Elastic(float strength = 1f, float period = 0.3f) {
            return new Easing(EasingKind.Elastic, Ease.Custom, null, Mathf.Max(strength, 0.05f), Mathf.Max(period, 0.02f));
        }

        public static implicit operator Easing(Ease ease) { return Of(ease); }

        /// <summary>Evaluates the ease at <paramref name="t"/>. BounceExact uses a distance of 1.</summary>
        public float Evaluate(float t) {
            switch (Kind) {
                case EasingKind.Standard: return EaseUtility.Evaluate(Standard, t);
                case EasingKind.Curve: return Curve != null ? Curve.Evaluate(Mathf.Clamp01(t)) : t;
                default: return EaseUtility.EvaluateParametric(Kind, t, A, B, 1f);
            }
        }
    }

    /// <summary>Evaluates easing functions. All methods are pure and allocation-free.</summary>
    public static class EaseUtility {
        const float BackOvershoot = 1.70158f;
        const float BounceFirstRebound = 0.25f; // Height of OutBounce's first rebound, as a fraction of the distance.

        /// <summary>
        /// Evaluates a parametric ease. <paramref name="distance"/> is the size of the change being
        /// eased, used by BounceExact to turn its amplitude into a strength.
        /// </summary>
        public static float EvaluateParametric(EasingKind kind, float t, float a, float b, float distance) {
            Debug.Assert(!float.IsNaN(t), "Ease time must be a number.");
            t = Mathf.Clamp01(t);
            switch (kind) {
                case EasingKind.Overshoot: return OutBackWith(t, BackOvershoot * a);
                case EasingKind.Bounce: return OutBounceScaled(t, a);
                case EasingKind.BounceExact:
                    return OutBounceScaled(t, distance > 1e-6f ? a / (BounceFirstRebound * distance) : 0f);
                case EasingKind.Elastic: return OutElasticWith(t, a, b);
                default: return t;
            }
        }

        static float OutBackWith(float t, float s) {
            return 1f + (s + 1f) * Pow3(t - 1f) + s * Pow2(t - 1f);
        }

        // Rebounds after the first impact are scaled; the fall itself is unchanged.
        static float OutBounceScaled(float t, float strength) {
            if (t < 1f / 2.75f) { return OutBounce(t); }
            return 1f - (1f - OutBounce(t)) * strength;
        }

        // 1 - e(t)·cos(2πt / period), where the envelope e decays from 1 at t = 0 to exactly 0 at t = 1.
        static float OutElasticWith(float t, float strength, float period) {
            if (t <= 0f) { return 0f; }
            if (t >= 1f) { return 1f; }
            float envelope = Mathf.Pow(2f, -10f * t / strength) - Mathf.Pow(2f, -10f / strength) * t;
            return 1f - envelope * Mathf.Cos(2f * Mathf.PI * t / period);
        }

        /// <summary>Evaluates <paramref name="ease"/> at normalized time <paramref name="t"/> in [0, 1].</summary>
        public static float Evaluate(Ease ease, float t) {
            Debug.Assert(!float.IsNaN(t), "Ease time must be a number.");
            t = Mathf.Clamp01(t);
            switch (ease) {
                case Ease.Linear: return t;
                case Ease.InSine: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case Ease.OutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case Ease.InOutSine: return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return t * (2f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Pow2(-2f * t + 2f) * 0.5f;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1f - Pow3(1f - t);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Pow3(-2f * t + 2f) * 0.5f;
                case Ease.InQuart: return t * t * t * t;
                case Ease.OutQuart: return 1f - Pow4(1f - t);
                case Ease.InOutQuart: return t < 0.5f ? 8f * t * t * t * t : 1f - Pow4(-2f * t + 2f) * 0.5f;
                case Ease.InQuint: return t * t * t * t * t;
                case Ease.OutQuint: return 1f - Pow5(1f - t);
                case Ease.InOutQuint: return t < 0.5f ? 16f * t * t * t * t * t : 1f - Pow5(-2f * t + 2f) * 0.5f;
                case Ease.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.InOutExpo: return InOutExpo(t);
                case Ease.InCirc: return 1f - Mathf.Sqrt(1f - t * t);
                case Ease.OutCirc: return Mathf.Sqrt(1f - Pow2(t - 1f));
                case Ease.InOutCirc: return InOutCirc(t);
                case Ease.InBack: return (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;
                case Ease.OutBack: return 1f + (BackOvershoot + 1f) * Pow3(t - 1f) + BackOvershoot * Pow2(t - 1f);
                case Ease.InOutBack: return InOutBack(t);
                case Ease.InElastic: return 1f - OutElastic(1f - t);
                case Ease.OutElastic: return OutElastic(t);
                case Ease.InOutElastic: return InOutElastic(t);
                case Ease.InBounce: return 1f - OutBounce(1f - t);
                case Ease.OutBounce: return OutBounce(t);
                case Ease.InOutBounce: return t < 0.5f ? (1f - OutBounce(1f - 2f * t)) * 0.5f : (1f + OutBounce(2f * t - 1f)) * 0.5f;
                default:
                    Debug.Assert(ease != Ease.Custom, "Ease.Custom must be evaluated through its curve or delegate.");
                    return t;
            }
        }

        static float Pow2(float v) { return v * v; }
        static float Pow3(float v) { return v * v * v; }
        static float Pow4(float v) { return v * v * v * v; }
        static float Pow5(float v) { return v * v * v * v * v; }

        static float InOutExpo(float t) {
            if (t <= 0f) { return 0f; }
            if (t >= 1f) { return 1f; }
            return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
        }

        static float InOutCirc(float t) {
            return t < 0.5f
                ? (1f - Mathf.Sqrt(1f - Pow2(2f * t))) * 0.5f
                : (Mathf.Sqrt(1f - Pow2(-2f * t + 2f)) + 1f) * 0.5f;
        }

        static float InOutBack(float t) {
            const float c2 = BackOvershoot * 1.525f;
            return t < 0.5f
                ? Pow2(2f * t) * ((c2 + 1f) * 2f * t - c2) * 0.5f
                : (Pow2(2f * t - 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
        }

        static float OutElastic(float t) {
            if (t <= 0f) { return 0f; }
            if (t >= 1f) { return 1f; }
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        static float InOutElastic(float t) {
            if (t <= 0f) { return 0f; }
            if (t >= 1f) { return 1f; }
            const float c5 = 2f * Mathf.PI / 4.5f;
            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) * 0.5f
                : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5) * 0.5f + 1f;
        }

        static float OutBounce(float t) {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) { return n1 * t * t; }
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
