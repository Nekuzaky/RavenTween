using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Raven Show Hide inspector: the default fields, how to wire it, and previews.</summary>
    [CustomEditor(typeof(RavenShowHide))]
    public sealed class RavenShowHideEditor : UnityEditor.Editor {
        readonly List<Transform> _one = new List<Transform>(1);

        void OnDisable() {
            MotionPreview.Stop(this);
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var panel = (RavenShowHide)target;
            EditorGUILayout.HelpBox("Wire a button's On Click to RavenShowHide ▸ Show, Hide or Toggle.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying) {
                if (Button("Show")) { panel.Show(); }
                if (Button("Hide")) { panel.Hide(); }
                if (Button("Toggle")) { panel.Toggle(); }
            } else {
                if (Button("Preview Show")) { Preview(panel, true); }
                if (Button("Preview Hide")) { Preview(panel, false); }
                using (new EditorGUI.DisabledScope(!MotionPreview.IsPlaying(this))) {
                    if (Button("Reset")) { MotionPreview.Stop(this); }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void Preview(RavenShowHide panel, bool show) {
            var so = new SerializedObject(panel);
            string prefix = show ? "show" : "hide";
            var style = so.FindProperty(prefix + "Style").objectReferenceValue as MotionStyle;
            MotionPreset preset = style != null ? style.Preset : (MotionPreset)so.FindProperty(prefix + "Motion").intValue;
            MotionSettings settings = style != null ? style.Settings : ReadSettings(so.FindProperty(prefix + "Settings"));
            _one.Clear();
            _one.Add(panel.transform);
            MotionPreview.Play(this, _one, preset, settings, 0f);
        }

        static MotionSettings ReadSettings(SerializedProperty p) {
            return new MotionSettings {
                duration = p.FindPropertyRelative("duration").floatValue,
                intensity = p.FindPropertyRelative("intensity").floatValue,
                delay = p.FindPropertyRelative("delay").floatValue,
                distance = p.FindPropertyRelative("distance").floatValue,
                repeat = p.FindPropertyRelative("repeat").intValue,
                useUnscaledTime = p.FindPropertyRelative("useUnscaledTime").boolValue,
                color = p.FindPropertyRelative("color").colorValue
            };
        }

        static bool Button(string label) {
            return GUILayout.Button(label, GUILayout.Height(24f));
        }
    }
}
