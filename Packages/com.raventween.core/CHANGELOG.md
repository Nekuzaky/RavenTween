# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-07

### Added
- Core engine: pooled, versioned tween slots driven from the player loop; zero steady-state allocation; configurable update phase (`Update` / `LateUpdate`); global `Raven.TimeScale`.
- Fluent API: `Raven.Value`, `Raven.Delay`, property tweens for Transform, RectTransform, CanvasGroup, Graphic, Camera, AudioSource, Material, SpriteRenderer and Light, plus extension-method variants.
- Sequences: `Chain`, `Group`, `Insert`, `ChainDelay`, nesting, cycles, yoyo, infinite loops.
- Easing: 31 standard functions, `AnimationCurve` support, custom delegates.
- Lifetime safety: struct handles with versioning, automatic kill on target destruction, `OnTargetDestroyed` callback.
- Async/await (`await tween`, `ToCompletion()`) and coroutine (`ToYieldInstruction()`) integration.
- Designer workflow: `TweenTemplate` ScriptableObjects, `RavenAnimator` and `RavenSequencePlayer` components, serializable `TweenParams` with a custom drawer.
- Test suite: easing math, engine lifecycle, sequences, play-mode integration (destruction, timeScale 0, await, coroutines, templates).
- Samples: UI basics, gameplay motion, camera work, material properties, complex nested sequence.
