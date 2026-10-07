using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    static class RavenPreviewBar {
        const float ButtonHeight = 24f;

        /// <summary>Shared Play / Stop / Complete toolbar used by both component inspectors.</summary>
        public static void Draw(System.Action play, System.Action stop, System.Action complete) {
            Debug.Assert(play != null && stop != null && complete != null, "Preview bar needs all three actions.");
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                EditorGUILayout.BeginHorizontal();
                if (Button(RavenEditorIcons.Play, "Play", "Restart and play every entry")) { play(); }
                if (Button(RavenEditorIcons.Stop, "Stop", "Stop where it is (fires OnKill)")) { stop(); }
                if (Button(RavenEditorIcons.Complete, "Complete", "Jump to the end values (fires OnComplete)")) { complete(); }
                EditorGUILayout.EndHorizontal();
            }
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Preview buttons are available in Play Mode.", MessageType.None);
            }
        }

        static bool Button(Texture icon, string label, string tooltip) {
            return GUILayout.Button(RavenEditorIcons.Content(icon, label, tooltip), GUILayout.Height(ButtonHeight));
        }
    }

    /// <summary>Adds play-mode preview buttons under the default RavenAnimator inspector.</summary>
    [CustomEditor(typeof(RavenAnimator))]
    public sealed class RavenAnimatorEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var animator = (RavenAnimator)target;
            Debug.Assert(animator != null, "Editor target must be a RavenAnimator.");
            RavenPreviewBar.Draw(animator.Play, animator.Stop, animator.CompleteNow);
        }
    }

    /// <summary>Adds play-mode preview buttons under the default RavenSequencePlayer inspector.</summary>
    [CustomEditor(typeof(RavenSequencePlayer))]
    public sealed class RavenSequencePlayerEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var player = (RavenSequencePlayer)target;
            Debug.Assert(player != null, "Editor target must be a RavenSequencePlayer.");
            RavenPreviewBar.Draw(player.Play, player.Stop, player.CompleteNow);
        }
    }
}
