# Monitor

The Monitor shows every tween and sequence alive in your game, live, while you play. Open it from **Tools ▸ RavenTween ▸ Monitor**.

![The Raven Monitor window in Play Mode](images/monitor.png)

---

## At a glance

The top of the window summarizes what's running:

| Item | Meaning |
| :--- | :--- |
| **Alive** / **peak** | Live tweens and sequences right now, and the highest count since the window opened. |
| **Counts** | How many are tweens, sequences, and paused. |
| **GC this frame** | Garbage allocated by the whole game this frame. RavenTween itself allocates 0 B per frame, so a non-zero value points at other code. |
| **Time Scale** | A slider for `Raven.TimeScale`, to slow everything down and inspect motion. |
| **Activity graph** | The alive count over the last 12 seconds. A line that only goes up means tweens are never ending. |

---

## Finding a tween

| Control | Use |
| :--- | :--- |
| **Search** | Filters by target name or property (`Player`, `Alpha`, `Position`…). |
| **All / Tweens / Sequences** | Shows only one kind. |
| **Sort** | Creation order, progress, target name or property. |
| **Paused** | Shows only paused tweens — handy to find tweens someone forgot to resume. |
| **Children** | Also lists the tweens owned by sequences. |

---

## Reading a row

| Column | Meaning |
| :--- | :--- |
| **Kind** | `Tween` or `Sequence`. Children of a sequence are marked with `└`. |
| **Target** | The object being animated. **Click it** to select it in the Hierarchy. |
| **Property** | What is being animated, or `(value)` for value and custom tweens. |
| **Progress** | Progress through the current cycle, or `paused`. |
| **Cycles** | Cycles completed out of the total; `∞` for infinite loops. |

Click the arrow on a **sequence** to unfold its **mini-timeline**: one bar per child, placed where it plays, with a white playhead.

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
> Hunting an animation that never stops? Filter by **Sequences** or sort by **Progress**, and look for `∞` in the Cycles column. A rising activity graph while nothing moves usually means paused tweens that were never stopped.

> [!NOTE]
> The Monitor is Editor-only. It reads a snapshot of the engine each repaint and has no effect on builds. To preview animations **outside** Play Mode, use the Tween Template inspector or the Sequence Editor — see **[Designer Workflow](Designer-Workflow)**.

---

**Tools ▸ RavenTween ▸ Documentation** opens this manual.

---

#### ◀ **[Designer Workflow](Designer-Workflow)**  ·  Next: **[Performance ▶](Performance)**
