# Custom Tweens

`Raven.Custom` tweens anything you can assign — a field on your script, a property on a component, a shader global — through a setter you provide. Done right, it allocates **nothing**, not even when the tween is created.

```csharp
Raven.Custom(light, 0f, 8f, 1f, (l, value) => l.range = value);
```

The first argument is the **target**; the setter receives it back along with the current value.

---

## Why pass the target?

Compare these two versions:

```csharp
// Allocates: the lambda captures `light` from the surrounding method.
Raven.Value(0f, 8f, 1f).OnUpdate(v => light.range = v);

// Allocates nothing: the lambda only uses its own parameters.
Raven.Custom(light, 0f, 8f, 1f, (l, v) => l.range = v);
```

When a lambda uses nothing but its parameters, the C# compiler creates it **once** and caches it forever. RavenTween stores the target and that cached delegate in the tween's pooled slot, and calls it through a typed invoker that is also created once per type. Nothing is boxed and nothing is allocated, whether you create ten tweens or ten thousand.

> [!TIP]
> To animate a field on your own script, pass `this` as the target:
> ```csharp
> Raven.Custom(this, 0f, 1f, 0.5f, (self, v) => self.dissolve = v);
> ```

---

## Supported value types

| Overload | Setter signature |
| :--- | :--- |
| `Custom(target, float from, float to, duration, setter)` | `(T target, float value)` |
| `Custom(target, Vector2 from, Vector2 to, duration, setter)` | `(T target, Vector2 value)` |
| `Custom(target, Vector3 from, Vector3 to, duration, setter)` | `(T target, Vector3 value)` |
| `Custom(target, Color from, Color to, duration, setter)` | `(T target, Color value)` |

`T` can be any class. Custom tweens accept every option and callback a normal tween does.

---

## Starting from the current value

`Custom` uses the `from` value you pass, fixed when you create the tween. When the tween is delayed or sits later in a sequence, the property may have changed by the time it starts. `CustomTo` reads the start value **when the tween starts**, like the built-in tweens do:

```csharp
// Fades the volume weight from wherever it is when this step begins.
Raven.CustomTo(volume, v => v.weight, 0f, 0.6f, (v, w) => v.weight = w);

// In a sequence, each step starts where the previous one ended.
Raven.Sequence()
    .Chain(Raven.CustomTo(volume, v => v.weight, 1f, 0.3f, (v, w) => v.weight = w))
    .ChainDelay(1f)
    .Chain(Raven.CustomTo(volume, v => v.weight, 0f, 0.3f, (v, w) => v.weight = w));
```

`CustomTo` exists for `float` values. With non-capturing lambdas, it allocates nothing either.

---

## Destroyed targets

If the target is a `UnityEngine.Object` (a component, a GameObject, a ScriptableObject…), the tween **dies with it**: once the object is destroyed, the tween stops on its next update and fires `OnTargetDestroyed` instead of calling your setter. No `MissingReferenceException`, ever.

Creating a custom tween on an object that is already destroyed (or null) logs an error and returns a dead handle.

For plain C# objects there is no notion of destruction; stop the tween yourself when you no longer need it.

---

## Examples

```csharp
// A post-processing weight.
Raven.Custom(volume, 0f, 1f, 0.6f, (v, w) => v.weight = w);

// A global shader value (static, so any target works — use the component itself).
Raven.Custom(this, 0f, 1f, 2f, (_, t) => Shader.SetGlobalFloat(DissolveId, t));

// An audio mixer parameter.
Raven.Custom(mixer, -80f, 0f, 1.5f, (m, db) => m.SetFloat("MusicVolume", db));
```

> [!WARNING]
> The last example passes a string literal to `SetFloat`. That literal is created once by the runtime, so it is still allocation-free — but building a string inside the lambda (`"Vol" + i`) would allocate every frame.

---

#### ◀ **[Shake and Punch](Shake-and-Punch)**  ·  Next: **[TextMeshPro ▶](TextMeshPro)**
