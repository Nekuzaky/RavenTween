using UnityEngine;
using UnityEngine.UIElements;

namespace RavenTween {
    /// <summary>
    /// Tweens for UI Toolkit elements. Each one starts from the element's current resolved style
    /// and writes the inline style, so it also works on elements styled by USS. The setters
    /// capture nothing, so these tweens allocate nothing while they play.
    /// </summary>
    public static class UIToolkitTweens {
        // An element not laid out yet (just created, not in a panel) has no resolved size: the
        // inline value is used, or 0, never NaN.
        static float Laid(float resolved, StyleLength inline) {
            if (!float.IsNaN(resolved)) { return resolved; }
            return inline.keyword == StyleKeyword.Undefined && inline.value.unit == LengthUnit.Pixel ? inline.value.value : 0f;
        }

        public static Tween TweenOpacity(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Opacity tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.opacity, to, duration, (e, v) => e.style.opacity = v);
        }

        /// <summary>Moves the element by its translate style, in pixels (layout is not affected).</summary>
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

        /// <summary>Rotates the element around its transform origin, in degrees.</summary>
        public static Tween TweenRotate(this VisualElement target, float degrees, float duration) {
            Debug.Assert(target != null, "Rotate tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.rotate.angle.ToDegrees(), degrees, duration,
                (e, v) => e.style.rotate = new Rotate(new Angle(v, AngleUnit.Degree)));
        }

        public static Tween TweenBackgroundColor(this VisualElement target, Color to, float duration) {
            Debug.Assert(target != null, "Background color tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.backgroundColor, to, duration, (e, v) => e.style.backgroundColor = v);
        }

        /// <summary>Text color of the element (and of the children that inherit it).</summary>
        public static Tween TweenColor(this VisualElement target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => e.resolvedStyle.color, to, duration, (e, v) => e.style.color = v);
        }

        /// <summary>Width in pixels (affects layout).</summary>
        public static Tween TweenWidth(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Width tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => Laid(e.resolvedStyle.width, e.style.width), to, duration, (e, v) => e.style.width = v);
        }

        /// <summary>Height in pixels (affects layout).</summary>
        public static Tween TweenHeight(this VisualElement target, float to, float duration) {
            Debug.Assert(target != null, "Height tween needs a live VisualElement.");
            return Raven.CustomTo(target, e => Laid(e.resolvedStyle.height, e.style.height), to, duration, (e, v) => e.style.height = v);
        }
    }
}
