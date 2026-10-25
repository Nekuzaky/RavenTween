using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RavenTween.Editor {
    static class TemplateTargets {
        public static Type TargetType(PropertyKind kind) {
            return PropertyAccessor.TargetType(kind);
        }

        public static Object FindOn(GameObject go, PropertyKind kind) {
            return go == null ? null : PropertyAccessor.ResolveTarget(go, kind, false);
        }

        public static bool Accepts(Object target, PropertyKind kind) {
            return target != null && TargetType(kind).IsInstanceOfType(target);
        }
    }
}
