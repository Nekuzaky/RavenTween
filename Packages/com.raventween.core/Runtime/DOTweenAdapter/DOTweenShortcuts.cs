using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.DOTweenAdapter {
    /// <summary>DOTween's shortcut extensions (DOMove, DOFade, …), running on RavenTween.</summary>
    public static class DOTweenShortcuts {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        // ----- Transform -----

        public static Tween DOMove(this Transform target, Vector3 endValue, float duration) { return Raven.Position(target, endValue, duration); }
        public static Tween DOMoveX(this Transform target, float endValue, float duration) { return Raven.PositionX(target, endValue, duration); }
        public static Tween DOMoveY(this Transform target, float endValue, float duration) { return Raven.PositionY(target, endValue, duration); }
        public static Tween DOMoveZ(this Transform target, float endValue, float duration) { return Raven.PositionZ(target, endValue, duration); }
        public static Tween DOLocalMove(this Transform target, Vector3 endValue, float duration) { return Raven.LocalPosition(target, endValue, duration); }
        public static Tween DOLocalMoveX(this Transform target, float endValue, float duration) { return Raven.LocalPositionX(target, endValue, duration); }
        public static Tween DOLocalMoveY(this Transform target, float endValue, float duration) { return Raven.LocalPositionY(target, endValue, duration); }
        public static Tween DOLocalMoveZ(this Transform target, float endValue, float duration) { return Raven.LocalPositionZ(target, endValue, duration); }

        /// <summary>Shortest-path rotation, like DOTween's default RotateMode.Fast.</summary>
        public static Tween DORotate(this Transform target, Vector3 endValue, float duration) { return Raven.Rotation(target, Quaternion.Euler(endValue), duration); }
        public static Tween DOLocalRotate(this Transform target, Vector3 endValue, float duration) { return Raven.LocalRotation(target, Quaternion.Euler(endValue), duration); }
        public static Tween DORotateQuaternion(this Transform target, Quaternion endValue, float duration) { return Raven.Rotation(target, endValue, duration); }
        public static Tween DOLocalRotateQuaternion(this Transform target, Quaternion endValue, float duration) { return Raven.LocalRotation(target, endValue, duration); }

        public static Tween DOScale(this Transform target, Vector3 endValue, float duration) { return Raven.Scale(target, endValue, duration); }
        public static Tween DOScale(this Transform target, float endValue, float duration) { return Raven.Scale(target, endValue, duration); }
        public static Tween DOScaleX(this Transform target, float endValue, float duration) { return Raven.ScaleX(target, endValue, duration); }
        public static Tween DOScaleY(this Transform target, float endValue, float duration) { return Raven.ScaleY(target, endValue, duration); }
        public static Tween DOScaleZ(this Transform target, float endValue, float duration) { return Raven.ScaleZ(target, endValue, duration); }

        // DOTween's vibrato counts oscillations over the whole effect; RavenTween's frequency is per second.
        public static Tween DOPunchPosition(this Transform target, Vector3 punch, float duration, int vibrato = 10) {
            return Raven.PunchPosition(target, punch, duration, Frequency(vibrato, duration));
        }

        public static Tween DOPunchRotation(this Transform target, Vector3 punch, float duration, int vibrato = 10) {
            return Raven.PunchRotation(target, punch, duration, Frequency(vibrato, duration));
        }

        public static Tween DOPunchScale(this Transform target, Vector3 punch, float duration, int vibrato = 10) {
            return Raven.PunchScale(target, punch, duration, Frequency(vibrato, duration));
        }

        public static Tween DOShakePosition(this Transform target, float duration, float strength = 1f, int vibrato = 10) {
            return Raven.ShakePosition(target, Vector3.one * strength, duration, Frequency(vibrato, duration));
        }

        public static Tween DOShakePosition(this Transform target, float duration, Vector3 strength, int vibrato = 10) {
            return Raven.ShakePosition(target, strength, duration, Frequency(vibrato, duration));
        }

        public static Tween DOShakeRotation(this Transform target, float duration, float strength = 90f, int vibrato = 10) {
            return Raven.ShakeRotation(target, Vector3.one * strength, duration, Frequency(vibrato, duration));
        }

        public static Tween DOShakeScale(this Transform target, float duration, float strength = 1f, int vibrato = 10) {
            return Raven.ShakeScale(target, Vector3.one * strength, duration, Frequency(vibrato, duration));
        }

        static float Frequency(int vibrato, float duration) {
            return Mathf.Max(vibrato, 1) / Mathf.Max(duration, 0.01f);
        }

        /// <summary>Stops (or completes) every tween on this component. Returns how many.</summary>
        public static int DOKill(this Component target, bool complete = false) {
            return complete ? Raven.CompleteAll(target) : Raven.StopAll(target);
        }

        public static int DOComplete(this Component target) { return Raven.CompleteAll(target); }
        public static int DOPause(this Component target) { return Raven.PauseAll(target); }
        public static int DOPlay(this Component target) { return Raven.ResumeAll(target); }

        // ----- UI -----

        public static Tween DOAnchorPos(this RectTransform target, Vector2 endValue, float duration) { return Raven.AnchoredPosition(target, endValue, duration); }
        public static Tween DOAnchorPosX(this RectTransform target, float endValue, float duration) { return Raven.AnchoredPositionX(target, endValue, duration); }
        public static Tween DOAnchorPosY(this RectTransform target, float endValue, float duration) { return Raven.AnchoredPositionY(target, endValue, duration); }
        public static Tween DOSizeDelta(this RectTransform target, Vector2 endValue, float duration) { return Raven.SizeDelta(target, endValue, duration); }
        public static Tween DOFade(this CanvasGroup target, float endValue, float duration) { return Raven.Alpha(target, endValue, duration); }
        public static Tween DOColor(this Graphic target, Color endValue, float duration) { return Raven.Color(target, endValue, duration); }
        public static Tween DOFade(this Graphic target, float endValue, float duration) { return Raven.Alpha(target, endValue, duration); }

        // ----- Rendering, camera, audio, light -----

        public static Tween DOColor(this SpriteRenderer target, Color endValue, float duration) { return Raven.Color(target, endValue, duration); }
        public static Tween DOFade(this SpriteRenderer target, float endValue, float duration) { return Raven.Alpha(target, endValue, duration); }
        public static Tween DOColor(this Material target, Color endValue, float duration) { return Raven.MaterialColor(target, ColorId, endValue, duration); }
        public static Tween DOColor(this Material target, Color endValue, string property, float duration) { return Raven.MaterialColor(target, property, endValue, duration); }
        public static Tween DOFloat(this Material target, float endValue, string property, float duration) { return Raven.MaterialFloat(target, property, endValue, duration); }

        public static Tween DOFade(this Material target, float endValue, float duration) {
            return Raven.CustomTo(target, m => m.color.a, endValue, duration, (m, a) => {
                Color c = m.color;
                c.a = a;
                m.color = c;
            });
        }

        public static Tween DOFieldOfView(this Camera target, float endValue, float duration) { return Raven.FieldOfView(target, endValue, duration); }
        public static Tween DOOrthoSize(this Camera target, float endValue, float duration) { return Raven.OrthographicSize(target, endValue, duration); }
        public static Tween DOColor(this Camera target, Color endValue, float duration) { return Raven.BackgroundColor(target, endValue, duration); }

        public static Tween DOShakePosition(this Camera target, float duration, float strength = 3f, int vibrato = 10) {
            return Raven.ShakePosition(target.transform, Vector3.one * strength, duration, Frequency(vibrato, duration));
        }

        public static Tween DOFade(this AudioSource target, float endValue, float duration) { return Raven.Volume(target, endValue, duration); }
        public static Tween DOPitch(this AudioSource target, float endValue, float duration) { return Raven.Pitch(target, endValue, duration); }
        public static Tween DOIntensity(this Light target, float endValue, float duration) { return Raven.Intensity(target, endValue, duration); }
        public static Tween DOColor(this Light target, Color endValue, float duration) { return Raven.Color(target, endValue, duration); }
    }

    /// <summary>DOTween's tween settings and control methods (SetEase, SetLoops, Kill, …).</summary>
    public static class DOTweenSettings {
        public static Tween SetEase(this Tween tween, Ease ease) { return tween.Ease(ease); }
        public static Tween SetEase(this Tween tween, AnimationCurve curve) { return tween.Ease(curve); }

        /// <summary>OutBack / InBack with DOTween's overshoot (1.70158 = default).</summary>
        public static Tween SetEase(this Tween tween, Ease ease, float overshoot) {
            return ease == Ease.OutBack ? tween.Ease(Easing.Overshoot(overshoot / 1.70158f)) : tween.Ease(ease);
        }

        public static Tween SetLoops(this Tween tween, int loops, LoopType loopType = LoopType.Restart) {
            return tween.Cycles(loops < 0 ? -1 : Mathf.Max(loops, 1), Map(loopType));
        }

        public static Sequence SetLoops(this Sequence sequence, int loops, LoopType loopType = LoopType.Restart) {
            return sequence.Cycles(loops < 0 ? -1 : Mathf.Max(loops, 1), Map(loopType));
        }

        static CycleMode Map(LoopType loopType) {
            switch (loopType) {
                case LoopType.Yoyo: return CycleMode.Yoyo;
                case LoopType.Incremental: return CycleMode.Incremental;
                default: return CycleMode.Restart;
            }
        }

        public static Tween SetDelay(this Tween tween, float delay) { return tween.Delay(delay); }
        public static Sequence SetDelay(this Sequence sequence, float delay) { return sequence.Delay(delay); }

        public static Tween SetUpdate(this Tween tween, bool isIndependentUpdate) { return tween.UnscaledTime(isIndependentUpdate); }
        public static Sequence SetUpdate(this Sequence sequence, bool isIndependentUpdate) { return sequence.UnscaledTime(isIndependentUpdate); }

        public static Tween SetUpdate(this Tween tween, UpdateType updateType, bool isIndependentUpdate = false) {
            return tween.UpdateIn(Map(updateType)).UnscaledTime(isIndependentUpdate);
        }

        public static Sequence SetUpdate(this Sequence sequence, UpdateType updateType, bool isIndependentUpdate = false) {
            return sequence.UpdateIn(Map(updateType)).UnscaledTime(isIndependentUpdate);
        }

        static UpdatePhase Map(UpdateType updateType) {
            switch (updateType) {
                case UpdateType.Late: return UpdatePhase.LateUpdate;
                case UpdateType.Fixed: return UpdatePhase.FixedUpdate;
                default: return UpdatePhase.Update;
            }
        }

        public static Tween OnComplete(this Tween tween, TweenCallback callback) { return tween.OnComplete(() => callback()); }
        public static Tween OnStart(this Tween tween, TweenCallback callback) { return tween.OnStart(() => callback()); }
        public static Tween OnUpdate(this Tween tween, TweenCallback callback) { return tween.OnUpdate(() => callback()); }
        public static Tween OnKill(this Tween tween, TweenCallback callback) { return tween.OnKill(() => callback()); }

        public static void Kill(this Tween tween, bool complete = false) { if (complete) { tween.Complete(); } else { tween.Stop(); } }
        public static void Kill(this Sequence sequence, bool complete = false) { if (complete) { sequence.Complete(); } else { sequence.Stop(); } }
        public static void Play(this Tween tween) { tween.Resume(); }
        public static void Play(this Sequence sequence) { sequence.Resume(); }
        public static void TogglePause(this Tween tween) { if (tween.IsPaused) { tween.Resume(); } else { tween.Pause(); } }
        public static bool IsActive(this Tween tween) { return tween.IsAlive; }
        public static bool IsActive(this Sequence sequence) { return sequence.IsAlive; }
        public static bool IsPlaying(this Tween tween) { return tween.IsAlive && !tween.IsPaused; }
        public static bool IsPlaying(this Sequence sequence) { return sequence.IsAlive && !sequence.IsPaused; }
        public static bool IsComplete(this Tween tween) { return !tween.IsAlive; }
        public static float Elapsed(this Tween tween, bool includeLoops = true) {
            return includeLoops ? tween.CyclesDone * tween.Duration + tween.ElapsedTime : tween.ElapsedTime;
        }
        public static float ElapsedPercentage(this Tween tween, bool includeLoops = true) { return includeLoops ? tween.ProgressTotal : tween.Progress; }

        /// <summary>Jumps to <paramref name="to"/> seconds; pauses the tween unless <paramref name="andPlay"/>.</summary>
        public static void Goto(this Tween tween, float to, bool andPlay = false) {
            tween.ElapsedTimeTotal = to;
            if (andPlay) { tween.Resume(); } else { tween.Pause(); }
        }

        public static CustomYieldInstruction WaitForCompletion(this Tween tween) { return tween.ToYieldInstruction(); }
        public static CustomYieldInstruction WaitForCompletion(this Sequence sequence) { return sequence.ToYieldInstruction(); }

        public static Task AsyncWaitForCompletion(this Tween tween) {
            var source = new TaskCompletionSource<bool>();
            tween.GetAwaiter().OnCompleted(() => source.TrySetResult(true));
            return source.Task;
        }

        public static Task AsyncWaitForCompletion(this Sequence sequence) {
            var source = new TaskCompletionSource<bool>();
            sequence.GetAwaiter().OnCompleted(() => source.TrySetResult(true));
            return source.Task;
        }

        // ----- Sequences -----

        public static Sequence Append(this Sequence sequence, Tween tween) { return sequence.Chain(tween); }
        public static Sequence Append(this Sequence sequence, Sequence nested) { return sequence.Chain(nested); }
        public static Sequence Join(this Sequence sequence, Tween tween) { return sequence.Group(tween); }
        public static Sequence Join(this Sequence sequence, Sequence nested) { return sequence.Group(nested); }
        public static Sequence AppendInterval(this Sequence sequence, float interval) { return sequence.ChainDelay(interval); }
        public static Sequence AppendCallback(this Sequence sequence, TweenCallback callback) { return sequence.ChainCallback(() => callback()); }
        public static Sequence InsertCallback(this Sequence sequence, float atPosition, TweenCallback callback) { return sequence.InsertCallback(atPosition, () => callback()); }
        public static Sequence OnComplete(this Sequence sequence, TweenCallback callback) { return sequence.OnComplete(() => callback()); }
    }
}
