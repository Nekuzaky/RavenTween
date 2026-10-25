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
| 5,000 mixed tweens, engine cost (Editor) | **about 0.5 ms per frame** |
| Starting 4,000 shakes on 4,000 objects | **under 1 ms** in total |

Timings were measured with `Throughput_5000Tweens_ReportsFrameCost` and `EffectCreation_ScalesLinearly` on an Intel Core i7-14700KF, in the Unity 6.3 Editor (Mono). Expect two to three times more on a laptop CPU. This is the engine step alone in the Editor; for numbers in a built player, side by side with other libraries, see the comparison below. Run the same tests on your machine (**Window ▸ General ▸ Test Runner**, with the package listed under `testables`) to get your own numbers.

---

## Compared with DOTween, PrimeTween and LitMotion

Measured in a **built player**, not the Editor, with the benchmark in [`Benchmarks/`](https://github.com/Nekuzaky/RavenTween/tree/main/Benchmarks) (`Tools/Benchmark-Competitors.ps1`). Every library runs the same scenario, in its own process, with its pool reserved beforehand (LitMotion's `Vector3` motion storage included): N world-position tweens (`InOutQuad`), and N sequences of 3 chained position tweens on the same object. **Update** is the CPU time a whole player-loop frame takes minus the same process's baseline frame, measured with the same objects before the library is touched (no pool reserved, no tween created), so it includes the library's update, any idle walk over its reserved pool, and its transform writes. Median of 5 runs.

| Setting | Value |
| :--- | :--- |
| CPU | Intel Core i7-14700KF (28 logical cores), Windows 11 |
| Unity | 6000.3.13f1, Windows release player, `-batchmode -nographics`, vSync off |
| Scripting backend | **Mono** (the IL2CPP Windows module was not installed on the benchmark machine) |
| Versions | RavenTween 1.7.1 + the unreleased changes on `main` (Vector3 fast path, live-range update loop), DOTween 1.3.045 (free, `DOTween.Init()` defaults), PrimeTween 1.5.1, LitMotion 2.0.2 (Burst 1.8.29) |

**N position tweens**

| N | Library | Start (ms) | Update avg (ms/frame) | Update p95 (ms/frame) | GC per frame | GC at start |
| ---: | :--- | ---: | ---: | ---: | ---: | ---: |
| 1,000 | DOTween | 0.39 | **0.052** | 0.059 | 0 B | 680 KB |
| 1,000 | LitMotion | 0.22 | 0.056 | 0.086 | 0 B | **0 B** |
| 1,000 | RavenTween | **0.08** | 0.062 | 0.068 | 0 B | **0 B** |
| 1,000 | PrimeTween | 0.28 | 0.121 | 0.138 | 0 B | **0 B** |
| 10,000 | LitMotion | 2.19 | **0.243** | 0.265 | 0 B | **0 B** |
| 10,000 | DOTween | 3.80 | 0.521 | 0.581 | 0 B | 6.7 MB |
| 10,000 | RavenTween | **0.81** | 0.664 | 0.765 | 0 B | **0 B** |
| 10,000 | PrimeTween | 2.74 | 1.201 | 1.310 | 0 B | **0 B** |
| 50,000 | LitMotion | 10.78 | **1.569** | 2.065 | 0 B | **0 B** |
| 50,000 | DOTween | 19.68 | 4.018 | 4.622 | 0 B | 33.3 MB |
| 50,000 | RavenTween | **4.24** | 4.061 | 4.756 | 0 B | **0 B** |
| 50,000 | PrimeTween | 13.76 | 6.136 | 6.525 | 0 B | **0 B** |

**N sequences of 3 chained position tweens**

| N | Library | Start (ms) | Update avg (ms/frame) | Update p95 (ms/frame) | GC per frame | GC at start |
| ---: | :--- | ---: | ---: | ---: | ---: | ---: |
| 1,000 | DOTween | 1.51 | **0.104** | 0.118 | 0 B | 2.5 MB |
| 1,000 | RavenTween | **0.57** | 0.276 | 0.332 | 0 B | 148 KB |
| 1,000 | PrimeTween | 2.42 | 0.363 | 0.406 | 0 B | **0 B** |
| 1,000 | LitMotion | 2.27 | 0.391 | 0.451 | 0 B | 2.0 MB |
| 10,000 | DOTween | 16.53 | **1.207** | 1.476 | 0 B | 25.1 MB |
| 10,000 | LitMotion | 22.78 | 3.344 | 3.817 | 0 B | 22.8 MB |
| 10,000 | RavenTween | **5.87** | 3.414 | 4.166 | 0 B | 1.5 MB |
| 10,000 | PrimeTween | 23.86 | 3.923 | 4.452 | 0 B | **0 B** |
| 50,000 | DOTween | 82.19 | **8.625** | 9.684 | 0 B | 125.5 MB |
| 50,000 | LitMotion | 113.24 | 16.252 | 18.369 | 0 B | 108.7 MB |
| 50,000 | PrimeTween | 119.82 | 19.207 | 20.763 | 0 B | **0 B** |
| 50,000 | RavenTween | **29.10** | 20.329 | 22.416 | 0 B | 7.7 MB |

What this says, honestly:

- **Starting tweens** is where RavenTween leads: 2.5 to 4.9 times faster than the others at every N, for tweens and sequences, with no allocation for single tweens.
- **Per-frame update** of plain tweens: RavenTween is ahead of PrimeTween at every N (1.5 to 2 times faster), but behind DOTween (1.2 times at 1,000, 1.3 times at 10,000, almost level at 50,000: 4.06 against 4.02 ms) and LitMotion (Burst: 1.1 times faster at 1,000, about 2.6 to 2.7 times from 10,000 on). In 1.7.1 as released, 10,000 tweens took 1.18 ms; the next release writes `Position`, `LocalPosition` and `LocalScale` tweens through a direct `Vector3` path and walks only the live slot range, which brings it to 0.66 ms. The rest is the general engine loop (one large pooled slot object per tween, several checks per tween every frame).
- **Sequences update slower than DOTween** at every N (2.4 to 2.8 times). At 1,000 RavenTween is ahead of PrimeTween and LitMotion; at 10,000 it is ahead of PrimeTween and just behind LitMotion (3.41 against 3.34 ms); at 50,000 it is the slowest of the four (20.3 ms, against 19.2 ms for PrimeTween and 16.3 ms for LitMotion). RavenTween re-evaluates every child that has already started on each frame, so a sequence of 3 chained tweens writes its transform up to three times per frame once the first child is done. Skipping settled children is planned; it needs care because jumps and rewinds rely on that re-evaluation.
- **Garbage**: every library allocates 0 B per frame in this scenario. At start, RavenTween, PrimeTween and LitMotion (with its storage reserved) allocate nothing for tweens; DOTween allocates. For sequences, PrimeTween allocates nothing, RavenTween a little the first time its pooled slots store children (about 150 B per sequence, once), DOTween and LitMotion more.
- The p95 column shows the run-to-run noise. Run `Tools/Benchmark-Competitors.ps1` on your hardware; IL2CPP will change the absolute numbers.

Raw data: `Benchmarks/Results/latest` (this table) and `Benchmarks/Results/1.7.1-release` (the same benchmark on 1.7.1 as released).

---

## Earlier comparison with PrimeTween (Editor)

An independent comparison ran both libraries in the Unity 6.3 Editor on Linux, timing from the start of `Update` to the end of `PreLateUpdate` with a mixed workload:

| Measure | RavenTween | PrimeTween 1.4.12 |
| :--- | :--- | :--- |
| 5,000 mixed tweens, cost per frame | **3.1–3.5 ms** | 4.8–5.4 ms |
| 500 looping sequences, cost per frame | **0.5–0.7 ms** | 1.6–2.1 ms |
| Creating 4,000 tweens | **1.1–1.5 ms** | 9.8–12.7 ms |
| Creating 500 sequences | **3 ms** | 23 ms |
| Allocated per frame | 0 B | 0 B |

The Editor numbers are not comparable with the player table above (other harness, other CPU, Editor overhead). Compare libraries on the same machine and harness, never across tables.

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
