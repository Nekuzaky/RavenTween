using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Tween Template inspector: presets, value fields that match the animated property, a live
    /// easing curve, a timing summary, and a one-click preview on a scene object.
    /// </summary>
    [CustomEditor(typeof(TweenTemplate))]
    public sealed class TweenTemplateEditor : UnityEditor.Editor {
        const float CurveHeight = 110f;

        SerializedProperty _property, _materialProperty, _endValue, _useFrom, _fromValue;
        SerializedProperty _duration, _delay, _ease, _curve, _cycles, _mode, _unscaled;
        Object _previewTarget;
        bool _loopPreview;

        void OnEnable() {
            _property = serializedObject.FindProperty("property");
            _materialProperty = serializedObject.FindProperty("materialProperty");
            _endValue = serializedObject.FindProperty("endValue");
            _useFrom = serializedObject.FindProperty("useExplicitFrom");
            _fromValue = serializedObject.FindProperty("fromValue");
            SerializedProperty settings = serializedObject.FindProperty("settings");
            _duration = settings.FindPropertyRelative("duration");
            _delay = settings.FindPropertyRelative("startDelay");
            _ease = settings.FindPropertyRelative("ease");
            _curve = settings.FindPropertyRelative("customCurve");
            _cycles = settings.FindPropertyRelative("cycles");
            _mode = settings.FindPropertyRelative("cycleMode");
            _unscaled = settings.FindPropertyRelative("useUnscaledTime");
            EditorTweenPreview.Changed += Repaint;
        }

        void OnDisable() {
            EditorTweenPreview.Changed -= Repaint;
            if (EditorTweenPreview.Owner == (object)this) { EditorTweenPreview.End(); }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            DrawHeaderRow();
            EditorGUILayout.Space(4f);
            DrawPropertySection();
            EditorGUILayout.Space(6f);
            DrawTimingSection();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(6f);
            DrawPreviewSection();
        }

        PropertyKind Kind { get { return (PropertyKind)_property.intValue; } }

        // ----- Header & presets -----

        void DrawHeaderRow() {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Animation", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.DropdownButton(new GUIContent("Presets"), FocusType.Passive, GUILayout.Width(90f))) {
                ShowPresetMenu();
            }
            EditorGUILayout.EndHorizontal();
        }

        void ShowPresetMenu() {
            var menu = new GenericMenu();
            foreach (TemplatePresets.Preset preset in TemplatePresets.All) {
                TemplatePresets.Preset captured = preset;
                menu.AddItem(new GUIContent(preset.Menu), false, () => ApplyPreset(captured));
            }
            menu.ShowAsContext();
        }

        void ApplyPreset(TemplatePresets.Preset preset) {
            var template = (TweenTemplate)target;
            Undo.RecordObject(template, "Apply Tween Preset");
            preset.Apply(template);
            EditorUtility.SetDirty(template);
            serializedObject.Update();
        }

        // ----- Property & values -----

        void DrawPropertySection() {
            EditorGUILayout.PropertyField(_property, new GUIContent("Property"));
            if (Kind == PropertyKind.MaterialFloat || Kind == PropertyKind.MaterialColor) {
                EditorGUILayout.PropertyField(_materialProperty, new GUIContent("Shader Property", "Name of the shader property, e.g. _BaseColor."));
            }
            EditorGUILayout.HelpBox("Animates the " + TemplateTargets.TargetType(Kind).Name + " you play it on.", MessageType.None);
            DrawValueField(_endValue, "To");
            EditorGUILayout.PropertyField(_useFrom, new GUIContent("Start From Fixed Value", "Off: starts from the object's current value."));
            if (_useFrom.boolValue) { DrawValueField(_fromValue, "From"); }
        }

        // One serialized Vector4 shown as the field type the property actually uses.
        void DrawValueField(SerializedProperty raw, string label) {
            Vector4 v = raw.vector4Value;
            EditorGUI.BeginChangeCheck();
            switch (PropertyAccessor.KindOf(Kind)) {
                case ValueKind.Float: v.x = DrawFloat(label, v.x); break;
                case ValueKind.Vector2: v = EditorGUILayout.Vector2Field(label, v); break;
                case ValueKind.Quaternion: v = EditorGUILayout.Vector3Field(label + " (euler)", v); break;
                case ValueKind.Color: v = EditorGUILayout.ColorField(new GUIContent(label), v, true, true, Kind == PropertyKind.MaterialColor); break;
                default: v = EditorGUILayout.Vector3Field(label, v); break;
            }
            if (EditorGUI.EndChangeCheck()) { raw.vector4Value = v; }
        }

        float DrawFloat(string label, float value) {
            bool unit = Kind == PropertyKind.CanvasGroupAlpha || Kind == PropertyKind.GraphicAlpha || Kind == PropertyKind.AudioVolume;
            return unit ? EditorGUILayout.Slider(label, value, 0f, 1f) : EditorGUILayout.FloatField(label, value);
        }

        // ----- Timing & easing -----

        void DrawTimingSection() {
            GUILayout.Label("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_duration, new GUIContent("Duration (s)"));
            EditorGUILayout.PropertyField(_delay, new GUIContent("Start Delay (s)"));
            EditorGUILayout.PropertyField(_ease);
            var ease = (Ease)_ease.intValue;
            if (ease == Ease.Custom) { EditorGUILayout.PropertyField(_curve, new GUIContent("Custom Curve")); }
            Rect curveRect = GUILayoutUtility.GetRect(10f, CurveHeight, GUILayout.ExpandWidth(true));
            EaseCurveView.Draw(curveRect, ease, _curve.animationCurveValue, CurrentPlayhead());
            DrawCycles();
            EditorGUILayout.PropertyField(_unscaled, new GUIContent("Ignore Time Scale"));
            EditorGUILayout.LabelField(Summary(), EditorStyles.miniLabel);
        }

        void DrawCycles() {
            EditorGUILayout.BeginHorizontal();
            bool loop = _cycles.intValue < 0;
            bool newLoop = EditorGUILayout.ToggleLeft("Loop Forever", loop, GUILayout.Width(110f));
            if (newLoop != loop) { _cycles.intValue = newLoop ? -1 : 1; }
            using (new EditorGUI.DisabledScope(newLoop)) {
                int shown = Mathf.Max(_cycles.intValue, 1);
                int edited = EditorGUILayout.IntField("Cycles", shown);
                if (!newLoop && edited != shown) { _cycles.intValue = Mathf.Max(edited, 1); }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_mode, new GUIContent("Cycle Mode", "Restart jumps back each cycle; Yoyo plays back and forth."));
        }

        string Summary() {
            float duration = _duration.floatValue;
            float delay = _delay.floatValue;
            int cycles = _cycles.intValue;
            string mode = (CycleMode)_mode.intValue == CycleMode.Yoyo ? "yoyo" : "restart";
            if (cycles < 0) { return string.Format("{0:0.##} s per cycle, loops forever ({1})", duration, mode); }
            int count = Mathf.Max(cycles, 1);
            float total = delay + duration * count;
            return count == 1
                ? string.Format("Total {0:0.##} s", total)
                : string.Format("{0} × {1:0.##} s ({2}) — total {3:0.##} s", count, duration, mode, total);
        }

        // ----- Preview -----

        void DrawPreviewSection() {
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorGUILayout.HelpBox("Previewing is for Edit Mode. In Play Mode, use a Raven Animator or call template.Play(target).", MessageType.None);
                return;
            }
            AutoPickTarget();
            _previewTarget = EditorGUILayout.ObjectField("Target", _previewTarget, TemplateTargets.TargetType(Kind), true);
            bool mine = EditorTweenPreview.Owner == (object)this;
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!TemplateTargets.Accepts(_previewTarget, Kind))) {
                bool playing = mine && EditorTweenPreview.IsPlaying;
                if (GUILayout.Button(RavenEditorIcons.Content(playing ? RavenEditorIcons.Pause : RavenEditorIcons.Play, playing ? "Pause" : "Preview", "Play the animation on the target, then restore it"), GUILayout.Height(24f))) {
                    TogglePreview(mine, playing);
                }
            }
            using (new EditorGUI.DisabledScope(!mine)) {
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Stop, "Reset", "Stop and restore the target"), GUILayout.Height(24f))) {
                    EditorTweenPreview.End();
                }
            }
            _loopPreview = GUILayout.Toggle(_loopPreview, "Loop", "Button", GUILayout.Width(50f), GUILayout.Height(24f));
            EditorGUILayout.EndHorizontal();
            if (mine) { DrawScrubber(); }
        }

        void AutoPickTarget() {
            if (TemplateTargets.Accepts(_previewTarget, Kind)) { return; }
            _previewTarget = TemplateTargets.FindOn(Selection.activeGameObject, Kind);
        }

        void TogglePreview(bool mine, bool playing) {
            if (mine && playing) { EditorTweenPreview.Pause(); return; }
            if (mine) { EditorTweenPreview.Play(); return; }
            StartPreview();
        }

        void StartPreview() {
            var template = (TweenTemplate)target;
            EditorTweenPreview.End();
            int id = Kind == PropertyKind.MaterialFloat || Kind == PropertyKind.MaterialColor
                ? Shader.PropertyToID(template.materialProperty) : 0;
            EditorTweenPreview.Record(_previewTarget, Kind, id);
            Tween tween = template.Play(_previewTarget);
            if (!tween.IsAlive) { EditorTweenPreview.End(); return; }
            int cycles = template.LoopsForever ? template.CyclesInsideSequence : Mathf.Max(template.settings.cycles, 1);
            float length = template.settings.startDelay + template.settings.duration * cycles;
            EditorTweenPreview.Begin(this, tween, length, _loopPreview);
            EditorTweenPreview.Play();
        }

        void DrawScrubber() {
            EditorGUI.BeginChangeCheck();
            float t = EditorGUILayout.Slider("Time", EditorTweenPreview.Time, 0f, EditorTweenPreview.Length);
            if (EditorGUI.EndChangeCheck()) {
                EditorTweenPreview.Pause();
                EditorTweenPreview.Seek(t);
            }
        }

        float CurrentPlayhead() {
            if (EditorTweenPreview.Owner != (object)this) { return -1f; }
            float duration = Mathf.Max(_duration.floatValue, 0.0001f);
            float local = EditorTweenPreview.Time - _delay.floatValue;
            if (local < 0f) { return 0f; }
            int cycle = (int)(local / duration);
            float progress = Mathf.Clamp01((local - cycle * duration) / duration);
            bool backwards = (CycleMode)_mode.intValue == CycleMode.Yoyo && (cycle & 1) == 1;
            return backwards ? 1f - progress : progress;
        }
    }
}
