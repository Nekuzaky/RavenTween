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
| **Settings** | Duration, start delay, ease (or a custom curve), cycles or **Loop Forever**, cycle mode, unscaled time. |

The inspector is built to iterate quickly:

- **Presets** — pick *Pop In*, *Fade Out*, *Slide In From Left*, *Bounce Drop*, *Spin*, *Blink*, *Flicker* and more to fill every field at once, then adjust.
- **Fields that fit the property** — sliders for alpha and volume, a color picker for colors, euler angles for rotations, two components for UI positions.
- **Live curve** — the easing curve is drawn as you change it, with a summary of the total length underneath.
- **Preview** — choose a target (it's picked from the selection automatically), press **Preview**, and the animation plays on the scene object in Edit Mode. Drag the **Time** slider to scrub. **Reset** puts the object back exactly as it was; values are also restored before entering Play Mode.

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

> [!IMPORTANT]
> To loop a timeline, set the **player's** Cycles to `-1` — not the templates'. A step whose template loops forever can't have a length on the timeline, so it plays once per sequence cycle: one out-and-back for a **Yoyo** template (it ends where it started), one pass for **Restart**. The inspector shows a warning on such steps.

### Sequence Editor

Press **Open Timeline** on a Raven Sequence Player (or use **Tools ▸ RavenTween ▸ Sequence Editor**) for a visual timeline:

- **One track per step**, colored by mode: Chain, Group or Insert. Steps whose template loops forever are marked, and incomplete steps are greyed out.
- **Drag a block** to move it in time — it becomes an *Insert* at that time. Moves snap to 0.05 s; hold **Alt** for free placement.
- **Edit in place**: change a step's mode, target or template, reorder with the arrow, add with **+ Add Step**, remove with the cross.
- **Scrub and play**: click or drag on the ruler to see the scene at any time, or press Play (optionally Loop). Stop restores the scene exactly.
- Every change supports **Undo**. The window follows the selection; **Lock** keeps it on one player.

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
