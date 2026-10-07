# Time and physics sample

A kinematic platform carries a crate through the physics engine (`TweenMovePosition`, running in `FixedUpdate`, with a `PingPong` loop), a clock hand ticks with `Incremental` cycles and an overshoot ease, and a beacon flashes on a timeline callback through a per-renderer property block. Press **Space** for a hit stop: `Time.timeScale` drops with `Raven.GlobalTimeScale` and the camera shakes in unscaled time.

Create an empty scene, add `TimeAndPhysicsDemo` to an empty GameObject, and press Play. Needs the built-in Physics module (enabled by default); the sample has its own assembly definition and is skipped cleanly without it.
