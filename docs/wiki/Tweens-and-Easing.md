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
| **SpriteRenderer** | `Color` | `Color` |
| **Light** | `Intensity`, `Color` | `float`, `Color` |

Every method also exists as an extension: `transform.TweenPosition(...)`, `canvasGroup.TweenAlpha(...)`, `material.TweenColor(...)`, `light.TweenIntensity(...)`.

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
| `.Ease(animationCurve)` | Use any `AnimationCurve` (time and value in 0–1). |
| `.Ease(t => …)` | Use your own function. |
| `.From(value)` | Start from this value instead of the current one. |
| `.Delay(seconds)` | Wait before starting. `OnStart` fires when the delay ends. |
| `.Cycles(n)` | Play `n` times. |
| `.Cycles(n, CycleMode.Yoyo)` | Play forward, then backward, alternately. |
| `.Infinite()` | Loop forever. Defaults to `CycleMode.Yoyo` for tweens. |
| `.UnscaledTime()` | Ignore `Time.timeScale` (pause menus). |

```csharp
// Fade in from fully transparent, wait 0.2 s, ease out.
Raven.Alpha(canvasGroup, 1f, 0.4f).From(0f).Delay(0.2f).Ease(Ease.OutCubic);

// A breathing glow, forever.
Raven.Intensity(lamp, 3f, 1.2f).Ease(Ease.InOutSine).Infinite();
```

### Restart vs. Yoyo

- **Restart** jumps back to the start value at the beginning of each cycle.
- **Yoyo** plays odd cycles backwards. With an even number of cycles, the tween ends where it started — perfect for pulses: `.Cycles(2, CycleMode.Yoyo)`.

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

---

#### ◀ **[Getting Started](Getting-Started)**  ·  Next: **[Sequences ▶](Sequences)**
