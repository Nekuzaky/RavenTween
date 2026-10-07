using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RavenTween.Editor {
    /// <summary>Which object each animated property needs, and how to find one from a selection.</summary>
    /// <remarks>Same rules as the runtime, so a target accepted here is accepted by Play.</remarks>
    static class TemplateTargets {
        public static Type TargetType(PropertyKind kind) {
            return PropertyAccessor.TargetType(kind);
        }

        /// <summary>Finds a usable target on a GameObject (a renderer's shared material for material properties).</summary>
        public static Object FindOn(GameObject go, PropertyKind kind) {
            return go == null ? null : PropertyAccessor.ResolveTarget(go, kind, false);
        }

        /// <summary>True when <paramref name="target"/> can receive <paramref name="kind"/>.</summary>
        public static bool Accepts(Object target, PropertyKind kind) {
            return target != null && TargetType(kind).IsInstanceOfType(target);
        }
    }
}
