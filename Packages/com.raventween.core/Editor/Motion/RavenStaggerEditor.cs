using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Raven Stagger inspector: the default fields plus a cascade preview in the scene.</summary>
    [CustomEditor(typeof(RavenStagger))]
    public sealed class RavenStaggerEditor : UnityEditor.Editor {
        readonly List<Transform> _children = new List<Transform>(16);

        void OnDisable() {
            MotionPreview.Stop(this);
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var stagger = (RavenStagger)target;
            if (stagger.EffectivePreset != MotionPreset.None) {
                EditorGUILayout.LabelField(MotionLibrary.Description(stagger.EffectivePreset), EditorStyles.wordWrappedMiniLabel);
            }
            if (stagger.transform.childCount == 0) {
                EditorGUILayout.HelpBox("Raven Stagger animates the children of this object. Add some children first.", MessageType.Info);
            }
            if (MotionLibrary.Category(stagger.EffectivePreset) == MotionCategory.Loop) {
                EditorGUILayout.HelpBox("Loops start one after another and then keep going: a wave. The preview shows the first child.", MessageType.None);
            }
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying) {
                if (Button(RavenEditorIcons.Play, "Play")) { stagger.Play(); }
                if (Button(RavenEditorIcons.Stop, "Stop")) { stagger.Stop(); }
            } else {
                if (Button(RavenEditorIcons.Play, MotionPreview.IsPlaying(this) ? "Replay" : "Preview")) { Preview(stagger); }
                using (new EditorGUI.DisabledScope(!MotionPreview.IsPlaying(this))) {
                    if (Button(RavenEditorIcons.Stop, "Reset")) { MotionPreview.Stop(this); }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void Preview(RavenStagger stagger) {
            _children.Clear();
            Transform root = stagger.transform;
            for (int i = 0; i < root.childCount; i++) {
                Transform child = root.GetChild(i);
                if (child.gameObject.activeInHierarchy) { _children.Add(child); }
            }
            MotionPreview.Play(this, _children, stagger.EffectivePreset, stagger.EffectiveSettings, stagger.Interval);
        }

        static bool Button(Texture icon, string label) {
            return GUILayout.Button(RavenEditorIcons.Content(icon, label, label), GUILayout.Height(24f));
        }
    }
}
