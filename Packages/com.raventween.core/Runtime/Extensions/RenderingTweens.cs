using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens a SpriteRenderer's color.</summary>
        public static Tween Color(SpriteRenderer target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live SpriteRenderer.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.SpriteColor, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens a Light's intensity.</summary>
        public static Tween Intensity(Light target, float to, float duration) {
            Debug.Assert(target != null, "Intensity tween needs a live Light.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LightIntensity, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens a Light's color.</summary>
        public static Tween Color(Light target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live Light.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.LightColor, 0, new TweenValue(to), duration);
        }
    }

    /// <summary>Extension-method style access to renderer and light tweens.</summary>
    public static class RenderingTweens {
        public static Tween TweenColor(this SpriteRenderer target, Color to, float duration) {
            return Raven.Color(target, to, duration);
        }

        public static Tween TweenIntensity(this Light target, float to, float duration) {
            return Raven.Intensity(target, to, duration);
        }

        public static Tween TweenColor(this Light target, Color to, float duration) {
            return Raven.Color(target, to, duration);
        }
    }
}
