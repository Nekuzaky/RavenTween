using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// A number that counts up or down on a TextMeshPro text: scores, coins, damage, timers.
    /// Call CountTo(value) from code or a UnityEvent; it counts from what is shown now. Formatting
    /// goes through TextMeshPro's allocation-free SetText.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Counter")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class RavenCounter : MonoBehaviour {
        [Tooltip("Value shown before anything counts.")]
        [SerializeField] float startValue;
        [Tooltip("When set, counts from Start Value to this value when the object is enabled.")]
        [SerializeField] bool countOnEnable;
        [SerializeField] float endValue = 100f;
        [SerializeField, Min(0f)] float duration = 0.8f;
        [SerializeField] Ease ease = Ease.OutCubic;
        [Tooltip("Digits after the decimal point.")]
        [SerializeField, Range(0, 4)] int decimals;
        [Tooltip("Text before the number, e.g. \"Score: \" or \"$\".")]
        [SerializeField] string prefix = "";
        [Tooltip("Text after the number, e.g. \" pts\" or \"%\".")]
        [SerializeField] string suffix = "";
        [Tooltip("A small punch when the count lands.")]
        [SerializeField] bool punchOnFinish = true;
        [Tooltip("Keep counting while the game is paused (Time.timeScale = 0).")]
        [SerializeField] bool useUnscaledTime;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        TMP_Text _text;
        Tween _tween;
        string _format;
        float _shown;
        float _target;

        /// <summary>The value on screen right now.</summary>
        public float Value { get { return _shown; } }
        public UnityEvent OnComplete { get { return onComplete; } }

        void Awake() {
            _text = GetComponent<TMP_Text>();
            BuildFormat();
            Write(startValue);
        }

        void OnEnable() {
            if (countOnEnable) {
                Write(startValue);
                CountTo(endValue);
            }
        }

        void OnDisable() {
            if (_tween.IsAlive) { _tween.Stop(); }
        }

        /// <summary>Counts from the value shown now to <paramref name="value"/>.</summary>
        public void CountTo(float value) {
            if (!isActiveAndEnabled) { SetValue(value); return; }
            if (_tween.IsAlive) { _tween.Stop(); }
            _target = value;
            _tween = Raven.Custom(this, _shown, value, duration, (counter, v) => counter.Write(v))
                .Ease(ease).UnscaledTime(useUnscaledTime)
                .OnComplete(this, counter => counter.Finish());
        }

        /// <summary>Adds <paramref name="amount"/> to the value being counted to (score += 50), even mid-count.</summary>
        public void Add(float amount) {
            float from = _tween.IsAlive ? _target : _shown;
            CountTo(from + amount);
        }

        /// <summary>Shows <paramref name="value"/> at once.</summary>
        public void SetValue(float value) {
            if (_tween.IsAlive) { _tween.Stop(); }
            if (_text == null) { _text = GetComponent<TMP_Text>(); BuildFormat(); }
            _target = value;
            Write(value);
        }

        void Write(float value) {
            _shown = value;
            _text.SetText(_format, value);
        }

        void Finish() {
            if (punchOnFinish) { Raven.PunchScale(transform, Vector3.one * 0.15f, 0.3f).UnscaledTime(useUnscaledTime); }
            onComplete.Invoke();
        }

        // TextMeshPro's SetText reads "{0:N}" as N decimals. Braces would start another
        // placeholder, so they are left out of the prefix and suffix.
        void BuildFormat() {
            _format = Clean(prefix) + "{0:" + decimals + "}" + Clean(suffix);
        }

        static string Clean(string text) {
            return string.IsNullOrEmpty(text) ? "" : text.Replace("{", "").Replace("}", "");
        }

        void OnValidate() {
            if (_text != null) { BuildFormat(); }
        }
    }
}
