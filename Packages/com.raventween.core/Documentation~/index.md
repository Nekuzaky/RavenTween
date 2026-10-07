# RavenTween Documentation

RavenTween is a code-first tweening engine for Unity with zero steady-state GC allocation, struct handles, sequences, async/await and an optional inspector workflow.

Start with the [README](../README.md) for installation and the API tour.

## Architecture overview

```
Raven (static API)            Public entry: Value, Delay, Sequence, property tweens.
  └── TweenEngine (internal)  Player-loop driven. Owns pooled TweenSlot storage.
        ├── TweenSlot         Fixed-size state for one tween or sequence. Pooled, versioned.
        ├── PropertyAccessor  Switch-based read/write of built-in properties. No reflection.
        ├── EaseUtility       Pure easing math.
        └── TweenValue        Tagged union (Vector4 + kind). No boxing.
Tween / Sequence (structs)    Handles: index + version. Dead handles are safe no-ops.
Templates                     TweenTemplate (SO), RavenAnimator, RavenSequencePlayer.
```

Key invariants:

- A slot is only addressable while its version matches the handle. Completion, kill or target destruction bumps the version, so stale handles can never touch recycled slots.
- Sequences own their children: children are stepped by the sequence timeline, never by the root loop, and are released with their sequence.
- Callbacks are isolated: an exception in one callback is logged and never breaks other tweens.

## Determinism and testing

The engine advances through a single internal `Process(scaledDelta, unscaledDelta)` entry point. The test suite calls it directly with fixed deltas, which makes every core behavior reproducible independent of frame rate. See `Tests/` for examples.

## Platform notes

- **WebGL / AOT**: no threads, no reflection, no runtime codegen. Fully compatible.
- **Enter Play Mode without domain reload**: engine state is reset via `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`.
- **IL2CPP**: all generic use is value-type-free at the API boundary; no AOT pitfalls.
