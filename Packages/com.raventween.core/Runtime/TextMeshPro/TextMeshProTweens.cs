using TMPro;
using UnityEngine;

namespace RavenTween {
    public static class TextMeshProTweens {
        const int AllCharacters = 99999;

        public static Tween TweenTypewriter(this TMP_Text target, float duration) {
            Debug.Assert(target != null, "Typewriter tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (target == null) { return default; }
            return Raven.Custom(target, 0f, 1f, duration, (text, progress) => {
                text.maxVisibleCharacters = progress >= 1f
                    ? AllCharacters
                    : Mathf.FloorToInt(progress * text.textInfo.characterCount);
            });
        }

        public static Tween TweenMaxVisibleCharacters(this TMP_Text target, int from, int to, float duration) {
            Debug.Assert(target != null, "MaxVisibleCharacters tween needs a live TMP_Text.");
            Debug.Assert(from >= 0 && to >= 0, "Character counts cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.maxVisibleCharacters = Mathf.RoundToInt(value));
        }

        public static Tween TweenFontSize(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "FontSize tween needs a live TMP_Text.");
            Debug.Assert(to > 0f, "Font size must be positive.");
            if (target == null) { return default; }
            return Raven.CustomTo(target, text => text.fontSize, to, duration, (text, value) => text.fontSize = value);
        }

        public static Tween TweenCharacterSpacing(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "CharacterSpacing tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (target == null) { return default; }
            return Raven.CustomTo(target, text => text.characterSpacing, to, duration, (text, value) => text.characterSpacing = value);
        }

        public static Tween TweenNumber(this TMP_Text target, float from, float to, float duration) {
            Debug.Assert(target != null, "Number tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.SetText("{0:0}", value));
        }
    }
}
