# API Details

Every public type and member of the runtime, with what it does. The guide pages explain how to use them together; this page is the place for the exact behavior of one member. Optional assemblies compile only when their package is in the project (see **[Integrations](Integrations)**).

---

## Core

### `Ease`

Standard easing functions. Use `Ease.Custom` with an AnimationCurve or delegate.


### `EasingKind`

What an `Easing` evaluates.


### `Easing`

An ease with parameters: a standard `Ease`, an AnimationCurve, or a tunable Overshoot / Bounce / Elastic. Pass it to `tween.Ease(...)`. A plain struct: no allocation.

- `static Easing Of(Ease ease)` — A standard ease.
- `static Easing FromCurve(AnimationCurve curve)` — An AnimationCurve evaluated over [0, 1].
- `static Easing Overshoot(float strength = 1f)` — Goes past the end and settles back (OutBack). 1 = the classic overshoot, 0 = none, 2 = twice as far.
- `static Easing Bounce(float strength = 1f)` — Bounces on the end value (OutBounce). 1 = the classic bounce, 0.5 = bounces half as high.
- `static Easing BounceExact(float amplitude)` — Bounces exactly `amplitude` back from the end value, in the tween's own units (meters, degrees…), whatever the distance travelled.
- `static Easing Elastic(float strength = 1f, float period = 0.3f)` — Springs around the end value (OutElastic). Higher `strength` keeps oscillating longer; `period` is the length of one oscillation, as a fraction of the duration.
- `float Evaluate(float t)` — Evaluates the ease at `t`. BounceExact uses a distance of 1.

### `EaseUtility`

Evaluates easing functions. All methods are pure and allocation-free.

- `static float EvaluateParametric(EasingKind kind, float t, float a, float b, float distance)` — Evaluates a parametric ease. `distance` is the size of the change being eased, used by BounceExact to turn its amplitude into a strength.
- `static float Evaluate(Ease ease, float t)` — Evaluates `ease` at normalized time `t` in [0, 1].

### `PropertyKind`

Built-in animated properties. Driven internally without reflection or boxing.


### `Raven`

Main entry point of RavenTween. Create value tweens, delays and sequences here; target-specific tweens live in extension classes (TransformTweens, UITweens, ...).

- `static float TimeScale` — Global time multiplier applied to every tween, independent of Time.timeScale.
- `static UpdatePhase UpdatePhase` — Player loop phase in which tweens advance. Default: Update.
- `static int AliveCount` — Number of live tweens and sequences, paused ones included.
- `static bool Diagnostics` — When true, logs every tween that stops because its target was destroyed (normally silent). A debugging aid for development; resets to false when Play Mode starts.
- `static Tween Value(float from, float to, float duration)` — Tweens a float. Read the value in OnUpdate.
- `static Tween Delay(float seconds)` — Creates a timer tween. Await it, yield it, or attach OnComplete.
- `static Tween Delay(float seconds, Action onComplete)` — Creates a timer tween that fires `onComplete` when it elapses.
- `static Sequence Sequence()` — Creates an empty sequence. Fill it with Chain, Group and Insert.
- `static void StopAll()` — Stops every live tween and sequence without completing them.
- `static void CompleteAll()` — Completes every live tween and sequence, jumping them to their end values.
- `static int StopAll(object target)` — Stops the tweens animating `target`: a component, a material, a custom tween's target, or a GameObject (all its components). Null means everything. Returns how many stopped.
- `static int CompleteAll(object target)` — Completes the tweens animating `target` (see `StopAll(object)`).
- `static int PauseAll(object target = null)` — Pauses the tweens animating `target`, or every tween and sequence when null.
- `static int ResumeAll(object target = null)` — Resumes the tweens animating `target`, or every tween and sequence when null.
- `static int CountTweens(object target)` — Number of live tweens animating `target` (sequence children not counted).
- `static void SetCapacity(int capacity)` — Creates `capacity` pooled slots up front (e.g. at loading time) so the first tweens of a busy scene never allocate. The pool still grows on demand beyond it.
- `static void Step(float deltaTime)` — Advances every tween and sequence by `deltaTime` seconds right away, whatever its update phase and whether it uses scaled or unscaled time (Time.timeScale is not applied; `TimeScale` and per-tween time scales are). For editor tools, frame-by-frame recording and tests: it also works outside Play Mode, where nothing steps tweens otherwise. In Play Mode tweens keep advancing on their own as well. A call made from a tween callback while tweens are updating is ignored.
- `static void Step(float deltaTime, UpdatePhase phase)` — Advances only the tweens and sequences that run in `phase`, typically `RavenTween.UpdatePhase.Manual` for tweens your own code drives.
- `static void Step(float deltaTime, float unscaledDeltaTime, UpdatePhase phase)` — Like `Step(float, UpdatePhase)`, with a separate delta for tweens that use unscaled time.
- `static Tween GlobalTimeScale(float to, float duration)` — Tweens `Time.timeScale` (slow motion, hit stop). Runs in unscaled time.
- `static Tween TweenTimeScale(Tween tween, float to, float duration)` — Tweens the `RavenTween.Tween.TimeScale` of `tween`, e.g. to slow one animation down smoothly.
- `static Tween TweenTimeScale(Sequence sequence, float to, float duration)` — Tweens the `RavenTween.Sequence.TimeScale` of `sequence`.

### `Sequence`

Handle to a live sequence. A sequence owns its child tweens and drives them on a shared timeline through `Chain`, `Group` and `Insert`.

- `bool IsAlive` — True while the sequence is running or paused.
- `float Duration` — Total length of one sequence cycle, in seconds.
- `Sequence Chain(Tween tween)` — Appends a tween after everything already in the sequence.
- `Sequence Group(Tween tween)` — Plays a tween alongside the previously added item.
- `Sequence Insert(float time, Tween tween)` — Places a tween at an absolute time on the sequence timeline.
- `Sequence Chain(Sequence sequence)` — Appends a nested sequence after everything already in this sequence.
- `Sequence Group(Sequence sequence)` — Plays a nested sequence alongside the previously added item.
- `Sequence Insert(float time, Sequence sequence)` — Places a nested sequence at an absolute time on the timeline.
- `Sequence ChainDelay(float seconds)` — Appends a pause of `seconds` after the current chain end.
- `Sequence ChainCallback(Action callback)` — Runs `callback` when the timeline reaches the current chain end.
- `Sequence InsertCallback(float time, Action callback)` — Runs `callback` when the timeline reaches `time` seconds.
- `Sequence ChainCallback<T>(T target, Action<T> callback) where T : class` — Allocation-free `ChainCallback(Action)`: `target` is passed back to a non-capturing lambda, e.g. `ChainCallback(this, self => self.Spawn())`. Skipped if the target is a destroyed Unity object.
- `Sequence InsertCallback<T>(float time, T target, Action<T> callback) where T : class` — Allocation-free `InsertCallback(float, Action)`; see `ChainCallback<T>`.
- `Sequence Cycles(int count, CycleMode mode = CycleMode.Restart)` — Repeats the whole sequence. Use -1 for an infinite loop. Sequences support Restart and Yoyo; PingPong plays as Yoyo and Incremental as Restart.
- `Sequence Infinite(CycleMode mode = CycleMode.Restart)` — Loops the sequence forever.
- `Sequence Delay(float seconds)` — Delays the start of the whole sequence.
- `Sequence UnscaledTime(bool unscaled = true)` — Uses unscaled time so the sequence ignores Time.timeScale.
- `Sequence UpdateIn(UpdatePhase phase)` — Runs the sequence in another player loop phase (FixedUpdate for physics). Children follow it.
- `Sequence WithCancellation(CancellationToken token)` — Stops the sequence (firing OnKill) as soon as `token` is cancelled.
- `bool IsPaused` — True when the sequence is alive and paused.
- `float DurationTotal` — Delay + every cycle, in seconds. Infinity when the sequence loops forever.
- `int CyclesDone` — Cycles completed so far.
- `int CyclesTotal` — Number of cycles; -1 when infinite.
- `float ElapsedTime` — Seconds into the current cycle. Setting it jumps the whole timeline there.
- `float ElapsedTimeTotal` — Seconds since the sequence was created, delay included. Setting it jumps there (past the end completes it).
- `float Progress` — 0–1 through the current cycle. Settable.
- `float ProgressTotal` — 0–1 through the whole sequence (0 when infinite). Settable.
- `float TimeScale` — Speed multiplier for this sequence only (1 = normal, 0 = frozen). Tween it with Raven.TweenTimeScale.
- `Sequence SetRemainingCycles(int cycles)` — Changes how many cycles remain, the current one included (1 = this is the last, -1 = forever).
- `Sequence SetRemainingCycles(bool stopAtEnd)` — For Yoyo loops: completes the next time the timeline reaches its end (true) or its start (false).
- `Sequence OnComplete<T>(T target, Action<T> callback) where T : class` — Allocation-free OnComplete: `target` is passed back to a non-capturing lambda. Skipped if the target is a destroyed Unity object. One per sequence.
- `Sequence Start()` — Sequences play automatically; Start() documents intent and resumes a paused sequence.
- `void Stop()` — Stops the sequence and its children where they are and fires OnKill.
- `void Complete()` — Jumps everything to its final value and fires OnComplete.
- `TweenAwaiter GetAwaiter()` — Awaits sequence completion (or kill). Usage: await sequence;
- `TweenAwaiter ToCompletion()` — Awaits sequence completion.
- `CustomYieldInstruction ToYieldInstruction()` — Returns a coroutine instruction that waits until the sequence dies.

### `Tween`

Lightweight handle to a live tween. Handles are structs: copying them is free and a handle to a finished tween is safely dead (`IsAlive` returns false). Tweens play automatically; `Start` exists for explicit call sites.

- `bool IsAlive` — True while the tween is running or paused.
- `bool IsPaused` — True when the tween is alive and paused.
- `Tween Ease(Ease ease)` — Sets the easing function.
- `Tween Ease(AnimationCurve curve)` — Uses a custom AnimationCurve as the easing function.
- `Tween Ease(Func<float, float> easeFunction)` — Uses a custom easing delegate mapping [0,1] to the eased factor.
- `Tween Ease(Easing easing)` — Uses a parametric ease: `Easing.Overshoot(2f)`, `Easing.Bounce(0.5f)`, `Easing.BounceExact(0.2f)`, `Easing.Elastic(1.5f, 0.2f)`, or any standard ease.
- `Tween Delay(float seconds)` — Delays the start of the tween by `seconds`.
- `Tween Cycles(int count, CycleMode mode = CycleMode.Restart)` — Repeats the tween. Use -1 for an infinite loop.
- `Tween Infinite(CycleMode mode = CycleMode.Yoyo)` — Loops forever. Equivalent to Cycles(-1, mode).
- `Tween UnscaledTime(bool unscaled = true)` — Uses unscaled time so the tween ignores Time.timeScale.
- `Tween UpdateIn(UpdatePhase phase)` — Runs the tween in another player loop phase: FixedUpdate for physics, LateUpdate after your scripts.
- `Tween WithCancellation(CancellationToken token)` — Stops the tween (firing OnKill) as soon as `token` is cancelled. No allocation.
- `Tween SetRemainingCycles(int cycles)` — Changes how many cycles remain, the current one included (1 = this is the last, -1 = forever).
- `Tween SetRemainingCycles(bool stopAtEndValue)` — For Yoyo and PingPong loops: completes the next time the tween reaches its end value (true) or its start value (false).
- `float Duration` — Length of one cycle, in seconds, without the delay.
- `float DurationTotal` — Delay + every cycle, in seconds. Infinity when the tween loops forever.
- `int CyclesDone` — Cycles completed so far.
- `int CyclesTotal` — Number of cycles; -1 when infinite.
- `float ElapsedTime` — Seconds into the current cycle. Setting it jumps the tween there.
- `float ElapsedTimeTotal` — Seconds since the tween was created, delay included. Setting it jumps there (past the end completes it).
- `float Progress` — 0–1 through the current cycle. Settable.
- `float ProgressTotal` — 0–1 through the whole tween (0 when infinite). Settable.
- `float InterpolationFactor` — The eased factor shown right now: 0 on the start value, 1 on the end value (beyond 1 while overshooting).
- `float TimeScale` — Speed multiplier for this tween only (1 = normal, 0 = frozen). Tween it with Raven.TweenTimeScale.
- `Tween OnUpdate(Action callback)` — Called every frame after the value has been applied.
- `Tween OnUpdate(Action<float> callback)` — Receives the interpolated float value. Intended for Raven.Value tweens.
- `Tween OnUpdate(Action<TweenValue> callback)` — Receives the full interpolated value for any value kind.
- `Tween OnKill(Action callback)` — Called when the tween is stopped before completing.
- `Tween OnComplete<T>(T target, Action<T> callback) where T : class` — Allocation-free OnComplete: `target` is passed back to a non-capturing lambda, e.g. `OnComplete(this, self => self.Open())`. Skipped if the target is a destroyed Unity object. One per tween.
- `Tween OnUpdate<T>(T target, Action<T, Tween> callback) where T : class` — Allocation-free OnUpdate: receives `target` and this tween every frame, e.g. `OnUpdate(bar, (b, t) => b.fill = t.Progress)`. One per tween.
- `Tween OnTargetDestroyed(Action callback)` — Called when the tween target is destroyed while the tween is alive.
- `Tween Start()` — Tweens play automatically; Start() documents intent and resumes a paused tween.
- `void Stop()` — Stops the tween where it is and fires OnKill.
- `void Complete()` — Jumps to the final value and fires OnComplete.
- `TweenAwaiter GetAwaiter()` — Awaits tween completion (or kill). Usage: await tween;
- `TweenAwaiter ToCompletion()` — Awaits tween completion. Alias for await-friendliness in expression contexts.
- `CustomYieldInstruction ToYieldInstruction()` — Returns a coroutine instruction that waits until the tween dies.

### `TweenAwaiter`

Awaiter that completes when its tween dies (completed or killed).

- `TweenAwaiter GetAwaiter()` — Allows "await tween.ToCompletion()".

### `UpdatePhase`

Player loop phase in which the engine advances tweens.

- `FixedUpdate` — Physics rate, with Time.fixedDeltaTime. Use it for Rigidbody tweens.
- `Manual` — Never stepped by the player loop: only Raven.Step advances it (replays, frame-by-frame rendering).

### `TweenParams`

Serializable tween settings. Expose a field of this type in any MonoBehaviour to let designers tune duration, easing and cycles without recompiling, then apply it with `ApplyTo`.

- `static TweenParams Default` — Sensible defaults: 0.3s, OutQuad, single cycle.
- `Tween ApplyTo(Tween tween)` — Applies delay, easing, cycles and time mode to a live tween and returns it. `duration` is not applied: a tween's duration is set when it is created, so pass it to the factory, e.g. `p.ApplyTo(Raven.Scale(t, 1.2f, p.duration))`. Call it before adding the tween to a sequence.

### `TweenSettings`

A whole tween in one serializable field: end value, optional start value and timing. Expose it in a component so designers tune the values in the Inspector, then play it with the matching overload, e.g. `Raven.LocalPosition(transform, settings)`. A field declared without an initializer has a duration of 0: give it one with the constructor.

- `TweenSettings(T endValue, float duration, Ease ease = Ease.OutQuad)` — Starts from the current value.
- `TweenSettings(T startValue, T endValue, float duration, Ease ease = Ease.OutQuad) : this(endValue, duration, ease)` — Starts from `startValue`.

### `CycleMode`

How a tween behaves when it repeats.

- `Restart` — Each cycle restarts from the initial value.
- `Yoyo` — Odd cycles play the animation backwards in time, so the way back mirrors the ease (an OutQuad rise falls back like an InQuad). Same as DOTween's Yoyo.
- `Incremental` — Each cycle continues from where the previous one ended, adding the same change again (move 1 m, then 1 m further…). Tweens only; sequences play it as Restart.
- `PingPong` — Odd cycles go from the end value back to the start value with the ease applied as is (an OutQuad rise also falls back like an OutQuad). Tweens only; sequences play it as Yoyo.

### `ValueKind`

Kind of value carried by a `TweenValue`.


### `TweenValue`

Compact tagged union for every interpolated type. Stored as a Vector4 so tween slots stay fixed-size and the engine never boxes values.

- `static TweenValue Lerp(in TweenValue from, in TweenValue to, float t)` — Interpolates between two values of the same kind. Quaternions use slerp.
- `static TweenValue AddCycles(in TweenValue value, in TweenValue from, in TweenValue to, int cycles)` — Adds `cycles` times the change from `from` to `to` to `value` (Incremental cycles). Quaternions apply the rotation that many more times.
- `static float Distance(in TweenValue from, in TweenValue to)` — Size of the change between two values: a length, or degrees for quaternions.

---

## Property, effect and custom tweens

### `Raven` (AudioSource)

- `static Tween Volume(AudioSource target, float to, float duration)` — Tweens an AudioSource's volume.
- `static Tween Pitch(AudioSource target, float to, float duration)` — Tweens an AudioSource's pitch.

### `AudioTweens`

Extension-method style access to AudioSource tweens.


### `Raven` (Camera)

- `static Tween FieldOfView(Camera target, float to, float duration)` — Tweens a camera's field of view (perspective cameras).
- `static Tween OrthographicSize(Camera target, float to, float duration)` — Tweens a camera's orthographic size (orthographic cameras).
- `static Tween BackgroundColor(Camera target, Color to, float duration)` — Tweens a camera's background color.

### `CameraTweens`

Extension-method style access to Camera tweens.


### `Raven` (custom tweens)

- `static Tween Custom<T>(T target, float from, float to, float duration, Action<T, float> setter) where T : class` — Tweens any float on any target through a setter. Pass a non-capturing lambda (e.g. `(t, v) => t.fontSize = v`) and the tween allocates nothing. If `target` is a UnityEngine.Object, the tween dies with it.
- `static Tween Custom<T>(T target, Vector2 from, Vector2 to, float duration, Action<T, Vector2> setter) where T : class` — Zero-allocation Vector2 custom tween. See the float overload.
- `static Tween Custom<T>(T target, Vector3 from, Vector3 to, float duration, Action<T, Vector3> setter) where T : class` — Zero-allocation Vector3 custom tween. See the float overload.
- `static Tween Custom<T>(T target, Color from, Color to, float duration, Action<T, Color> setter) where T : class` — Zero-allocation Color custom tween. See the float overload.
- `static Tween Custom<T>(T target, Vector4 from, Vector4 to, float duration, Action<T, Vector4> setter) where T : class` — Zero-allocation Vector4 custom tween. See the float overload.
- `static Tween Custom<T>(T target, Quaternion from, Quaternion to, float duration, Action<T, Quaternion> setter) where T : class` — Zero-allocation Quaternion custom tween (spherical interpolation). See the float overload.
- `static Tween Custom<T>(T target, Rect from, Rect to, float duration, Action<T, Rect> setter) where T : class` — Zero-allocation Rect custom tween (position and size). See the float overload.
- `static Tween CustomTo<T>(T target, Func<T, float> getter, float to, float duration, Action<T, float> setter) where T : class` — Tweens a float from its value **when the tween starts** (read with `getter`) to `to`. Unlike Custom, the start value is not frozen at creation, so it stays correct after a Delay or inside a sequence. Use non-capturing lambdas for zero allocations.
- `static Tween CustomTo<T>(T target, Func<T, Vector2> getter, Vector2 to, float duration, Action<T, Vector2> setter) where T : class` — Vector2 `CustomTo<T>(T, Func<T, float>, float, float, Action<T, float>)`.
- `static Tween CustomTo<T>(T target, Func<T, Vector3> getter, Vector3 to, float duration, Action<T, Vector3> setter) where T : class` — Vector3 `CustomTo<T>(T, Func<T, float>, float, float, Action<T, float>)`.
- `static Tween CustomTo<T>(T target, Func<T, Quaternion> getter, Quaternion to, float duration, Action<T, Quaternion> setter) where T : class` — Quaternion `CustomTo<T>(T, Func<T, float>, float, float, Action<T, float>)`.
- `static Tween CustomTo<T>(T target, Func<T, Color> getter, Color to, float duration, Action<T, Color> setter) where T : class` — Color `CustomTo<T>(T, Func<T, float>, float, float, Action<T, float>)`.

### `Raven` (shake and punch)

- `static Tween ShakePosition(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency)` — Noise-driven shake around the current **local** position, decaying to rest. Same as `ShakeLocalPosition`.
- `static Tween ShakeRotation(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency)` — Noise-driven shake around the current **local** euler angles, in degrees. Same as `ShakeLocalRotation`.
- `static Tween ShakeScale(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency)` — Noise-driven shake around the current local scale.
- `static Tween PunchPosition(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency)` — Damped spring kick along `punch` from the current **local** position. Same as `PunchLocalPosition`.
- `static Tween PunchRotation(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency)` — Damped spring kick on **local** euler angles, in degrees. Same as `PunchLocalRotation`.
- `static Tween PunchScale(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency)` — Damped spring kick on local scale. Classic "button press" feedback.
- `static Tween ShakeLocalPosition(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency)` — Noise-driven shake around the current local position (explicit name for `ShakePosition`).
- `static Tween ShakeLocalRotation(Transform target, Vector3 strength, float duration, float frequency = DefaultShakeFrequency)` — Noise-driven shake around the current local euler angles (explicit name for `ShakeRotation`).
- `static Tween PunchLocalPosition(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency)` — Damped spring kick from the current local position (explicit name for `PunchPosition`).
- `static Tween PunchLocalRotation(Transform target, Vector3 punch, float duration, float frequency = DefaultPunchFrequency)` — Damped spring kick on local euler angles (explicit name for `PunchRotation`).
- `static Tween ShakeCamera(Camera camera, float strength = 1f, float duration = 0.5f, float frequency = 15f, bool tilt = true)` — A ready-made camera shake: a small position jitter plus a tilt, scaled by `strength` (1 = a solid hit, 0.3 = a light rumble). Repeated hits restart the shake around the same rest pose. Returns the position shake; the tilt has the same length. Stop both with `Raven.StopAll(camera.transform)`. If a script moves the camera every frame, shake a parent of the camera instead. Pass `tilt` = false when something else owns the camera's rotation (LookAt, a mouse-look controller).

### `EffectTweens`

Extension-method style access to shake and punch effects.


### `Raven` (Material)

- `static Tween MaterialFloat(Material target, int propertyId, float to, float duration)` — Tweens a material float property. Prefer Shader.PropertyToID over name strings.
- `static Tween MaterialFloat(Material target, string propertyName, float to, float duration)` — Tweens a material float property by name. Resolves the ID once at creation.
- `static Tween MaterialColor(Material target, int propertyId, Color to, float duration)` — Tweens a material color property. Prefer Shader.PropertyToID over name strings.
- `static Tween MaterialColor(Material target, string propertyName, Color to, float duration)` — Tweens a material color property by name. Resolves the ID once at creation.

### `MaterialTweens`

Extension-method style access to Material tweens.


### `ProceduralTweens`

Weight tweens for the procedural components. Blending a look-at or a spring chain in and out over time is what turns them from always-on effects into animation. Every tween starts from the weight the component has **when the tween starts**, so they can be chained in sequences or delayed.

- `static Tween TweenWeight(this RavenLookAt lookAt, float to, float duration)` — Blends the look-at in (1) or out (0) from its current weight.
- `static Tween TweenWeight(this RavenSpringChain chain, float to, float duration)` — Blends the spring motion in (1) or out (0) from its current weight.
- `static Tween LookAtTarget(this RavenLookAt lookAt, Transform newTarget, float blendDuration)` — Switches the look-at to a new target and blends it in. With a weight already at 1 the smoothing handles the turn; from 0 the head eases from its animated pose.

### `Raven` (renderers and lights)

- `static Tween Color(SpriteRenderer target, Color to, float duration)` — Tweens a SpriteRenderer's color.
- `static Tween Alpha(SpriteRenderer target, float to, float duration)` — Tweens only the alpha of a SpriteRenderer's color.
- `static Tween PropertyBlockFloat(Renderer target, int propertyId, float to, float duration)` — Tweens a shader float on this renderer only, through a MaterialPropertyBlock: no material copy, other renderers sharing the material are untouched.
- `static Tween PropertyBlockFloat(Renderer target, string propertyName, float to, float duration)` — Same as `PropertyBlockFloat(Renderer, int, float, float)`, by property name.
- `static Tween PropertyBlockColor(Renderer target, int propertyId, Color to, float duration)` — Tweens a shader color on this renderer only, through a MaterialPropertyBlock.
- `static Tween PropertyBlockColor(Renderer target, string propertyName, Color to, float duration)` — Same as `PropertyBlockColor(Renderer, int, Color, float)`, by property name.
- `static Tween Intensity(Light target, float to, float duration)` — Tweens a Light's intensity.
- `static Tween Color(Light target, Color to, float duration)` — Tweens a Light's color.

### `RenderingTweens`

Extension-method style access to renderer and light tweens.


### `Raven` (Transform)

- `static Tween Position(Transform target, Vector3 to, float duration)` — Tweens world position.
- `static Tween LocalPosition(Transform target, Vector3 to, float duration)` — Tweens local position.
- `static Tween Rotation(Transform target, Quaternion to, float duration)` — Tweens world rotation (slerp).
- `static Tween LocalRotation(Transform target, Quaternion to, float duration)` — Tweens local rotation (slerp).
- `static Tween EulerAngles(Transform target, Vector3 to, float duration)` — Tweens world euler angles (lerped component-wise).
- `static Tween LocalEulerAngles(Transform target, Vector3 to, float duration)` — Tweens local euler angles (lerped component-wise).
- `static Tween Scale(Transform target, Vector3 to, float duration)` — Tweens local scale.
- `static Tween Scale(Transform target, float to, float duration)` — Tweens local scale uniformly.
- `static Tween PositionX(Transform target, float to, float duration)` — Tweens the world X position only.
- `static Tween PositionY(Transform target, float to, float duration)` — Tweens the world Y position only.
- `static Tween PositionZ(Transform target, float to, float duration)` — Tweens the world Z position only.
- `static Tween LocalPositionX(Transform target, float to, float duration)` — Tweens the local X position only.
- `static Tween LocalPositionY(Transform target, float to, float duration)` — Tweens the local Y position only.
- `static Tween LocalPositionZ(Transform target, float to, float duration)` — Tweens the local Z position only.
- `static Tween ScaleX(Transform target, float to, float duration)` — Tweens the local X scale only.
- `static Tween ScaleY(Transform target, float to, float duration)` — Tweens the local Y scale only.
- `static Tween ScaleZ(Transform target, float to, float duration)` — Tweens the local Z scale only.
- `static Tween PositionAtSpeed(Transform target, Vector3 to, float speed)` — Moves to `to` at `speed` units per second (world space).
- `static Tween LocalPositionAtSpeed(Transform target, Vector3 to, float speed)` — Moves to `to` at `speed` units per second (local space).
- `static Tween RotationAtSpeed(Transform target, Quaternion to, float degreesPerSecond)` — Turns to `to` at `degreesPerSecond` (world space, shortest path).
- `static Tween LocalRotationAtSpeed(Transform target, Quaternion to, float degreesPerSecond)` — Turns to `to` at `degreesPerSecond` (local space, shortest path).

### `TransformTweens`

Extension-method style access to Transform tweens.


### `Raven` (UI)

- `static Tween AnchoredPosition(RectTransform target, Vector2 to, float duration)` — Tweens a RectTransform's anchored position.
- `static Tween SizeDelta(RectTransform target, Vector2 to, float duration)` — Tweens a RectTransform's size delta.
- `static Tween Alpha(CanvasGroup target, float to, float duration)` — Tweens a CanvasGroup's alpha.
- `static Tween Color(Graphic target, Color to, float duration)` — Tweens a Graphic's color (Image, Text, RawImage, ...).
- `static Tween Alpha(Graphic target, float to, float duration)` — Tweens only the alpha channel of a Graphic's color.
- `static Tween AnchoredPositionX(RectTransform target, float to, float duration)` — Tweens the anchored X position only.
- `static Tween AnchoredPositionY(RectTransform target, float to, float duration)` — Tweens the anchored Y position only.
- `static Tween AnchoredPositionAtSpeed(RectTransform target, Vector2 to, float speed)` — Moves to `to` at `speed` UI units per second.

### `UITweens`

Extension-method style access to UI tweens.


---

## Motions and no-code components

### `Raven` (time scale)

- `static void HitStop(float timeScale = 0.05f, float duration = 0.08f)` — Freezes the game for an instant, the classic heavy-hit effect: `Time.timeScale` drops to `timeScale` for `duration` real seconds, then comes back. Overlapping hit stops extend the freeze and restore the original time scale once. The countdown runs in real time inside the engine, whatever the update phase, `TimeScale` or stopped tweens, so the game can never stay frozen. It never speeds the game up: started while the game already runs slower (a pause), it does nothing.
- `static bool IsHitStopping` — True while a hit stop holds the time scale.
- `static void EndHitStop()` — Ends a running hit stop at once and restores the time scale.
- `static bool IsInSlowMotion` — True while a slow motion holds the time scale down.
- `static void SlowMotion(float timeScale = 0.3f, float duration = 1f)` — Slows the game down smoothly to `timeScale`, holds, then speeds back up, in `duration` real seconds. A new slow motion replaces a running one and keeps the original speed; if the game changes the time scale meanwhile (a pause menu), the speed is not restored over it. It never speeds the game up: started while the game already runs slower (a pause), it does nothing. During a hit stop it is compared with the speed the hit stop will restore, and restores that speed when it ends; if the game changed the time scale during the hit stop (a pause menu), it does nothing.

### `Raven` (motions)

- `static Sequence Motion(Component target, MotionPreset preset)` — Plays a ready-made `preset` on `target`'s object, around its current pose. Entrances hide the object at once, then reveal it. Calling it again on the same object while a motion runs replaces that motion and keeps its rest pose, so rapid repeats (a pulse on every click) never drift.
- `static Sequence Motion(Component target, MotionPreset preset, MotionSettings settings)` — Plays a ready-made `preset` with custom timing, strength or distance.
- `static Sequence Motion(in MotionTarget target, MotionPreset preset, MotionSettings settings, in MotionRest rest)` — Plays a preset around a rest pose you captured earlier (see `MotionRest.Capture`).

### `MotionTweens`

Extension-method style access to motions.


### `MotionPreset`

Ready-made animations that need no animation knowledge: pick one, play it on any object. They adapt to UI (anchored position, CanvasGroup / Graphic alpha) and to scene objects (local transform, SpriteRenderer alpha). Values are explicit so that saved scenes keep their choice when presets are added.


### `MotionCategory`

The four families of `MotionPreset`.


### `MotionTrigger`

What starts a `RavenMotion`.

- `OnEnable` — Every time the object is enabled (and when the scene starts).
- `OnStart` — Once, when the scene starts.
- `OnClick` — When the object is clicked (a UI Button's click, or a pointer click).
- `OnPointerEnter` — When the pointer moves over the object.
- `OnPointerExit` — When the pointer leaves the object.
- `OnPointerDown` — When the pointer is pressed on the object.
- `OnPointerUp` — When the pointer is released after a press on the object.
- `OnTriggerEnter` — When something enters this object's trigger collider (3D or 2D).
- `OnTriggerExit` — When something leaves this object's trigger collider (3D or 2D).
- `OnCollisionEnter` — When something hits this object's collider (3D or 2D).
- `Manual` — Only when `RavenMotion.Play` is called (a UnityEvent, a button, code).

### `MotionFinish`

What happens to the object once its motion has finished.

- `Deactivate` — Deactivate the GameObject (typical after an exit).
- `Destroy` — Destroy the GameObject.

### `MotionSettings`

The few knobs a motion needs. Every field has a sensible automatic value.

- `static readonly Color DefaultTint = new Color(1f, 0.22f, 0.2f, 1f)` — The Tint color used when none is set: a damage red.
- `static MotionSettings Default` — Automatic timing and distance, full intensity, no delay, plays once.

### `MotionLibrary`

Names, families and default timings of the presets, for tools and inspectors.

- `static readonly MotionPreset[] All` — Every preset, in display order.
- `static float DefaultDuration(MotionPreset preset)` — Seconds one play (or one loop cycle) takes when the duration is automatic.
- `static bool NeedsAlpha(MotionPreset preset)` — True when the preset is about visibility (fades, flashes) and needs something with an alpha.
- `static bool NeedsColor(MotionPreset preset)` — True when the preset changes the object's color and needs a Graphic, a Sprite Renderer or a renderer with a color.
- `static bool MovesPosition(MotionPreset preset)` — True when the preset moves the object, which a parent Layout Group would fight.
- `static bool UsesDistance(MotionPreset preset)` — True when the distance setting changes the preset.
- `static string DisplayName(MotionPreset preset)` — Readable name with spaces ("Slide In Left").
- `static string Description(MotionPreset preset)` — One sentence saying what the preset looks like.

### `MotionStyle`

A motion with its settings saved as an asset: your team's own "Pop In", tuned once and shared by every Raven Motion, Raven Stagger and Raven Show Hide that uses it. Changing the asset changes them all. Create one with Create ▸ RavenTween ▸ Motion Style, or with "Save as style" in the Motion Library.

- `Sequence Play(Component target)` — Plays this style on `target` around its current pose.

### `IMotionTriggerable`

Implemented by components that start on pointer events (clicks, hovers). The pointer relay forwards each event to every such component on its GameObject.


### `MotionTriggers`

Trigger helpers shared by the no-code components.

- `static void EnsurePointerRelay(GameObject go, MotionTrigger trigger)` — Adds the relay `trigger` needs to `go`: hover, press or click. Each relay only takes its own events, so it never steals the others from parents.
- `static bool PassesTag(string filterTag, GameObject other)` — True when `other` passes a tag filter (empty = any object).

### `MotionChannels`

Which parts of an object a motion animates.


### `MotionTarget`

What a motion animates on one object: its transform (anchored position for UI), the first thing with an alpha it finds (a CanvasGroup, then a UI Graphic, then a SpriteRenderer) and the first thing with a color (a Graphic, a SpriteRenderer, or a renderer's material color).

- `Renderer Renderer` — A mesh (or other) renderer whose material color can be tinted; null otherwise.
- `int ColorId` — The material color property the renderer tints: _BaseColor (URP / HDRP) or _Color.
- `bool HasAlpha` — True when fades have something to fade.
- `bool IsFlat` — UI and sprites turn around Z (in the screen plane); 3D objects spin around Y.
- `bool HasColor` — True when the Tint motion has something to color.
- `float AutoDistance(in MotionRest rest)` — The object's size, used for automatic slide distances: pixels for UI, units otherwise.

### `MotionRest`

The pose an object returns to: entrances end on it, exits start from it, attention and loop motions move around it. Positions are anchored positions for UI, local otherwise.

- `void Apply(in MotionTarget target, MotionChannels channels)` — Puts the chosen channels of `target` back on this pose.

### `RavenButtonFeel`

Makes a button feel alive with no setup: it grows a little under the pointer, sinks when pressed and springs back when released. Works on UI and on scene objects (with a collider and a Physics Raycaster). Respects a Selectable's Interactable flag.


### `RavenButtonFeel.Style`

- `Subtle` — Barely there: for dense menus.
- `Bouncy` — Clear and springy: a good default.
- `Juicy` — Big and playful: for casual and kids' games.
- `Custom` — Your own sizes and timing below.

### `FeedbackAction`

One step of a `RavenFeedback`: a shake, a flash, a sound, a hit stop…


### `FeedbackAction.Kind`

- `Motion` — Any ready-made motion on an object.
- `CameraShake` — Shakes a camera.
- `Punch` — A springy scale kick.
- `ColorFlash` — Flashes a color and fades back.
- `Sound` — Plays a sound.
- `HitStop` — Freezes the game for an instant, like a heavy hit.
- `Particles` — Plays a particle system.
- `SetActive` — Turns an object on or off.
- `Event` — Calls anything through a UnityEvent.
- `LightFlash` — Flashes a light brighter, then back.
- `FieldOfView` — Widens a camera's field of view for an instant (speed, impact).
- `Nudge` — Kicks an object along a direction and springs it back.
- `Twist` — Twists an object around an axis and springs it back.
- `SlowMotion` — Slows the game down smoothly, holds, then speeds back up.
- `AnimatorTrigger` — Sets a trigger on an Animator.
- `Spawn` — Spawns a prefab at the object (a puff, sparks, a popup).
- `Rumble` — Vibrates the gamepad (needs the Input System package).
- `VolumeFlash` — Flashes a post-processing Volume in and out (needs URP or HDRP).
- `CameraImpulse` — Sends a Cinemachine impulse (needs Cinemachine).

### `FeedbackHooks`

Where the optional integrations plug their feedback actions in (Input System rumble, SRP volumes, Cinemachine impulses). Each is set by its assembly when its package is present.


### `RavenFeedback`

"Juice" without code: a list of actions — camera shake, punch, color flash, sound, hit stop, particles, any motion — played together, each after its own delay. Start it from a click, a hover, a trigger zone, a collision, or any UnityEvent (Play).

- `float Cooldown` — Seconds before the feedback can play again (0 = no limit).
- `void Play()` — Plays every action, each after its own delay.
- `void Stop()` — Cancels delayed actions and puts animated objects back.

### `RavenMotion`

Plays a ready-made `MotionPreset` without code: pick a motion, pick what starts it (enable, click, hover, a trigger zone, a collision, or a UnityEvent), done. Several Raven Motions can sit on one object, e.g. an entrance plus a hover pulse, and one can start the next ("Then Play"), e.g. Pop In then Float. Added from a script, it plays as soon as it is enabled: add it to an inactive object to configure it first.

- `MotionPreset EffectivePreset` — The motion that plays: the style's when a style is set, otherwise `Preset`.
- `MotionSettings EffectiveSettings` — The settings in use: the style's when a style is set, otherwise `Settings`.
- `Transform AnimatedTransform` — Object being animated: `target`, or this one.
- `Sequence Current` — The running motion, if any.
- `void Play()` — Plays the motion from the start, replacing a run that is still going.
- `void Stop()` — Stops the motion and puts back what it animated. An idle object is left alone, unless an exit hid it: then it is shown again on its rest pose.
- `void Complete()` — Jumps to the end of the motion at once (loops: back to the rest pose).
- `void CaptureRest()` — Takes the object's current pose as its rest pose (after moving it from a script).
- `void HandleTrigger(MotionTrigger started)` — Starts the motion if `started` is this component's trigger.

### `RavenPointerRelay`

Forwards pointer events to the no-code components of its GameObject (Raven Motion, Raven Feedback…). The input module sends each event to the first object up the hierarchy that handles it, so a handler would steal it from parent objects: there is one relay per kind of event, added on demand, so hovering an icon never stops its parent Button's clicks.


### `RavenHoverRelay`

Hover events (enter, leave).


### `RavenPressRelay`

Press events (down, up).


### `RavenClickRelay`

Click events.


### `RavenPath`

Moves its object along a path of points drawn in the Scene view: moving platforms, patrols, flying pickups, camera fly-bys. The path starts where the object stands and is stored relative to it, so moving the object moves the whole path. Constant speed along curves.

- `Vector3 Origin` — Where the path starts, in the parent's space: the object's position when it first played.
- `float Length` — Length of the path in the parent's units.
- `float TripDuration` — Seconds one trip takes.
- `void Play()` — Travels the path from its start.
- `void Stop()` — Stops where the object is.
- `void ResetToStart()` — Stops and puts the object back at the start of the path.
- `Tween CreateTween()` — One trip as a tween (no loop), for previews and custom sequences.
- `void ForgetOrigin()` — Forgets the start point: the next play starts from where the object stands now.
- `Vector3 Evaluate(float t)` — Point at `t` (0 = start, 1 = end) along the path, at constant speed, in the parent's space.
- `void GetWorldPolyline(List<Vector3> world)` — Fills `world` with points along the path in world space, for drawing.
- `Vector3 GetWorldPoint(int index)` — World position of control point `index` (-1 = the start).
- `void SetWorldPoint(int index, Vector3 world)` — Moves control point `index` to a world position.

### `RavenPath.PathLoop`

- `Once` — Travel once and stop at the end.
- `Loop` — Start over from the beginning forever (close the path to make a circuit).
- `BackAndForth` — Go to the end and back, forever.

### `RavenPath.Orientation`

- `None` — Keep the object's rotation.
- `FacePath` — Face the direction of travel (3D: forward along the path, up stays up).
- `FacePath2D` — Face the direction of travel in the screen plane (2D: the object's right side leads).

### `RavenPath.Timing`

- `Duration` — The whole trip takes Duration seconds.
- `Speed` — Travel at Speed units per second, whatever the length.

### `RavenShowHide`

Shows and hides a panel, a popup or any object with an entrance and an exit motion. Wire Show, Hide or Toggle to buttons; the object is deactivated (or made invisible and non-interactive) while hidden and always comes back exactly where it was designed.

- `bool IsShown` — True when shown or showing; false when hidden or hiding.
- `bool DeactivateWhenHidden` — Deactivate the object while hidden (otherwise: invisible and unclickable).
- `void Show()` — Plays the entrance, activating the object first if needed.
- `void Hide()` — Plays the exit, then deactivates the object (or hides it and blocks its input).
- `void SetVisible(bool visible)` — Shows or hides at once, without animation and without raising the events.

### `RavenStagger`

Plays one `MotionPreset` on every child, one after another: menus that cascade in, list rows, inventory slots, a row of coins. No code needed.

- `void Play()` — Plays the motion on every active child, one after another.
- `void Stop()` — Stops every child's motion and puts back what it animated. Children at rest are left alone, unless an exit hid them: then they are shown again on their rest poses.

### `RavenStagger.Order`

- `FirstToLast` — First child first.
- `LastToFirst` — Last child first.
- `CenterOut` — From the middle child outwards.

---

## Templates

### `RavenAnimator`

Plays one or more tween templates on scene targets without writing code. Hook `Play` to a UnityEvent, enable Play On Enable, or call it from code.

- `List<Entry> Entries` — Entries played by this animator. Edit at runtime before calling Play.
- `UnityEvent OnAllComplete` — Raised once every entry of the current Play call has completed.
- `void Play()` — Stops any previous run, then plays every entry.
- `void Stop()` — Stops every tween started by this animator. Does not raise On All Complete.
- `void CompleteNow()` — Jumps every running tween to its end value, then raises On All Complete.

### `RavenAnimator.Entry`

One template played by the animator.

- `Object target` — Component or asset the template animates (Transform, CanvasGroup, Material, …). A GameObject also works: the template finds the component its property needs on it.
- `TweenTemplate template` — The tween to play on `target`. An entry with no target or no template is skipped with a warning.

### `RavenSequencePlayer`

Builds and plays a sequence from serialized steps without writing code. Each step chains, groups or inserts a template on a scene target.

- `List<Step> Steps` — Steps played by this component. Edit at runtime before calling Play.
- `UnityEvent OnComplete` — Raised when the built sequence completes.
- `Sequence Current` — Handle to the sequence of the current run; dead when nothing plays.
- `void Play()` — Stops any previous run, rebuilds the sequence from steps, and plays it.
- `void Stop()` — Stops the running sequence, if any.
- `void CompleteNow()` — Jumps the running sequence to its end. The handle is cleared first: On Complete may start a new run (Play) safely.

### `RavenSequencePlayer.StepMode`

- `Chain` — Plays after everything before it.
- `Group` — Plays alongside the previous step.
- `Insert` — Plays at an absolute time on the sequence timeline.

### `RavenSequencePlayer.Step`

One step of the built sequence.

- `StepMode mode` — How the step is placed: chained after everything before it, grouped with the previous step, or inserted at `insertTime`.
- `float insertTime` — Position on the sequence timeline, in seconds; used only by `Insert`.
- `Object target` — Component or asset the template animates.
- `TweenTemplate template` — The tween to play on `target`. A looping template plays a finite number of cycles inside the sequence (`CyclesInsideSequence`). A step with no target or no template is skipped with a warning.

### `TweenTemplate`

Reusable tween configuration. Tweens themselves are single-use; templates hold the settings and spawn a fresh tween for any target via `Play`.

- `bool LoopsForever` — True when this template loops forever (cycles = -1).
- `int CyclesInsideSequence` — Finite stand-in used when a looping template plays inside a sequence: one full out-and-back for Yoyo (so it ends where it started), one pass for Restart.
- `bool UsesShaderProperty` — True when the property is a shader property and needs `materialProperty`.
- `Tween Play(Object target)` — Creates and plays a tween of this template on `target`. `target` may be the exact object the property needs, or any GameObject or component carrying it: the right component (or the renderer's material) is found.

---

## Procedural animation

### `RavenLookAt`

Turns a transform (a head, an eye, a turret) toward a moving target every frame, on top of whatever already posed it — an Animator, a tween or your own script. Smoothed, angle-limited, and blended by `Weight`: tween the weight to look at or away. The component tracks the base pose instead of resetting it: if the rotation it wrote last frame is still there, nobody else touched the bone and the previous base pose is reused; otherwise whatever wrote the new rotation defines the new base pose.

- `Transform Target` — What to look at. Null eases back to the base pose.
- `float Weight` — Blend between the base pose (0) and fully looking at the target (1).
- `float MaxAngle` — Maximum turn away from the base pose, in degrees.
- `float SmoothTime` — Seconds to catch up with the target; 0 snaps instantly.
- `void ResetSmoothing()` — Snaps the smoothing state so the next frame starts from the base pose.

### `RavenSpringChain`

Secondary motion for a chain of bones — ponytails, antennae, tails, cables, ears. Each bone lags behind, swings and settles back toward its base pose (from an Animator, a tween or a script). Runs after animation, allocates nothing per frame, blends by `Weight`. Like `RavenLookAt`, the base pose is tracked rather than reset: a bone whose rotation is still the one written last frame keeps its previous base pose; any other rotation was written by someone else and becomes the new base pose.

- `float Weight` — Blend between the base pose (0) and full spring motion (1).
- `int BoneCount` — Number of bones driven by this chain, root included.
- `void Build()` — Re-reads the chain from the root and snaps the simulation to the current base pose. Call it after changing the bone hierarchy. After a teleport, `ResetPhysics` is enough.
- `void ResetPhysics()` — Puts every simulated point back on the base pose, with no velocity (e.g. after a teleport).

---

## TextMeshPro

### `RavenCounter`

A number that counts up or down on a TextMeshPro text: scores, coins, damage, timers. Call CountTo(value) from code or a UnityEvent; it counts from what is shown now. Formatting goes through TextMeshPro's allocation-free SetText.

- `float Value` — The value on screen right now.
- `void CountTo(float value)` — Counts from the value shown now to `value`.
- `void Add(float amount)` — Adds `amount` to the value being counted to (score += 50), even mid-count.
- `void SetValue(float value)` — Shows `value` at once.

### `RavenTextEffect`

Animates the letters of a TextMeshPro text one by one: a wave, a shake, a wobble, a pulse or a rainbow, and letters that pop or fade in as they appear (with Raven Typewriter or any change to maxVisibleCharacters). Works on the mesh only: the text, its layout and its colors in the Inspector are never changed. No allocation while it plays.

- `bool IsShaping` — True while it has a laid-out text to shape (false for an empty text).
- `void Replay()` — Plays the Appear animation again for every letter (e.g. when the text is shown again).

### `RavenTextEffect.Effect`

- `None` — No continuous effect (letters can still appear with Appear).
- `Wave` — Letters bob up and down in a wave.
- `Shake` — Letters tremble nervously (anger, cold, fear).
- `Wobble` — Letters tilt left and right.
- `Pulse` — Letters swell and shrink in turn.
- `Rainbow` — Colors flow through the letters.

### `RavenTextEffect.Appear`

- `Instant` — Letters show at once.
- `Fade` — Each letter fades in.
- `Pop` — Each letter pops in with a small overshoot.
- `Rise` — Each letter rises into place while fading in.

### `RavenTypewriter`

Reveals a TextMeshPro text letter by letter, with an optional typing sound: dialogue, tutorials, intros. Call Skip() to show everything at once (a "skip" button).

- `void Play()` — Types the current text from the first letter.
- `void Play(string text)` — Replaces the text, then types it.
- `void Skip()` — Shows the whole text at once and raises On Complete.

### `TextMeshProTweens`

TextMeshPro tweens. Color and alpha already work through the Graphic overloads (TMP_Text derives from Graphic); this adds text-specific properties. All setters are non-capturing, so these tweens allocate nothing.

- `static Tween TweenTypewriter(this TMP_Text target, float duration)` — Reveals the characters one by one. The character count is read every frame, so the text can change (or the object can still be inactive) when the tween is created; at the end every character is visible, whatever the text has become.
- `static Tween TweenMaxVisibleCharacters(this TMP_Text target, int from, int to, float duration)` — Tweens maxVisibleCharacters between two explicit counts.
- `static Tween TweenFontSize(this TMP_Text target, float to, float duration)` — Tweens the font size from the size it has when the tween starts.
- `static Tween TweenCharacterSpacing(this TMP_Text target, float to, float duration)` — Tweens character spacing from the spacing it has when the tween starts.
- `static Tween TweenNumber(this TMP_Text target, float from, float to, float duration)` — Counts a whole number up or down using TMP's allocation-free SetText formatter.

---

## Physics

### `PhysicsTweens`

Rigidbody tweens. They move the body through MovePosition / MoveRotation, so collisions and interpolation keep working, and run in FixedUpdate by default. Compiled only when the Physics module is enabled.

- `static Tween TweenMovePosition(this Rigidbody target, Vector3 to, float duration)` — Moves a Rigidbody to `to` (world space) with MovePosition, in FixedUpdate.
- `static Tween TweenMoveRotation(this Rigidbody target, Quaternion to, float duration)` — Turns a Rigidbody to `to` (world space) with MoveRotation, in FixedUpdate.
- `static Tween TweenMovePositionAtSpeed(this Rigidbody target, Vector3 to, float speed)` — Moves a Rigidbody at `speed` units per second.

---

## Physics 2D

### `Physics2DTweens`

Rigidbody2D tweens through MovePosition / MoveRotation, run in FixedUpdate by default. Compiled only when the Physics 2D module is enabled.

- `static Tween TweenMovePosition(this Rigidbody2D target, Vector2 to, float duration)` — Moves a Rigidbody2D to `to` with MovePosition, in FixedUpdate.
- `static Tween TweenMoveRotation(this Rigidbody2D target, float angle, float duration)` — Turns a Rigidbody2D to `angle` degrees with MoveRotation, in FixedUpdate.
- `static Tween TweenMovePositionAtSpeed(this Rigidbody2D target, Vector2 to, float speed)` — Moves a Rigidbody2D at `speed` units per second.

---

## Animation Rigging

### `AnimationRiggingTweens`

Tweens for Unity's Animation Rigging package. Compiled only when the package is installed. Constraint and rig weights blend procedural poses in and out over time; reach and release drive a two-bone IK target. All setters are non-capturing, so these tweens allocate nothing.

- `static Tween TweenWeight<T>(this T constraint, float to, float duration)` — Blends any rig constraint (TwoBoneIK, MultiAim, Damped Transform…) from the weight it has when the tween starts.
- `static Tween TweenWeight(this Rig rig, float to, float duration)` — Blends a whole Rig layer from its current weight.
- `static Sequence TweenReach(this TwoBoneIKConstraint ik, Vector3 worldPosition, float duration)` — Moves the IK target to `worldPosition` while blending the constraint to full weight — a hand reaching for a door handle, a foot planting on a step.
- `static Tween TweenRelease(this TwoBoneIKConstraint ik, float duration)` — Blends the IK constraint back to the animation (weight 0).

---

## UI Toolkit

### `UIToolkitTweens`

Tweens for UI Toolkit elements. Each one starts from the element's current resolved style and writes the inline style, so it also works on elements styled by USS. The setters capture nothing, so these tweens allocate nothing while they play.

- `static Tween TweenTranslate(this VisualElement target, Vector2 to, float duration)` — Moves the element by its translate style, in pixels (layout is not affected).
- `static Tween TweenRotate(this VisualElement target, float degrees, float duration)` — Rotates the element around its transform origin, in degrees.
- `static Tween TweenColor(this VisualElement target, Color to, float duration)` — Text color of the element (and of the children that inherit it).
- `static Tween TweenWidth(this VisualElement target, float to, float duration)` — Width in pixels (affects layout).
- `static Tween TweenHeight(this VisualElement target, float to, float duration)` — Height in pixels (affects layout).

---

## Timeline

### `RavenMotionTrack`

A Timeline track that plays ready-made motions on the bound object: drop "Pop In", "Tada" or a Motion Style on a cutscene's timeline, stretch it, scrub it. Every frame is exact (the motion is sampled at the playhead), and previewing in the editor never leaves a change in the scene.


### `RavenMotionClip`

One motion on a Raven Motion Track. Its length defaults to the motion's own timing.

- `override double duration` — One play (or one loop cycle); stretch the clip to repeat a loop or an attention motion.

### `RavenMotionMixer`

Drives a Raven Motion Track: at every frame it shows the clip under the playhead, the end of the last clip already passed, or, before the first clip, the object as designed. The designed pose is read once for the whole track, before any clip moves the object.


---

## Integrations

### `CinemachineFeedback`

Serves the Camera Impulse action of Raven Feedback: the impulse source fires with the action's strength, and every Cinemachine camera with an Impulse Listener reacts (works with Cinemachine 2 and 3).


### `RavenRumble`

Gamepad vibration for a moment: `RavenRumble.Play(0.6f, 0.3f, 0.2f)`. Overlapping rumbles use the strongest motors and stop when the last one ends. Timed in real time, so a hit stop does not stretch it. Also serves the Rumble action of Raven Feedback.

- `static void Play(float lowFrequency, float highFrequency, float duration)` — Vibrates the current gamepad, if any, for `duration` real seconds.
- `static void Stop()` — Stops any vibration at once.

### `VolumeTweens`

Post-processing for URP and HDRP: `volume.TweenWeight(1f, 0.3f)` fades a Volume's whole effect (a red vignette when hit, a blur on pause). Also serves the Volume Flash action of Raven Feedback.

- `static Tween FlashWeight(this Volume target, float peak, float duration)` — Raises the weight to `peak` and brings it back, in `duration` seconds.

### `UniTaskTweens`

UniTask support: `await tween.ToUniTask(token)`. It ends when the tween ends (completed or stopped); cancelling the token stops the tween and throws OperationCanceledException. UniTask pools its async state machines, so awaiting does not allocate once warmed up. The token is attached with WithCancellation, which replaces a token set earlier and only applies to root tweens and sequences (a tween inside a sequence follows its sequence).


---

## DOTween adapter

### `LoopType`

DOTween's loop types, for `SetLoops(loops, LoopType)`.

- `Restart` — Same as `CycleMode.Restart`.
- `Yoyo` — Same as `CycleMode.Yoyo`.
- `Incremental` — Same as `CycleMode.Incremental`.

### `UpdateType`

DOTween's update types, for `SetUpdate(UpdateType)`.

- `Normal` — `UpdatePhase.Update`.
- `Late` — `UpdatePhase.LateUpdate`.
- `Fixed` — `UpdatePhase.FixedUpdate`.
- `Manual` — `UpdatePhase.Manual`, advanced by `DOTween.ManualUpdate`.

### `DOTween`

DOTween's static entry points.

- `static void ManualUpdate(float deltaTime, float unscaledDeltaTime)` — Advances the tweens set to `UpdateType.Manual`.

### `DOVirtual`

DOTween's DOVirtual helpers.

- `static Tween DelayedCall(float delay, TweenCallback callback, bool ignoreTimeScale = true)` — Like DOTween, ignores Time.timeScale unless `ignoreTimeScale` is false.

### `DOTweenShortcuts`

DOTween's shortcut extensions (DOMove, DOFade, …), running on RavenTween.

- `static Tween DORotate(this Transform target, Vector3 endValue, float duration)` — Shortest-path rotation, like DOTween's default RotateMode.Fast.
- `static int DOKill(this Component target, bool complete = false)` — Stops (or completes) every tween on this component. Returns how many.
- `static Tween DOColor(this Material target, Color endValue, float duration)` — Tweens the shader's main color, like Material.color: _BaseColor on URP and HDRP Lit, _Color on built-in shaders.

### `DOTweenSettings`

DOTween's tween settings and control methods (SetEase, SetLoops, Kill, …).

- `static Tween SetEase(this Tween tween, Ease ease, float overshoot)` — OutBack / InBack with DOTween's overshoot (1.70158 = default).
- `static void Goto(this Tween tween, float to, bool andPlay = false)` — Jumps to `to` seconds; pauses the tween unless `andPlay`.

---

#### ◀ **[Reference](Reference)**  ·  Next: **[Engine Internals ▶](Engine-Internals)**
