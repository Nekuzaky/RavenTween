# Migrating

Coming from DOTween or PrimeTween? Most of your knowledge transfers directly. This page lists the equivalents and the few differences in behavior.

---

## Equivalents

| DOTween | PrimeTween | RavenTween |
| :--- | :--- | :--- |
| `transform.DOMove(p, d)` | `Tween.Position(t, p, d)` | `Raven.Position(t, p, d)` |
| `transform.DOLocalMove(p, d)` | `Tween.LocalPosition(t, p, d)` | `Raven.LocalPosition(t, p, d)` |
| `transform.DORotateQuaternion(q, d)` | `Tween.Rotation(t, q, d)` | `Raven.Rotation(t, q, d)` |
| `transform.DOScale(s, d)` | `Tween.Scale(t, s, d)` | `Raven.Scale(t, s, d)` |
| `canvasGroup.DOFade(a, d)` | `Tween.Alpha(cg, a, d)` | `Raven.Alpha(cg, a, d)` |
| `image.DOColor(c, d)` | `Tween.Color(img, c, d)` | `Raven.Color(img, c, d)` |
| `DOVirtual.Float(a, b, d, cb)` | `Tween.Custom(a, b, d, cb)` | `Raven.Value(a, b, d).OnUpdate(cb)` |
| `DOTween.To(getter, setter, …)` | `Tween.Custom(target, a, b, d, setter)` | `Raven.Custom(target, a, b, d, setter)` |
| `DOVirtual.DelayedCall(d, cb)` | `Tween.Delay(d, cb)` | `Raven.Delay(d, cb)` |
| `transform.DOShakePosition(d, s)` | `Tween.ShakeLocalPosition(t, s, d)` | `Raven.ShakePosition(t, s, d)` |
| `transform.DOPunchScale(p, d)` | `Tween.PunchScale(t, p, d)` | `Raven.PunchScale(t, p, d)` |
| `DOTween.Sequence()` | `Sequence.Create()` | `Raven.Sequence()` |
| `.Append(x)` | `.Chain(x)` | `.Chain(x)` |
| `.Join(x)` | `.Group(x)` | `.Group(x)` |
| `.Insert(t, x)` | `.Insert(t, x)` | `.Insert(t, x)` |
| `.AppendInterval(s)` | `.ChainDelay(s)` | `.ChainDelay(s)` |
| `.SetEase(Ease.OutQuad)` | `ease: Ease.OutQuad` | `.Ease(Ease.OutQuad)` |
| `.SetLoops(n, LoopType.Yoyo)` | `cycles: n, cycleMode: CycleMode.Yoyo` | `.Cycles(n, CycleMode.Yoyo)` |
| `.SetLoops(-1)` | `cycles: -1` | `.Infinite(CycleMode.Restart)` |
| `.SetLoops(-1, LoopType.Yoyo)` | `cycles: -1, cycleMode: CycleMode.Yoyo` | `.Infinite()` |
| `.SetDelay(s)` | `startDelay: s` | `.Delay(s)` |
| `.From(v)` | `startValue: v` | `.From(v)` |
| `.SetUpdate(true)` | `useUnscaledTime: true` | `.UnscaledTime()` |
| `.OnComplete(cb)` | `.OnComplete(cb)` | `.OnComplete(cb)` |
| `.Kill()` | `.Stop()` | `.Stop()` |
| `.Complete()` | `.Complete()` | `.Complete()` |
| `.Pause()` / `.Play()` | `.isPaused = …` | `.Pause()` / `.Resume()` |
| `.IsActive()` | `.isAlive` | `.IsAlive` |
| `.WaitForCompletion()` | `.ToYieldInstruction()` | `.ToYieldInstruction()` |
| `await t.AsyncWaitForCompletion()` | `await tween` | `await tween` |
| `DOTween.KillAll()` | `Tween.StopAll()` | `Raven.StopAll()` |
| `DOTween.timeScale` | `Tween.globalTimeScale` | `Raven.TimeScale` |

---

## From DOTween

> [!IMPORTANT]
> **Tweens are single-use.** DOTween lets you keep a tween alive with `SetAutoKill(false)` and replay it with `Restart()`. RavenTween doesn't: create a new tween each time — it's free, because slots are pooled. For a reusable configuration, use a **[Tween Template](Designer-Workflow)** or a `TweenParams` field.

- **No `Play()` needed** — tweens start on creation.
- **No setup call** — there is no `DOTween.Init()` and no capacity to configure; the pool grows on demand.
- **No `SetLink` / `SetTarget`** — every tween on a Unity object already dies with it.
- **Sequence children are driven by their sequence.** Calling `Stop()`, `Complete()` or `Pause()` on a tween after adding it to a sequence logs a warning and does nothing: control the sequence instead. Set `Delay()` and `Cycles()` before adding the tween.
- **The handle is already dead inside `OnComplete`**: `IsAlive` is `false` there, so starting a new tween from the callback is safe.
- **Both libraries can coexist during a migration.** Extension methods don't collide (`TweenPosition` vs. `DOMove`), but both define `Ease`, `Tween` and `Sequence`: in a file that imports both namespaces, qualify them (`RavenTween.Ease.OutQuad`) or add an alias (`using Ease = RavenTween.Ease;`).

## From PrimeTween

- **Options are chained instead of passed as parameters**: `Raven.Position(t, p, 0.5f).Ease(Ease.OutQuad).Delay(0.1f)` rather than named arguments.
- **Value tweens use `OnUpdate`**: `Raven.Value(a, b, d).OnUpdate(cb)`. The allocation-free `Raven.Custom(target, …)` matches PrimeTween's `Tween.Custom(target, …)`.
- **`Tween.Infinite()` defaults to Yoyo**, the most common loop for tweens; pass `CycleMode.Restart` to override. `Sequence.Infinite()` defaults to Restart.
- **Inspector tooling** — Tween Templates, the Raven Animator / Sequence Player components and the **[Monitor](Monitor)** are included at no extra cost.

---

#### ◀ **[Performance](Performance)**  ·  Next: **[Reference ▶](Reference)**
