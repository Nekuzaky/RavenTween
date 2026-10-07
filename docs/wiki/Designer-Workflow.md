# Designer Workflow

Not every animation should live in code. RavenTween gives designers three tools that need no programming: **Tween Templates**, the **Raven Animator** and the **Raven Sequence Player**. Programmers get a fourth: `TweenParams`, a serializable struct that exposes timing to the Inspector.

---

## Tween Templates

A template is a reusable animation recipe saved as an asset. Create one with **Create ▸ RavenTween ▸ Tween Template**.

![A Tween Template asset in the Inspector](images/inspector-tween-template.png)

| Field | Meaning |
| :--- | :--- |
| **Property** | What to animate: position, scale, alpha, color, field of view, a material property… |
| **Material Property** | The shader property name, used only by the material properties. |
| **End Value** | Where to go. Uses `x` for single numbers, `xyz` for vectors and euler rotations, `xyzw` as RGBA for colors. |
| **Use Explicit From** / **From Value** | Start from a fixed value instead of the object's current one. |
| **Settings** | Duration, start delay, ease (or a custom curve), cycles (`-1` loops forever), cycle mode, unscaled time. |

Because tweens themselves are single-use, a template is how you **reuse** an animation: every time it plays, it spawns a fresh tween. One `PopIn` template can serve every button in the game.

---

## Raven Animator

Plays one or more templates on scene objects. Add it with **Add Component ▸ RavenTween ▸ Raven Animator**.

![The Raven Animator component](images/inspector-animator.png)

1. Add an **Entry** per animation: drag the object to animate into **Target** (the Transform, CanvasGroup, Image, Camera… that matches the template's property), and the template into **Template**.
2. Tick **Play On Enable** to play whenever the object is enabled, or call `Play()` from a button's **On Click** or any UnityEvent.
3. Use **On All Complete** to trigger something when every entry has finished.

All entries play at the same time. Disabling the object stops them.

---

## Raven Sequence Player

Builds a full timeline from templates. Add it with **Add Component ▸ RavenTween ▸ Raven Sequence Player**.

![The Raven Sequence Player component](images/inspector-sequence-player.png)

Each **Step** has a **Mode**:

| Mode | Plays |
| :--- | :--- |
| **Chain** | After everything before it. |
| **Group** | Alongside the previous step. |
| **Insert** | At **Insert Time** seconds from the start. |

The whole sequence takes **Cycles** (`-1` loops forever), **Cycle Mode** and **Use Unscaled Time**, and raises **On Complete** at the end. See **[Sequences](Sequences)** for how the three modes interact.

> [!TIP]
> Both components show **Play**, **Stop** and **Complete** buttons in the Inspector during Play Mode, so you can iterate on timing without restarting.

---

## TweenParams in your own scripts

Programmers can let designers tune an animation without touching code by exposing a `TweenParams` field:

```csharp
public class Door : MonoBehaviour {
    [SerializeField] Transform pivot;
    [SerializeField] TweenParams openSettings = TweenParams.Default;

    public void Open() {
        Tween t = Raven.LocalEulerAngles(pivot, new Vector3(0f, 95f, 0f), openSettings.duration);
        openSettings.ApplyTo(t);
    }
}
```

In the Inspector, `TweenParams` shows **duration, start delay, ease, cycles, cycle mode** and **unscaled time**. The custom-curve field appears only when **Ease** is set to **Custom**. `TweenParams.Default` is 0.3 s, `OutQuad`, one cycle.

---

## Scripting the components

Both components can also be driven from code:

```csharp
animator.Play();            // restart every entry
animator.Stop();
animator.CompleteNow();

player.Play();              // rebuild the sequence from its steps and play it
Sequence s = player.Current; // handle to the running sequence
```

---

#### ◀ **[Procedural Animation](Procedural-Animation)**  ·  Next: **[Monitor ▶](Monitor)**
