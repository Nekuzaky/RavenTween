# Getting Started

Everything starts from one static class, `Raven`, in the `RavenTween` namespace.

```csharp
using RavenTween;
```

---

## Your first tween

```csharp
Raven.Position(transform, new Vector3(0f, 2f, 0f), 0.5f);
```

That line moves the object to `(0, 2, 0)` over half a second. **Tweens play as soon as they are created** — there is no `Play()` to forget. The start value is read from the object when the tween begins.

Add an easing curve by chaining:

```csharp
Raven.Position(transform, target, 0.5f).Ease(Ease.OutBack);
```

Every built-in tween also exists as an extension method, if you prefer that style:

```csharp
transform.TweenPosition(target, 0.5f).Ease(Ease.OutBack);
transform.TweenScale(1.2f, 0.25f).Cycles(2, CycleMode.Yoyo);
```

---

## Tweening a plain value

To drive anything that isn't a built-in property, tween a value and read it in `OnUpdate`:

```csharp
Raven.Value(0f, 1f, 0.8f)
    .Ease(Ease.InOutSine)
    .OnUpdate(v => healthBar.fillAmount = v);
```

> [!TIP]
> If the lambda captures a local variable, C# allocates a small closure once when the tween is created. For a fully allocation-free version, see **[Custom Tweens](Custom-Tweens)**.

---

## Chaining animations

A sequence plays tweens one after another, or side by side:

```csharp
Raven.Sequence()
    .Chain(Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack))
    .Group(Raven.Alpha(canvasGroup, 1f, 0.4f))
    .Chain(Raven.PunchScale(button, Vector3.one * 0.15f, 0.3f));
```

`Chain` waits for everything before it; `Group` starts together with the previous item. See **[Sequences](Sequences)**.

---

## Waiting for a tween

Await it:

```csharp
await Raven.Position(transform, target, 0.5f);
await Raven.Delay(1f);
```

Or yield it from a coroutine:

```csharp
yield return Raven.Scale(transform, 0f, 0.3f).ToYieldInstruction();
```

---

## Keeping a handle

Every factory returns a `Tween` (or `Sequence`) handle — a tiny struct you can store and control later:

```csharp
Tween spin = Raven.LocalEulerAngles(wheel, new Vector3(0f, 0f, 360f), 1f).Infinite(CycleMode.Restart);

spin.Pause();
spin.Resume();
spin.Stop();       // stops where it is
```

When the tween finishes, the handle simply goes dead: `spin.IsAlive` becomes `false` and every call on it does nothing. You never need to null-check or clean up.

---

## Not a coder?

Designers can build the same animations without code using **Tween Template** assets and the **Raven Animator** component. See **[Designer Workflow](Designer-Workflow)**.

---

#### ◀ **[Installation](Installation)**  ·  Next: **[Tweens and Easing ▶](Tweens-and-Easing)**
