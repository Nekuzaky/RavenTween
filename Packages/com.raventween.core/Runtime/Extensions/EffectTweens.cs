using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        const float DefaultShakeFrequency = 12f;
        const float DefaultPunchFrequency = 5f;

        /// <summary>Noise-driven shake around the current local position, decaying to rest.</summary>
        public static Tween ShakePosition(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalPosition, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current local euler angles, in degrees.</summary>
        public static Tween ShakeRotation(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalEulerAngles, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current local scale.</summary>
        public static Tween ShakeScale(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalScale, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Damped spring kick along <paramref name="punch"/> from the current local position.</summary>
        public static Tween PunchPosition(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalPosition, EffectKind.Punch, punch, duration, frequency);
        }

        /// <summary>Damped spring kick on local euler angles, in degrees.</summary>
        public static Tween PunchRotation(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalEulerAngles, EffectKind.Punch, punch, duration, frequency);
        }

        /// <summary>Damped spring kick on local scale. Classic "button press" feedback.</summary>
        public static Tween PunchScale(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalScale, EffectKind.Punch, punch, duration, frequency);
        }

        static Tween CreateEffect(Transform target, PropertyKind property, EffectKind effect,
                                  Vector3 strength, float duration, float frequency) {
            Debug.Assert(duration > 0f, "Effects need a positive duration.");
            Debug.Assert(frequency > 0f, "Effect frequency must be positive.");
            if (target == null) { return CreatePropertyTween(null, property, 0, new TweenValue(Vector3.zero), duration); }
            bool takeOver = TweenEngine.TryTakeOverEffect(target, property, out TweenValue rest);
            Tween tween = CreatePropertyTween(target, property, 0, new TweenValue(Vector3.zero), duration);
            if (!TweenEngine.TryGetSlot(tween.Index, tween.Version, out TweenSlot slot)) { return tween; }
            if (takeOver) {
                slot.StartValue = rest;
                slot.HasExplicitFrom = true;
            }
            slot.Effect = effect;
            slot.EffectStrength = strength;
            slot.EffectFrequency = Mathf.Max(frequency, 0.01f) * Mathf.Max(duration, 0.0001f);
            slot.EffectSeed = TweenEngine.NextSeed();
            return tween;
        }
    }

    /// <summary>Extension-method style access to shake and punch effects.</summary>
    public static class EffectTweens {
        public static Tween ShakePosition(this Transform target, Vector3 strength, float duration, float frequency = 12f) {
            return Raven.ShakePosition(target, strength, duration, frequency);
        }

        public static Tween ShakeRotation(this Transform target, Vector3 strength, float duration, float frequency = 12f) {
            return Raven.ShakeRotation(target, strength, duration, frequency);
        }

        public static Tween ShakeScale(this Transform target, Vector3 strength, float duration, float frequency = 12f) {
            return Raven.ShakeScale(target, strength, duration, frequency);
        }

        public static Tween PunchPosition(this Transform target, Vector3 punch, float duration, float frequency = 5f) {
            return Raven.PunchPosition(target, punch, duration, frequency);
        }

        public static Tween PunchRotation(this Transform target, Vector3 punch, float duration, float frequency = 5f) {
            return Raven.PunchRotation(target, punch, duration, frequency);
        }

        public static Tween PunchScale(this Transform target, Vector3 punch, float duration, float frequency = 5f) {
            return Raven.PunchScale(target, punch, duration, frequency);
        }
    }
}
