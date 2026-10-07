# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/).

## [1.5.0] - 2026-10-07

A robustness release: every fix below comes with a regression test.

### Added
- `Raven.CustomTo(target, getter, to, duration, setter)`: a custom tween that reads its start value when it starts, so it stays correct after a delay or later in a sequence. `TweenWeight` on Raven Look At, Raven Spring Chain, rig constraints and rigs, and the TextMeshPro font-size and character-spacing tweens now use it.
- Templates, Raven Animator entries and Sequence Player steps accept a GameObject or any component as target: the component the property needs (or the renderer's material) is found automatically.
- Material templates accept `_Color` and `_BaseColor` interchangeably, so one template works in the built-in pipeline and in URP / HDRP.
- Sample *08 - Procedural* has a README; sample *07 - TextMeshPro* has its own assembly definition and is skipped cleanly in projects without TextMeshPro.

### Changed
- The tween handle is released **before** `OnComplete`, `OnKill` and `OnTargetDestroyed` run: `IsAlive` is `false` inside them, and starting a new tween from them is always safe.
- Calling `Stop()`, `Complete()` or `Pause()` on a tween that belongs to a sequence logs a warning and is ignored; `Delay()` and `Cycles()` on such a tween log an error and are ignored. Control the sequence instead.
- `From()` with a value of the wrong kind (a `float` on a `Vector3` tween) logs an error and is ignored instead of producing garbage values.
- Raven Look At and Raven Spring Chain track the base pose instead of resetting it every frame: rotations set by an Animator, a tween or a script are kept underneath. The *Restore Pose Each Frame* option is gone.
- Raven Spring Chain interpolates between its 60 Hz steps, so it stays smooth at high frame rates.
- Starting a shake or punch on a property that is already shaking or punching replaces the running effect and keeps the original rest value.
- The package declares `com.unity.modules.jsonserialize`, used by the update checker.

### Fixed
- Engine: calling `Complete()` from a tween's own `OnUpdate`, or `CompleteAll()` / `StopAll()` from a callback, recursed until the stack overflowed.
- Engine: a tween created inside a callback could be updated in the same frame, or be stopped by the `StopAll()` that triggered the callback.
- Engine: an exception thrown by a custom ease or setter froze every tween. It is now logged and stops only the tween that threw it.
- Engine: awaiting a tween that belongs to a sequence never resumed.
- Engine: a sequence could be added to itself, directly or through nested sequences, and loop forever.
- Engine: completing a Yoyo sequence with an even cycle count ended on the wrong side.
- Engine: a huge time step on a very short infinite tween could stall the frame; `NaN` or infinite time scales and deltas are ignored.
- Engine: zero-length tweens with several cycles never completed.
- Sequences: items inserted out of time order on the same property played in insertion order instead of timeline order.
- Templates played on a target of the wrong type threw an `InvalidCastException`; they now log a clear error and return a dead handle.
- Custom tweens created on an already destroyed object no longer run.
- Raven Animator: `CompleteNow()` threw when an entry had already finished, and **On All Complete** never fired if an entry was stopped from elsewhere or lost its target.
- Raven Sequence Player: calling `Play()` from **On Complete** left the new run without a handle.
- Raven Look At kept snapping a bone back to its pose from when the component was enabled.
- Raven Spring Chain: a destroyed bone threw every frame (the chain now rebuilds), and calling `Build()` mid-swing captured the swung pose as the new rest pose.
- TextMeshPro: the typewriter revealed nothing when the text changed after the tween was created.
- Edit Mode previews: values are now restored before saving a scene, a prefab or any asset and before quitting, so a preview can never be saved; transforms are restored in local space, so a previewed parent and child come back exactly; editing a previewed object stops the preview first; a preview that failed to start no longer restores stale values later.
- Sequence Editor: a drag released outside the window kept dragging; clicks on a horizontally scrolled timeline hit the wrong place; Stop stayed disabled after the player was deleted mid-preview.
- Tween Template inspector: the curve playhead jumped back to the start on the last frame; previews on a GameObject or with a URP material resolved the wrong target.
- Monitor: progress and sequence timelines included the start delay; Yoyo sequences showed the playhead moving the wrong way; sequence children showed a meaningless progress.
- Update checker: a pre-release (`1.5.0-beta.1`) was never offered the final release; clicking **Check Now** during a background check did nothing; no window pops up during Play Mode anymore.
- Docs: the DOTween `SetLoops(-1)` equivalent is `Infinite(CycleMode.Restart)`; `TweenParams.ApplyTo` documents that the duration is passed to the factory.

## [1.4.1] - 2026-10-07

### Fixed
- Sequence Editor: removing a step threw an `ArgumentOutOfRangeException`. Removing, reordering and adding steps now apply after the timeline is drawn.

## [1.4.0] - 2026-10-07

### Added
- **Sequence Editor** (**Tools ▸ RavenTween ▸ Sequence Editor**, or *Open Timeline* on a Raven Sequence Player): one track per step, drag blocks to retime them (snaps to 0.05 s, Alt for free), change modes, reorder, add and remove steps, and scrub or play the whole sequence in the scene outside Play Mode. Every change supports Undo.
- **Tween Template inspector**: 15 presets (pop, fade, slide, bounce, spin, blink, flicker…), value fields that match the animated property (sliders for alpha and volume, color pickers, euler rotations), a live easing-curve view, a timing summary, and **Preview** on a scene object with a time scrubber.
- **Monitor**: search, kind and paused filters, sorting, an activity graph with peak, a GC readout, a global time-scale slider, clickable targets, and expandable sequences with a mini-timeline of their children.
- Edit Mode previews always restore every touched value, including before entering Play Mode and before script reloads.

### Fixed
- **Sequences**: a tween chained after another tween on the **same property** started too early, captured the wrong start value and overrode the earlier tween for its whole duration (for example, *move to A, then move to B* on one transform). Children now start exactly at their time, and Yoyo sequences retrace their path.
- The update checker no longer runs during Play Mode domain reloads.

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
