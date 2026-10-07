# Control and Lifecycle

This page covers everything that happens around a tween: its callbacks, controlling it after creation, time, what happens when objects are destroyed, and waiting for it from `async` code or coroutines.

---

## The life of a tween

1. **Created** — the factory call rents a pooled slot and returns a handle. The tween is already scheduled.
2. **Delay** — if `.Delay()` was set, it waits.
3. **Started** — the start value is captured from the target and `OnStart` fires.
4. **Updating** — every frame the value is written and `OnUpdate` fires.
5. **Ended** — either **completed** (`OnComplete`) or **stopped** (`OnKill`), or its target was **destroyed** (`OnTargetDestroyed`). The slot returns to the pool and the handle goes dead.

---

## Callbacks

```csharp
Raven.Position(enemy, target, 0.8f)
    .OnStart(() => footsteps.Play())
    .OnUpdate(() => UpdateShadow())
    .OnComplete(() => enemy.Attack())
    .OnKill(() => footsteps.Stop())
    .OnTargetDestroyed(() => Debug.Log("Enemy died mid-move"));
```

| Callback | Fires |
| :--- | :--- |
| `OnStart` | Once, when the tween actually starts (after its delay). |
| `OnUpdate` | Every frame after the value is applied. Also available as `OnUpdate(Action<float>)` and `OnUpdate(Action<TweenValue>)` to receive the value. |
| `OnComplete` | When the tween reaches its end, or when you call `Complete()`. |
| `OnKill` | When you call `Stop()` before it finished. |
| `OnTargetDestroyed` | When the target object was destroyed while the tween was alive. |

> [!NOTE]
> An exception thrown inside a callback is logged and contained — it never stops other tweens from updating.

---

## Controlling a tween

| Call | Effect |
| :--- | :--- |
| `Pause()` / `Resume()` | Freeze and unfreeze. A paused tween keeps its slot. |
| `Stop()` | End now, leaving the target where it is. Fires `OnKill`. |
| `Complete()` | Jump to the end value and fire `OnComplete`. |
| `Start()` | Tweens already play on creation; `Start()` documents intent and resumes a paused tween. |
| `IsAlive` | `true` while running or paused. |
| `IsPaused` | `true` while paused. |

Globally:

```csharp
Raven.StopAll();       // stop everything
Raven.CompleteAll();   // finish everything instantly — handy when skipping a cutscene
int live = Raven.AliveCount;
```

### Dead handles are safe

A `Tween` handle is a 2-number struct: a slot index and a version. When the tween ends, the version moves on, so an old handle can never touch a tween that later reuses the same slot. Every method on a dead handle simply does nothing.

```csharp
Tween fade = Raven.Alpha(group, 0f, 0.3f);
// ...much later, the tween is long finished:
fade.Stop();   // safe, does nothing
```

---

## Time

| Setting | Effect |
| :--- | :--- |
| `Time.timeScale` | Scales every tween, like the rest of your game. At `0`, tweens freeze. |
| `.UnscaledTime()` | This tween ignores `Time.timeScale` — use it for pause menus. |
| `Raven.TimeScale` | An extra multiplier applied to all tweens only, independent of `Time.timeScale`. |
| `Raven.UpdatePhase` | `UpdatePhase.Update` (default) or `UpdatePhase.LateUpdate`, to run after your own `Update` scripts. |

```csharp
// Pause the game, keep the pause menu animating.
Time.timeScale = 0f;
Raven.AnchoredPosition(pauseMenu, Vector2.zero, 0.3f).UnscaledTime();
```

---

## Destroyed objects and scene changes

Destroying a GameObject mid-tween — or loading a new scene — is always safe. On its next update the tween notices the target is gone, fires `OnTargetDestroyed` if you subscribed, and returns its slot to the pool. It never throws a `MissingReferenceException`.

> [!TIP]
> Value tweens (`Raven.Value`) and custom tweens on plain C# objects have no Unity target to watch. If their callbacks touch objects that may be destroyed, stop them in your component's `OnDestroy`.

---

## async / await

Any `Tween` or `Sequence` can be awaited:

```csharp
async void ShowReward() {
    await Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack);
    await Raven.Delay(1f);
    await Raven.Alpha(panelGroup, 0f, 0.3f);
}
```

`ToCompletion()` returns the same awaiter, for readability. The await resumes when the tween **ends for any reason** — completed, stopped or target destroyed. Check `IsAlive` or your own state if the difference matters.

Awaiting works on every platform, WebGL included: RavenTween resumes your code on the main thread from its own update, with no threads involved.

---

## Coroutines

```csharp
IEnumerator Intro() {
    yield return Raven.Scale(logo, 1f, 0.4f).From(0f).ToYieldInstruction();
    yield return Raven.Delay(0.5f).ToYieldInstruction();
    yield return BuildMenuSequence().ToYieldInstruction();
}
```

---

## Enter Play Mode options

RavenTween resets its engine on `SubsystemRegistration`, so it works with **Enter Play Mode Options** and domain reload disabled.

---

#### ◀ **[TextMeshPro](TextMeshPro)**  ·  Next: **[Designer Workflow ▶](Designer-Workflow)**
