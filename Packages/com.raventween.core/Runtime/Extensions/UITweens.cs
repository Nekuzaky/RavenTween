using UnityEngine;
using UnityEngine.UI;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens a RectTransform's anchored position.</summary>
        public static Tween AnchoredPosition(RectTransform target, Vector2 to, float duration) {
            Debug.Assert(target != null, "AnchoredPosition tween needs a live RectTransform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.AnchoredPosition, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens a RectTransform's size delta.</summary>
        public static Tween SizeDelta(RectTransform target, Vector2 to, float duration) {
            Debug.Assert(target != null, "SizeDelta tween needs a live RectTransform.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.SizeDelta, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens a CanvasGroup's alpha.</summary>
        public static Tween Alpha(CanvasGroup target, float to, float duration) {
            Debug.Assert(target != null, "Alpha tween needs a live CanvasGroup.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.CanvasGroupAlpha, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens a Graphic's color (Image, Text, RawImage, ...).</summary>
        public static Tween Color(Graphic target, Color to, float duration) {
            Debug.Assert(target != null, "Color tween needs a live Graphic.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.GraphicColor, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens only the alpha channel of a Graphic's color.</summary>
        public static Tween Alpha(Graphic target, float to, float duration) {
            Debug.Assert(target != null, "Alpha tween needs a live Graphic.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.GraphicAlpha, 0, new TweenValue(to), duration);
        }

        /// <summary>Tweens the anchored X position only.</summary>
        public static Tween AnchoredPositionX(RectTransform target, float to, float duration) {
            return Axis(target, PropertyKind.AnchoredPositionX, to, duration);
        }

        /// <summary>Tweens the anchored Y position only.</summary>
        public static Tween AnchoredPositionY(RectTransform target, float to, float duration) {
            return Axis(target, PropertyKind.AnchoredPositionY, to, duration);
        }

        /// <summary>Moves to <paramref name="to"/> at <paramref name="speed"/> UI units per second.</summary>
        public static Tween AnchoredPositionAtSpeed(RectTransform target, Vector2 to, float speed) {
            float distance = target != null ? Vector2.Distance(target.anchoredPosition, to) : 0f;
            return AnchoredPosition(target, to, DurationFor(distance, speed));
        }
    }

    /// <summary>Extension-method style access to UI tweens.</summary>
    public static class UITweens {
        public static Tween TweenAnchoredPosition(this RectTransform target, Vector2 to, float duration) {
            return Raven.AnchoredPosition(target, to, duration);
        }

        public static Tween TweenSizeDelta(this RectTransform target, Vector2 to, float duration) {
            return Raven.SizeDelta(target, to, duration);
        }

        public static Tween TweenAlpha(this CanvasGroup target, float to, float duration) {
            return Raven.Alpha(target, to, duration);
        }

        public static Tween TweenColor(this Graphic target, Color to, float duration) {
            return Raven.Color(target, to, duration);
        }

        public static Tween TweenAlpha(this Graphic target, float to, float duration) {
            return Raven.Alpha(target, to, duration);
        }

        public static Tween TweenAnchoredPositionX(this RectTransform target, float to, float duration) {
            return Raven.AnchoredPositionX(target, to, duration);
        }

        public static Tween TweenAnchoredPositionY(this RectTransform target, float to, float duration) {
            return Raven.AnchoredPositionY(target, to, duration);
        }

        public static Tween TweenAnchoredPositionAtSpeed(this RectTransform target, Vector2 to, float speed) {
            return Raven.AnchoredPositionAtSpeed(target, to, speed);
        }
    }
}
