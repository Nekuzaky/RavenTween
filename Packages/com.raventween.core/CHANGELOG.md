# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-10-07

### Added
- Shake and punch effects: `ShakePosition`, `ShakeRotation`, `ShakeScale`, `PunchPosition`, `PunchRotation`, `PunchScale`. Effects settle exactly on the value the target had when they started.
- `Raven.Custom(target, from, to, duration, setter)` for float, Vector2, Vector3 and Color. Allocation-free with non-capturing lambdas; dies with its target when the target is a `UnityEngine.Object`.
- Optional TextMeshPro module (`RavenTween.TextMeshPro`), compiled only when TMP is present: `TweenTypewriter`, `TweenMaxVisibleCharacters`, `TweenNumber`, `TweenFontSize`, `TweenCharacterSpacing`.
- **Tools → RavenTween → Monitor**: live list of tweens and sequences with progress, cycles and per-row pause / complete / kill.
- Editor icon set: package logo, per-component icons, Bootstrap Icons glyphs on inspector and monitor controls.
- Performance test suite enforcing 0 B per-frame allocation for property, effect, sequence and custom tweens, plus a 5,000-tween throughput check.
- Samples: *06 - Benchmark* and *07 - TextMeshPro*.
- CI workflow testing the package on Unity 2021.3, 2022.3 and 6 (Linux runners).

### Changed
- Sequence cycle reset uses a plain array work stack instead of a generic collection.

### Fixed
- Camera sample threw when only the Input System package was enabled; samples now support both input backends.
- Package README contained corrupted characters.

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
