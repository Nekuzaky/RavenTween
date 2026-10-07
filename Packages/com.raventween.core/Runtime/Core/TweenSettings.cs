using System;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>
    /// A whole tween in one serializable field: end value, optional start value and timing.
    /// Expose it in a component so designers tune the values in the Inspector, then play it with
    /// the matching overload, e.g. <c>Raven.LocalPosition(transform, settings)</c>.
    /// </summary>
    /// <remarks>A field declared without an initializer has a duration of 0: give it one with the constructor.</remarks>
    [Serializable]
    public struct TweenSettings<T> where T : struct {
        [Tooltip("Off: the tween starts from the object's current value.")]
        public bool useStartValue;
        public T startValue;
        public T endValue;
        public TweenParams settings;

        /// <summary>Starts from the current value.</summary>
        public TweenSettings(T endValue, float duration, Ease ease = Ease.OutQuad) {
            useStartValue = false;
            startValue = default;
            this.endValue = endValue;
            settings = TweenParams.Default;
            settings.duration = Mathf.Max(duration, 0f);
            settings.ease = ease;
        }

        /// <summary>Starts from <paramref name="startValue"/>.</summary>
        public TweenSettings(T startValue, T endValue, float duration, Ease ease = Ease.OutQuad) : this(endValue, duration, ease) {
            useStartValue = true;
            this.startValue = startValue;
        }
    }

    public static partial class Raven {
        public static Tween Position(Transform target, TweenSettings<Vector3> s) {
            return Apply(Position(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween LocalPosition(Transform target, TweenSettings<Vector3> s) {
            return Apply(LocalPosition(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween EulerAngles(Transform target, TweenSettings<Vector3> s) {
            return Apply(EulerAngles(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween LocalEulerAngles(Transform target, TweenSettings<Vector3> s) {
            return Apply(LocalEulerAngles(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Scale(Transform target, TweenSettings<Vector3> s) {
            return Apply(Scale(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween AnchoredPosition(RectTransform target, TweenSettings<Vector2> s) {
            return Apply(AnchoredPosition(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween SizeDelta(RectTransform target, TweenSettings<Vector2> s) {
            return Apply(SizeDelta(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Alpha(CanvasGroup target, TweenSettings<float> s) {
            return Apply(Alpha(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Alpha(Graphic target, TweenSettings<float> s) {
            return Apply(Alpha(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Color(Graphic target, TweenSettings<Color> s) {
            return Apply(Color(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Alpha(SpriteRenderer target, TweenSettings<float> s) {
            return Apply(Alpha(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        public static Tween Color(SpriteRenderer target, TweenSettings<Color> s) {
            return Apply(Color(target, s.endValue, s.settings.duration), s.useStartValue, new TweenValue(s.startValue), s.settings);
        }

        static Tween Apply(Tween tween, bool useStartValue, in TweenValue startValue, TweenParams settings) {
            if (!tween.IsAlive) { return tween; }
            if (useStartValue) { tween.FromValue(startValue); }
            return settings.ApplyTo(tween);
        }
    }
}
