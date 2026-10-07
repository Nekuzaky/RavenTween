# RavenTween

High-performance, allocation-free tweening engine for Unity. This repository is the development project; the package itself lives in [`Packages/com.raventween.core`](Packages/com.raventween.core).

**Full documentation, API tour and migration guide:** [package README](Packages/com.raventween.core/README.md)

## Install

Package Manager → `+` → *Add package from git URL...*:

```
https://github.com/Nekuzaky/RavenTween.git?path=/Packages/com.raventween.core
```

Pin a release: append `#v1.0.0`.

## At a glance

```csharp
using RavenTween;

Raven.Position(transform, targetPos, 0.6f).Ease(Ease.OutQuad);

Raven.Sequence()
    .Chain(Raven.AnchoredPosition(panel, Vector2.zero, 0.5f).Ease(Ease.OutBack))
    .Group(Raven.Alpha(canvasGroup, 1f, 0.4f))
    .OnComplete(() => Debug.Log("done"));

await Raven.Delay(1.2f);
```

MIT licensed. See the package [CHANGELOG](Packages/com.raventween.core/CHANGELOG.md) and [CONTRIBUTING](Packages/com.raventween.core/CONTRIBUTING.md).
