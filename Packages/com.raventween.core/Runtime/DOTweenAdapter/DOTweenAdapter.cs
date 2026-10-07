using UnityEngine;

// Migration helper: replace `using DG.Tweening;` with `using RavenTween; using RavenTween.DOTweenAdapter;`
// and the most common DOTween calls compile unchanged, running on RavenTween. Lives in its own
// namespace, so it never clashes with DOTween itself while both are installed.
namespace RavenTween.DOTweenAdapter {
    public enum LoopType { Restart = 0, Yoyo = 1, Incremental = 2 }

    public enum UpdateType { Normal = 0, Late = 1, Fixed = 2 }

    public delegate void TweenCallback();
    public delegate void TweenCallback<in T>(T value);
    public delegate T DOGetter<out T>();
    public delegate void DOSetter<in T>(T value);

    /// <summary>DOTween's static entry points.</summary>
    public static class DOTween {
        public static float timeScale {
            get { return Raven.TimeScale; }
            set { Raven.TimeScale = value; }
        }

        public static Sequence Sequence() { return Raven.Sequence(); }

        public static int KillAll(bool complete = false) {
            int count = Raven.CountTweens(null);
            if (complete) { Raven.CompleteAll(); } else { Raven.StopAll(); }
            return count;
        }

        public static int Kill(object target, bool complete = false) {
            return complete ? Raven.CompleteAll(target) : Raven.StopAll(target);
        }

        public static int CompleteAll() { return Raven.CompleteAll(null); }
        public static int Complete(object target) { return Raven.CompleteAll(target); }
        public static int PauseAll() { return Raven.PauseAll(); }
        public static int Pause(object target) { return Raven.PauseAll(target); }
        public static int PlayAll() { return Raven.ResumeAll(); }
        public static int Play(object target) { return Raven.ResumeAll(target); }
        public static int TotalPlayingTweens() { return Raven.AliveCount; }

        public static Tween To(DOGetter<float> getter, DOSetter<float> setter, float endValue, float duration) {
            return Raven.CustomTo(new Accessor<float>(getter, setter), a => a.Get(), endValue, duration, (a, v) => a.Set(v));
        }

        public static Tween To(DOGetter<Vector2> getter, DOSetter<Vector2> setter, Vector2 endValue, float duration) {
            return Raven.CustomTo(new Accessor<Vector2>(getter, setter), a => a.Get(), endValue, duration, (a, v) => a.Set(v));
        }

        public static Tween To(DOGetter<Vector3> getter, DOSetter<Vector3> setter, Vector3 endValue, float duration) {
            return Raven.CustomTo(new Accessor<Vector3>(getter, setter), a => a.Get(), endValue, duration, (a, v) => a.Set(v));
        }

        public static Tween To(DOGetter<Color> getter, DOSetter<Color> setter, Color endValue, float duration) {
            return Raven.CustomTo(new Accessor<Color>(getter, setter), a => a.Get(), endValue, duration, (a, v) => a.Set(v));
        }

        sealed class Accessor<T> {
            readonly DOGetter<T> _getter;
            readonly DOSetter<T> _setter;
            public Accessor(DOGetter<T> getter, DOSetter<T> setter) { _getter = getter; _setter = setter; }
            public T Get() { return _getter(); }
            public void Set(T value) { _setter(value); }
        }
    }

    /// <summary>DOTween's DOVirtual helpers.</summary>
    public static class DOVirtual {
        public static Tween Float(float from, float to, float duration, TweenCallback<float> onVirtualUpdate) {
            return Raven.Value(from, to, duration).OnUpdate(v => onVirtualUpdate(v));
        }

        public static Tween Vector3(Vector3 from, Vector3 to, float duration, TweenCallback<Vector3> onVirtualUpdate) {
            return Raven.Value(from, to, duration).OnUpdate((TweenValue v) => onVirtualUpdate(v.Vector3));
        }

        public static Tween Color(Color from, Color to, float duration, TweenCallback<Color> onVirtualUpdate) {
            return Raven.Value(from, to, duration).OnUpdate((TweenValue v) => onVirtualUpdate(v.Color));
        }

        /// <summary>Like DOTween, ignores Time.timeScale unless <paramref name="ignoreTimeScale"/> is false.</summary>
        public static Tween DelayedCall(float delay, TweenCallback callback, bool ignoreTimeScale = true) {
            return Raven.Delay(delay, () => callback()).UnscaledTime(ignoreTimeScale);
        }
    }
}
