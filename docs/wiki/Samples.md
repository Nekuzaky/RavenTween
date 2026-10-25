# Samples

The package ships ten samples (**Package Manager ▸ RavenTween ▸ Samples ▸ Import**). Each one is a single component: create an empty scene, add it to an empty GameObject and press Play. It builds everything it needs at runtime, so no scene file is involved. This page says what to look at in each one.

---

## 01 - UI Basics (`UIBasicsDemo`)

Builds a small UI and animates it. The panel slides in from the left while the whole Canvas Group fades in as it arrives, and the button pulses forever with an infinite Yoyo scale: the classic "press me" pulse.

## 02 - Gameplay (`GameplayDemo`)

A few cubes with typical gameplay motion.

- **Hop**: up with `OutQuad`, back down with `InQuad`, through Yoyo cycles (the way back mirrors the ease).
- **Spin**: each Restart cycle turns half a turn and jumps back to the start; a cube looks the same after half a turn, so the loop reads as one continuous spin.
- **Patrol**: a sequence that moves right, pauses, moves back and squashes on arrival.

## 03 - Camera (`CameraDemo`)

A slow, endless breathing zoom on the field of view, blended with a background color going from day to night. Press **Space** for a quick punch-in zoom: two Yoyo cycles, in and back out.

## 04 - Materials (`MaterialsDemo`)

A grid of spheres using the URP Lit shader. Each sphere tweens its own material instance (safe to animate, unlike a shared material) through shader property IDs, and each one starts after a small delay so the grid shimmers as a wave.

## 05 - Complex Sequence (`ComplexSequenceDemo`)

A staged intro where three cubes enter, regroup and celebrate. The two side cubes slide in together (`Chain` + `Group`), the center cube drops in slightly before the slides end (`Insert`), then the celebration — a nested sequence built as its own unit — is chained after them. An `async` loop awaits the whole intro, pauses with an awaited `Raven.Delay`, and plays it again. Both are given the object's `destroyCancellationToken` with `WithCancellation`, so destroying the object stops them and ends the loop.

## 06 - Benchmark (`BenchmarkDemo`)

Spawns thousands of cubes, each running two independent infinite tweens (position and rotation), and shows the frame time and the GC allocations of the frame. **+** adds a batch, **-** stops every tween. Build a Development player for real numbers; the overlay's `OnGUI` itself allocates.

## 07 - TextMeshPro (`TextDemo`)

A typewriter reveals the title, then a score counter starts awarding points and punches on every update.

## 08 - Procedural (`ProceduralDemo`)

A head tracks a target orbiting the character with **Raven Look At**, an antenna swings with **Raven Spring Chain** as the body hops, and every few seconds the head looks away and back through a weight tween on the look-at. The bones are unscaled empty transforms with the meshes hanging off them as children, so rotations never shear; a spring chain follows each bone's first child, so the bones come first in the hierarchy.

## 09 - Time and Physics (`TimeAndPhysicsDemo`)

- A kinematic platform moves through the physics engine (`TweenMovePosition`, in `FixedUpdate`), so the crate on top rides along.
- A clock hand uses **Incremental** cycles: each one-second cycle snaps it 30 degrees further, with a small overshoot.
- A beacon loops on a timeline — grow, flash on a callback, shrink — and the flash goes through a property block, so the shared material stays untouched.
- **Space** drops the game to 5% speed for a moment and eases back, while the camera shakes in real time.

## 10 - No-Code Motion (`NoCodeMotionDemo`)

Everything that moves is driven by the no-code components. They are added by script only so that the sample needs no scene file; in your project, add the same components from the Inspector or the **Motion Library**.

- The title rises in when the scene starts (**Raven Motion**, *On Start*).
- A coin floats and spins forever: two loop motions on different channels.
- Three buttons cascade in with **Raven Stagger** when the menu is enabled, feel springy with **Raven Button Feel**, and celebrate when clicked.
- A dialog is shown and hidden by **Raven Show Hide**: it slides in, then slides out and deactivates.
- A hit (**Raven Feedback**): the camera shakes, the button punches and flashes, and the game freezes for an instant.
- A platform travels back and forth along a **Raven Path**, like one drawn in the Scene view.

Components added from a script are configured while their object is still inactive, so they don't play with their default settings first.

---

#### ◀ **[Installation](Installation)**  ·  Next: **[Getting Started ▶](Getting-Started)**
