using System;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Main entry point of RavenTween. Create value tweens, delays and sequences here;
    /// target-specific tweens live in extension classes (TransformTweens, UITweens, ...).
    /// </summary>
    public static partial class Raven {
        /// <summary>Global time multiplier applied to every tween, independent of Time.timeScale.</summary>
        public static float TimeScale {
            get { return TweenEngine.TimeScale; }
            set {
                Debug.Assert(value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value), "Raven.TimeScale must be a finite, non-negative number.");
                if (float.IsNaN(value) || float.IsInfinity(value)) { return; }
                TweenEngine.TimeScale = Mathf.Max(value, 0f);
            }
        }

        /// <summary>Player loop phase in which tweens advance. Default: Update.</summary>
        public static UpdatePhase UpdatePhase {
            get { return TweenEngine.Phase; }
            set { TweenEngine.Phase = value; }
        }

        /// <summary>Number of live tweens and sequences, paused ones included.</summary>
        public static int AliveCount {
            get { return TweenEngine.AliveCount; }
        }

        // ----- Value tweens -----

        /// <summary>Tweens a float. Read the value in OnUpdate.</summary>
        public static Tween Value(float from, float to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        public static Tween Value(Vector2 from, Vector2 to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        public static Tween Value(Vector3 from, Vector3 to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        public static Tween Value(Vector4 from, Vector4 to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        public static Tween Value(Quaternion from, Quaternion to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        public static Tween Value(Color from, Color to, float duration) {
            return CreateValueTween(new TweenValue(from), new TweenValue(to), duration);
        }

        // ----- Delays -----

        /// <summary>Creates a timer tween. Await it, yield it, or attach OnComplete.</summary>
        public static Tween Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay duration cannot be negative.");
            return CreateValueTween(new TweenValue(0f), new TweenValue(1f), Mathf.Max(seconds, 0f));
        }

        /// <summary>Creates a timer tween that fires <paramref name="onComplete"/> when it elapses.</summary>
        public static Tween Delay(float seconds, Action onComplete) {
            Debug.Assert(onComplete != null, "Delay completion callback cannot be null.");
            Tween tween = Delay(seconds);
            if (onComplete != null) { tween.OnComplete(onComplete); }
            return tween;
        }

        // ----- Sequences -----

        /// <summary>Creates an empty sequence. Fill it with Chain, Group and Insert.</summary>
        public static Sequence Sequence() {
            int index = TweenEngine.Rent(out TweenSlot slot);
            slot.IsSequence = true;
            return new Sequence(index, slot.Version);
        }

        // ----- Global control -----

        /// <summary>Stops every live tween and sequence without completing them.</summary>
        public static void StopAll() { TweenEngine.KillAll(false); }

        /// <summary>Completes every live tween and sequence, jumping them to their end values.</summary>
        public static void CompleteAll() { TweenEngine.KillAll(true); }

        // ----- Internal factories (used by extension classes) -----

        internal static Tween CreateValueTween(in TweenValue from, in TweenValue to, float duration) {
            Debug.Assert(from.Kind == to.Kind, "Value tween endpoints must share the same kind.");
            Debug.Assert(duration >= 0f, "Tween duration cannot be negative.");
            int index = TweenEngine.Rent(out TweenSlot slot);
            slot.StartValue = from;
            slot.EndValue = to;
            slot.HasExplicitFrom = true;
            slot.Duration = Mathf.Max(duration, 0f);
            return new Tween(index, slot.Version);
        }

        internal static Tween CreatePropertyTween(
            UnityEngine.Object target, PropertyKind property, int propertyId, in TweenValue endValue, float duration) {
            Debug.Assert(property != PropertyKind.None, "Property tweens need a concrete property.");
            Debug.Assert(duration >= 0f, "Tween duration cannot be negative.");
            if (target == null) {
                Debug.LogError("RavenTween: cannot tween a null or destroyed target.");
                return default;
            }
            if (!PropertyAccessor.TargetType(property).IsInstanceOfType(target)) {
                Debug.LogError("RavenTween: " + property + " needs a " + PropertyAccessor.TargetType(property).Name +
                               ", got a " + target.GetType().Name + " (" + target.name + ").", target);
                return default;
            }
            int index = TweenEngine.Rent(out TweenSlot slot);
            slot.UnityTarget = target;
            slot.RequiresTarget = true;
            slot.Property = property;
            slot.PropertyId = propertyId;
            slot.EndValue = endValue;
            slot.Duration = Mathf.Max(duration, 0f);
            return new Tween(index, slot.Version);
        }
    }
}
