using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>
    /// Reveals a TextMeshPro text letter by letter, with an optional typing sound: dialogue,
    /// tutorials, intros. Call Skip() to show everything at once (a "skip" button).
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Typewriter")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class RavenTypewriter : MonoBehaviour, IMotionTriggerable {
        const int AllCharacters = 99999;

        [SerializeField] MotionTrigger trigger = MotionTrigger.OnEnable;
        [Tooltip("Letters revealed per second.")]
        [SerializeField, Min(1f)] float lettersPerSecond = 40f;
        [Tooltip("Seconds before typing starts.")]
        [SerializeField, Min(0f)] float delay;
        [Tooltip("Optional typing sound, played every few letters.")]
        [SerializeField] AudioClip typingSound;
        [SerializeField] AudioSource soundSource;
        [Tooltip("Play the sound every this many letters.")]
        [SerializeField, Min(1)] int soundEvery = 2;
        [Tooltip("Keep typing while the game is paused (Time.timeScale = 0).")]
        [SerializeField] bool useUnscaledTime;
        [SerializeField] UnityEvent onComplete = new UnityEvent();

        TMP_Text _text;
        Tween _tween;
        int _visible = -1;

        public UnityEvent OnComplete { get { return onComplete; } }
        public bool IsTyping { get { return _tween.IsAlive; } }
        public float LettersPerSecond { get { return lettersPerSecond; } set { lettersPerSecond = Mathf.Max(value, 1f); } }

        void Awake() {
            _text = GetComponent<TMP_Text>();
            MotionTriggers.EnsurePointerRelay(gameObject, trigger);
        }

        void OnEnable() {
            if (trigger == MotionTrigger.OnEnable) { Play(); }
        }

        void Start() {
            if (trigger == MotionTrigger.OnStart) { Play(); }
        }

        void OnDisable() {
            if (_tween.IsAlive) { _tween.Stop(); }
            if (_text != null) { _text.maxVisibleCharacters = AllCharacters; }
        }

        /// <summary>Types the current text from the first letter.</summary>
        public void Play() {
            if (!isActiveAndEnabled) { return; }
            if (_tween.IsAlive) { _tween.Stop(); }
            _text.ForceMeshUpdate();
            // Before its first layout (no Canvas yet, just enabled) the text reports no letters:
            // the timing then comes from the raw text, and the count is read again every frame.
            int letters = _text.textInfo.characterCount;
            if (letters == 0 && _text.text != null) { letters = _text.text.Length; }
            _visible = -1;
            _text.maxVisibleCharacters = 0;
            float duration = letters / lettersPerSecond;
            _tween = Raven.Custom(this, 0f, 1f, duration, (writer, progress) => writer.Reveal(progress))
                .Ease(Ease.Linear).Delay(delay).UnscaledTime(useUnscaledTime)
                .OnComplete(this, writer => writer.Finish());
        }

        /// <summary>Replaces the text, then types it.</summary>
        public void Play(string text) {
            if (_text == null) { _text = GetComponent<TMP_Text>(); }
            _text.text = text;
            Play();
        }

        /// <summary>Shows the whole text at once and raises On Complete.</summary>
        public void Skip() {
            if (_tween.IsAlive) { _tween.Complete(); }
        }

        public void HandleTrigger(MotionTrigger started) {
            if (started == trigger) { Play(); }
        }

        void Reveal(float progress) {
            int letters = _text.textInfo.characterCount;
            if (letters == 0 && _text.text != null) { letters = _text.text.Length; }
            int count = Mathf.FloorToInt(progress * letters);
            if (count == _visible) { return; }
            int previous = Mathf.Max(_visible, 0);
            _visible = count;
            _text.maxVisibleCharacters = count;
            // Several letters can appear in one frame: tick whenever a multiple is crossed. No tick
            // on the last call, which is a Skip or the end.
            bool crossed = count / soundEvery > previous / soundEvery;
            if (typingSound != null && crossed && progress < 1f) { PlayTick(); }
        }

        void PlayTick() {
            if (soundSource != null) { soundSource.PlayOneShot(typingSound); }
            else { AudioSource.PlayClipAtPoint(typingSound, transform.position); }
        }

        void Finish() {
            _text.maxVisibleCharacters = AllCharacters;
            onComplete.Invoke();
        }
    }
}
