using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        const float DefaultShakeFrequency = 12f;
        const float DefaultPunchFrequency = 5f;

        /// <summary>Noise-driven shake around the current <b>local</b> position, decaying to rest. Same as <see cref="ShakeLocalPosition"/>.</summary>
        public static Tween ShakePosition(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalPosition, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current <b>local</b> euler angles, in degrees. Same as <see cref="ShakeLocalRotation"/>.</summary>
        public static Tween ShakeRotation(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalEulerAngles, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current local scale.</summary>
        public static Tween ShakeScale(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return CreateEffect(target, PropertyKind.LocalScale, EffectKind.Shake, strength, duration, frequency);
        }

        /// <summary>Damped spring kick along <paramref name="punch"/> from the current <b>local</b> position. Same as <see cref="PunchLocalPosition"/>.</summary>
        public static Tween PunchPosition(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalPosition, EffectKind.Punch, punch, duration, frequency);
        }

        /// <summary>Damped spring kick on <b>local</b> euler angles, in degrees. Same as <see cref="PunchLocalRotation"/>.</summary>
        public static Tween PunchRotation(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalEulerAngles, EffectKind.Punch, punch, duration, frequency);
        }

        /// <summary>Damped spring kick on local scale. Classic "button press" feedback.</summary>
        public static Tween PunchScale(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return CreateEffect(target, PropertyKind.LocalScale, EffectKind.Punch, punch, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current local position (explicit name for <see cref="ShakePosition"/>).</summary>
        public static Tween ShakeLocalPosition(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return ShakePosition(target, strength, duration, frequency);
        }

        /// <summary>Noise-driven shake around the current local euler angles (explicit name for <see cref="ShakeRotation"/>).</summary>
        public static Tween ShakeLocalRotation(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency) {
            return ShakeRotation(target, strength, duration, frequency);
        }

        /// <summary>Damped spring kick from the current local position (explicit name for <see cref="PunchPosition"/>).</summary>
        public static Tween PunchLocalPosition(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return PunchPosition(target, punch, duration, frequency);
        }

        /// <summary>Damped spring kick on local euler angles (explicit name for <see cref="PunchRotation"/>).</summary>
        public static Tween PunchLocalRotation(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency) {
            return PunchRotation(target, punch, duration, frequency);
        }

        /// <summary>
        /// A ready-made camera shake: a small position jitter plus a tilt, scaled by
        /// <paramref name="strength"/> (1 = a solid hit, 0.3 = a light rumble). Repeated hits
        /// restart the shake around the same rest pose. Returns the position shake; the tilt has
        /// the same length. Stop both with <c>Raven.StopAll(camera.transform)</c>. If a script moves
        /// the camera every frame, shake a parent of the camera instead. Pass <paramref name="tilt"/> = false
        /// when something else owns the camera's rotation (LookAt, a mouse-look controller).
        /// </summary>
        public static Tween ShakeCamera(Camera camera, float strength = 1f, float duration = 0.5f, float frequency = 15f, bool tilt = true) {
            Debug.Assert(camera != null, "ShakeCamera needs a live Camera.");
            if (camera == null) {
                Debug.LogError("RavenTween: cannot shake a null or destroyed camera.");
                return default;
            }
            Transform t = camera.transform;
            float s = Mathf.Max(strength, 0f);
            if (tilt) { ShakeRotation(t, new Vector3(1.2f, 1.2f, 2.5f) * s, duration, frequency); }
            return ShakePosition(t, new Vector3(0.12f, 0.12f, 0f) * s, duration, frequency);
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
            TweenEngine.RegisterEffect(tween.Index);
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

        public static Tween Shake(this Camera camera, float strength = 1f, float duration = 0.5f, float frequency = 15f, bool tilt = true) {
            return Raven.ShakeCamera(camera, strength, duration, frequency, tilt);
        }

        public static Tween ShakeLocalPosition(this Transform target, Vector3 strength, float duration, float frequency = 12f) {
            return Raven.ShakeLocalPosition(target, strength, duration, frequency);
        }

        public static Tween ShakeLocalRotation(this Transform target, Vector3 strength, float duration, float frequency = 12f) {
            return Raven.ShakeLocalRotation(target, strength, duration, frequency);
        }

        public static Tween PunchLocalPosition(this Transform target, Vector3 punch, float duration, float frequency = 5f) {
            return Raven.PunchLocalPosition(target, punch, duration, frequency);
        }

        public static Tween PunchLocalRotation(this Transform target, Vector3 punch, float duration, float frequency = 5f) {
            return Raven.PunchLocalRotation(target, punch, duration, frequency);
        }
    }
}
