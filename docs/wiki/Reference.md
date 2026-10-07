# Reference

The complete public API, in tables. Everything lives in the `RavenTween` namespace, except the optional DOTween adapter (`RavenTween.DOTweenAdapter`).

---

## `Raven` — factories and global settings

### Values and timers

| Member | Returns |
| :--- | :--- |
| `Value(float from, float to, float duration)` — also `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color` | `Tween` |
| `Delay(float seconds)` | `Tween` |
| `Delay(float seconds, Action onComplete)` | `Tween` |
| `Custom<T>(T target, float from, float to, float duration, Action<T, float> setter)` — also `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Rect` | `Tween` |
| `CustomTo<T>(T target, Func<T, float> getter, float to, float duration, Action<T, float> setter)` — also `Vector2`, `Vector3`, `Quaternion`, `Color`; reads the start value when the tween starts | `Tween` |
| `Sequence()` | `Sequence` |

### Property tweens

| Member | Target |
| :--- | :--- |
| `Position`, `LocalPosition(Transform, Vector3, float)` | Transform |
| `PositionX/Y/Z`, `LocalPositionX/Y/Z(Transform, float, float)` | Transform, one axis |
| `Rotation`, `LocalRotation(Transform, Quaternion, float)` | Transform |
| `EulerAngles`, `LocalEulerAngles(Transform, Vector3, float)` | Transform |
| `Scale(Transform, Vector3 \| float, float)` | Transform (local scale) |
| `ScaleX/Y/Z(Transform, float, float)` | Transform (local scale), one axis |
| `PositionAtSpeed`, `LocalPositionAtSpeed(Transform, Vector3, float speed)` | Transform; duration = distance / speed |
| `RotationAtSpeed`, `LocalRotationAtSpeed(Transform, Quaternion, float degreesPerSecond)` | Transform |
| `AnchoredPosition`, `SizeDelta(RectTransform, Vector2, float)` | RectTransform |
| `AnchoredPositionX/Y(RectTransform, float, float)`, `AnchoredPositionAtSpeed(RectTransform, Vector2, float speed)` | RectTransform |
| `Alpha(CanvasGroup, float, float)` | CanvasGroup |
| `Color(Graphic, Color, float)`, `Alpha(Graphic, float, float)` | Image, Text, TMP_Text… |
| `FieldOfView`, `OrthographicSize(Camera, float, float)` | Camera |
| `BackgroundColor(Camera, Color, float)` | Camera |
| `Volume`, `Pitch(AudioSource, float, float)` | AudioSource |
| `MaterialFloat(Material, int id \| string name, float, float)` | Material |
| `MaterialColor(Material, int id \| string name, Color, float)` | Material |
| `PropertyBlockFloat(Renderer, int id \| string name, float, float)` | One renderer, through a MaterialPropertyBlock |
| `PropertyBlockColor(Renderer, int id \| string name, Color, float)` | One renderer, through a MaterialPropertyBlock |
| `Color(SpriteRenderer, Color, float)`, `Alpha(SpriteRenderer, float, float)` | SpriteRenderer |
| `Intensity(Light, float, float)`, `Color(Light, Color, float)` | Light |
| Overloads taking a `TweenSettings<T>` instead of `(to, duration)` | `Position`, `LocalPosition`, `EulerAngles`, `LocalEulerAngles`, `Scale`, `AnchoredPosition`, `SizeDelta`, `Alpha`, `Color` |

### Effects

| Member | Defaults |
| :--- | :--- |
| `ShakePosition`, `ShakeRotation`, `ShakeScale(Transform, Vector3 strength, float duration, float frequency)` | frequency `12` |
| `PunchPosition`, `PunchRotation`, `PunchScale(Transform, Vector3 punch, float duration, float frequency)` | frequency `5` |
| `ShakeCamera(Camera, float strength = 1, float duration = 0.5, float frequency = 15)` | Position jitter + tilt |

### Time

| Member | Description |
| :--- | :--- |
| `TimeScale` | Multiplier for all tweens, independent of `Time.timeScale`. Default `1`. |
| `UpdatePhase` | Default phase: `Update` (default), `LateUpdate` or `FixedUpdate`. |
| `GlobalTimeScale(float to, float duration)` | Tweens `Time.timeScale`, in unscaled time. |
| `TweenTimeScale(Tween \| Sequence, float to, float duration)` | Tweens the `TimeScale` of another tween or sequence. |

### Global control

| Member | Description |
| :--- | :--- |
| `AliveCount` | Live tweens and sequences, paused ones included. |
| `StopAll()`, `CompleteAll()` | Stop or complete everything. |
| `StopAll(object target)`, `CompleteAll(object target)` | Only the tweens animating `target` (component, material, custom target, or every component of a GameObject). Return the count. |
| `PauseAll(object target = null)`, `ResumeAll(object target = null)` | Pause or resume, for one target or everything. Return the count. |
| `CountTweens(object target)` | Live tweens animating `target`. |
| `SetCapacity(int)` | Pre-create pooled slots. |

---

## `Tween` — handle

| Member | Description |
| :--- | :--- |
| `Ease(Ease)`, `Ease(Easing)`, `Ease(AnimationCurve)`, `Ease(Func<float, float>)` | Easing. |
| `From(value)` | Explicit start value (`float`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`). |
| `Delay(float)` | Start delay in seconds. |
| `Cycles(int count, CycleMode mode = Restart)` | Repeat; `-1` = infinite. |
| `Infinite(CycleMode mode = Yoyo)` | Loop forever. |
| `SetRemainingCycles(int)`, `SetRemainingCycles(bool stopAtEndValue)` | Change the remaining cycles while playing. |
| `UnscaledTime(bool = true)` | Ignore `Time.timeScale`. |
| `UpdateIn(UpdatePhase)` | Run in `Update`, `LateUpdate` or `FixedUpdate`. |
| `WithCancellation(CancellationToken)` | Stop when the token is cancelled. |
| `OnStart`, `OnUpdate`, `OnComplete`, `OnKill`, `OnTargetDestroyed` | Callbacks. `OnUpdate` also takes `Action<float>` and `Action<TweenValue>`. |
| `OnComplete<T>(T target, Action<T>)`, `OnUpdate<T>(T target, Action<T, Tween>)` | Allocation-free callbacks. |
| `Start()`, `Pause()`, `Resume()`, `Stop()`, `Complete()` | Control. |
| `IsAlive`, `IsPaused` | State. |
| `Duration`, `DurationTotal`, `CyclesDone`, `CyclesTotal`, `InterpolationFactor` | Timing (read-only). |
| `ElapsedTime`, `ElapsedTimeTotal`, `Progress`, `ProgressTotal`, `TimeScale` | Timing (settable: jumps or changes speed). |
| `GetAwaiter()`, `ToCompletion()` | `await` support. |
| `ToYieldInstruction()` | Coroutine support. |

---

## `Sequence` — handle

| Member | Description |
| :--- | :--- |
| `Chain(Tween \| Sequence)` | Append after everything so far. |
| `Group(Tween \| Sequence)` | Play alongside the previous item. |
| `Insert(float time, Tween \| Sequence)` | Place at an absolute time. |
| `ChainDelay(float)` | Append an empty gap. |
| `ChainCallback(Action)`, `InsertCallback(float, Action)` | Run code at a point of the timeline. Target-based forms `(target, Action<T>)` allocate nothing. |
| `Cycles(int, CycleMode = Restart)`, `Infinite(CycleMode = Restart)` | Repeat the whole timeline (`Restart` or `Yoyo`). |
| `SetRemainingCycles(int)`, `SetRemainingCycles(bool stopAtEnd)` | Change the remaining cycles while playing. |
| `Delay(float)`, `UnscaledTime(bool = true)`, `UpdateIn(UpdatePhase)`, `WithCancellation(CancellationToken)` | Timing. |
| `OnStart`, `OnComplete`, `OnKill`, `OnComplete<T>(T, Action<T>)` | Callbacks. |
| `Start()`, `Pause()`, `Resume()`, `Stop()`, `Complete()` | Control. |
| `IsAlive`, `IsPaused`, `Duration`, `DurationTotal`, `CyclesDone`, `CyclesTotal` | State; `Duration` is one cycle, in seconds. |
| `ElapsedTime`, `ElapsedTimeTotal`, `Progress`, `ProgressTotal`, `TimeScale` | Settable timing. |
| `GetAwaiter()`, `ToCompletion()`, `ToYieldInstruction()` | Waiting. |

---

## Physics extensions

Compiled when the Physics / Physics 2D module is enabled (the default). They run in `FixedUpdate`.

| Member | Description |
| :--- | :--- |
| `TweenMovePosition(Vector3, float)`, `TweenMoveRotation(Quaternion, float)`, `TweenMovePositionAtSpeed(Vector3, float)` on `Rigidbody` | Through `MovePosition` / `MoveRotation`. |
| `TweenMovePosition(Vector2, float)`, `TweenMoveRotation(float angle, float)`, `TweenMovePositionAtSpeed(Vector2, float)` on `Rigidbody2D` | Same for 2D. |

## TextMeshPro extensions (`TMP_Text`)

| Member | Description |
| :--- | :--- |
| `TweenTypewriter(float duration)` | Reveal every character. |
| `TweenMaxVisibleCharacters(int from, int to, float duration)` | Reveal between two counts. |
| `TweenNumber(float from, float to, float duration)` | Count a whole number. |
| `TweenFontSize(float to, float duration)` | Font size from the current value. |
| `TweenCharacterSpacing(float to, float duration)` | Spacing from the current value. |

---

## Procedural components

| Type | Members |
| :--- | :--- |
| `RavenLookAt` | `Target`, `Weight`, `MaxAngle`, `SmoothTime`; `ResetSmoothing()`; extensions `TweenWeight(to, duration)`, `LookAtTarget(target, blendDuration)` |
| `RavenSpringChain` | `Weight`, `Stiffness`, `Damping`, `Gravity`, `BoneCount`; `Build()`, `ResetPhysics()`; extension `TweenWeight(to, duration)` |

## Animation Rigging extensions

Compiled only when `com.unity.animation.rigging` is installed.

| Member | Description |
| :--- | :--- |
| `TweenWeight(float to, float duration)` on any `IRigConstraint` | Blend a constraint's weight. |
| `TweenWeight(float to, float duration)` on `Rig` | Blend a rig layer's weight. |
| `TweenReach(Vector3 worldPosition, float duration)` on `TwoBoneIKConstraint` | Move the IK target and blend to full weight; returns a `Sequence`. |
| `TweenRelease(float duration)` on `TwoBoneIKConstraint` | Blend the IK back to 0. |

---

## Types

| Type | Description |
| :--- | :--- |
| `Ease` | `Linear`, `In/Out/InOut` × `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`, `Back`, `Elastic`, `Bounce`, and `Custom`. |
| `Easing` | Parametric ease: `Easing.Of(Ease)`, `FromCurve(curve)`, `Overshoot(strength)`, `Bounce(strength)`, `BounceExact(amplitude)`, `Elastic(strength, period)`; `Evaluate(t)`. An `Ease` converts to it implicitly. |
| `CycleMode` | `Restart`, `Yoyo`, `Incremental`, `PingPong`. |
| `UpdatePhase` | `Update`, `LateUpdate`, `FixedUpdate`. |
| `TweenValue` | Read-only value passed to `OnUpdate(Action<TweenValue>)`: `.Float`, `.Vector2`, `.Vector3`, `.Vector4`, `.Quaternion`, `.Color`, `.Kind`. |
| `TweenParams` | Serializable settings: `duration`, `startDelay`, `ease`, `customCurve`, `cycles`, `cycleMode`, `useUnscaledTime`; `ApplyTo(Tween)` applies everything except `duration`, which you pass to the factory; `TweenParams.Default`. |
| `TweenSettings<T>` | Serializable whole tween: `useStartValue`, `startValue`, `endValue`, `settings` (a `TweenParams`). |
| `PropertyKind` | Properties a `TweenTemplate` can animate. |

---

## Components and assets

| Type | Members |
| :--- | :--- |
| `TweenTemplate` (ScriptableObject) | `property`, `materialProperty`, `endValue`, `useExplicitFrom`, `fromValue`, `settings`; `Play(Object target)` — the target may be the exact object or any GameObject / component carrying it |
| `RavenAnimator` | `Entries`, `OnAllComplete`; `Play()`, `Stop()`, `CompleteNow()` |
| `RavenSequencePlayer` | `Steps`, `OnComplete`, `Current`; `Play()`, `Stop()`, `CompleteNow()` |

---

## Extension methods

Every property tween has an extension form on its target type: `TweenPosition`, `TweenLocalPosition`, `TweenPositionX/Y/Z`, `TweenLocalPositionX/Y/Z`, `TweenRotation`, `TweenLocalRotation`, `TweenEulerAngles`, `TweenLocalEulerAngles`, `TweenScale`, `TweenScaleX/Y/Z`, `TweenPositionAtSpeed`, `TweenLocalPositionAtSpeed`, `TweenRotationAtSpeed`, `TweenLocalRotationAtSpeed` (Transform); `TweenAnchoredPosition`, `TweenAnchoredPositionX/Y`, `TweenAnchoredPositionAtSpeed`, `TweenSizeDelta` (RectTransform); `TweenAlpha` (CanvasGroup, Graphic, SpriteRenderer); `TweenColor` (Graphic, SpriteRenderer, Light, Material); `TweenFloat` (Material); `TweenPropertyBlockFloat`, `TweenPropertyBlockColor` (Renderer); `TweenFieldOfView`, `TweenOrthographicSize`, `TweenBackgroundColor`, `Shake` (Camera); `TweenVolume`, `TweenPitch` (AudioSource); `TweenIntensity` (Light). Effects too: `transform.ShakePosition(...)`, `transform.PunchScale(...)`, and so on.

---

## DOTween adapter (`RavenTween.DOTweenAdapter`)

DOTween names running on RavenTween, for migrating code: `DOMove`, `DOMoveX/Y/Z`, `DOLocalMove(X/Y/Z)`, `DORotate`, `DOLocalRotate`, `DORotateQuaternion`, `DOScale(X/Y/Z)`, `DOPunch…`, `DOShake…`, `DOKill`, `DOComplete`, `DOPause`, `DOPlay` (Transform / Component); `DOAnchorPos(X/Y)`, `DOSizeDelta`, `DOFade`, `DOColor` (UI, sprites, materials, camera, audio, lights); `SetEase`, `SetLoops`, `SetDelay`, `SetUpdate`, `Kill`, `Play`, `Goto`, `IsActive`, `IsPlaying`, `Elapsed`, `ElapsedPercentage`, `WaitForCompletion`, `AsyncWaitForCompletion`; `Append`, `Join`, `AppendInterval`, `AppendCallback`, `InsertCallback` on sequences; `DOTween.Sequence/To/KillAll/Kill/CompleteAll/PauseAll/PlayAll/timeScale`; `DOVirtual.Float/Vector3/Color/DelayedCall`; `LoopType`, `UpdateType`. See **[Migrating](Migrating)**.

---

## Editor

| Menu | Opens |
| :--- | :--- |
| **Tools ▸ RavenTween ▸ Monitor** | The live **[Monitor](Monitor)** window. |
| **Tools ▸ RavenTween ▸ Sequence Editor** | The visual timeline for a Raven Sequence Player. |
| **Tools ▸ RavenTween ▸ Documentation** | This manual. |
| **Tools ▸ RavenTween ▸ Updates ▸ Check Now** | Checks for a newer release immediately. |
| **Tools ▸ RavenTween ▸ Updates ▸ Check Automatically** | Daily check on or off (on by default). |
| **Tools ▸ RavenTween ▸ Updates ▸ Install Automatically** | Install new releases without asking (off by default). |
| **Create ▸ RavenTween ▸ Tween Template** | A new template asset. |
| **Add Component ▸ RavenTween** | Raven Animator, Raven Sequence Player, Raven Look At, Raven Spring Chain. |

---

#### ◀ **[Migrating](Migrating)**  ·  Next: **[FAQ ▶](FAQ)**
