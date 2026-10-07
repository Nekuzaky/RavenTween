# RavenTween

> **High-performance, allocation-free tweening for Unity — a fluent code-first API, sequences, shake & punch, TextMeshPro, async/await, and designer templates, with 0 bytes of garbage per frame.**

RavenTween animates anything in Unity — transforms, UI, cameras, audio, materials, text, or your own fields — through one short, readable API. Every tween lives in a pooled slot that is recycled forever, so animations generate **no garbage while they play**. That is not a promise in prose: the package's own test suite fails if a single byte is allocated per frame.

![The live Monitor window listing running tweens and sequences](images/monitor.png)

> [!TIP]
> New here? Go straight to **[Installation](Installation)**, then **[Getting Started](Getting-Started)** — your first tween is one line of code.

---

## Documentation

| Page | What you'll find |
| :--- | :--- |
| **[Installation](Installation)** | Add RavenTween through the Package Manager with a Git URL. |
| **[Getting Started](Getting-Started)** | Your first tweens, sequences and awaits in five minutes. |
| **[Tweens and Easing](Tweens-and-Easing)** | Every built-in target, `From`, delays, cycles, yoyo, and the 31 easing curves. |
| **[Sequences](Sequences)** | `Chain`, `Group`, `Insert`, delays, nesting, and looping whole timelines. |
| **[Shake and Punch](Shake-and-Punch)** | Screen shake, hit feedback and button presses that always settle back. |
| **[Custom Tweens](Custom-Tweens)** | Tween any field or property with zero allocations. |
| **[TextMeshPro](TextMeshPro)** | Typewriter reveal, score counters, font size and spacing. |
| **[Control and Lifecycle](Control-and-Lifecycle)** | Callbacks, pause, stop, complete, time scale, destroyed targets, async/await and coroutines. |
| **[Designer Workflow](Designer-Workflow)** | Tween Templates, Raven Animator, Raven Sequence Player and `TweenParams`. |
| **[Monitor](Monitor)** | The live debugging window under **Tools ▸ RavenTween**. |
| **[Performance](Performance)** | How the engine stays at 0 B per frame, measured numbers, and what does allocate. |
| **[Migrating](Migrating)** | Side-by-side equivalents for DOTween and PrimeTween. |
| **[Reference](Reference)** | The complete API, tables only. |
| **[FAQ](FAQ)** | Short answers to the questions that come up most. |

---

## What's new in 1.1.0

- **Shake and punch** on position, rotation and scale. Effects always settle exactly on the starting value.
- **`Raven.Custom`** tweens any field through a setter, with zero allocations when the lambda captures nothing.
- **TextMeshPro module**: typewriter reveal, allocation-free number counter, font size and character spacing.
- **The Monitor window**: every live tween with its progress, and pause / complete / kill buttons.
- **A performance test suite** that enforces 0 B per frame and a 5,000-tween frame budget.

The full list is in the **[Changelog](Changelog)**.

---

## Why RavenTween?

- **Zero garbage while playing** — property, effect, sequence and custom tweens allocate 0 B per frame, enforced by tests.
- **Fast** — 5,000 simultaneous tweens step in about 0.77 ms per frame.
- **Safe by construction** — handles are small structs; a handle to a finished tween is simply dead, and every call on it does nothing. Destroying a target mid-tween never throws.
- **No hidden state** — tweens are single-use. Reusable configurations live in Tween Template assets instead.
- **Runs everywhere** — pure managed C#: no threads, reflection, native plugins or runtime code generation. IL2CPP, WebGL, mobile and consoles are all fine.
- **Free and open source** — MIT licensed, on [GitHub](https://github.com/Nekuzaky/RavenTween).

---

## In short

> [!NOTE]
> - One line per animation: `Raven.Position(transform, target, 0.5f).Ease(Ease.OutBack);`
> - 0 B per frame, enforced by the test suite.
> - Unity 2021.3 and newer, including Unity 6.

---

#### Next: **[Installation ▶](Installation)**
