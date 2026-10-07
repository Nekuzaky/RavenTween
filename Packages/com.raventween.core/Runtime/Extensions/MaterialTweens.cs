using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens a material float property. Prefer Shader.PropertyToID over name strings.</summary>
        public static Tween MaterialFloat(Material target, int propertyId, float to, float duration) {
            Debug.Assert(target != null, "MaterialFloat tween needs a live Material.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.MaterialFloat, propertyId, new TweenValue(to), duration);
        }

        /// <summary>Tweens a material float property by name. Resolves the ID once at creation.</summary>
        public static Tween MaterialFloat(Material target, string propertyName, float to, float duration) {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "Material property name cannot be empty.");
            return MaterialFloat(target, Shader.PropertyToID(propertyName), to, duration);
        }

        /// <summary>Tweens a material color property. Prefer Shader.PropertyToID over name strings.</summary>
        public static Tween MaterialColor(Material target, int propertyId, Color to, float duration) {
            Debug.Assert(target != null, "MaterialColor tween needs a live Material.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.MaterialColor, propertyId, new TweenValue(to), duration);
        }

        /// <summary>Tweens a material color property by name. Resolves the ID once at creation.</summary>
        public static Tween MaterialColor(Material target, string propertyName, Color to, float duration) {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "Material property name cannot be empty.");
            return MaterialColor(target, Shader.PropertyToID(propertyName), to, duration);
        }
    }

    /// <summary>Extension-method style access to Material tweens.</summary>
    public static class MaterialTweens {
        public static Tween TweenFloat(this Material target, int propertyId, float to, float duration) {
            return Raven.MaterialFloat(target, propertyId, to, duration);
        }

        public static Tween TweenFloat(this Material target, string propertyName, float to, float duration) {
            return Raven.MaterialFloat(target, propertyName, to, duration);
        }

        public static Tween TweenColor(this Material target, int propertyId, Color to, float duration) {
            return Raven.MaterialColor(target, propertyId, to, duration);
        }

        public static Tween TweenColor(this Material target, string propertyName, Color to, float duration) {
            return Raven.MaterialColor(target, propertyName, to, duration);
        }
    }
}
