# Installation

RavenTween is a standard Unity package installed from its Git repository. There is nothing to import by hand and nothing to configure afterwards.

---

## Option 1 — Package Manager *(recommended)*

1. In Unity, open **Window ▸ Package Manager**.
2. Click **+** in the top-left corner, then **Add package from git URL…**
3. Paste the URL below and click **Add**.

```
https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.2.0
```

> [!TIP]
> The `#v1.2.0` suffix pins the version, so every teammate and every CI build gets exactly the same code. To update later, change the tag to the newer version.

---

## Option 2 — `manifest.json`

Add this line to the `dependencies` block of your project's `Packages/manifest.json`:

```json
"com.raventween.core": "https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.2.0"
```

Unity resolves the package the next time it gains focus.

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
| **07 - TextMeshPro** | Typewriter reveal, score counter, punch feedback |

> [!NOTE]
> The samples work with both the legacy Input Manager and the Input System package.

---

## Requirements

| Requirement | Detail |
| :--- | :--- |
| **Unity version** | Unity 2021.3 LTS or newer, including Unity 6 |
| **Dependencies** | `com.unity.ugui` (installed automatically) |
| **TextMeshPro** | Optional. The TMP module compiles only when TextMeshPro is present |
| **Platforms** | Every platform Unity targets: Windows, macOS, Linux, Android, iOS, WebGL, consoles |
| **Scripting backends** | Mono and IL2CPP |

---

> [!TIP]
> Once installed, continue to **[Getting Started](Getting-Started)** to write your first tween.

---

#### ◀ **[Home](Home)**  ·  Next: **[Getting Started ▶](Getting-Started)**
