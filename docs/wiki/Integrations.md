# Integrations

RavenTween works with the packages a project already uses. Each integration is a small separate assembly that compiles **only when its package is in the project**, so a project without it never sees an error or an extra dependency.

| Package | What you get |
| :--- | :--- |
| **Unity Timeline** | A **Raven Motion Track** for cutscenes |
| **UI Toolkit** | Tweens for `VisualElement` styles |
| **UniTask** | `await tween.ToUniTask(token)` |
| **Input System** | Gamepad rumble (`RavenRumble`, and the Rumble feedback) |
| **URP / HDRP** | Post-processing `Volume` weight tweens and flashes |
| **Cinemachine 2 or 3** | The Camera Impulse feedback |
| **TextMeshPro** | See **[TextMeshPro](TextMeshPro)** |
| **Animation Rigging** | See **[Procedural Animation](Procedural-Animation)** |

---

## Unity Timeline

Add a **RavenTween ▸ Raven Motion Track** to a Timeline, bind it to an object, and add **Raven Motion** clips. Each clip plays one of the ready-made motions (or a Motion Style); its length defaults to the motion's own timing. Stretch a loop or an attention motion to repeat it; entrances and exits hold their last frame after the clip.

The motion is **sampled at the playhead**: scrubbing forwards or backwards shows exactly the right pose, and previewing in the editor puts everything back afterwards, as Timeline's own tracks do.

---

## UI Toolkit

```csharp
using RavenTween;

panel.TweenOpacity(1f, 0.3f);
panel.TweenTranslate(new Vector2(0f, -40f), 0.4f).Ease(Ease.OutBack);
icon.TweenScale(1.2f, 0.2f).Cycles(2, CycleMode.Yoyo);
icon.TweenRotate(360f, 1f);
button.TweenBackgroundColor(Color.red, 0.2f);
label.TweenColor(Color.white, 0.2f);
bar.TweenWidth(240f, 0.5f);
```

Each tween starts from the element's current resolved style (so USS styling is respected) and writes the inline style. They allocate nothing while they play.

---

## UniTask

```csharp
await Raven.Position(transform, target, 0.5f).ToUniTask(destroyCancellationToken);
await sequence.ToUniTask();
```

The task ends when the tween ends. Cancelling the token stops the tween and throws `OperationCanceledException`, as UniTask code expects.

---

## Input System — gamepad rumble

```csharp
RavenRumble.Play(lowFrequency: 0.6f, highFrequency: 0.3f, duration: 0.2f);
```

Overlapping rumbles use the strongest motors and stop when the last one ends, in real time (a hit stop doesn't stretch them). In **Raven Feedback**, the **Rumble** action does the same without code.

---

## URP / HDRP — post-processing

```csharp
damageVolume.FlashWeight(1f, 0.4f);   // up and back: a red vignette on a hit
pauseVolume.TweenWeight(1f, 0.3f);    // blur in when the game pauses
```

A flash repeated mid-flash comes back to the volume's own weight, never to a mid-flash value. In **Raven Feedback**, the **Volume Flash** action does the same without code.

---

## Cinemachine

In **Raven Feedback**, the **Camera Impulse** action fires a **Cinemachine Impulse Source** with the action's strength; every Cinemachine camera with an Impulse Listener reacts. It works with Cinemachine 2 and Cinemachine 3.

---

#### ◀ **[Designer Workflow](Designer-Workflow)**  ·  Next: **[Monitor ▶](Monitor)**
