# Known Pitfalls

Five behaviors that are intended, but surprise people the first time. Each one comes with what to do about it.

---

### Nothing moves: `Time.timeScale` is 0

Tweens follow `Time.timeScale`, like the rest of the game. A pause menu or a tutorial popup that sets it to `0` freezes every tween, which can look as if RavenTween stopped working. The **[Monitor](Monitor)** shows a warning when this happens.

- Tweens that must play while the game is paused (menus, popups): add `.UnscaledTime()`.
- Tools and recordings: `Raven.Step(deltaTime)` advances tweens by hand and ignores `Time.timeScale`.

### Shakes and punches are local

`Raven.Position` animates the **world** position, but `ShakePosition` and `PunchPosition` move the **local** position, so they stay relative to a moving parent. The explicit names `ShakeLocalPosition`, `ShakeLocalRotation`, `PunchLocalPosition` and `PunchLocalRotation` say it out loud and do the same thing.

### `ShakeCamera` fights camera controllers

`ShakeCamera` tilts the camera as well as moving it. If a `LookAt`, a mouse-look script or Cinemachine owns the rotation, pass `tilt: false`, or shake an empty pivot between the rig and the camera. See **[Shake and Punch](Shake-and-Punch)**.

### A tween started from a callback moves one frame later

A tween never advances during the engine update that created it. A tween created in your `Update` steps the same frame, since the engine runs after scripts; but one created from a tween callback (`OnComplete`, a sequence's `ChainCallback`) takes its first step on the next frame. This keeps results independent of creation order and makes callbacks safe. When that frame matters, write the start value to the object yourself as you create the tween.

### A second effect replaces the first

Starting a shake or punch on a property that is already shaking or punching **replaces** the running effect, and keeps the original rest value. Rapid hits never make an object drift. To stack two effects, put them on different properties (position and rotation) or on a parent and a child.

---

#### ◀ **[Engine Internals](Engine-Internals)**  ·  Next: **[FAQ ▶](FAQ)**
