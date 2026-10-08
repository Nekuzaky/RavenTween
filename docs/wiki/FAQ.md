# FAQ

---

### Is RavenTween free?

Yes. It is open source under the MIT license, free for commercial projects. If it saves you time, you can support it on [GitHub Sponsors](https://github.com/sponsors/nekuzaky) or [Buy Me a Coffee](https://buymeacoffee.com/nekuzaky).

### Which Unity versions are supported?

Unity 2021.3 LTS and newer, including Unity 6. See **[Installation](Installation)**.

### How do I update?

You usually don't have to think about it: RavenTween checks for new releases once a day and offers to install them. You can also install updates fully automatically, or check by hand from **Tools ▸ RavenTween ▸ Updates**. See **[Installation](Installation)**.

### "Could not create asset from Packages/com.raventween.core/Editor/Icons/… File could not be read"

Versions before 1.6.1 stored their icons with Git LFS, and a machine without Git LFS installed (often Linux) downloads them as text files. Update to 1.6.1 or later — change the tag in the install URL to `#v1.6.1` — or install Git LFS (`git lfs install`) and reimport.

### Which platforms?

All of them. The runtime is pure managed C# with no threads, reflection, native plugins or runtime code generation, so it runs on Windows, macOS, Linux, Android, iOS, WebGL and consoles, under Mono or IL2CPP.

### Why can't I restart a tween?

Tweens are single-use on purpose: a tween that can be rewound and replayed carries hidden state that causes subtle bugs. Creating a new tween is free because slots are pooled. To reuse a configuration, use a **[Tween Template](Designer-Workflow)** or a `TweenParams` field.

### Do I need to call `Play()` or `Start()`?

No. Tweens start playing on creation. `Start()` exists for readability and to resume a paused tween.

### What happens if I destroy an object while it's being tweened?

Nothing bad. The tween notices on its next update, fires `OnTargetDestroyed` if you subscribed, and returns its slot to the pool. No exception is thrown. The same goes for loading a new scene. See **[Control and Lifecycle](Control-and-Lifecycle)**.

### My animation doesn't play while the game is paused.

`Time.timeScale = 0` freezes tweens like everything else. Add `.UnscaledTime()` to tweens that should keep playing, such as pause-menu animations.

### Two tweens on the same object fight each other.

Two tweens writing the same property both run, and the last one to update each frame wins. Stop the old tween before starting a new one — keep its handle and call `Stop()`, or call `Raven.StopAll(target)` — or tween different properties. Single-axis tweens (`PositionX`, `ScaleY`…) let two tweens share a transform without fighting.

### How do I move a physics object?

Use `rigidbody.TweenMovePosition(...)` / `TweenMoveRotation(...)` (also on `Rigidbody2D`). They go through the physics engine and run in `FixedUpdate`, so collisions keep working. See **[Tweens and Easing](Tweens-and-Easing)**.

### Can I keep my DOTween code?

Mostly, yes: swap `using DG.Tweening;` for `using RavenTween; using RavenTween.DOTweenAdapter;` and the common DOTween calls compile and run on RavenTween. See **[Migrating](Migrating)**.

### Can I put an infinite tween in a sequence?

No: a sequence needs a finite length. An infinite child logs an error and is clamped to one cycle. Loop the **sequence** instead with `.Infinite()`.

### Does `await` work on WebGL?

Yes. RavenTween resumes awaiting code from its own update on the main thread; no threads are involved.

### Does `OnUpdate(v => …)` allocate?

Only if the lambda captures something, and then only once when the tween is created — that's how C# closures work. For zero allocations, pass a target: `OnUpdate(target, (t, tween) => …)`, `OnComplete(target, t => …)`, or use **[Custom Tweens](Custom-Tweens)**. See **[Performance](Performance)**.

### Does it work with domain reload disabled?

Yes. The engine resets itself on `SubsystemRegistration`, so **Enter Play Mode Options** with domain reload off is supported.

### How do I see what's running?

Open **Tools ▸ RavenTween ▸ Monitor**. See **[Monitor](Monitor)**.

### Where do I report a bug?

On [GitHub Issues](https://github.com/Nekuzaky/RavenTween/issues). Please include your Unity version, the RavenTween version and a minimal script that reproduces it.

---

#### ◀ **[Reference](Reference)**  ·  Next: **[Changelog ▶](Changelog)**
