using UnityEngine;
using UnityEngine.UIElements;

namespace RavenTween {
    public static class UIToolkitTweens {
        static float Laid(float resolved, StyleLength inline) {
            if (!float.IsNaN(resolved)) { return resolved; }
            return inline.keyword == StyleKeyword.Undefined && inline.value.unit == LengthUnit.Pixel ? inline.value.value : 0f;
        }

        public static Tween TweenOpacity(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Opacity tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.opacity, to, duration, (e, v) => e.style.opacity = v);
        }

        public static Tween TweenTranslate(this VisualElement target, Vector2 to, float duration) {
            Debug.Assert(target != null, "Translate tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => (Vector2)e.resolvedStyle.translate, to, duration,
                (e, v) => e.style.translate = new Translate(new Length(v.x), new Length(v.y)));
        }

        public static Tween TweenScale(this VisualElement target, Vector2 to, float duration) {
            Debug.Assert(target != null, "Scale tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => (Vector2)e.resolvedStyle.scale.value, to, duration,
                (e, v) => e.style.scale = new Scale(new Vector3(v.x, v.y, 1f)));
        }

        public static Tween TweenScale(this VisualElement target, float to, float duration) {
            return TweenScale(target, new Vector2(to, to), duration);
        }

        public static Tween TweenRotate(this VisualElement target, float degrees, float duration) {
            Debug.Assert(target != null, "Rotate tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.rotate.angle.ToDegrees(), degrees, duration,
                (e, v) => e.style.rotate = new Rotate(new Angle(v, AngleUnit.Degree)));
        }

        public static Tween TweenBackgroundColor(this VisualElement target, Color to, float duration) {
            Debug.Assert(target != null, "Background color tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.backgroundColor, to, duration, (e, v) => e.style.backgroundColor = v);
        }

        public static Tween TweenColor(this VisualElement target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.color, to, duration, (e, v) => e.style.color = v);
        }

        public static Tween TweenWidth(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Width tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => Laid(e.resolvedStyle.width, e.style.width), to, duration, (e, v) => e.style.width = v);
        }

        public static Tween TweenHeight(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Height tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => Laid(e.resolvedStyle.height, e.style.height), to, duration, (e, v) => e.style.height = v);
        }
    }
}
