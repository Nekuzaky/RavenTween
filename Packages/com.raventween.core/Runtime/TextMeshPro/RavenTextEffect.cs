using TMPro;
using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// Animates the letters of a TextMeshPro text one by one: a wave, a shake, a wobble, a pulse
    /// or a rainbow, and letters that pop or fade in as they appear (with Raven Typewriter or any
    /// change to maxVisibleCharacters). Works on the mesh only: the text, its layout and its
    /// colors in the Inspector are never changed. No allocation while it plays.
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Text Effect")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class RavenTextEffect : MonoBehaviour {
        public enum Effect : byte {
            /// <summary>No continuous effect (letters can still appear with Appear).</summary>
            None = 0,
            /// <summary>Letters bob up and down in a wave.</summary>
            Wave = 1,
            /// <summary>Letters tremble nervously (anger, cold, fear).</summary>
            Shake = 2,
            /// <summary>Letters tilt left and right.</summary>
            Wobble = 3,
            /// <summary>Letters swell and shrink in turn.</summary>
            Pulse = 4,
            /// <summary>Colors flow through the letters.</summary>
            Rainbow = 5
        }

        public enum Appear : byte {
            /// <summary>Letters show at once.</summary>
            Instant = 0,
            /// <summary>Each letter fades in.</summary>
            Fade = 1,
            /// <summary>Each letter pops in with a small overshoot.</summary>
            Pop = 2,
            /// <summary>Each letter rises into place while fading in.</summary>
            Rise = 3
        }

        [SerializeField] Effect effect = Effect.Wave;
        [Tooltip("How strong the effect is, relative to the font size.")]
        [SerializeField, Range(0f, 2f)] float strength = 0.5f;
        [Tooltip("How fast it moves.")]
        [SerializeField, Range(0.1f, 5f)] float speed = 1f;
        [Tooltip("How much each letter is ahead of the previous one: 0 = all together.")]
        [SerializeField, Range(0f, 1f)] float spread = 0.3f;
        [Tooltip("How each letter appears when it becomes visible (pairs with Raven Typewriter).")]
        [SerializeField] Appear appear = Appear.Instant;
        [Tooltip("Seconds each letter takes to appear.")]
        [SerializeField, Min(0.01f)] float appearDuration = 0.25f;
        [Tooltip("First letter affected (0 = the first).")]
        [SerializeField, Min(0)] int fromLetter;
        [Tooltip("Letters affected after the first one (-1 = all the rest).")]
        [SerializeField, Min(-1)] int letterCount = -1;
        [Tooltip("Keep animating while the game is paused (Time.timeScale = 0).")]
        [SerializeField] bool useUnscaledTime = true;

        TMP_Text _text;
        Vector3[][] _vertices = new Vector3[0][];
        Color32[][] _colors = new Color32[0][];
        float[] _appeared = new float[0];
        int _lastChecksum = -1;
        bool _hasSnapshot;
        bool _applying;

        public Effect TextEffect { get { return effect; } set { effect = value; } }
        public Appear AppearStyle { get { return appear; } set { appear = value; } }
        public float Strength { get { return strength; } set { strength = Mathf.Max(value, 0f); } }

        void Awake() {
            _text = GetComponent<TMP_Text>();
        }

        void OnEnable() {
            if (_text == null) { _text = GetComponent<TMP_Text>(); }
            _text.OnPreRenderText += OnTextRebuilt;
            _hasSnapshot = false;
            _text.ForceMeshUpdate();
        }

        void OnDisable() {
            if (_text == null) { return; }
            _text.OnPreRenderText -= OnTextRebuilt;
            // Back to the text as TextMeshPro laid it out.
            if (_hasSnapshot) { Restore(); }
            _hasSnapshot = false;
        }

        // TextMeshPro rebuilt the mesh (new text, layout, visible count) and is about to upload
        // it: that is the new original, shaped in place before the upload.
        void OnTextRebuilt(TMP_TextInfo info) {
            if (_applying) { return; }
            Snapshot(info);
            Apply(info, false);
        }

        void LateUpdate() {
            if (!_hasSnapshot) { return; }
            Apply(_text.textInfo, true);
        }

        void Snapshot(TMP_TextInfo info) {
            // New words restart the appear animation; a longer typewriter reveal does not.
            int checksum = Checksum(info);
            if (checksum != _lastChecksum) {
                _lastChecksum = checksum;
                ResetAppearTimes(info.characterCount);
            }
            EnsureBuffers(info);
            for (int m = 0; m < info.meshInfo.Length && m < _vertices.Length; m++) {
                Vector3[] source = info.meshInfo[m].vertices;
                Color32[] colors = info.meshInfo[m].colors32;
                if (source != null) { System.Array.Copy(source, _vertices[m], Mathf.Min(source.Length, _vertices[m].Length)); }
                if (colors != null) { System.Array.Copy(colors, _colors[m], Mathf.Min(colors.Length, _colors[m].Length)); }
            }
            _hasSnapshot = true;
        }

        static int Checksum(TMP_TextInfo info) {
            int hash = info.characterCount;
            for (int i = 0; i < info.characterCount; i++) { hash = hash * 31 + info.characterInfo[i].character; }
            return hash;
        }

        // Buffers grow with the text and are reused: allocation only when the text gets longer.
        void EnsureBuffers(TMP_TextInfo info) {
            int meshes = info.meshInfo.Length;
            if (_vertices.Length != meshes) {
                _vertices = new Vector3[meshes][];
                _colors = new Color32[meshes][];
            }
            for (int m = 0; m < meshes; m++) {
                int size = info.meshInfo[m].vertices != null ? info.meshInfo[m].vertices.Length : 0;
                if (_vertices[m] == null || _vertices[m].Length < size) { _vertices[m] = new Vector3[size]; }
                if (_colors[m] == null || _colors[m].Length < size) { _colors[m] = new Color32[size]; }
            }
            if (_appeared.Length < info.characterCount) { ResetAppearTimes(info.characterCount); }
        }

        void ResetAppearTimes(int count) {
            if (_appeared.Length < count) { _appeared = new float[Mathf.Max(count, 16)]; }
            for (int i = 0; i < _appeared.Length; i++) { _appeared[i] = -1f; }
        }

        void Apply(TMP_TextInfo info, bool upload) {
            float now = useUnscaledTime ? Time.unscaledTime : Time.time;
            int visible = Mathf.Min(info.characterCount, _text.maxVisibleCharacters);
            int last = letterCount < 0 ? info.characterCount : Mathf.Min(info.characterCount, fromLetter + letterCount);
            for (int i = 0; i < info.characterCount; i++) {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) { continue; }
                int m = character.materialReferenceIndex;
                if (m >= _vertices.Length) { continue; }
                int v = character.vertexIndex;
                if (v + 3 >= _vertices[m].Length || v + 3 >= info.meshInfo[m].vertices.Length) { continue; }
                bool affected = i >= fromLetter && i < last;
                ShapeLetter(info.meshInfo[m].vertices, info.meshInfo[m].colors32, m, v, i, now, i < visible, affected);
            }
            if (!upload) { return; }
            _applying = true;
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            _applying = false;
        }

        void ShapeLetter(Vector3[] vertices, Color32[] colors, int m, int v, int index, float now, bool shown, bool affected) {
            Vector3[] origin = _vertices[m];
            Color32[] originColors = _colors[m];
            Vector3 center = (origin[v] + origin[v + 2]) * 0.5f;
            float size = _text.fontSize * 0.25f * strength;
            Vector3 offset = Vector3.zero;
            float angle = 0f;
            float scale = 1f;
            float alpha = 1f;
            if (affected) { Continuous(index, now, size, ref offset, ref angle, ref scale); }
            if (shown) { AppearAt(index, now, ref offset, ref scale, ref alpha); }
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int k = 0; k < 4; k++) {
                Vector3 local = origin[v + k] - center;
                vertices[v + k] = center + offset + rotation * (local * scale);
                Color32 c = originColors[v + k];
                if (affected && effect == Effect.Rainbow) { c = Rainbow(index, now, k, c.a); }
                c.a = (byte)(c.a * Mathf.Clamp01(alpha));
                colors[v + k] = c;
            }
        }

        void Continuous(int index, float now, float size, ref Vector3 offset, ref float angle, ref float scale) {
            float phase = now * speed * Mathf.PI * 2f - index * spread * Mathf.PI;
            switch (effect) {
                case Effect.Wave: offset.y = Mathf.Sin(phase) * size; break;
                case Effect.Shake:
                    float t = now * speed * 12f;
                    offset.x = (Mathf.PerlinNoise(t, index * 7.13f) - 0.5f) * size;
                    offset.y = (Mathf.PerlinNoise(index * 3.71f, t) - 0.5f) * size;
                    break;
                case Effect.Wobble: angle = Mathf.Sin(phase) * 12f * strength; break;
                case Effect.Pulse: scale = 1f + Mathf.Sin(phase) * 0.25f * strength; break;
            }
        }

        // A letter appears the first frame it is visible, then animates in over appearDuration.
        void AppearAt(int index, float now, ref Vector3 offset, ref float scale, ref float alpha) {
            if (appear == Appear.Instant || index >= _appeared.Length) { return; }
            if (_appeared[index] < 0f) { _appeared[index] = now; }
            float k = Mathf.Clamp01((now - _appeared[index]) / appearDuration);
            if (k >= 1f) { return; }
            alpha *= k;
            if (appear == Appear.Pop) { scale *= EaseUtility.Evaluate(Ease.OutBack, k); }
            else if (appear == Appear.Rise) { offset.y -= (1f - EaseUtility.Evaluate(Ease.OutCubic, k)) * _text.fontSize * 0.5f; }
        }

        Color32 Rainbow(int index, float now, int corner, byte alpha) {
            float hue = Mathf.Repeat(now * speed * 0.25f + index * spread * 0.1f + (corner >= 2 ? 0.02f : 0f), 1f);
            Color c = Color.HSVToRGB(hue, 0.75f, 1f);
            return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), alpha);
        }

        void Restore() {
            TMP_TextInfo info = _text.textInfo;
            for (int m = 0; m < info.meshInfo.Length && m < _vertices.Length; m++) {
                Vector3[] target = info.meshInfo[m].vertices;
                Color32[] colors = info.meshInfo[m].colors32;
                if (target != null) { System.Array.Copy(_vertices[m], target, Mathf.Min(target.Length, _vertices[m].Length)); }
                if (colors != null) { System.Array.Copy(_colors[m], colors, Mathf.Min(colors.Length, _colors[m].Length)); }
            }
            _applying = true;
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            _applying = false;
        }

        /// <summary>Plays the Appear animation again for every letter (e.g. when the text is shown again).</summary>
        public void Replay() {
            ResetAppearTimes(_appeared.Length);
        }
    }
}
