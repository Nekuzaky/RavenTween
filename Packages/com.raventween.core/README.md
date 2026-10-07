# RavenTween

High-performance tweening for Unity. Code-first, allocation-free in steady state, with a fluent API, sequences, async/await, coroutine support, and optional inspector-driven templates for designers.

- **Zero steady-state GC** â€” tween state lives in pooled, fixed-size slots; built-in property tweens (Transform, UI, Camera, Audio, Material, ...) allocate nothing per frame.
- **Safe by construction** â€” handles are structs; a handle to a finished tween is simply dead. Destroyed targets kill their tweens cleanly and can notify you via `OnTargetDestroyed`.
- **Single-use tweens, reusable templates** â€” tweens cannot be restarted (no hidden state); `TweenTemplate` ScriptableObjects hold reusable configurations.
- **No threads, no reflection** â€” WebGL-friendly; the engine runs inside the player loop.

Supports Unity 2021.3 and newer, including Unity 6.

## Installation

Install via Git URL (Package Manager â†’ `+` â†’ *Add package from git URL...*):

```
https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core
```

Pin a version with a tag: `...?path=/Packages/com.raventween.core#v1.0.0`.

## Quick start

```csharp
using RavenTween;

// Values
Raven.Value(0f, 1f, duration: 0.5f)
    .OnUpdate(v => fillImage.fillAmount = v)
    .Ease(Ease.InOutSine);

// Transforms
Raven.Position(transform, targetPos, 0.6f).Ease(Ease.OutQuad);
transform.TweenScale(1.2f, 0.25f).Cycles(2, CycleMode.Yoyo); // extension style

// Delays
Raven.Delay(1.2f, () => Debug.Log("done"));
```

Tweens play automatically on creation. `Start()` exists for explicit call sites and resumes a paused tween.

### Sequences

```csharp
Sequence s = Raven.Sequence()
    .Chain(Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack)) // after previous
    .Group(Raven.Alpha(canvasGroup, 1f, 0.4f))                                   // alongside previous
    .Insert(0.2f, Raven.Scale(icon, 1f, 0.3f))                                   // at absolute time
    .ChainDelay(0.25f)
    .Chain(Raven.Color(title, Color.white, 0.3f))
    .Cycles(2, CycleMode.Yoyo)
    .OnComplete(() => Debug.Log("sequence done"));
```

Sequences own their children, support nesting (`Chain(otherSequence)`), cycles, yoyo, and infinite loops.

### Async / await and coroutines

```csharp
// async/await
await Raven.Delay(1.2f);
await tween.ToCompletion();
await sequence;

// coroutines
yield return tween.ToYieldInstruction();
yield return sequence.ToYieldInstruction();
```

Awaiting a tween resumes when it completes **or** is killed; check your own state if the distinction matters.

### Cycles, yoyo, infinite

```csharp
Raven.Scale(transform, 1.1f, 0.4f).Cycles(4, CycleMode.Yoyo); // 4 cycles, ping-pong
Raven.Position(t, top, 0.5f).Infinite(CycleMode.Yoyo);        // forever
```

### Callbacks

`OnStart`, `OnUpdate` (`Action`, `Action<float>`, or `Action<TweenValue>`), `OnComplete`, `OnKill`, `OnTargetDestroyed`.

### Time control

```csharp
Raven.TimeScale = 0.5f;                   // global, independent of Time.timeScale
tween.UnscaledTime();                     // per-tween, ignores Time.timeScale
Raven.UpdatePhase = UpdatePhase.LateUpdate; // step tweens in LateUpdate instead
```

### Custom easing

```csharp
tween.Ease(Ease.OutElastic);          // 31 standard easings
tween.Ease(myAnimationCurve);         // AnimationCurve
tween.Ease(t => t * t * (3f - 2f * t)); // delegate
```

## Inspector workflow (no code)

1. Create a **Tween Template** asset: *Create â†’ RavenTween â†’ Tween Template*. Choose the property, end value, duration, ease, cycles.
2. Add a **Raven Animator** component, reference targets + templates, enable *Play On Enable* or wire `Play()` to a UnityEvent / button.
3. For timelines, use **Raven Sequence Player**: each step chains, groups or inserts a template on a target.

Expose a `TweenParams` field in your own MonoBehaviours to let designers tune duration/ease/cycles without recompiling:

```csharp
[SerializeField] TweenParams entranceSettings = TweenParams.Default;

void Show() {
    entranceSettings.ApplyTo(Raven.AnchoredPosition(panel, Vector2.zero, entranceSettings.duration));
}
```

## Built-in tween targets

| Area | Methods |
| --- | --- |
| Transform | `Position`, `LocalPosition`, `Rotation`, `LocalRotation`, `EulerAngles`, `LocalEulerAngles`, `Scale` |
| UI | `AnchoredPosition`, `SizeDelta`, `Alpha(CanvasGroup)`, `Color(Graphic)`, `Alpha(Graphic)` |
| Camera | `FieldOfView`, `OrthographicSize`, `BackgroundColor` |
| Audio | `Volume`, `Pitch` |
| Material | `MaterialFloat`, `MaterialColor` (by ID or name) |
| Rendering | `Color(SpriteRenderer)`, `Intensity(Light)`, `Color(Light)` |
| Values | `Value(float/Vector2/Vector3/Vector4/Quaternion/Color)`, `Delay` |

Every method also exists in extension form (`transform.TweenPosition(...)`, `material.TweenColor(...)`, ...).

## Performance notes

- Built-in property tweens allocate **zero** bytes per frame and zero at creation beyond the (pooled) slot.
- `OnUpdate(v => ...)` lambdas that capture locals allocate a closure **once at creation** â€” standard C# behavior. For hot paths, prefer built-in property tweens, or cache the delegate.
- `ToYieldInstruction()` allocates one small object per call; `await` allocates the async state machine. Use them for flow control, not per-frame work.
- The engine never uses reflection, threads, or `DOTS`-style codegen â€” it is fully AOT/WebGL safe.

## Migrating from DOTween / PrimeTween

| DOTween | PrimeTween | RavenTween |
| --- | --- | --- |
| `transform.DOMove(p, d)` | `Tween.Position(t, p, d)` | `Raven.Position(t, p, d)` |
| `DOVirtual.Float(a, b, d, cb)` | `Tween.Custom(a, b, d, cb)` | `Raven.Value(a, b, d).OnUpdate(cb)` |
| `DOVirtual.DelayedCall(d, cb)` | `Tween.Delay(d, cb)` | `Raven.Delay(d, cb)` |
| `DOTween.Sequence().Append(x)` | `Sequence.Create().Chain(x)` | `Raven.Sequence().Chain(x)` |
| `.Join(x)` | `.Group(x)` | `.Group(x)` |
| `.Insert(t, x)` | â€” | `.Insert(t, x)` |
| `.SetLoops(n, LoopType.Yoyo)` | `cycles, CycleMode.Yoyo` | `.Cycles(n, CycleMode.Yoyo)` |
| `.SetEase(Ease.OutQuad)` | `ease: Ease.OutQuad` | `.Ease(Ease.OutQuad)` |
| `.SetUpdate(true)` | `useUnscaledTime: true` | `.UnscaledTime()` |
| `.Kill()` / `.Complete()` | `.Stop()` / `.Complete()` | `.Stop()` / `.Complete()` |
| `.WaitForCompletion()` | `.ToYieldInstruction()` | `.ToYieldInstruction()` |
| `await t.AsyncWaitForCompletion()` | `await tween` | `await tween` |

Like PrimeTween (and unlike DOTween), tweens are **not** reusable: create a new one each time, or use a `TweenTemplate`.

## FAQ

**Does it survive object destruction mid-tween?** Yes. The tween dies silently (plus `OnTargetDestroyed` if you subscribed). No exceptions, no leaks.

**What happens at `Time.timeScale = 0`?** Scaled tweens freeze; `UnscaledTime()` tweens keep playing (menus, pause screens).

**Is it thread-safe?** The API must be called from the main thread, like the Unity API it drives. Internally no threads are used.

**Can I reuse a `Tween` handle?** Handles are cheap value types; once the tween dies, every method on the handle becomes a safe no-op.

## License

MIT â€” see [LICENSE.md](LICENSE.md).
