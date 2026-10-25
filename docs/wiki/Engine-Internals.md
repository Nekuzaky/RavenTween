# Engine Internals

How the engine is put together, and the rules its code relies on. You don't need this page to use RavenTween; it is for contributors and for anyone who wants to know why a behavior is the way it is. The source carries no comments: this page is where its design notes live.

---

## Slots and handles

- Every tween, sequence and sequence callback lives in a pooled `TweenSlot`. Slots are created once, kept in a plain array, and recycled through a free-slot bitmap (one bit per slot, plus one summary bit per 64 slots) that always hands out the lowest free index without scanning the pool or allocating. `Raven.SetCapacity` creates them up front.
- The engine keeps the end of the live range: one past the highest slot in use. Renting raises it, releasing the top slot lowers it past the free slots below. Because new tweens take the lowest free index, live slots stay packed at the start of the array, so the update loop, `StopAll` / `CompleteAll`, the per-target calls (`StopAll(target)`, `CompleteAll(target)`, `PauseAll`, `ResumeAll`, `CountTweens`) and the monitor walk only that range: with nothing playing they cost nothing, whatever the capacity reserved.
- A `Tween` or `Sequence` handle is a slot index plus a version. Releasing a slot bumps its version, so every old handle to it is dead (`IsAlive` is false, every call on it is a no-op).
- The Play Mode reset (`SubsystemRegistration`) and the end of Play Mode (`Application.quitting`) free every slot and bump its version, instead of dropping the pool: a handle kept across sessions (no domain reload) can never match a new tween.
- Releasing a slot clears every reference it holds (targets, callbacks, curves), so a large pool never keeps a scene or a closure alive. A reused slot is therefore only reset, never cleared twice.
- `Raven.AliveCount` is a counter kept by renting and releasing; sequence callback holders are not counted.

## The update loop

- The engine inserts one system in `Update`, one in `PreLateUpdate`, and one in `FixedUpdate` right after `ScriptRunBehaviourFixedUpdate`, so a Rigidbody moved by a tween is simulated in the same physics step. Its presence is checked on every Play Mode entry (the editor may reset the player loop) and again after `AfterSceneLoad`, in case a package replaced the loop at startup.
- If another system replaces the player loop later, a tween created while the engine has not ticked for more than two frames triggers a check (at most once per frame) and the engine installs itself again, with a warning.
- Each pass increments a pass counter. A slot created during a pass (by a callback) is skipped by that pass, even when the callback started another pass: results never depend on creation order.
- Negative, NaN or infinite deltas (a recorder, `Time.captureFramerate`) are clamped to 0. `Raven.TimeScale` is applied once per pass, the per-tween time scale only when it is not 1.
- Cancellation tokens are polled, never registered: no allocation, and a token cancelled on another thread is only acted on during a pass, on the main thread. A paused tween is still stopped when its token is cancelled.
- The whole pass is one protected region. A tween whose setter or ease throws is logged and stopped, and the pass resumes with the next one; user callbacks are contained one by one and never stop their tween. The index of the tween being stepped is reset at the start of every range, so an exception can never be blamed on, or resume from, a slot of an earlier pass.
- A property tween's target is checked before it is stepped, and again after `OnStart` (the only user code that runs before the first write). Writes then go straight to the target.
- `Position`, `LocalPosition` and `LocalScale` tweens with no update callback, effect, link or incremental cycles interpolate and write their `Vector3` directly instead of going through the generic value path. The target was validated before, so this path only checks the cast by reference, without Unity's `== null` lookup; `FastPathEquivalenceTests` checks it writes bit for bit what the generic path writes.
- Whole cycles are skipped in closed form, so a huge delta costs the same as a small one. Infinite loops keep counting cycles but wrap before overflow, keeping Yoyo parity.

## Reentrancy

User code (callbacks, eases, setters, await continuations) may create, stop, complete or move any tween at any time.

- End-of-life paths release the slot first and run user code after: inside `OnComplete`, `OnKill` or `OnTargetDestroyed`, the handle is already dead, so stopping or completing it again (or `StopAll`) can never re-enter.
- After each piece of user code, the engine checks the slot's version again before running the next one.
- While a tween completes (`Completing`), a nested `Complete()`, a time jump or a change of cycles requested by its last callbacks is ignored: a finishing tween always finishes. While it jumps in time (`Moving`), a nested jump is ignored.
- Await continuations of a whole sequence tree are collected while releasing it and resumed afterwards, each one protected, so one failing awaiter never strands the others.
- Completing never aborts half-way: a throwing setter or ease stops that tween only, also inside `CompleteAll()`.

## Sequences

- Items are kept sorted by the moment they start writing (their own delay included). Rewinds run first, latest item first, so the earliest child's start value wins; active children then run in order, so the latest active child wins the property. Equal times keep insertion order.
- At a cycle wrap, the closing cycle is evaluated to its end first, so children reach their end values and fire their per-cycle callbacks; while those callbacks run it is still the current cycle, so `SetRemainingCycles(1)` there makes it the last one. A long frame replays at most 16 cycles; the rest are skipped in closed form.
- Timeline callbacks fire once per cycle, when the time played passes their spot, mirrored on Yoyo's backward cycles. A spot exactly on a Yoyo turning point fires only on the cycle that reaches it. Nested sequences only fire their callbacks while their parent plays forward.
- A time jump is silent: nothing passed over fires, and it lands just before its target on the root timeline so callbacks exactly there still fire when playing resumes. A jump that lands exactly on a child's start leaves that child unstarted, so its `OnStart` fires on resume.
- Every evaluation re-validates the sequence (version and revision) after each child, and a jump of the root requested from deep inside a nested sequence stops every level at once.
- A zero-length sequence (only callbacks, or empty) happens all at once: its callbacks fire once, then it completes; an infinite one stays alive without firing again.
- Nesting is walked with explicit work stacks and limited to 8 levels; there is no recursion over the tree.

## Effects

- Shakes and punches oscillate around the captured start value with an envelope that is zero at both ends, so they start and settle exactly on the rest value, also when cycled or rewound.
- The standalone effect of each target and property is kept in a table (validated on lookup, removed on release), so starting a new one finds the effect it replaces without scanning, and takes over its rest value.

## Time scale helpers

- A hit stop is counted down by the `Update` pass in unscaled time, whatever the global phase, `Raven.TimeScale` or stopped tweens, so it always ends. Its scale is never exactly 0, so a pause set during the freeze is told apart from it and kept.
- Hit stops and slow motions never speed the game up: started while `Time.timeScale` is already lower, they do nothing. During a hit stop, a slow motion compares its scale with the speed the hit stop will restore (the speed from before the hit stop), not with the frozen scale, and restores that speed when it ends; if `Time.timeScale` no longer equals the hit-stop scale, the game has taken over (a pause menu) and the slow motion does nothing.

## Editor previews

- Every Edit Mode preview (Motion Library, Raven Motion / Stagger / Show Hide / Path inspectors, Tween Template inspector, Sequence Editor, Timeline track) goes through one service, `EditorTweenPreview`. It records each value before the preview touches it and puts it back before entering Play Mode, before a script reload, before any scene, prefab or asset is saved, when a scene or prefab stage closes, and before quitting, so a preview never ends up saved.
- Transform values are recorded in local space (anchored space for UI), so restoring a parent and its child in any order gives back the exact layout. A renderer that had no property block before the preview gets none back.
- Editing an animated object while a preview is shown ends the preview first and keeps the edit; an undo during a preview keeps the undo's result.
- Scrubbing evaluates the tween from scratch at the requested time (`TweenEngine.Seek`): every child captures its start value again, in timeline order, and no callback runs.
- The Motion Library's preview cards run the motion on a hidden, never-saved proxy object and sample it by time.

## Update checker

- At most one anonymous request a day to the GitHub "latest release" endpoint, from the Editor only, never in batch mode or while entering or leaving Play Mode. Drafts and pre-releases are ignored; a release is newer than a pre-release of the same number.
- Asset Store and Unity registry installs are left to the Package Manager. Embedded and local copies are never updated automatically: the copy you edit is its own source of truth. Git installs keep their URL and `?path=` and only change the `#tag`.
- A missing, corrupt or future last-check date simply makes the check due.

## Serialized values

- `PropertyKind` values are stored in templates: new kinds are only ever appended. `MotionPreset` values are explicit for the same reason.

---

#### ◀ **[API Details](API-Details)**  ·  Next: **[Known Pitfalls ▶](Known-Pitfalls)**
