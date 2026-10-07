# Tweens and Easing

A tween animates one property of one target from its current value to an end value over a duration. This page lists every built-in target and every option you can chain on a tween.

---

## Built-in targets

Each method has the shape `Raven.Method(target, endValue, duration)` and returns a `Tween`. The start value is read from the target when the tween starts (after any delay).

| Target | Methods | Value |
| :--- | :--- | :--- |
| **Transform** | `Position`, `LocalPosition` | `Vector3` |
| | `Rotation`, `LocalRotation` | `Quaternion` (spherical interpolation) |
| | `EulerAngles`, `LocalEulerAngles` | `Vector3`, interpolated per axis |
| | `Scale` | `Vector3`, or a `float` for uniform scale |
| **RectTransform** | `AnchoredPosition`, `SizeDelta` | `Vector2` |
| **CanvasGroup** | `Alpha` | `float` |
| **Graphic** (Image, Text, TMP…) | `Color`, `Alpha` | `Color`, `float` |
| **Camera** | `FieldOfView`, `OrthographicSize` | `float` |
| | `BackgroundColor` | `Color` |
| **AudioSource** | `Volume` (clamped to 0–1), `Pitch` | `float` |
| **Material** | `MaterialFloat`, `MaterialColor` | by property ID or name |
| **SpriteRenderer** | `Color`, `Alpha` | `Color`, `float` |
| **Renderer** | `PropertyBlockFloat`, `PropertyBlockColor` | by property ID or name, this renderer only |
| **Light** | `Intensity`, `Color` | `float`, `Color` |

Every method also exists as an extension: `transform.TweenPosition(...)`, `canvasGroup.TweenAlpha(...)`, `material.TweenColor(...)`, `light.TweenIntensity(...)`.

### One axis at a time

Animate a single axis and leave the others free — for another tween, for physics, or for your own script:

| Target | Methods |
| :--- | :--- |
| **Transform** | `PositionX`, `PositionY`, `PositionZ`, `LocalPositionX/Y/Z`, `ScaleX`, `ScaleY`, `ScaleZ` |
| **RectTransform** | `AnchoredPositionX`, `AnchoredPositionY` |

```csharp
Raven.PositionY(coin, coin.position.y + 1f, 0.4f).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo); // hop
Raven.LocalPositionX(door, 2f, 1f);   // slides sideways while something else moves it up
```

Two axis tweens on the same object combine: one can move X while the other moves Y.

### At a speed

When the distance varies, give a speed instead of a duration. The duration is computed from the distance **when you create the tween**.

```csharp
Raven.PositionAtSpeed(enemy, waypoint, 3f);               // 3 units per second
Raven.LocalRotationAtSpeed(turret, aim, 90f);             // 90 degrees per second
Raven.AnchoredPositionAtSpeed(cursor, slot, 800f);        // 800 UI units per second
```

### Per-renderer shader values

`Raven.PropertyBlockFloat` and `Raven.PropertyBlockColor` animate a shader property on **one renderer** through a `MaterialPropertyBlock`. Unlike `renderer.material`, nothing is copied: other renderers sharing the material are untouched, batching stays intact, and nothing is allocated.

```csharp
static readonly int Flash = Shader.PropertyToID("_EmissionColor");
Raven.PropertyBlockColor(enemyRenderer, Flash, Color.white, 0.08f).Cycles(2, CycleMode.Yoyo); // hit flash
```

The start value comes from the renderer's block if it holds the property, otherwise from its shared material.

### Rigidbodies

With the Physics or Physics 2D module enabled (the default), `Rigidbody` and `Rigidbody2D` get extension methods that move them **through the physics engine** — `MovePosition` and `MoveRotation` — so collisions and interpolation keep working. They run in `FixedUpdate` by default.

```csharp
platform.TweenMovePosition(endPoint, 2f).Ease(Ease.InOutSine).Infinite();
crate2D.TweenMoveRotation(90f, 0.5f);
body.TweenMovePositionAtSpeed(target, 4f);
```

> [!TIP]
> Use a **kinematic** body for platforms and doors driven by tweens: a dynamic body also reacts to forces, so it would drift from the path.

> [!NOTE]
> Euler tweens start from the angles Unity reports, which are always between 0 and 360. A bone at −10° reads as 350°, so tweening it to `0` turns 350° the long way round. Use `Rotation` / `LocalRotation` for the shortest path, or set the start explicitly with `.From(new Vector3(0f, 0f, -10f))`. Euler tweens are the right tool for spins of more than 180°, like `new Vector3(0f, 0f, 360f)`.

> [!TIP]
> For materials, resolve the property ID once and reuse it: `static readonly int Tint = Shader.PropertyToID("_BaseColor");`. The name overloads call `PropertyToID` for you each time a tween is created.

> [!WARNING]
> Tweening `renderer.material` creates a material instance — that is Unity's behavior, not RavenTween's. Tween `sharedMaterial` only if you mean to change every object using it.

---

## Plain values

`Raven.Value` tweens a number or vector that isn't attached to any object. Read the result in `OnUpdate`.

```csharp
Raven.Value(0f, 100f, 2f).OnUpdate(v => score = Mathf.RoundToInt(v));
Raven.Value(Color.black, Color.white, 1f).OnUpdate((TweenValue v) => tint = v.Color);
```

Overloads exist for `float`, `Vector2`, `Vector3`, `Vector4`, `Quaternion` and `Color`.

---

## Options

Chain any of these after the factory call. They must be set before the tween starts, so set them on the same line.

| Option | Effect |
| :--- | :--- |
| `.Ease(Ease.OutQuad)` | Easing curve — see below. |
| `.Ease(Easing.Overshoot(2f))` | A tunable ease — see *Parametric eases* below. |
| `.Ease(animationCurve)` | Use any `AnimationCurve` (time and value in 0–1). |
| `.Ease(t => …)` | Use your own function. |
| `.From(value)` | Start from this value instead of the current one. |
| `.Delay(seconds)` | Wait before starting. `OnStart` fires when the delay ends. |
| `.Cycles(n)` | Play `n` times. |
| `.Cycles(n, CycleMode.Yoyo)` | Repeat with a cycle mode — see below. |
| `.Infinite()` | Loop forever. Defaults to `CycleMode.Yoyo` for tweens. |
| `.UnscaledTime()` | Ignore `Time.timeScale` (pause menus). |
| `.UpdateIn(UpdatePhase.FixedUpdate)` | Run at the physics rate, or in `LateUpdate`. See **[Control and Lifecycle](Control-and-Lifecycle)**. |

```csharp
// Fade in from fully transparent, wait 0.2 s, ease out.
Raven.Alpha(canvasGroup, 1f, 0.4f).From(0f).Delay(0.2f).Ease(Ease.OutCubic);

// A breathing glow, forever.
Raven.Intensity(lamp, 3f, 1.2f).Ease(Ease.InOutSine).Infinite();
```

### Cycle modes

| Mode | Each new cycle… | Use it for |
| :--- | :--- | :--- |
| `Restart` | jumps back to the start value. | Spinners, scrolling textures. |
| `Yoyo` | plays the previous one **backwards in time**, so the way back mirrors the ease (an `OutQuad` rise falls like an `InQuad`). | Pulses, hops, breathing. |
| `PingPong` | goes from the end back to the start with the **same ease** (an `OutQuad` rise also falls like an `OutQuad`). | Swings that should feel the same both ways. |
| `Incremental` | continues from where the last one ended, adding the same change again. | Steps: a clock hand, a conveyor, a staircase. |

With `Yoyo` or `PingPong` and an even number of cycles, the tween ends where it started — perfect for pulses: `.Cycles(2, CycleMode.Yoyo)`.

```csharp
Raven.LocalEulerAngles(hand, new Vector3(0f, 0f, -6f), 0.15f).Cycles(-1, CycleMode.Incremental); // ticks forever
```

Sequences cycle with `Restart` or `Yoyo`; `PingPong` plays as `Yoyo` and `Incremental` as `Restart` there.

---

## Inspector-tuned tweens

`TweenSettings<T>` holds a whole tween in one serialized field — end value, optional start value, duration, ease and cycles — so designers can tune it in the Inspector:

```csharp
[SerializeField] TweenSettings<Vector3> slideIn = new TweenSettings<Vector3>(Vector3.zero, 0.4f, Ease.OutBack);

void Show() => Raven.LocalPosition(panel, slideIn);
```

Overloads exist for `Position`, `LocalPosition`, `EulerAngles`, `LocalEulerAngles`, `Scale`, `AnchoredPosition`, `SizeDelta`, `Alpha` (CanvasGroup, Graphic, SpriteRenderer) and `Color` (Graphic, SpriteRenderer). Tick **Use Start Value** to start from **Start Value** instead of the current value.

> [!NOTE]
> A `TweenSettings<T>` field declared without an initializer has a duration of 0 until you set one in the Inspector. Give it one with the constructor, as above.

---

## Easing

RavenTween ships the 30 standard Penner easing curves plus `Linear`. The dashed diagonal in each chart is linear, for comparison. `Back` and `Elastic` deliberately overshoot.

![All 31 easing curves available in RavenTween](images/easing-curves.png)

| Family | Feel |
| :--- | :--- |
| `Sine`, `Quad` | Gentle and natural — good defaults for UI. |
| `Cubic`, `Quart`, `Quint` | Increasingly snappy. |
| `Expo`, `Circ` | Very fast start or end. |
| `Back` | Overshoots slightly, then settles — great for pop-ins. |
| `Elastic` | Springy wobble. |
| `Bounce` | Bounces at the end like a dropped ball. |

`In` accelerates, `Out` decelerates, `InOut` does both. As a rule of thumb, things **entering** the screen use `Out`, things **leaving** use `In`.

> [!NOTE]
> All easing functions are pure math with no allocation. A custom `AnimationCurve` is evaluated by Unity and doesn't allocate either.

### Parametric eases

`Easing` tunes the three springy families with a parameter:

| Ease | Parameter |
| :--- | :--- |
| `Easing.Overshoot(strength)` | How far past the end it goes. `1` = the classic `OutBack`, `0` = none (`OutCubic`), `2` = twice as far. |
| `Easing.Bounce(strength)` | How high the rebounds are. `1` = the classic `OutBounce`, `0.5` = half as high. |
| `Easing.BounceExact(amplitude)` | The first rebound is exactly `amplitude` in the tween's own units (meters, degrees…), whatever the distance. |
| `Easing.Elastic(strength, period)` | Higher strength keeps oscillating longer; `period` is one oscillation as a fraction of the duration (default `0.3`). |

```csharp
Raven.Scale(button, 1f, 0.35f).From(Vector3.zero).Ease(Easing.Overshoot(2.2f));   // a bigger pop
Raven.PositionY(ball, 0f, 0.8f).Ease(Easing.BounceExact(0.25f));                  // rebounds 25 cm
```

`Easing` is a plain struct: no allocation. `Easing.Evaluate(t)` returns its value for your own code.

---

#### ◀ **[Getting Started](Getting-Started)**  ·  Next: **[Sequences ▶](Sequences)**
