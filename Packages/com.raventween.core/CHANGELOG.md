# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed
- A sequence with no tween in it (empty, or only a `ChainDelay` used as a timer) threw a `NullReferenceException` and never completed when it got a pool slot that had never held a sequence.
- With *Enter Play Mode Options* (no domain reload), a `Tween` or `Sequence` handle kept from an earlier Play Mode session (a static field, an editor window) could point at a new, unrelated tween and stop or change it: the engine reset restarted handle versions at 1. Handles from before a reset now stay dead, and tweens still alive when Play Mode ends are released instead of keeping scene objects and callbacks referenced in Edit Mode.
- `Raven.HitStop` and `Raven.SlowMotion` (and the Hit Stop / Slow Motion feedback actions) started while the game was paused (`Time.timeScale = 0`) ran the game at the hit-stop or slow-motion speed until they ended. They now never speed the game up: when the time scale is already lower, they do nothing.
- A slow motion started during a hit stop restored `Time.timeScale` to 1 instead of the speed the game had before the hit stop.

## [1.7.1] - 2026-10-16

### Changed
- Available on the Unity Asset Store. Asset Store and Unity registry installs are updated by the Package Manager: the built-in GitHub update check skips them, and **Check Now** points to the Package Manager window.
- The samples and the documentation folder ship with their `.meta` files, so their GUIDs stay stable across installs and updates.

## [1.7.0] - 2026-10-14

### Added
- **Motions — animation without animation knowledge.** 44 ready-made motions in four families (entrances, exits, attention, loops) that adapt to UI and scene objects, fade Canvas Groups, Graphics and Sprite Renderers, and end exactly on the rest pose (exits leave the object hidden; stopping puts it back). From code: `Raven.Motion(target, MotionPreset.PopIn)`.
- **Raven Motion** component: a motion plus what starts it — enable, scene start, click, hover, pointer leave, press, release, trigger enter / exit, collision, or a UnityEvent — with intensity, duration, delay, distance and repeat, and *Deactivate* / *Destroy* when finished. Its inspector previews in the scene and fixes common setup mistakes in one click.
- **Raven Stagger** component: plays a motion on each child, one after another (first to last, last to first, or from the center).
- **Raven Button Feel** component: hover grow, press sink and spring back, in *Subtle*, *Bouncy*, *Juicy* or custom styles.
- **Motion Library** window (**Tools ▸ RavenTween ▸ Motion Library**, or **GameObject ▸ RavenTween ▸ Add Motion…**): every motion on one page with an animated preview card, previewed on the selection, added in one click, with quick setups.
- **Raven Show Hide** component: Show / Hide / Toggle for panels and popups, with an entrance, an exit, deactivation (or blocked input) while hidden, and On Shown / On Hidden events.
- **Raven Feedback** component: "juice" for an event — camera shake, punch, color flash, any motion, sound with random pitch, hit stop, particles, set active, UnityEvent — each with its own delay, a cooldown, and Hit / Pickup / Damage recipes. Also light flash, field-of-view kick, nudge, twist, slow motion, Animator trigger and prefab spawn, with a chance and a random strength variation per action.
- **Raven Path** component: moves an object along points drawn in the Scene view, at constant speed, smooth or straight, open or closed, once / loop / back and forth, optionally facing the path; Edit Mode preview.
- **Raven Typewriter** and **Raven Counter** components for TextMeshPro: letter-by-letter reveal with a typing sound and Skip; animated numbers with prefix, suffix, decimals, `CountTo` and `Add`.
- **Raven Text Effect** for TextMeshPro: per-letter Wave, Shake, Wobble, Pulse and Rainbow, and letters that Fade, Pop or Rise in as they appear (pairs with Raven Typewriter); limited to a range of letters if needed; mesh only, no allocation while playing.
- **Motion Styles**: a motion and its settings saved as an asset (**Create ▸ RavenTween ▸ Motion Style** or **Save as style…**), shared by Raven Motion, Raven Stagger and Raven Show Hide.
- **Tint** motion (flashes a color: Graphics, Sprite Renderers and mesh materials), **Then Play** chaining on Raven Motion, `Raven.HitStop(timeScale, duration)` and `Raven.SlowMotion(timeScale, duration)`.
- **Integrations**, each compiled only when its package is present: a **Raven Motion Track** for Unity Timeline (motions sampled exactly at the playhead, editor preview restored), UI Toolkit `VisualElement` tweens, UniTask `ToUniTask`, gamepad rumble with the Input System, URP/HDRP `Volume` weight tweens and flashes, and Cinemachine 2/3 impulses. Raven Feedback gains Rumble, Volume Flash and Camera Impulse actions.
- Sample *10 - No-Code Motion*.
- `Raven.Step(deltaTime)`: advances every tween and sequence right away, whatever their update phase, also outside Play Mode — for editor tools, frame-by-frame recording and tests. `Raven.Step(deltaTime, phase)` advances one phase only.
- `UpdatePhase.Manual`: tweens the player loop never steps, advanced only by `Raven.Step(deltaTime, UpdatePhase.Manual)` (replays, offline rendering). The DOTween adapter maps `UpdateType.Manual` and `DOTween.ManualUpdate` to it.
- `ShakeLocalPosition`, `ShakeLocalRotation`, `PunchLocalPosition`, `PunchLocalRotation`: explicit names for the existing effects, which always worked in local space.
- `ShakeCamera(..., tilt: false)` and `camera.Shake(..., tilt: false)` shake the position only, for cameras whose rotation belongs to a `LookAt`, a mouse-look controller or Cinemachine.
- `Raven.Diagnostics`: logs every tween stopped because its target was destroyed (off by default, reset on entering Play Mode).
- The engine reinstalls itself, with a warning, if another system replaces the player loop without it — tweens used to stop advancing silently.
- Monitor: a warning when tweens are alive but frozen by `Time.timeScale` (or `Raven.TimeScale`) at 0.
- Documentation: a **Known Pitfalls** page, the camera pivot pattern, manual stepping, and reproducible benchmark figures (CPU, Unity version and test names). The no-code designer workflow is now presented up front, with a two-minute walkthrough and screenshots of the Sequence Editor and the Tween Template inspector. Animated previews of the motions, Raven Feedback, Raven Stagger and the text effects, plus screenshots of the Motion Library and the new inspectors.

### Changed
- Minimum Unity version is now **2022.3 LTS** (the 2021.3 claim was never tested). Every release is tested on Unity 6.3 and Unity 6.5, with no use of APIs Unity has marked obsolete.
- Starting a shake or punch no longer scans every live tween to find the effect it replaces: standalone effects are indexed by target and property. 4,000 shakes start in 0.9 ms instead of 33 ms, and creation cost no longer grows with the number of live effects. A test now guards this.

### Fixed
- Recording with `Time.captureFramerate` (frame recorders, trailer capture) could make Unity report a negative unscaled frame time, which tripped an engine assertion in development builds. Unity's frame times are now clamped before the engine sees them.

## [1.6.2] - 2026-10-12

### Fixed
- DOTween adapter: `material.DOColor(color, duration)` wrote `_Color`, which URP and HDRP Lit materials ignore, so the color never changed. It now tweens the shader's main color, like `Material.color` (`_BaseColor` on URP and HDRP Lit, `_Color` on built-in shaders).

## [1.6.1] - 2026-10-08

### Fixed
- Installing from the Git URL on a machine without Git LFS (common on Linux) imported the editor icons as text files: Unity logged "Could not create asset from Packages/com.raventween.core/Editor/Icons/… File could not be read" for every icon. The package's images are now regular Git files, and CI rejects any Git LFS file in the package.

## [1.6.0] - 2026-10-08

### Added
- **Update phases**: `UpdatePhase.FixedUpdate`, and `.UpdateIn(phase)` on any tween or sequence. Fixed tweens run right after scripts' `FixedUpdate`, before the physics step.
- **Physics** (optional assemblies, compiled with the Physics / Physics 2D modules): `TweenMovePosition`, `TweenMoveRotation` and `TweenMovePositionAtSpeed` on `Rigidbody` and `Rigidbody2D`, through `MovePosition` / `MoveRotation`, in `FixedUpdate`.
- **Time control** on `Tween` and `Sequence`: `Duration`, `DurationTotal`, `CyclesDone`, `CyclesTotal`, `InterpolationFactor`, and settable `ElapsedTime`, `ElapsedTimeTotal`, `Progress`, `ProgressTotal` (silent jumps forwards or backwards; sequences replay their children in timeline order) and `TimeScale` (per tween). They also work on sequence children (read-only there). `SetRemainingCycles(int)` and `SetRemainingCycles(bool stopAtEndValue)`. `Raven.TweenTimeScale(tween, to, duration)` and `Raven.GlobalTimeScale(to, duration)`.
- **Sequence callbacks**: `ChainCallback` and `InsertCallback`, with allocation-free target forms. They fire once per cycle, mirrored on Yoyo's way back.
- **Cycle modes** `Incremental` (each cycle continues from the last end) and `PingPong` (back with the same ease).
- **Parametric eases**: `Easing.Overshoot`, `Easing.Bounce`, `Easing.BounceExact`, `Easing.Elastic`, usable through `tween.Ease(Easing)`.
- **More tweens**: single-axis `PositionX/Y/Z`, `LocalPositionX/Y/Z`, `ScaleX/Y/Z`, `AnchoredPositionX/Y`; speed-based `PositionAtSpeed`, `LocalPositionAtSpeed`, `RotationAtSpeed`, `LocalRotationAtSpeed`, `AnchoredPositionAtSpeed`; per-renderer `PropertyBlockFloat` / `PropertyBlockColor` through a MaterialPropertyBlock; `Alpha(SpriteRenderer)`; `Raven.ShakeCamera` / `camera.Shake()`.
- **Custom tweens** for `Vector4`, `Quaternion` and `Rect`; `CustomTo` for `Vector2`, `Vector3`, `Quaternion` and `Color`.
- **Allocation-free callbacks**: `OnComplete(target, t => …)` and `OnUpdate(target, (t, tween) => …)`.
- **Control by target**: `Raven.StopAll(target)`, `CompleteAll(target)`, `PauseAll(target)`, `ResumeAll(target)`, `CountTweens(target)` — a GameObject covers all its components.
- **Cancellation**: `.WithCancellation(CancellationToken)`, polled without allocation.
- `Raven.SetCapacity(n)` to pre-create pooled slots.
- `TweenSettings<T>`: a whole tween (values and timing) in one serialized field, with overloads for the common properties.
- **DOTween adapter** (`RavenTween.DOTweenAdapter`): DOTween's shortcuts, settings, sequences, `DOTween.To`, `DOVirtual` and global calls on top of RavenTween, for migrating code one file at a time.
- Templates support the new properties (single axes, property blocks, sprite alpha) and cycle modes; editor previews restore them, removing a property block the renderer didn't have.
- The Monitor shows per-tween time scale, the update phase and sequence callbacks.
- Sample *09 - Time and Physics*: a Rigidbody platform carrying a crate, an Incremental clock, timeline callbacks with a property-block flash, and a hit stop.

### Changed
- Sequence children are ordered by when they actually start writing (their own delay included), so a delayed child takes over a shared property at the right moment.
- An exception in a custom setter now stops the tween (or its sequence) instead of being logged every frame.

- Editor scrubbing (template and Sequence Editor previews) no longer runs `OnStart` / `OnComplete` callbacks.

### Fixed
- A `StopAll` / `CompleteAll` called from a callback during an update could let a tween created later in that callback update in the same frame.
- Stopping a tween from its `OnUpdate` and creating a new one there could run the new tween's callbacks with the old tween's value.
- `Complete()` / `CompleteAll()` no longer abort half-way when a custom setter or ease throws: that tween is stopped and the others complete.

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
