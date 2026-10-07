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

The tween is already released when `OnComplete`, `OnKill` or `OnTargetDestroyed` runs: its handle is dead (`IsAlive` is `false`), so starting a new tween from these callbacks — even on the same target — is safe. Calling `Complete()` or `Stop()` on a tween from its own `OnUpdate` is safe too.

> [!NOTE]
> An exception thrown inside a callback is logged and contained — it never stops other tweens from updating. An exception thrown by a custom ease or a custom setter is logged and stops the tween that threw it (or the sequence it belongs to).

### Callbacks without allocation

A lambda that uses a field or a local of your script allocates a small closure when the tween is created. For tweens created very often, pass the object the callback needs as a **target** and use a lambda that only uses its parameters:

```csharp
Raven.Scale(icon, 1.2f, 0.2f).OnComplete(this, self => self.OnPopped());
Raven.Value(0f, 1f, 2f).OnUpdate(healthBar, (bar, tween) => bar.fillAmount = tween.Progress);
```

Both allocate nothing. A tween takes one target-based `OnComplete` and one target-based `OnUpdate`, on top of any number of regular callbacks. The callback is skipped if the target is a Unity object that has been destroyed.

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
| `SetRemainingCycles(n)` | Change how many cycles remain, the current one included: `1` ends a loop cleanly at the end of this cycle. |
| `SetRemainingCycles(true)` | For Yoyo and PingPong loops: complete the next time the tween reaches its end value (`false`: its start value). |
| `.WithCancellation(token)` | Stop the tween (firing `OnKill`) as soon as a `CancellationToken` is cancelled. No allocation. |

### Where a tween is in time

Every handle exposes its timing. The setters jump the tween there immediately, forwards or backwards; jumping past the end completes it. A jump is **silent**: `OnStart`, `OnComplete` and timeline callbacks of what it passes over don't fire (`OnUpdate` does, since values change). Everything after the new time fires normally as the tween plays on. To finish a tween *with* its callbacks, use `Complete()`.

| Property | Meaning |
| :--- | :--- |
| `Duration` | One cycle, in seconds, without the delay. |
| `DurationTotal` | Delay + all cycles. Infinity when looping forever. |
| `ElapsedTime` *(settable)* | Seconds into the current cycle. |
| `ElapsedTimeTotal` *(settable)* | Seconds since the tween was created, delay included. |
| `Progress` *(settable)* | 0–1 through the current cycle. |
| `ProgressTotal` *(settable)* | 0–1 through the whole tween (0 when infinite). |
| `CyclesDone`, `CyclesTotal` | Cycles completed so far; total (`-1` = infinite). |
| `InterpolationFactor` | The eased factor shown right now: 0 on the start value, 1 on the end value. |
| `TimeScale` *(settable)* | Speed of this tween only: `0.5` = half speed, `0` = frozen. |

```csharp
Tween spin = Raven.LocalEulerAngles(wheel, new Vector3(0f, 0f, 360f), 1f).Infinite(CycleMode.Restart);
spin.TimeScale = 3f;                         // spin faster
Raven.TweenTimeScale(spin, 0f, 2f);          // ...then wind down smoothly over 2 s
```

`Sequence` has the same properties. On a tween inside a sequence, the sequence drives the timing: set it on the sequence.

### Global control

```csharp
Raven.StopAll();                 // stop everything
Raven.CompleteAll();             // finish everything instantly — handy when skipping a cutscene
Raven.PauseAll(); Raven.ResumeAll();
int live = Raven.AliveCount;

Raven.StopAll(enemy);            // only the tweens animating this object
Raven.CompleteAll(door.transform);
int moving = Raven.CountTweens(player.transform);
```

The target can be any component or material a tween animates, the target of a custom tween, or a **GameObject** — which covers the tweens of all its components. Each call returns how many tweens it affected.

### Dead handles are safe

A `Tween` handle is a 2-number struct: a slot index and a version. When the tween ends, the version moves on, so an old handle can never touch a tween that later reuses the same slot. Every method on a dead handle does nothing.

```csharp
Tween fade = Raven.Alpha(group, 0f, 0.3f);
// ...much later, the tween is long finished:
fade.Stop();   // safe, does nothing
```

Control calls (`Stop`, `Complete`, `Pause`, `Resume`) are silent on a dead handle. Configuring one (`Ease`, `Delay`, `OnComplete`…) also does nothing, but logs an assertion in the Editor and development builds, because it usually means the tween could not be created (for example, its target was null).

### Tweens inside a sequence

Once a tween is added to a sequence, the sequence drives it:

- `Stop()`, `Complete()` and `Pause()` on the child log a warning and are ignored: control the sequence instead.
- `Delay()` and `Cycles()` must be set **before** adding the tween; set afterwards, they log an error and are ignored, because the sequence measured the child when it was added.
- Awaiting a child works: the await resumes when the sequence ends.

---

## Time

| Setting | Effect |
| :--- | :--- |
| `Time.timeScale` | Scales every tween, like the rest of your game. At `0`, tweens freeze. |
| `.UnscaledTime()` | This tween ignores `Time.timeScale` — use it for pause menus. |
| `Raven.TimeScale` | An extra multiplier applied to all tweens only, independent of `Time.timeScale`. Negative values clamp to 0; `NaN` and infinity are ignored. |
| `tween.TimeScale` | A multiplier for one tween or sequence (see above). |
| `Raven.GlobalTimeScale(to, duration)` | Tweens `Time.timeScale` itself — slow motion, hit stop — in unscaled time. |
| `Raven.UpdatePhase` | When tweens run by default: `UpdatePhase.Update` (default), `LateUpdate` (after your own `Update` scripts) or `FixedUpdate` (at the physics rate). |
| `.UpdateIn(phase)` | When this tween or sequence runs, whatever the default. |

```csharp
// Pause the game, keep the pause menu animating.
Time.timeScale = 0f;
Raven.AnchoredPosition(pauseMenu, Vector2.zero, 0.3f).UnscaledTime();

// A hit stop: drop to 5% speed, then come back.
Raven.GlobalTimeScale(0.05f, 0.05f).OnComplete(() => Raven.GlobalTimeScale(1f, 0.25f));

// Move a physics object at the physics rate (Rigidbody tweens do this by default).
Raven.LocalPositionY(lift, 3f, 2f).UpdateIn(UpdatePhase.FixedUpdate);
```

`FixedUpdate` tweens run right after your scripts' `FixedUpdate`, before the physics step, with `Time.fixedDeltaTime`.

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

`ToCompletion()` returns the same awaiter, for readability. The await resumes when the tween **ends for any reason** — completed, stopped or target destroyed. If the difference matters, set a flag in `OnComplete` or `OnKill`.

Awaiting works on every platform, WebGL included: RavenTween resumes your code on the main thread from its own update, with no threads involved.

---

## Coroutines

```csharp
IEnumerator Intro() {
    yield return Raven.Scale(logo, 1f, 0.4f).From(Vector3.zero).ToYieldInstruction();
    yield return Raven.Delay(0.5f).ToYieldInstruction();
    yield return BuildMenuSequence().ToYieldInstruction();
}
```

---

## Enter Play Mode options

RavenTween resets its engine on `SubsystemRegistration`, so it works with **Enter Play Mode Options** and domain reload disabled.

---

#### ◀ **[TextMeshPro](TextMeshPro)**  ·  Next: **[Procedural Animation ▶](Procedural-Animation)**
