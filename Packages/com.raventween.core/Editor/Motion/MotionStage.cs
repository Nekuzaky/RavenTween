using System;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    sealed class MotionStage : IDisposable {
        const float UnitPixels = 22f;
        const float CardSize = 30f;
        const float RestBetweenPlays = 0.6f;

        static readonly Color Background = new Color(0.13f, 0.13f, 0.15f);
        static readonly Color Floor = new Color(1f, 1f, 1f, 0.06f);

        GameObject _proxy;
        MotionTarget _target;
        MotionRest _rest;
        Sequence _sequence;
        MotionPreset _preset;
        MotionSettings _settings;
        float _length;
        bool _loop;
        double _started;

        public void Show(MotionPreset preset, MotionSettings settings) {
            settings.delay = 0f;
            settings.repeat = 1;
            if (_sequence.IsAlive && preset == _preset && Same(settings, _settings)) { return; }
            _preset = preset;
            _settings = settings;
            Rebuild();
        }

        static bool Same(in MotionSettings a, in MotionSettings b) {
            return Mathf.Approximately(a.duration, b.duration) && Mathf.Approximately(a.intensity, b.intensity)
                && Mathf.Approximately(a.distance, b.distance) && a.color == b.color;
        }

        void Rebuild() {
            Kill();
            EnsureProxy();
            _rest.Apply(_target, MotionChannels.All);
            if (_preset == MotionPreset.None) { return; }
            _sequence = MotionBuilder.Build(_target, _preset, _settings, _rest, out _);
            if (!_sequence.IsAlive) { return; }
            _sequence.Pause();
            _loop = MotionLibrary.Category(_preset) == MotionCategory.Loop;
            _length = Mathf.Max(_sequence.Duration, 0.05f);
            _started = EditorApplication.timeSinceStartup;
        }

        void EnsureProxy() {
            if (_proxy != null) { return; }
            _proxy = new GameObject("Raven Motion Stage") { hideFlags = HideFlags.HideAndDontSave };
            _proxy.AddComponent<SpriteRenderer>().color = Color.white;
            _target = MotionTarget.Resolve(_proxy.transform);
            _rest = MotionRest.Capture(_target);
        }

        public void Draw(Rect area, Color tint) {
            EditorGUI.DrawRect(area, Background);
            EditorGUI.DrawRect(new Rect(area.x, area.center.y + CardSize * 0.5f + 1f, area.width, 1f), Floor);
            if (!_sequence.IsAlive || _proxy == null) { return; }
            Sample();
            Transform t = _proxy.transform;
            Vector3 p = t.localPosition;
            Vector3 euler = t.localEulerAngles;
            Vector3 s = t.localScale;
            Color color = _target.Sprite.color;
            var center = new Vector2(area.center.x + p.x * UnitPixels, area.center.y - p.y * UnitPixels);
            float flip = Mathf.Cos(euler.y * Mathf.Deg2Rad);
            Matrix4x4 saved = GUI.matrix;
            GUI.BeginClip(area);
            Vector2 local = center - area.position;
            GUIUtility.RotateAroundPivot(-euler.z, local);
            GUIUtility.ScaleAroundPivot(new Vector2(s.x * flip, s.y), local);
            var card = new Rect(local.x - CardSize * 0.5f, local.y - CardSize * 0.5f, CardSize, CardSize);
            Color fill = new Color(tint.r * color.r, tint.g * color.g, tint.b * color.b, color.a);
            EditorGUI.DrawRect(card, fill);
            GUI.matrix = saved;
            GUI.EndClip();
        }

        void Sample() {
            float elapsed = (float)(EditorApplication.timeSinceStartup - _started);
            float cycle = _loop ? _length : _length + RestBetweenPlays;
            float time = Mathf.Min(elapsed % cycle, _length);
            _rest.Apply(_target, MotionChannels.All);
            TweenEngine.Seek(_sequence.Index, _sequence.Version, time);
        }

        void Kill() {
            if (_sequence.IsAlive) { _sequence.Stop(); }
            _sequence = default;
        }

        public void Dispose() {
            Kill();
            if (_proxy != null) { UnityEngine.Object.DestroyImmediate(_proxy); }
            _proxy = null;
        }
    }
}
