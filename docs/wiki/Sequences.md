# Sequences

A sequence is a timeline. You place tweens on it, then play, loop, await or stop the whole thing as one unit.

```csharp
Sequence intro = Raven.Sequence()
    .Chain(Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack))
    .Group(Raven.Alpha(canvasGroup, 1f, 0.4f))
    .Insert(0.2f, Raven.Scale(icon, 1f, 0.3f))
    .ChainDelay(0.25f)
    .Chain(Raven.Color(title, Color.white, 0.6f));
```

---

## Placing tweens

![How Chain, Group, Insert and ChainDelay position tweens on the timeline](images/sequence-timeline.png)

| Method | Where the tween starts |
| :--- | :--- |
| `Chain(tween)` | After everything already in the sequence. |
| `Group(tween)` | At the same time as the previously added item. |
| `Insert(time, tween)` | At an absolute time, in seconds from the sequence start. |
| `ChainDelay(seconds)` | Adds an empty gap; the next `Chain` starts after it. |

The sequence's `Duration` is the end of its last item. A tween's own `Delay` and `Cycles` count toward its length on the timeline.

> [!TIP]
> Build a sequence in one expression, as above. A tween handed to a sequence belongs to it from then on: the sequence drives it, so don't pause or stop it individually.

---

## Looping and timing

Sequences take the same options as tweens:

```csharp
Raven.Sequence()
    .Chain(Raven.LocalPosition(t, Vector3.up, 0.4f))
    .Chain(Raven.LocalPosition(t, Vector3.zero, 0.4f))
    .Cycles(3, CycleMode.Yoyo)   // or .Infinite()
    .Delay(0.5f)
    .UnscaledTime();
```

With **Yoyo**, the whole timeline plays backwards on odd cycles. `Sequence.Infinite()` defaults to `CycleMode.Restart`.

Child tweens fire their `OnComplete` once per cycle of the sequence; the sequence's own `OnComplete` fires once, at the very end.

---

## Nesting

A sequence can contain other sequences — build reusable pieces and assemble them:

```csharp
Sequence celebrate = Raven.Sequence()
    .Chain(Raven.PunchScale(trophy, Vector3.one * 0.3f, 0.4f))
    .Group(Raven.ShakeRotation(trophy, new Vector3(0f, 0f, 10f), 0.4f));

Raven.Sequence()
    .Chain(Raven.AnchoredPosition(popup, Vector2.zero, 0.5f).Ease(Ease.OutBack))
    .Chain(celebrate)
    .Chain(Raven.Alpha(popupGroup, 0f, 0.3f).Delay(1f));
```

Nesting goes up to 8 levels deep, which is far beyond anything practical.

---

## Rules

> [!IMPORTANT]
> - A tween can belong to **one** sequence only. Adding it to a second one logs an error and is ignored. A sequence can't contain itself, directly or through nested sequences.
> - **Infinite tweens can't be nested** — a sequence needs a finite length. An infinite child logs an error and is clamped to one cycle.
> - Add tweens right after creating them, before the next frame, so they don't start on their own.
> - Set a child's `Delay()` and `Cycles()` **before** adding it. Once added, the sequence drives the child: `Stop()`, `Complete()`, `Pause()`, `Delay()` and `Cycles()` on the child log a message and are ignored.
> - Several children may animate the **same property** at different times: each one starts from where the previous one left it, also after a rewind or a Yoyo cycle. Items can be inserted in any order.

---

## Controlling a sequence

```csharp
intro.Pause();
intro.Resume();
intro.Complete();   // jumps every child to its end value
intro.Stop();       // stops everything where it is

await intro;        // or yield return intro.ToYieldInstruction();
```

Stopping or completing a sequence also ends all of its children.

---

#### ◀ **[Tweens and Easing](Tweens-and-Easing)**  ·  Next: **[Shake and Punch ▶](Shake-and-Punch)**
