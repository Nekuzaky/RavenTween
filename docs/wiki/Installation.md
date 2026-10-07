# Installation

RavenTween is a standard Unity package installed from its Git repository. There is nothing to import by hand and nothing to configure afterwards.

---

## Option 1 — Package Manager *(recommended)*

1. In Unity, open **Window ▸ Package Manager**.
2. Click **+** in the top-left corner, then **Add package from git URL…**
3. Paste the URL below and click **Add**.

```
https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.6.0
```

> [!TIP]
> The `#v1.6.0` suffix pins the version, so every teammate and every CI build gets exactly the same code. To update later, change the tag to the newer version.

---

## Option 2 — `manifest.json`

Add this line to the `dependencies` block of your project's `Packages/manifest.json`:

```json
"com.raventween.core": "https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.6.0"
```

Unity resolves the package the next time it gains focus.

---

## Staying up to date

RavenTween keeps itself current from inside the Editor — no need to edit the URL by hand.

- **Once a day**, it asks GitHub whether a newer release exists. If so, a small window offers **Update now**, **What's new**, **Skip this version** or **Later**.
- **Update now** installs the new version through the Package Manager and rewrites the tag in your `manifest.json` for you.
- **Fully automatic**: enable **Tools ▸ RavenTween ▸ Updates ▸ Install Automatically** and new releases install on their own, without asking.
- **Check now**: **Tools ▸ RavenTween ▸ Updates ▸ Check Now** checks immediately and tells you if you're already up to date.
- **Off switch**: untick **Tools ▸ RavenTween ▸ Updates ▸ Check Automatically**.

> [!NOTE]
> The check is a single anonymous request to the public GitHub API — nothing about your project or your machine is sent. It never runs in batch mode (CI, build servers), and it leaves embedded or local copies of the package alone, since those are meant to be edited by hand.

> [!TIP]
> On a team, leave automatic installs **off** and update on purpose: the version tag is shared through `manifest.json`, so one person updating and committing moves the whole team together. Settings are per machine, never per project.

---

## Samples

Open **Window ▸ Package Manager**, select **RavenTween**, then the **Samples** tab. Each sample builds its own scene at runtime: import it, add its demo component to an empty GameObject in an empty scene, and press Play.

| Sample | Shows |
| :--- | :--- |
| **01 - UI Basics** | Panel slide-in, canvas group fade, pulsing button |
| **02 - Gameplay** | Hops, spins, a patrol loop built from a sequence |
| **03 - Camera** | Breathing zoom, background color blend, punch zoom on Space |
| **04 - Materials** | Staggered material color waves on a grid of spheres |
| **05 - Complex Sequence** | Nested `Chain` / `Group` / `Insert` driven by a coroutine |
| **06 - Benchmark** | Thousands of tweens with a live frame-time and GC readout |
| **07 - TextMeshPro** | Typewriter reveal, score counter, punch feedback (needs TextMeshPro) |
| **08 - Procedural** | A head tracking an orbiting target, a swinging antenna, a look-away driven by weight tweens |
| **09 - Time and Physics** | A platform carrying a crate through the physics engine, a ticking clock, timeline callbacks, a hit stop on Space (needs the Physics module) |

> [!NOTE]
> The samples work with both the legacy Input Manager and the Input System package.

---

## Requirements

| Requirement | Detail |
| :--- | :--- |
| **Unity version** | Unity 2021.3 LTS or newer, including Unity 6 |
| **Dependencies** | `com.unity.ugui` and the built-in Audio, JSON Serialize and Unity Web Request modules — all installed automatically |
| **TextMeshPro** | Optional. The TMP module compiles only when TextMeshPro is present |
| **Animation Rigging** | Optional. The rigging module compiles only when `com.unity.animation.rigging` is present |
| **Physics / Physics 2D** | Optional. The Rigidbody tweens compile only when the built-in Physics or Physics 2D module is enabled (it is by default) |
| **Platforms** | Every platform Unity targets: Windows, macOS, Linux, Android, iOS, WebGL, consoles |
| **Scripting backends** | Mono and IL2CPP |

---

> [!TIP]
> Once installed, continue to **[Getting Started](Getting-Started)** to write your first tween.

---

#### ◀ **[Home](Home)**  ·  Next: **[Getting Started ▶](Getting-Started)**
