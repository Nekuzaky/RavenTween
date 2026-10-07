using TMPro;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// TextMeshPro tweens. Color and alpha already work through the Graphic overloads
    /// (TMP_Text derives from Graphic); this adds text-specific properties.
    /// All setters are non-capturing, so these tweens allocate nothing.
    /// </summary>
    public static class TextMeshProTweens {
        const int AllCharacters = 99999;

        /// <summary>
        /// Reveals the characters one by one. The character count is read every frame, so the
        /// text can change (or the object can still be inactive) when the tween is created; at
        /// the end every character is visible, whatever the text has become.
        /// </summary>
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

        /// <summary>Tweens maxVisibleCharacters between two explicit counts.</summary>
        public static Tween TweenMaxVisibleCharacters(this TMP_Text target, int from, int to, float duration) {
            Debug.Assert(target != null, "MaxVisibleCharacters tween needs a live TMP_Text.");
            Debug.Assert(from >= 0 && to >= 0, "Character counts cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.maxVisibleCharacters = Mathf.RoundToInt(value));
        }

        /// <summary>Tweens the font size from the size it has when the tween starts.</summary>
        public static Tween TweenFontSize(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "FontSize tween needs a live TMP_Text.");
            Debug.Assert(to > 0f, "Font size must be positive.");
            if (target == null) { return default; }
            return Raven.CustomTo(target, text => text.fontSize, to, duration, (text, value) => text.fontSize = value);
        }

        /// <summary>Tweens character spacing from the spacing it has when the tween starts.</summary>
        public static Tween TweenCharacterSpacing(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "CharacterSpacing tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (target == null) { return default; }
            return Raven.CustomTo(target, text => text.characterSpacing, to, duration, (text, value) => text.characterSpacing = value);
        }

        /// <summary>Counts a whole number up or down using TMP's allocation-free SetText formatter.</summary>
        public static Tween TweenNumber(this TMP_Text target, float from, float to, float duration) {
            Debug.Assert(target != null, "Number tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.SetText("{0:0}", value));
        }
    }
}
