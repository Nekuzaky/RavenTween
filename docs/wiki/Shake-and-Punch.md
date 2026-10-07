# Shake and Punch

Effects add feel: screen shake on an explosion, a kick when a button is pressed, a wobble when something takes damage. Unlike a normal tween, an effect has no end value — it **oscillates around the value the object had when the effect started and always comes back to exactly that value**.

---

## Shake

Shake uses smooth noise, so it looks organic rather than jittery. Its strength fades out over the duration.

```csharp
// Screen shake: 0.3 units on X and Y, for 0.4 seconds.
Raven.ShakePosition(Camera.main.transform, new Vector3(0.3f, 0.3f, 0f), 0.4f);

// Rattle a door on its Z axis, faster.
Raven.ShakeRotation(door, new Vector3(0f, 0f, 6f), 0.5f, frequency: 25f);

// Squash-and-stretch jitter.
Raven.ShakeScale(slime, Vector3.one * 0.1f, 0.3f);
```

| Parameter | Meaning |
| :--- | :--- |
| `strength` | Maximum offset per axis, in local units (degrees for rotation). Set an axis to `0` to lock it. |
| `duration` | Length in seconds. |
| `frequency` | How fast the noise moves, in oscillations per second. Default `12`. |

---

## Punch

Punch is a damped spring: a single directional kick that springs back and forth, losing energy until it rests.

```csharp
// Button press: bulge 15% then settle.
Raven.PunchScale(button, Vector3.one * 0.15f, 0.3f);

// Recoil: kick backward along local Z.
Raven.PunchPosition(gun, new Vector3(0f, 0f, -0.2f), 0.25f);

// Nod.
Raven.PunchRotation(head, new Vector3(12f, 0f, 0f), 0.5f, frequency: 3f);
```

| Parameter | Meaning |
| :--- | :--- |
| `punch` | Direction and size of the kick. |
| `duration` | Length in seconds. |
| `frequency` | Spring oscillations per second. Default `5`. |

---

## Good to know

> [!NOTE]
> - Effects work on **local** position, local euler angles and local scale, so they compose with a parent's motion.
> - They are regular tweens: they accept `Delay`, `OnComplete`, `UnscaledTime`, can go in a **[sequence](Sequences)**, and can be awaited.
> - `Complete()` on an effect puts the object straight back to its starting value.
> - Each shake gets its own noise seed, so two shakes started on the same frame don't move in lockstep.

> [!TIP]
> Avoid running a shake and a normal position tween on the **same** transform at the same time — both write the same property, and the last one to run each frame wins. Shake a parent or child object instead, as is common for cameras.

---

#### ◀ **[Sequences](Sequences)**  ·  Next: **[Custom Tweens ▶](Custom-Tweens)**
