# Monitor

The Monitor shows every tween and sequence alive in your game, live, while you play. Open it from **Tools ▸ RavenTween ▸ Monitor**.

![The Raven Monitor window in Play Mode](images/monitor.png)

---

## Reading the window

| Column | Meaning |
| :--- | :--- |
| **Kind** | `Tween` or `Sequence`. Children of a sequence are marked with `└`. |
| **Target** | The object being animated. Sequences show `(timeline)`, value tweens `(no target)`. |
| **Property** | What is being animated, or `(value)` for value and custom tweens. |
| **Progress** | Progress through the current cycle, or `paused`. |
| **Cycles** | Cycles completed out of the total; `∞` for infinite loops. |

The toolbar shows the **Alive** count. **Sequence children** toggles whether tweens owned by a sequence are listed individually.

---

## Controls

Each row has three buttons:

| Button | Effect |
| :--- | :--- |
| **Pause / Resume** | Freeze or unfreeze that tween. |
| **Complete** | Jump it to its end value. |
| **Kill** | Stop it where it is. |

The toolbar's **Complete All** and **Stop All** act on everything. Rows for sequence children are read-only: control them through their sequence.

> [!TIP]
> Hunting an animation that never stops? Open the Monitor and look for `∞` in the Cycles column. A growing Alive count while nothing is moving usually means paused tweens that were never stopped.

> [!NOTE]
> The Monitor is Editor-only. It reads a snapshot of the engine each repaint and has no effect on builds.

---

**Tools ▸ RavenTween ▸ Documentation** opens the RavenTween repository on GitHub.

---

#### ◀ **[Designer Workflow](Designer-Workflow)**  ·  Next: **[Performance ▶](Performance)**
