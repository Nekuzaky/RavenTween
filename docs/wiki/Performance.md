# Performance

RavenTween is built so that animations create **no garbage while they play**. Garbage is what triggers the garbage collector, and the collector is what causes frame hitches — especially on mobile and consoles.

---

## Measured numbers

These figures come from the package's own test suite (`Tests/PerformanceTests.cs`). The allocation tests **fail** on a single allocated byte, so zero garbage can't silently regress. The timing test logs the engine cost and only fails above a full 60 FPS frame (16 ms), because timings vary from one machine to another.

| Scenario | Result |
| :--- | :--- |
| 5,000 mixed tweens (position, scale, rotation, shake, custom), steady state | **0 B** allocated per frame |
| 200 infinite sequences, including the moment they loop | **0 B** allocated per frame |
| Axis, property-block, Incremental / PingPong, parametric-ease, time-scaled tweens, target callbacks and sequence callbacks | **0 B** allocated per frame |
| Creating 1,000 tweens from a warm pool | **0 B** allocated |
| 5,000 mixed tweens, engine cost | **0.6–0.8 ms per frame** |

The timing was measured in the Unity Editor (Mono) on a desktop CPU, which is slower than a built player with IL2CPP.

---

## How it stays at zero

- **Pooled slots.** Each tween's state lives in a slot object that is created once and recycled forever. Finishing a tween returns its slot; creating one reuses it.
- **Struct handles.** `Tween` and `Sequence` are 8-byte structs (slot index + version). Copying them costs nothing and they are never boxed.
- **No boxing.** Every value — float, vector, quaternion, color — travels through one fixed-size struct, `TweenValue`.
- **No reflection.** Built-in properties are written through a direct `switch`, not looked up by name.
- **Cached delegates.** Custom tweens call a typed invoker created once per type; see **[Custom Tweens](Custom-Tweens)**.
- **One update for everything.** A single system in Unity's player loop updates all tweens in a plain `for` loop — no `MonoBehaviour` per tween, no LINQ.

---

## Preallocating the pool

The pool grows on demand: the first time a scene needs 3,000 live tweens, the extra slots are created then. To move that cost to a loading screen, reserve them up front:

```csharp
Raven.SetCapacity(3000);   // e.g. in a bootstrap scene
```

Past that capacity the pool still grows by itself — it is a hint, never a limit.

---

## What does allocate

Some things allocate by design. All of them happen when you *create* something, never per frame.

| Code | Allocates |
| :--- | :--- |
| A lambda that **captures** a local variable or `this`: `OnUpdate(v => bar.value = v)` | A small closure, once, when the tween is created. Standard C#. Use the target-based `OnComplete(target, …)`, `OnUpdate(target, …)` and `ChainCallback(target, …)` to avoid it. |
| `ToYieldInstruction()` | One small object per call. |
| `await tween` | The `async` method's state machine, once. |
| The very first time a code path runs | The runtime materializes its constants once. The performance tests warm up before measuring for this reason. |

> [!TIP]
> For hot paths, prefer built-in tweens and **[Custom Tweens](Custom-Tweens)** with non-capturing lambdas. Keep `await` and coroutines for flow control — intros, cutscenes, menus — not for things spawned every frame.

---

## Benchmark sample

Import **06 - Benchmark** from the Package Manager's Samples tab. It spawns thousands of animated cubes and shows the frame time and the GC allocation of each frame. Press **+** to add a batch and **−** to stop all tweens.

> [!NOTE]
> The Editor adds overhead of its own, and the sample's on-screen readout (`OnGUI`) allocates by itself. For real numbers, profile a **Development build** with the Unity Profiler.

---

#### ◀ **[Monitor](Monitor)**  ·  Next: **[Migrating ▶](Migrating)**
