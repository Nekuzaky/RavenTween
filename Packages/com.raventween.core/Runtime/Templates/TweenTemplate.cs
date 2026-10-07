using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Reusable tween configuration. Tweens themselves are single-use; templates hold the
    /// settings and spawn a fresh tween for any target via <see cref="Play"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TweenTemplate", menuName = "RavenTween/Tween Template")]
    public sealed class TweenTemplate : ScriptableObject {
        [Tooltip("Property animated on the target.")]
        public PropertyKind property = PropertyKind.LocalPosition;

        [Tooltip("Material property name, used only by MaterialFloat / MaterialColor.")]
        public string materialProperty = "_Color";

        [Tooltip("End value. x is used for float properties; xyzw maps to rgba for colors.")]
        public Vector4 endValue = Vector4.zero;

        [Tooltip("When enabled, the tween starts from 'fromValue' instead of the current value.")]
        public bool useExplicitFrom;
        public Vector4 fromValue = Vector4.zero;

        public TweenParams settings = TweenParams.Default;

        /// <summary>True when this template loops forever (cycles = -1).</summary>
        public bool LoopsForever {
            get { return settings.cycles < 0; }
        }

        /// <summary>
        /// Finite stand-in used when a looping template plays inside a sequence: one full
        /// out-and-back for Yoyo (so it ends where it started), one pass for Restart.
        /// </summary>
        public int CyclesInsideSequence {
            get { return settings.cycleMode == CycleMode.Yoyo ? 2 : 1; }
        }

        /// <summary>Creates and plays a tween of this template on <paramref name="target"/>.</summary>
        public Tween Play(Object target) {
            Debug.Assert(property != PropertyKind.None, "Template has no property to animate.");
            if (target == null) {
                Debug.LogError("RavenTween: template '" + name + "' received a null target.", this);
                return default;
            }
            int propertyId = ResolvePropertyId();
            TweenValue end = Pack(endValue, PropertyAccessor.KindOf(property));
            Tween tween = Raven.CreatePropertyTween(target, property, propertyId, end, settings.duration);
            if (!tween.IsAlive) { return tween; }
            if (useExplicitFrom) { ApplyFrom(tween); }
            return settings.ApplyTo(tween);
        }

        int ResolvePropertyId() {
            bool needsId = property == PropertyKind.MaterialFloat || property == PropertyKind.MaterialColor;
            if (!needsId) { return 0; }
            Debug.Assert(!string.IsNullOrEmpty(materialProperty), "Material templates need a property name.");
            return Shader.PropertyToID(materialProperty);
        }

        void ApplyFrom(Tween tween) {
            ValueKind kind = PropertyAccessor.KindOf(property);
            switch (kind) {
                case ValueKind.Float: tween.From(fromValue.x); break;
                case ValueKind.Vector2: tween.From(new Vector2(fromValue.x, fromValue.y)); break;
                case ValueKind.Vector3: tween.From(new Vector3(fromValue.x, fromValue.y, fromValue.z)); break;
                case ValueKind.Quaternion: tween.From(Quaternion.Euler(fromValue.x, fromValue.y, fromValue.z)); break;
                case ValueKind.Color: tween.From(new Color(fromValue.x, fromValue.y, fromValue.z, fromValue.w)); break;
                default: tween.From(fromValue); break;
            }
        }

        static TweenValue Pack(Vector4 raw, ValueKind kind) {
            switch (kind) {
                case ValueKind.Float: return new TweenValue(raw.x);
                case ValueKind.Vector2: return new TweenValue(new Vector2(raw.x, raw.y));
                case ValueKind.Vector3: return new TweenValue(new Vector3(raw.x, raw.y, raw.z));
                case ValueKind.Quaternion: return new TweenValue(Quaternion.Euler(raw.x, raw.y, raw.z));
                case ValueKind.Color: return new TweenValue(new Color(raw.x, raw.y, raw.z, raw.w));
                default: return new TweenValue(raw);
            }
        }

        void OnValidate() {
            if (settings.duration < 0f) { settings.duration = 0f; }
            if (settings.startDelay < 0f) { settings.startDelay = 0f; }
            if (settings.cycles < -1) { settings.cycles = -1; }
        }
    }
}
