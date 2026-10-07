# Reference

The complete public API, in tables. Everything lives in the `RavenTween` namespace.

---

## `Raven` — factories and global settings

### Values and timers

| Member | Returns |
| :--- | :--- |
| `Value(float from, float to, float duration)` — also `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color` | `Tween` |
| `Delay(float seconds)` | `Tween` |
| `Delay(float seconds, Action onComplete)` | `Tween` |
| `Custom<T>(T target, float from, float to, float duration, Action<T, float> setter)` — also `Vector2`, `Vector3`, `Color` | `Tween` |
| `Sequence()` | `Sequence` |

### Property tweens

| Member | Target |
| :--- | :--- |
| `Position`, `LocalPosition(Transform, Vector3, float)` | Transform |
| `Rotation`, `LocalRotation(Transform, Quaternion, float)` | Transform |
| `EulerAngles`, `LocalEulerAngles(Transform, Vector3, float)` | Transform |
| `Scale(Transform, Vector3 \| float, float)` | Transform (local scale) |
| `AnchoredPosition`, `SizeDelta(RectTransform, Vector2, float)` | RectTransform |
| `Alpha(CanvasGroup, float, float)` | CanvasGroup |
| `Color(Graphic, Color, float)`, `Alpha(Graphic, float, float)` | Image, Text, TMP_Text… |
| `FieldOfView`, `OrthographicSize(Camera, float, float)` | Camera |
| `BackgroundColor(Camera, Color, float)` | Camera |
| `Volume`, `Pitch(AudioSource, float, float)` | AudioSource |
| `MaterialFloat(Material, int id \| string name, float, float)` | Material |
| `MaterialColor(Material, int id \| string name, Color, float)` | Material |
| `Color(SpriteRenderer, Color, float)` | SpriteRenderer |
| `Intensity(Light, float, float)`, `Color(Light, Color, float)` | Light |

### Effects

| Member | Defaults |
| :--- | :--- |
| `ShakePosition`, `ShakeRotation`, `ShakeScale(Transform, Vector3 strength, float duration, float frequency)` | frequency `12` |
| `PunchPosition`, `PunchRotation`, `PunchScale(Transform, Vector3 punch, float duration, float frequency)` | frequency `5` |

### Global

| Member | Description |
| :--- | :--- |
| `TimeScale` | Multiplier for all tweens, independent of `Time.timeScale`. Default `1`. |
| `UpdatePhase` | `UpdatePhase.Update` (default) or `UpdatePhase.LateUpdate`. |
| `AliveCount` | Live tweens and sequences, paused ones included. |
| `StopAll()` | Stop everything where it is. |
| `CompleteAll()` | Jump everything to its end. |

---

## `Tween` — handle

| Member | Description |
| :--- | :--- |
| `Ease(Ease)`, `Ease(AnimationCurve)`, `Ease(Func<float, float>)` | Easing. |
| `From(value)` | Explicit start value (`float`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`). |
| `Delay(float)` | Start delay in seconds. |
| `Cycles(int count, CycleMode mode = Restart)` | Repeat; `-1` = infinite. |
| `Infinite(CycleMode mode = Yoyo)` | Loop forever. |
| `UnscaledTime(bool = true)` | Ignore `Time.timeScale`. |
| `OnStart`, `OnUpdate`, `OnComplete`, `OnKill`, `OnTargetDestroyed` | Callbacks. `OnUpdate` also takes `Action<float>` and `Action<TweenValue>`. |
| `Start()`, `Pause()`, `Resume()`, `Stop()`, `Complete()` | Control. |
| `IsAlive`, `IsPaused` | State. |
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
| `Cycles(int, CycleMode = Restart)`, `Infinite(CycleMode = Restart)` | Repeat the whole timeline. |
| `Delay(float)`, `UnscaledTime(bool = true)` | Timing. |
| `OnStart`, `OnComplete`, `OnKill` | Callbacks. |
| `Start()`, `Pause()`, `Resume()`, `Stop()`, `Complete()` | Control. |
| `IsAlive`, `Duration` | State; `Duration` is one cycle, in seconds. |
| `GetAwaiter()`, `ToCompletion()`, `ToYieldInstruction()` | Waiting. |

---

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
| `CycleMode` | `Restart`, `Yoyo`. |
| `UpdatePhase` | `Update`, `LateUpdate`. |
| `TweenValue` | Read-only value passed to `OnUpdate(Action<TweenValue>)`: `.Float`, `.Vector2`, `.Vector3`, `.Vector4`, `.Quaternion`, `.Color`, `.Kind`. |
| `TweenParams` | Serializable settings: `duration`, `startDelay`, `ease`, `customCurve`, `cycles`, `cycleMode`, `useUnscaledTime`; `ApplyTo(Tween)`; `TweenParams.Default`. |
| `PropertyKind` | Properties a `TweenTemplate` can animate. |

---

## Components and assets

| Type | Members |
| :--- | :--- |
| `TweenTemplate` (ScriptableObject) | `property`, `materialProperty`, `endValue`, `useExplicitFrom`, `fromValue`, `settings`; `Play(Object target)` |
| `RavenAnimator` | `Entries`, `OnAllComplete`; `Play()`, `Stop()`, `CompleteNow()` |
| `RavenSequencePlayer` | `Steps`, `OnComplete`, `Current`; `Play()`, `Stop()`, `CompleteNow()` |

---

## Extension methods

Every property tween has an extension form on its target type: `TweenPosition`, `TweenLocalPosition`, `TweenRotation`, `TweenLocalRotation`, `TweenEulerAngles`, `TweenLocalEulerAngles`, `TweenScale` (Transform); `TweenAnchoredPosition`, `TweenSizeDelta` (RectTransform); `TweenAlpha` (CanvasGroup, Graphic); `TweenColor` (Graphic, SpriteRenderer, Light, Material); `TweenFloat` (Material); `TweenFieldOfView`, `TweenOrthographicSize`, `TweenBackgroundColor` (Camera); `TweenVolume`, `TweenPitch` (AudioSource); `TweenIntensity` (Light). Effects too: `transform.ShakePosition(...)`, `transform.PunchScale(...)`, and so on.

---

## Editor

| Menu | Opens |
| :--- | :--- |
| **Tools ▸ RavenTween ▸ Monitor** | The live **[Monitor](Monitor)** window. |
| **Tools ▸ RavenTween ▸ Documentation** | The repository on GitHub. |
| **Tools ▸ RavenTween ▸ Updates ▸ Check Now** | Checks for a newer release immediately. |
| **Tools ▸ RavenTween ▸ Updates ▸ Check Automatically** | Daily check on or off (on by default). |
| **Tools ▸ RavenTween ▸ Updates ▸ Install Automatically** | Install new releases without asking (off by default). |
| **Create ▸ RavenTween ▸ Tween Template** | A new template asset. |
| **Add Component ▸ RavenTween** | Raven Animator, Raven Sequence Player, Raven Look At, Raven Spring Chain. |

---

#### ◀ **[Migrating](Migrating)**  ·  Next: **[FAQ ▶](FAQ)**
