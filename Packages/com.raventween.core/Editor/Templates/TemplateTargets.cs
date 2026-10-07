using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RavenTween.Editor {
    /// <summary>Which object each animated property needs, and how to find one from a selection.</summary>
    static class TemplateTargets {
        public static Type TargetType(PropertyKind kind) {
            switch (kind) {
                case PropertyKind.AnchoredPosition:
                case PropertyKind.SizeDelta: return typeof(RectTransform);
                case PropertyKind.CanvasGroupAlpha: return typeof(CanvasGroup);
                case PropertyKind.GraphicColor:
                case PropertyKind.GraphicAlpha: return typeof(Graphic);
                case PropertyKind.CameraFieldOfView:
                case PropertyKind.CameraOrthographicSize:
                case PropertyKind.CameraBackgroundColor: return typeof(Camera);
                case PropertyKind.AudioVolume:
                case PropertyKind.AudioPitch: return typeof(AudioSource);
                case PropertyKind.MaterialFloat:
                case PropertyKind.MaterialColor: return typeof(Material);
                case PropertyKind.SpriteColor: return typeof(SpriteRenderer);
                case PropertyKind.LightIntensity:
                case PropertyKind.LightColor: return typeof(Light);
                default: return typeof(Transform);
            }
        }

        /// <summary>Finds a usable target on a GameObject (a renderer's material for material properties).</summary>
        public static Object FindOn(GameObject go, PropertyKind kind) {
            if (go == null) { return null; }
            Type type = TargetType(kind);
            if (type == typeof(Material)) {
                var renderer = go.GetComponent<Renderer>();
                return renderer != null ? renderer.sharedMaterial : null;
            }
            return go.GetComponent(type);
        }

        /// <summary>True when <paramref name="target"/> can receive <paramref name="kind"/>.</summary>
        public static bool Accepts(Object target, PropertyKind kind) {
            return target != null && TargetType(kind).IsInstanceOfType(target);
        }
    }
}
