using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Adds play-mode preview buttons under the default RavenAnimator inspector.</summary>
    [CustomEditor(typeof(RavenAnimator))]
    public sealed class RavenAnimatorEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var animator = (RavenAnimator)target;
            Debug.Assert(animator != null, "Editor target must be a RavenAnimator.");
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play")) { animator.Play(); }
                if (GUILayout.Button("Stop")) { animator.Stop(); }
                if (GUILayout.Button("Complete")) { animator.CompleteNow(); }
                EditorGUILayout.EndHorizontal();
            }
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Preview buttons are available in Play Mode.", MessageType.None);
            }
        }
    }

    /// <summary>Adds play-mode preview buttons under the default RavenSequencePlayer inspector.</summary>
    [CustomEditor(typeof(RavenSequencePlayer))]
    public sealed class RavenSequencePlayerEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var player = (RavenSequencePlayer)target;
            Debug.Assert(player != null, "Editor target must be a RavenSequencePlayer.");
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play")) { player.Play(); }
                if (GUILayout.Button("Stop")) { player.Stop(); }
                if (GUILayout.Button("Complete")) { player.CompleteNow(); }
                EditorGUILayout.EndHorizontal();
            }
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Preview buttons are available in Play Mode.", MessageType.None);
            }
        }
    }
}
