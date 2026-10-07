# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/).

## [1.3.1] - 2026-10-07

### Fixed
- A Raven Sequence Player step using a template that loops forever logged an error every time the player started. Such steps now play once per sequence cycle (one out-and-back for Yoyo, so they end where they started), without errors.

### Added
- The Raven Sequence Player inspector warns about steps whose template loops forever and explains how to loop the whole sequence instead.

## [1.3.0] - 2026-10-07

### Added
- In-editor update checker. At most once a day, RavenTween checks GitHub for a newer release and offers **Update now / What's new / Skip this version / Later**. Optional fully automatic installs. Menu: **Tools ▸ RavenTween ▸ Updates**. Silent in batch mode and for embedded or local copies of the package.
- Edit Mode tests for the update rules (version parsing and comparison, check interval, Git and registry update identifiers, release parsing).

### Fixed
- The package failed to compile in projects with the built-in Audio module disabled. It now declares `com.unity.modules.audio` and `com.unity.modules.unitywebrequest` as dependencies.
- Sample *08 - Procedural* no longer references the Physics module.

## [1.2.0] - 2026-10-07

### Added
- `RavenLookAt` component: turns a bone or object toward a moving target on top of its animation, with smoothing, an angle limit and a blend weight.
- `RavenSpringChain` component: secondary motion for bone chains (ponytails, antennae, tails, cables) with stiffness, damping, gravity and an optional tip, simulated at a fixed 60 Hz.
- `TweenWeight` for both components, and `LookAtTarget` to switch targets and blend in.
- Optional Animation Rigging module (`RavenTween.AnimationRigging`), compiled only when `com.unity.animation.rigging` is installed: `TweenWeight` for any rig constraint and for `Rig`, `TweenReach` and `TweenRelease` for two-bone IK.
- Sample *08 - Procedural*.
- Tests for both components (including a zero-allocation check) and for the Animation Rigging module.

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
