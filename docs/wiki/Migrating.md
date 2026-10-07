# Migrating

Coming from DOTween or PrimeTween? Most of your knowledge transfers directly. This page lists the equivalents and the few differences in behavior. For DOTween projects, a built-in **adapter** lets most existing code compile unchanged.

---

## Equivalents

| DOTween | PrimeTween | RavenTween |
| :--- | :--- | :--- |
| `transform.DOMove(p, d)` | `Tween.Position(t, p, d)` | `Raven.Position(t, p, d)` |
| `transform.DOMoveX(x, d)` | `Tween.PositionX(t, x, d)` | `Raven.PositionX(t, x, d)` |
| `transform.DOLocalMove(p, d)` | `Tween.LocalPosition(t, p, d)` | `Raven.LocalPosition(t, p, d)` |
| `transform.DORotateQuaternion(q, d)` | `Tween.Rotation(t, q, d)` | `Raven.Rotation(t, q, d)` |
| `transform.DOScale(s, d)` | `Tween.Scale(t, s, d)` | `Raven.Scale(t, s, d)` |
| `transform.DOScaleY(y, d)` | `Tween.ScaleY(t, y, d)` | `Raven.ScaleY(t, y, d)` |
| `rect.DOAnchorPosX(x, d)` | `Tween.UIAnchoredPositionX(r, x, d)` | `Raven.AnchoredPositionX(r, x, d)` |
| `canvasGroup.DOFade(a, d)` | `Tween.Alpha(cg, a, d)` | `Raven.Alpha(cg, a, d)` |
| `image.DOColor(c, d)` | `Tween.Color(img, c, d)` | `Raven.Color(img, c, d)` |
| `rigidbody.DOMove(p, d)` | `Tween.RigidbodyMovePosition(rb, p, d)` | `rigidbody.TweenMovePosition(p, d)` |
| `DOVirtual.Float(a, b, d, cb)` | `Tween.Custom(a, b, d, cb)` | `Raven.Value(a, b, d).OnUpdate(cb)` |
| `DOTween.To(getter, setter, …)` | `Tween.Custom(target, a, b, d, setter)` | `Raven.Custom(target, a, b, d, setter)` or `Raven.CustomTo(target, getter, b, d, setter)` |
| `DOVirtual.DelayedCall(d, cb)` | `Tween.Delay(d, cb)` | `Raven.Delay(d, cb)` |
| `transform.DOShakePosition(d, s)` | `Tween.ShakeLocalPosition(t, s, d)` | `Raven.ShakePosition(t, s, d)` |
| `camera.DOShakePosition(d, s)` | `Tween.ShakeCamera(cam, s)` | `Raven.ShakeCamera(cam, s)` |
| `transform.DOPunchScale(p, d)` | `Tween.PunchScale(t, p, d)` | `Raven.PunchScale(t, p, d)` |
| `DOTween.Sequence()` | `Sequence.Create()` | `Raven.Sequence()` |
| `.Append(x)` | `.Chain(x)` | `.Chain(x)` |
| `.Join(x)` | `.Group(x)` | `.Group(x)` |
| `.Insert(t, x)` | `.Insert(t, x)` | `.Insert(t, x)` |
| `.AppendInterval(s)` | `.ChainDelay(s)` | `.ChainDelay(s)` |
| `.AppendCallback(cb)` | `.ChainCallback(cb)` | `.ChainCallback(cb)` |
| `.InsertCallback(t, cb)` | `.InsertCallback(t, cb)` | `.InsertCallback(t, cb)` |
| `.SetEase(Ease.OutQuad)` | `ease: Ease.OutQuad` | `.Ease(Ease.OutQuad)` |
| `.SetEase(Ease.OutBack, 3f)` | `Easing.Overshoot(…)` | `.Ease(Easing.Overshoot(…))` |
| `.SetLoops(n, LoopType.Yoyo)` | `cycles: n, cycleMode: CycleMode.Rewind` | `.Cycles(n, CycleMode.Yoyo)` |
| `.SetLoops(n, LoopType.Incremental)` | `cycleMode: CycleMode.Incremental` | `.Cycles(n, CycleMode.Incremental)` |
| `.SetLoops(-1)` | `cycles: -1` | `.Infinite(CycleMode.Restart)` |
| `.SetDelay(s)` | `startDelay: s` | `.Delay(s)` |
| `.From(v)` | `startValue: v` | `.From(v)` |
| `.SetUpdate(true)` | `useUnscaledTime: true` | `.UnscaledTime()` |
| `.SetUpdate(UpdateType.Fixed)` | `updateType: UpdateType.FixedUpdate` | `.UpdateIn(UpdatePhase.FixedUpdate)` |
| `.timeScale = 2f` | `.timeScale = 2f` | `.TimeScale = 2f` |
| `.Goto(t)` | `.elapsedTimeTotal = t` | `.ElapsedTimeTotal = t` |
| `.ElapsedPercentage()` | `.progressTotal` | `.ProgressTotal` |
| `.OnComplete(cb)` | `.OnComplete(cb)` / `.OnComplete(target, cb)` | `.OnComplete(cb)` / `.OnComplete(target, cb)` |
| `.Kill()` | `.Stop()` | `.Stop()` |
| `.Complete()` | `.Complete()` | `.Complete()` |
| `.Pause()` / `.Play()` | `.isPaused = …` | `.Pause()` / `.Resume()` |
| `.IsActive()` | `.isAlive` | `.IsAlive` |
| `.WaitForCompletion()` | `.ToYieldInstruction()` | `.ToYieldInstruction()` |
| `await t.AsyncWaitForCompletion()` | `await tween` | `await tween` |
| `DOTween.KillAll()` | `Tween.StopAll()` | `Raven.StopAll()` |
| `transform.DOKill()` | `Tween.StopAll(onTarget: t)` | `Raven.StopAll(t)` |
| `DOTween.timeScale` | `Tween.globalTimeScale` | `Raven.TimeScale` |
| `DOTween.SetTweensCapacity(n, m)` | `PrimeTweenConfig.SetTweensCapacity(n)` | `Raven.SetCapacity(n)` |

> [!NOTE]
> **Yoyo naming.** RavenTween's `Yoyo` replays the cycle backwards in time, like DOTween's `LoopType.Yoyo` and PrimeTween's `CycleMode.Rewind`. PrimeTween's `CycleMode.Yoyo` (same ease on the way back) is RavenTween's `CycleMode.PingPong`.

---

## The DOTween adapter

The `RavenTween.DOTweenAdapter` namespace provides DOTween's names on top of RavenTween. In a file, replace

```csharp
using DG.Tweening;
```

with

```csharp
using RavenTween;
using RavenTween.DOTweenAdapter;
```

and code like this compiles and runs unchanged:

```csharp
transform.DOMoveY(2f, 0.4f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
DOTween.Sequence()
    .Append(panel.DOAnchorPos(Vector2.zero, 0.5f))
    .Join(group.DOFade(1f, 0.5f))
    .AppendInterval(1f)
    .AppendCallback(() => Debug.Log("shown"));
DOTween.To(() => volume, v => volume = v, 0f, 1f);
DOVirtual.DelayedCall(2f, Respawn);
transform.DOKill();
```

It covers the transform, UI, sprite, material, camera, audio and light shortcuts, punches and shakes, `SetEase` / `SetLoops` / `SetDelay` / `SetUpdate`, sequences (`Append`, `Join`, `Insert`, `AppendInterval`, `AppendCallback`, `InsertCallback`), `Kill` / `Complete` / `Pause` / `Play` / `Goto`, `WaitForCompletion`, `AsyncWaitForCompletion`, `DOTween.To`, `DOVirtual` and the global `DOTween` calls.

Anything it doesn't cover fails to compile, which points you straight to the lines to convert by hand — usually `SetRelative`, `SetAutoKill(false)` + `Restart()`, `Prepend`, `DOPath` and the `Flash` eases. The adapter lives in its own namespace, so it never conflicts with DOTween while both are installed: migrate one file at a time.

> [!TIP]
> The adapter is a bridge, not a destination. New code reads better with RavenTween's own API, and the adapter's `DOTween.To`, `DOVirtual` and `TweenCallback` overloads wrap your delegates in small allocations that RavenTween's native calls avoid.

---

## From DOTween

> [!IMPORTANT]
> **Tweens are single-use.** DOTween lets you keep a tween alive with `SetAutoKill(false)` and replay it with `Restart()`. RavenTween doesn't: create a new tween each time — it's free, because slots are pooled. For a reusable configuration, use a **[Tween Template](Designer-Workflow)**, a `TweenSettings<T>` or a `TweenParams` field.

- **No `Play()` needed** — tweens start on creation.
- **No setup call** — there is no `DOTween.Init()`; the pool grows on demand. `Raven.SetCapacity(n)` reserves slots up front if you want.
- **No `SetLink` / `SetTarget`** — every tween on a Unity object already dies with it, and `Raven.StopAll(target)` finds tweens by what they animate.
- **Sequence children are driven by their sequence.** Calling `Stop()`, `Complete()` or `Pause()` on a tween after adding it to a sequence logs a warning and does nothing: control the sequence instead. Set `Delay()` and `Cycles()` before adding the tween.
- **The handle is already dead inside `OnComplete`**: `IsAlive` is `false` there, so starting a new tween from the callback is safe.
- **Both libraries can coexist during a migration.** Extension methods don't collide (`TweenPosition` vs. `DOMove`), but both define `Ease`, `Tween` and `Sequence`: in a file that imports both namespaces, qualify them (`RavenTween.Ease.OutQuad`) or add an alias (`using Ease = RavenTween.Ease;`).

## From PrimeTween

- **Options are chained instead of passed as parameters**: `Raven.Position(t, p, 0.5f).Ease(Ease.OutQuad).Delay(0.1f)` rather than named arguments. `TweenSettings<T>` plays the role of PrimeTween's inspector settings.
- **Value tweens use `OnUpdate`**: `Raven.Value(a, b, d).OnUpdate(cb)`. The allocation-free `Raven.Custom(target, …)` matches PrimeTween's `Tween.Custom(target, …)`.
- **`Tween.Infinite()` defaults to Yoyo**, the most common loop for tweens; pass `CycleMode.Restart` to override. `Sequence.Infinite()` defaults to Restart.
- **Cycle modes** — see the note above: PrimeTween's `Rewind` is RavenTween's `Yoyo`, PrimeTween's `Yoyo` is RavenTween's `PingPong`.
- **Inspector tooling** — Tween Templates, the Raven Animator / Sequence Player components, the visual Sequence Editor and the **[Monitor](Monitor)** are included at no extra cost.

---

#### ◀ **[Performance](Performance)**  ·  Next: **[Reference ▶](Reference)**
