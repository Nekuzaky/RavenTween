using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        public static Tween FieldOfView(Camera target, float to, float duration) {
            Debug.Assert(target != null, "FieldOfView tween needs a live Camera.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.CameraFieldOfView, 0, new TweenValue(to), duration);
        }

        public static Tween OrthographicSize(Camera target, float to, float duration) {
            Debug.Assert(target != null, "OrthographicSize tween needs a live Camera.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.CameraOrthographicSize, 0, new TweenValue(to), duration);
        }

        public static Tween BackgroundColor(Camera target, Color to, float duration) {
            Debug.Assert(target != null, "BackgroundColor tween needs a live Camera.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.CameraBackgroundColor, 0, new TweenValue(to), duration);
        }
    }

    public static class CameraTweens {
        public static Tween TweenFieldOfView(this Camera target, float to, float duration) {
            return Raven.FieldOfView(target, to, duration);
        }

        public static Tween TweenOrthographicSize(this Camera target, float to, float duration) {
            return Raven.OrthographicSize(target, to, duration);
        }

        public static Tween TweenBackgroundColor(this Camera target, Color to, float duration) {
            return Raven.BackgroundColor(target, to, duration);
        }
    }
}
