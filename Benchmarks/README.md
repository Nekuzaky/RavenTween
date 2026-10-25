# Competitor benchmark

`CompetitorBench` is a separate Unity project (Unity 6000.3) that compares RavenTween with
DOTween, PrimeTween and LitMotion in a **release Windows player**. It references RavenTween by
local path (`file:../../../Packages/com.raventween.core`), so the competitors never enter the
package or the development project.

## Libraries

| Library | Version | Source |
| :--- | :--- | :--- |
| RavenTween | local package | `Packages/com.raventween.core` |
| PrimeTween | 1.5.1 | npm scoped registry `https://registry.npmjs.org` (scope `com.kyrylokuzyk`), the installation documented by PrimeTween |
| LitMotion | 2.0.2 (+ Burst, Collections, Mathematics) | OpenUPM `https://package.openupm.com` (scope `com.annulusgames`) |
| DOTween (free) | 1.3.045 | `https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip` (SHA-256 `32EBA5C48520A541775D00CB396E18975B83587BA79175DBB878D753725BA5D9`), **not committed** |

PrimeTween and LitMotion are pinned in `CompetitorBench/Packages/manifest.json` and resolved by
the Package Manager. PrimeTween's license allows depending on it as an npm package but not
shipping its source, which is what the manifest does.

DOTween's free license allows redistributing verbatim copies only together with its notices and
readme. To keep the repository free of third-party binaries it is not committed and is ignored by
git. Install it with:

```powershell
./Tools/Benchmark-Competitors.ps1 -InstallDOTween
```

The script checks the downloaded zip against the SHA-256 above (`-DOTweenSha256`) and refuses to
install it if it differs.

or extract the official zip yourself into `CompetitorBench/Assets/Plugins/Demigiant` (the zip's
`DOTween` folder ends up at `Assets/Plugins/Demigiant/DOTween`). DOTween's setup panel is not
needed. Without DOTween the build leaves its adapter out and the benchmark skips it.

## Running

```powershell
./Tools/Benchmark-Competitors.ps1                      # build, 3 repetitions, N = 1,000 / 10,000 / 50,000
./Tools/Benchmark-Competitors.ps1 -Repetitions 5
./Tools/Benchmark-Competitors.ps1 -SkipBuild -Libraries RavenTween,PrimeTween -Counts 10000
```

The script builds the player (IL2CPP when the editor has the Windows IL2CPP module, otherwise
Mono; force one with `-Backend`), then starts one player process per repetition, scenario, N and
library with `-batchmode -nographics`. Results go to `Benchmarks/Results/latest`:
`raw.csv` (one row per process), `summary.csv` (median of the repetitions) and `results.md`.

## What one process measures

1. Creates N empty GameObjects.
2. **Baseline**: 2 s of frames with the same objects, before any call to the library under test.
   Every library is compiled into the same player, so this is the same empty-process baseline for
   all of them; it is taken before the pool is reserved, so a library whose idle update walks its
   reserved pool pays for it in the result instead of having it subtracted.
3. Reserves the library's pool for the scenario (`Raven.SetCapacity`,
   `PrimeTweenConfig.SetTweensCapacity`, `DOTween.SetTweensCapacity`,
   `MotionDispatcher.EnsureStorageCapacity` for LitMotion's `Vector3` motions), runs 64 tweens for
   30 frames to JIT the code paths, then stops them.
4. **Start**: creates N tweens (or N sequences) in one frame, timed with a `Stopwatch`; the GC
   allocation of that call is recorded too.
5. **Warm-up** of 1 s, then **measurement** of 4 s. A player-loop system at the start of
   `Initialization` and one at the end of `PostLateUpdate` time every frame on the CPU, so the
   library's own update is included wherever it runs (player loop or `MonoBehaviour`), as are the
   transform writes.

Reported update cost = average frame - baseline average frame (the baseline of step 2); p95 = 95th percentile frame -
baseline median frame. Every process checks that the targets actually moved.

Scenarios, identical for every library:

- **Tweens**: N world-position tweens, `InOutQuad`, 20 s (still running when the measurement ends).
- **Sequences**: N sequences of 3 chained world-position tweens on the same object, `InOutQuad`,
  2 s each, so the measurement crosses two child transitions.

GC per frame is measured with `GC.GetAllocatedBytesForCurrentThread` when the runtime supports
it; Unity's Mono does not, so the GC is disabled during the window and `GC.GetTotalMemory` is
compared instead (`environment.csv` and `raw.csv` say which).

Each library uses its defaults otherwise (DOTween: `DOTween.Init()`, safe mode on, no
recycling). LitMotion is given its `from` value explicitly, which is how its API works; the
others read the current position when the tween starts.
