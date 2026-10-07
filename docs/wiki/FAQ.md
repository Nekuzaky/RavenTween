# FAQ

---

### Is RavenTween free?

Yes. It is open source under the MIT license, free for commercial projects. If it saves you time, you can support it on [GitHub Sponsors](https://github.com/sponsors/nekuzaky) or [Buy Me a Coffee](https://buymeacoffee.com/nekuzaky).

### Which Unity versions are supported?

Unity 2021.3 LTS and newer, including Unity 6. See **[Installation](Installation)**.

### How do I update?

You usually don't have to think about it: RavenTween checks for new releases once a day and offers to install them. You can also install updates fully automatically, or check by hand from **Tools ▸ RavenTween ▸ Updates**. See **[Installation](Installation)**.

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

Two tweens writing the same property both run, and the last one to update each frame wins. Stop the old tween before starting a new one — keep its handle and call `Stop()` — or tween different properties.

### Can I put an infinite tween in a sequence?

No: a sequence needs a finite length. An infinite child logs an error and is clamped to one cycle. Loop the **sequence** instead with `.Infinite()`.

### Does `await` work on WebGL?

Yes. RavenTween resumes awaiting code from its own update on the main thread; no threads are involved.

### Does `OnUpdate(v => …)` allocate?

Only if the lambda captures something, and then only once when the tween is created — that's how C# closures work. For zero allocations, use **[Custom Tweens](Custom-Tweens)**. See **[Performance](Performance)**.

### Does it work with domain reload disabled?

Yes. The engine resets itself on `SubsystemRegistration`, so **Enter Play Mode Options** with domain reload off is supported.

### How do I see what's running?

Open **Tools ▸ RavenTween ▸ Monitor**. See **[Monitor](Monitor)**.

### Where do I report a bug?

On [GitHub Issues](https://github.com/Nekuzaky/RavenTween/issues). Please include your Unity version, the RavenTween version and a minimal script that reproduces it.

---

#### ◀ **[Reference](Reference)**  ·  Next: **[Changelog ▶](Changelog)**
