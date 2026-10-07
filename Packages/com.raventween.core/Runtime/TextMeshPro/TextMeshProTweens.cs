using TMPro;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// TextMeshPro tweens. Color and alpha already work through the Graphic overloads
    /// (TMP_Text derives from Graphic); this adds text-specific properties.
    /// All setters are non-capturing, so these tweens allocate nothing.
    /// </summary>
    public static class TextMeshProTweens {
        /// <summary>Reveals characters one by one, from none to all.</summary>
        public static Tween TweenTypewriter(this TMP_Text target, float duration) {
            Debug.Assert(target != null, "Typewriter tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (target == null) { return default; }
            target.ForceMeshUpdate();
            int count = target.textInfo.characterCount;
            target.maxVisibleCharacters = 0;
            return Raven.Custom(target, 0f, count, duration, (text, value) => text.maxVisibleCharacters = Mathf.RoundToInt(value));
        }

        /// <summary>Tweens maxVisibleCharacters between two explicit counts.</summary>
        public static Tween TweenMaxVisibleCharacters(this TMP_Text target, int from, int to, float duration) {
            Debug.Assert(target != null, "MaxVisibleCharacters tween needs a live TMP_Text.");
            Debug.Assert(from >= 0 && to >= 0, "Character counts cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.maxVisibleCharacters = Mathf.RoundToInt(value));
        }

        /// <summary>Tweens the font size from its current value.</summary>
        public static Tween TweenFontSize(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "FontSize tween needs a live TMP_Text.");
            Debug.Assert(to > 0f, "Font size must be positive.");
            if (target == null) { return default; }
            return Raven.Custom(target, target.fontSize, to, duration, (text, value) => text.fontSize = value);
        }

        /// <summary>Tweens character spacing from its current value.</summary>
        public static Tween TweenCharacterSpacing(this TMP_Text target, float to, float duration) {
            Debug.Assert(target != null, "CharacterSpacing tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            if (target == null) { return default; }
            return Raven.Custom(target, target.characterSpacing, to, duration, (text, value) => text.characterSpacing = value);
        }

        /// <summary>Counts a whole number up or down using TMP's allocation-free SetText formatter.</summary>
        public static Tween TweenNumber(this TMP_Text target, float from, float to, float duration) {
            Debug.Assert(target != null, "Number tween needs a live TMP_Text.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return Raven.Custom(target, from, to, duration, (text, value) => text.SetText("{0:0}", value));
        }
    }
}
