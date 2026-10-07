using System;
using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>
        /// Tweens any float on any target through a setter. Pass a non-capturing lambda
        /// (e.g. <c>(t, v) =&gt; t.fontSize = v</c>) and the tween allocates nothing.
        /// If <paramref name="target"/> is a UnityEngine.Object, the tween dies with it.
        /// </summary>
        public static Tween Custom<T>(T target, float from, float to, float duration, Action<T, float> setter) where T : class {
            return CreateCustom(target, new TweenValue(from), new TweenValue(to), duration, setter, CustomInvokers<T>.ForFloat);
        }

        /// <summary>Zero-allocation Vector2 custom tween. See the float overload.</summary>
        public static Tween Custom<T>(T target, Vector2 from, Vector2 to, float duration, Action<T, Vector2> setter) where T : class {
            return CreateCustom(target, new TweenValue(from), new TweenValue(to), duration, setter, CustomInvokers<T>.ForVector2);
        }

        /// <summary>Zero-allocation Vector3 custom tween. See the float overload.</summary>
        public static Tween Custom<T>(T target, Vector3 from, Vector3 to, float duration, Action<T, Vector3> setter) where T : class {
            return CreateCustom(target, new TweenValue(from), new TweenValue(to), duration, setter, CustomInvokers<T>.ForVector3);
        }

        /// <summary>Zero-allocation Color custom tween. See the float overload.</summary>
        public static Tween Custom<T>(T target, Color from, Color to, float duration, Action<T, Color> setter) where T : class {
            return CreateCustom(target, new TweenValue(from), new TweenValue(to), duration, setter, CustomInvokers<T>.ForColor);
        }

        /// <summary>
        /// Tweens a float from its value <b>when the tween starts</b> (read with
        /// <paramref name="getter"/>) to <paramref name="to"/>. Unlike Custom, the start value is
        /// not frozen at creation, so it stays correct after a Delay or inside a sequence. Use
        /// non-capturing lambdas for zero allocations.
        /// </summary>
        public static Tween CustomTo<T>(T target, Func<T, float> getter, float to, float duration, Action<T, float> setter) where T : class {
            Debug.Assert(getter != null, "CustomTo needs a getter.");
            Tween tween = CreateCustom(target, new TweenValue(to), new TweenValue(to), duration, setter, CustomInvokers<T>.ForFloat);
            if (getter == null || !TweenEngine.TryGetSlot(tween.Index, tween.Version, out TweenSlot slot)) { return tween; }
            slot.HasExplicitFrom = false;
            slot.CustomGetterDelegate = getter;
            slot.CustomGetter = CustomInvokers<T>.GetFloat;
            return tween;
        }

        static Tween CreateCustom(object target, in TweenValue from, in TweenValue to, float duration,
                                  Delegate setter, Action<object, Delegate, TweenValue> invoker) {
            Debug.Assert(setter != null, "Custom tweens need a setter.");
            Debug.Assert(invoker != null, "Custom tweens need a typed invoker.");
            bool destroyed = target is UnityEngine.Object unityTarget && unityTarget == null;
            if (target == null || destroyed || setter == null) {
                Debug.LogError("RavenTween: Custom tween needs a live target and a setter.");
                return default;
            }
            Tween tween = CreateValueTween(from, to, duration);
            if (!TweenEngine.TryGetSlot(tween.Index, tween.Version, out TweenSlot slot)) { return tween; }
            slot.CustomTarget = target;
            slot.CustomSetter = setter;
            slot.CustomInvoker = invoker;
            var unityObject = target as UnityEngine.Object;
            if (!ReferenceEquals(unityObject, null)) {
                slot.UnityTarget = unityObject;
                slot.RequiresTarget = true;
            }
            return tween;
        }

        // One cached invoker per (T, value type): created once per closed generic type, never per tween.
        static class CustomInvokers<T> where T : class {
            public static readonly Action<object, Delegate, TweenValue> ForFloat =
                (target, setter, value) => ((Action<T, float>)setter)((T)target, value.Float);
            public static readonly Action<object, Delegate, TweenValue> ForVector2 =
                (target, setter, value) => ((Action<T, Vector2>)setter)((T)target, value.Vector2);
            public static readonly Action<object, Delegate, TweenValue> ForVector3 =
                (target, setter, value) => ((Action<T, Vector3>)setter)((T)target, value.Vector3);
            public static readonly Action<object, Delegate, TweenValue> ForColor =
                (target, setter, value) => ((Action<T, Color>)setter)((T)target, value.Color);
            public static readonly Func<object, Delegate, TweenValue> GetFloat =
                (target, getter) => new TweenValue(((Func<T, float>)getter)((T)target));
        }
    }
}
