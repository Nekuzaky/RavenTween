using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens a SpriteRenderer's color.</summary>
        public static Tween Color(SpriteRenderer target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live SpriteRenderer.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.SpriteColor, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens only the alpha of a SpriteRenderer's color.</summary>
        public static Tween Alpha(SpriteRenderer target, float to, float duration) {
            Debug.Assert(target != null, "Alpha tween needs a live SpriteRenderer.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.SpriteAlpha, 0, new TweenValue(to), duration);
        }

        /// <summary>
        /// Tweens a shader float on this renderer only, through a MaterialPropertyBlock: no material
        /// copy, other renderers sharing the material are untouched.
        /// </summary>
        public static Tween PropertyBlockFloat(Renderer target, int propertyId, float to, float duration) {
            Debug.Assert(target != null, "PropertyBlockFloat tween needs a live Renderer.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.PropertyBlockFloat, propertyId, new TweenValue(to), duration);
        }

        /// <summary>Same as <see cref="PropertyBlockFloat(Renderer, int, float, float)"/>, by property name.</summary>
        public static Tween PropertyBlockFloat(Renderer target, string propertyName, float to, float duration) {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "Shader property name cannot be empty.");
            return PropertyBlockFloat(target, Shader.PropertyToID(propertyName), to, duration);
        }

        /// <summary>Tweens a shader color on this renderer only, through a MaterialPropertyBlock.</summary>
        public static Tween PropertyBlockColor(Renderer target, int propertyId, Color to, float duration) {
            Debug.Assert(target != null, "PropertyBlockColor tween needs a live Renderer.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.PropertyBlockColor, propertyId, new TweenValue(to), duration);
        }

        /// <summary>Same as <see cref="PropertyBlockColor(Renderer, int, Color, float)"/>, by property name.</summary>
        public static Tween PropertyBlockColor(Renderer target, string propertyName, Color to, float duration) {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "Shader property name cannot be empty.");
            return PropertyBlockColor(target, Shader.PropertyToID(propertyName), to, duration);
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

        public static Tween TweenAlpha(this SpriteRenderer target, float to, float duration) {
            return Raven.Alpha(target, to, duration);
        }

        public static Tween TweenPropertyBlockFloat(this Renderer target, int propertyId, float to, float duration) {
            return Raven.PropertyBlockFloat(target, propertyId, to, duration);
        }

        public static Tween TweenPropertyBlockFloat(this Renderer target, string propertyName, float to, float duration) {
            return Raven.PropertyBlockFloat(target, propertyName, to, duration);
        }

        public static Tween TweenPropertyBlockColor(this Renderer target, int propertyId, Color to, float duration) {
            return Raven.PropertyBlockColor(target, propertyId, to, duration);
        }

        public static Tween TweenPropertyBlockColor(this Renderer target, string propertyName, Color to, float duration) {
            return Raven.PropertyBlockColor(target, propertyName, to, duration);
        }

        public static Tween TweenIntensity(this Light target, float to, float duration) {
            return Raven.Intensity(target, to, duration);
        }

        public static Tween TweenColor(this Light target, Color to, float duration) {
            return Raven.Color(target, to, duration);
        }
    }
}
