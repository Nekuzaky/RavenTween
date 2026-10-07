<p align="center">
  <img src="Packages/com.raventween.core/Documentation~/images/logo.png" width="112" alt="RavenTween logo">
</p>

<h1 align="center">RavenTween</h1>

<p align="center">
  High-performance, allocation-free tweening for Unity.
</p>

<p align="center">
  <a href="https://github.com/Nekuzaky/RavenTween/actions/workflows/tests.yml"><img src="https://github.com/Nekuzaky/RavenTween/actions/workflows/tests.yml/badge.svg" alt="Tests"></a>
  <img src="https://img.shields.io/badge/Unity-2021.3%2B-222?logo=unity" alt="Unity 2021.3+">
  <img src="https://img.shields.io/badge/GC-0%20B%2Fframe-2ea44f" alt="Zero GC">
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT">
</p>

<p align="center">
  <a href="https://github.com/sponsors/nekuzaky"><img src="https://img.shields.io/badge/Sponsor-GitHub-ea4aaa?logo=githubsponsors&logoColor=white" alt="Sponsor on GitHub"></a>
  <a href="https://buymeacoffee.com/nekuzaky"><img src="https://img.shields.io/badge/Buy%20me%20a%20coffee-nekuzaky-ffdd00?logo=buymeacoffee&logoColor=black" alt="Buy me a coffee"></a>
</p>

```csharp
using RavenTween;

Raven.Position(transform, target, 0.6f).Ease(Ease.OutQuad);

Raven.Sequence()
    .Chain(Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack))
    .Group(Raven.Alpha(canvasGroup, 1f, 0.4f))
    .Chain(Raven.PunchScale(button, Vector3.one * 0.15f, 0.3f));

await Raven.Delay(1.2f);
```

- **0 B per frame** for property, effect, sequence and custom tweens — enforced by the test suite.
- **~0.77 ms/frame** for 5,000 simultaneous tweens.
- Sequences, shake & punch, TextMeshPro, async/await, coroutines.
- Procedural look-at and spring chains, plus optional Animation Rigging integration.
- Designer templates and a live **Tools → RavenTween → Monitor** window.

<p align="center">
  <img src="Packages/com.raventween.core/Documentation~/images/monitor.png" width="720" alt="Raven Monitor">
</p>

## Install

Package Manager → `+` → *Add package from git URL…*

```
https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core#v1.2.0
```

## Documentation

**[Read the manual at nekuzaky.com/docs/raventween](https://nekuzaky.com/docs/raventween)** — getting started, guides, procedural animation, designer tools, performance, migration from DOTween / PrimeTween, full API reference and FAQ. Its source lives in [`docs/wiki`](docs/wiki). The [package README](Packages/com.raventween.core/README.md) has a one-page API tour.

[Changelog](Packages/com.raventween.core/CHANGELOG.md) · [Contributing](Packages/com.raventween.core/CONTRIBUTING.md) · [Security](SECURITY.md) · MIT license

## Support

RavenTween is free and MIT licensed. If it saves you time, you can support its development through [GitHub Sponsors](https://github.com/sponsors/nekuzaky) or [Buy Me a Coffee](https://buymeacoffee.com/nekuzaky).

---

This repository is the development project. The package itself is in [`Packages/com.raventween.core`](Packages/com.raventween.core).
