using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        public static float TimeScale {
            get { return TweenEngine.TimeScale; }
            set {
                Debug.Assert(value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value), "Raven.TimeScale must be a finite, non-negative number.");
                if (float.IsNaN(value) || float.IsInfinity(value)) { return; }
                TweenEngine.TimeScale = Mathf.Max(value, 0f);
            }
        }

        public static UpdatePhase UpdatePhase {
            get { return TweenEngine.Phase; }
            set { TweenEngine.Phase = value; }
        }

        public static int AliveCount {
            get { return TweenEngine.AliveCount; }
        }

        public static bool Diagnostics {
            get { return TweenEngine.Diagnostics; }
            set { TweenEngine.Diagnostics = value; }
        }

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

        public static Tween Delay(float seconds) {
            Debug.Assert(seconds >= 0f, "Delay duration cannot be negative.");
            return CreateValueTween(new TweenValue(0f), new TweenValue(1f), Mathf.Max(seconds, 0f));
        }

        public static Tween Delay(float seconds, Action onComplete) {
            Debug.Assert(onComplete != null, "Delay completion callback cannot be null.");
            Tween tween = Delay(seconds);
            if (onComplete != null) { tween.OnComplete(onComplete); }
            return tween;
        }

        public static Sequence Sequence() {
            int index = TweenEngine.Rent(out TweenSlot slot);
            slot.IsSequence = true;
            if (slot.Items == null) { slot.Items = new List<SequenceItem>(4); }
            return new Sequence(index, slot.Version);
        }

        public static void StopAll() { TweenEngine.KillAll(false); }

        public static void CompleteAll() { TweenEngine.KillAll(true); }

        public static int StopAll(object target) { return TweenEngine.ForEachOnTarget(target, TweenEngine.TargetAction.Stop); }

        public static int CompleteAll(object target) { return TweenEngine.ForEachOnTarget(target, TweenEngine.TargetAction.Complete); }

        public static int PauseAll(object target = null) { return TweenEngine.ForEachOnTarget(target, TweenEngine.TargetAction.Pause); }

        public static int ResumeAll(object target = null) { return TweenEngine.ForEachOnTarget(target, TweenEngine.TargetAction.Resume); }

        public static int CountTweens(object target) { return TweenEngine.ForEachOnTarget(target, TweenEngine.TargetAction.Count); }

        public static void SetCapacity(int capacity) { TweenEngine.EnsureCapacity(Mathf.Max(capacity, 0)); }

        public static void Step(float deltaTime) {
            Debug.Assert(deltaTime >= 0f, "Step needs a non-negative delta time.");
            TweenEngine.Process(deltaTime, deltaTime);
        }

        public static void Step(float deltaTime, UpdatePhase phase) {
            Step(deltaTime, deltaTime, phase);
        }

        public static void Step(float deltaTime, float unscaledDeltaTime, UpdatePhase phase) {
            Debug.Assert(deltaTime >= 0f && unscaledDeltaTime >= 0f, "Step needs non-negative delta times.");
            TweenEngine.ProcessPhase(phase, deltaTime, unscaledDeltaTime);
        }

        static readonly object TimeScaleTarget = new object();

        public static Tween GlobalTimeScale(float to, float duration) {
            Debug.Assert(to >= 0f, "Time.timeScale cannot be negative.");
            return CustomTo(TimeScaleTarget, _ => Time.timeScale, Mathf.Max(to, 0f), duration,
                            (_, value) => Time.timeScale = Mathf.Max(value, 0f)).UnscaledTime();
        }

        public static Tween TweenTimeScale(Tween tween, float to, float duration) {
            return CreateTimeScaleTween(tween.Index, tween.Version, to, duration);
        }

        public static Tween TweenTimeScale(Sequence sequence, float to, float duration) {
            return CreateTimeScaleTween(sequence.Index, sequence.Version, to, duration);
        }

        static Tween CreateTimeScaleTween(int index, uint version, float to, float duration) {
            if (!TweenEngine.TryGetSlot(index, version, out TweenSlot target) || target.OwnedBySequence) {
                Debug.LogError("RavenTween: TweenTimeScale needs a live tween or sequence that is not inside a sequence.");
                return default;
            }
            Tween tween = CreateValueTween(new TweenValue(target.TimeScale), new TweenValue(Mathf.Max(to, 0f)), duration);
            if (TweenEngine.TryGetSlot(tween.Index, tween.Version, out TweenSlot slot)) {
                slot.HasExplicitFrom = false;
                slot.LinkIndex = index;
                slot.LinkVersion = version;
            }
            return tween;
        }

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
