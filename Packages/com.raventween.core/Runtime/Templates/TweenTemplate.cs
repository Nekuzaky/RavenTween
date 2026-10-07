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
        /// <remarks>
        /// <paramref name="target"/> may be the exact object the property needs, or any GameObject
        /// or component carrying it: the right component (or the renderer's material) is found.
        /// </remarks>
        public Tween Play(Object target) {
            Debug.Assert(property != PropertyKind.None, "Template has no property to animate.");
            if (!TryBind(target, Application.isPlaying, true, out Object resolved, out int propertyId)) { return default; }
            TweenValue end = Pack(endValue, PropertyAccessor.KindOf(property));
            Tween tween = Raven.CreatePropertyTween(resolved, property, propertyId, end, settings.duration);
            if (!tween.IsAlive) { return tween; }
            if (useExplicitFrom) { ApplyFrom(tween); }
            return settings.ApplyTo(tween);
        }

        /// <summary>
        /// Finds the object and shader property id this template writes to when played on
        /// <paramref name="assigned"/>, exactly as <see cref="Play"/> does. Used by editor previews.
        /// </summary>
        internal bool TryBind(Object assigned, bool instanceMaterial, bool logErrors, out Object resolved, out int propertyId) {
            propertyId = 0;
            resolved = PropertyAccessor.ResolveTarget(assigned, property, instanceMaterial);
            if (resolved == null) {
                if (logErrors) {
                    Debug.LogError("RavenTween: template '" + name + "' animates " + property + " and needs a " +
                                   PropertyAccessor.TargetType(property).Name + "; got " + Describe(assigned) + ".", this);
                }
                return false;
            }
            return TryResolvePropertyId(resolved, logErrors, out propertyId);
        }

        static string Describe(Object target) {
            return target == null ? "nothing" : "a " + target.GetType().Name + " (" + target.name + ")";
        }

        // Built-in shaders call the main color _Color, URP/HDRP call it _BaseColor: either name
        // works for either pipeline.
        bool TryResolvePropertyId(Object resolved, bool logErrors, out int id) {
            id = 0;
            bool material = property == PropertyKind.MaterialFloat || property == PropertyKind.MaterialColor;
            if (!material) { return true; }
            var mat = (Material)resolved;
            if (string.IsNullOrEmpty(materialProperty)) {
                if (logErrors) { Debug.LogError("RavenTween: template '" + name + "' has no shader property name.", this); }
                return false;
            }
            id = Shader.PropertyToID(materialProperty);
            if (mat.HasProperty(id)) { return true; }
            string alias = materialProperty == "_Color" ? "_BaseColor" : materialProperty == "_BaseColor" ? "_Color" : null;
            if (alias != null && mat.HasProperty(alias)) {
                id = Shader.PropertyToID(alias);
                return true;
            }
            if (logErrors) {
                Debug.LogError("RavenTween: material '" + mat.name + "' has no property '" + materialProperty + "' (template '" + name + "').", this);
            }
            return false;
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
