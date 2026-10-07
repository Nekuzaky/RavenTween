# Procedural Animation

Two components add life on top of your animations: **Raven Look At** turns a head, an eye or a turret toward a moving target, and **Raven Spring Chain** gives ponytails, antennae, tails and cables secondary motion. Both expose a `weight`, and blending that weight in and out with RavenTween is what turns them from always-on effects into animation.

If your project uses Unity's **Animation Rigging** package, an optional module also lets you tween constraint and rig weights and drive two-bone IK — see the end of this page.

> [!NOTE]
> Both components run in `LateUpdate`, after the Animator, so they layer on top of whatever animation already posed the bones. They allocate nothing per frame.

---

## Raven Look At

Add it with **Add Component ▸ RavenTween ▸ Raven Look At** on the bone that should turn — a head, a neck, an eye, a turret.

```csharp
var look = head.gameObject.AddComponent<RavenLookAt>();
look.Target = player;
look.MaxAngle = 70f;
```

| Field | Meaning |
| :--- | :--- |
| **Target** | What to look at. When empty, the bone eases back to its animated pose. |
| **Weight** | 0 = animation only, 1 = fully looking at the target. |
| **Forward Axis** | The local axis that should point at the target. `(0, 0, 1)` for most rigs. |
| **Max Angle** | How far the bone may turn away from its animated pose, in degrees. |
| **Smooth Time** | Seconds to catch up with the target; `0` snaps instantly. |
| **Use Unscaled Time** | Keep tracking while `Time.timeScale` is 0. |
| **Restore Pose Each Frame** | Resets the bone to its rest rotation before animation. Leave it on unless your own script drives this rotation. |

### Blending with RavenTween

```csharp
look.TweenWeight(0f, 0.5f);                  // look away
look.TweenWeight(1f, 0.5f).Ease(Ease.OutSine); // look back
look.LookAtTarget(door, 0.4f);               // switch target and blend in
```

> [!TIP]
> For a whole-body look (spine, neck, head), put a Raven Look At on each bone with a growing **Max Angle** and **Weight** from the spine up — for example 0.2, 0.4 and 1.

---

## Raven Spring Chain

Add it with **Add Component ▸ RavenTween ▸ Raven Spring Chain** on the **first bone** of a chain: the root of a ponytail, the base of an antenna or a tail. The root stays attached; every bone below it lags behind, swings and settles back toward its animated pose.

The chain follows each bone's **first child**, up to 64 bones.

| Field | Meaning |
| :--- | :--- |
| **Root** | First bone of the chain. Defaults to the component's own transform. |
| **Weight** | 0 = animation only, 1 = full spring motion. |
| **Stiffness** | How strongly each bone is pulled back to its animated pose. Higher = stiffer. |
| **Damping** | How quickly swinging dies out. Higher = settles faster. |
| **Gravity** | A constant world-space force, e.g. `(0, -2, 0)` to make the chain droop. |
| **Tip Length** | Adds a virtual point past the last bone so the last bone swings too. |
| **Use Unscaled Time** | Simulate while `Time.timeScale` is 0. |
| **Restore Pose Each Frame** | Leave on unless your own script drives these bones. |

| Material | Stiffness | Damping | Gravity |
| :--- | :--- | :--- | :--- |
| Ponytail, hair strand | 0.03 – 0.06 | 0.1 | `(0, -3, 0)` |
| Antenna, feather | 0.06 – 0.12 | 0.1 – 0.15 | `(0, -1, 0)` |
| Tail | 0.08 – 0.15 | 0.15 – 0.2 | `(0, 0, 0)` |
| Heavy cable, chain | 0.01 – 0.03 | 0.05 | `(0, -9.8, 0)` |

The simulation runs at a fixed 60 steps per second, so it behaves the same at 30 or 144 FPS.

```csharp
var tail = tailRoot.gameObject.AddComponent<RavenSpringChain>();
tail.Stiffness = 0.1f;

tail.TweenWeight(0f, 0.3f);   // freeze into the animated pose, e.g. for a cutscene
tail.ResetPhysics();          // after teleporting the character
```

> [!IMPORTANT]
> Call `ResetPhysics()` after teleporting a character, or the chain will whip across the distance. Call `Build()` if you change the bone hierarchy at runtime.

> [!WARNING]
> Bones under a **non-uniformly scaled** parent shear when rotated — that's how Unity transforms work. Keep bones unscaled and put scaled meshes on child objects.

---

## Animation Rigging integration

When the **Animation Rigging** package (`com.unity.animation.rigging`) is installed, the `RavenTween.AnimationRigging` assembly compiles automatically and adds these extension methods. Without the package it simply isn't compiled.

| Method | Effect |
| :--- | :--- |
| `constraint.TweenWeight(to, duration)` | Blends any rig constraint — Two Bone IK, Multi-Aim, Damped Transform, Chain IK… |
| `rig.TweenWeight(to, duration)` | Blends a whole Rig layer. |
| `ik.TweenReach(worldPosition, duration)` | Moves the IK target there while blending the constraint to full weight. Returns a `Sequence`. |
| `ik.TweenRelease(duration)` | Blends the IK back to the animation. |

```csharp
// A hand reaches for a door handle, holds, and lets go.
await leftHandIK.TweenReach(handle.position, 0.4f);
await Raven.Delay(0.5f);
leftHandIK.TweenRelease(0.3f);

// Fade the whole aim layer out during a cutscene.
aimRig.TweenWeight(0f, 0.5f);
```

> [!NOTE]
> `TweenReach` needs a **Target** transform assigned on the Two Bone IK constraint — that is the transform it moves.

---

## Sample

Import **08 - Procedural** from the Package Manager's Samples tab: a character whose head tracks an orbiting target, an antenna that swings as it hops, and a look-away driven by weight tweens. It needs no Animation Rigging.

---

#### ◀ **[Control and Lifecycle](Control-and-Lifecycle)**  ·  Next: **[Designer Workflow ▶](Designer-Workflow)**
