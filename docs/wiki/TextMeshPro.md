# TextMeshPro

RavenTween includes a small module for TextMeshPro text. It is a separate assembly, `RavenTween.TextMeshPro`, that compiles **only when TextMeshPro is in your project** — so projects without TMP never see an error.

| Unity version | Where TextMeshPro comes from |
| :--- | :--- |
| Unity 6 | Built into **uGUI 2.0** — nothing to install |
| 2021.3 / 2022.3 | The **TextMeshPro** package (`com.unity.textmeshpro`) |

---

## Color and alpha

`TMP_Text` is a uGUI `Graphic`, so the regular color tweens already work on it — no module needed:

```csharp
Raven.Color(label, Color.red, 0.2f);
Raven.Alpha(label, 0f, 0.5f);
```

---

## Text-specific tweens

These are extension methods on `TMP_Text` (works with both `TextMeshProUGUI` and 3D `TextMeshPro`).

| Method | Effect |
| :--- | :--- |
| `TweenTypewriter(duration)` | Reveals the characters one by one, from none to all. |
| `TweenMaxVisibleCharacters(from, to, duration)` | Same, between two explicit character counts. |
| `TweenNumber(from, to, duration)` | Counts a whole number up or down. |
| `TweenFontSize(to, duration)` | Animates the font size from its current value. |
| `TweenCharacterSpacing(to, duration)` | Animates the spacing from its current value. |

```csharp
dialogue.text = "The raven takes flight.";
dialogue.TweenTypewriter(1.5f).OnComplete(ShowContinueArrow);

score.TweenNumber(oldScore, newScore, 0.6f).Ease(Ease.OutCubic);
Raven.PunchScale(score.transform, Vector3.one * 0.2f, 0.3f);

title.TweenFontSize(72f, 0.4f).Ease(Ease.OutBack);
```

> [!NOTE]
> `TweenNumber` writes the text with TextMeshPro's own `SetText` formatter, which does not allocate a new string every frame the way `text = value.ToString()` would.

> [!TIP]
> `TweenTypewriter` counts characters when it starts. If you change the text afterwards, start a new typewriter tween.

---

## TMP Essential Resources

TextMeshPro needs its **Essential Resources** (default font and settings) to render text at all. Unity offers to import them the first time you add a TMP text object; you can also use **Window ▸ TextMeshPro ▸ Import TMP Essential Resources**. `TweenNumber` relies on them too.

---

#### ◀ **[Custom Tweens](Custom-Tweens)**  ·  Next: **[Control and Lifecycle ▶](Control-and-Lifecycle)**
