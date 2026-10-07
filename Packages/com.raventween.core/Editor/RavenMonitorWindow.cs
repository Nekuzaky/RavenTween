using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Live view of every running tween and sequence: target, property, progress,
    /// cycles, with per-row pause / complete / kill controls. Play Mode only.
    /// </summary>
    public sealed class RavenMonitorWindow : EditorWindow {
        readonly List<TweenEngine.DebugInfo> _snapshot = new List<TweenEngine.DebugInfo>(64);
        Vector2 _scroll;
        bool _showSequenceChildren;

        [MenuItem("Tools/RavenTween/Monitor")]
        public static void Open() {
            var window = GetWindow<RavenMonitorWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture>(
                "Packages/com.raventween.core/Editor/Icons/raventween_icon_small.png");
            window.titleContent = new GUIContent("Raven Monitor", icon);
            window.minSize = new Vector2(540f, 220f);
            window.Show();
        }

        [MenuItem("Tools/RavenTween/Documentation")]
        public static void OpenDocumentation() {
            Application.OpenURL("https://github.com/Nekuzaky/RavenTween");
        }

        void OnEnable() {
            EditorApplication.update += Repaint; // Keep rows live while playing.
        }

        void OnDisable() {
            EditorApplication.update -= Repaint;
        }

        void OnGUI() {
            DrawToolbar();
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Enter Play Mode to monitor live tweens.", MessageType.Info);
                return;
            }
            TweenEngine.CollectDebugInfo(_snapshot);
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            int shown = 0;
            for (int i = 0; i < _snapshot.Count; i++) {
                TweenEngine.DebugInfo info = _snapshot[i];
                if (info.OwnedBySequence && !_showSequenceChildren) { continue; }
                DrawRow(info);
                shown++;
            }
            if (shown == 0) {
                EditorGUILayout.LabelField("No live tweens.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int alive = Application.isPlaying ? Raven.AliveCount : 0;
            GUILayout.Label(RavenEditorIcons.Content(RavenEditorIcons.Activity, "Alive: " + alive, "Live tweens and sequences"),
                EditorStyles.toolbarButton, GUILayout.Width(95f));
            _showSequenceChildren = GUILayout.Toggle(_showSequenceChildren, "Sequence children", EditorStyles.toolbarButton, GUILayout.Width(130f));
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.CompleteAll, "Complete All", "Jump everything to its end values"),
                        EditorStyles.toolbarButton, GUILayout.Width(110f))) {
                    Raven.CompleteAll();
                }
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Stop, "Stop All", "Kill everything where it is"),
                        EditorStyles.toolbarButton, GUILayout.Width(85f))) {
                    Raven.StopAll();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        static void DrawHeader() {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Kind", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            GUILayout.Label("Target", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            GUILayout.Label("Property", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            GUILayout.Label("Progress", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label("Cycles", EditorStyles.miniBoldLabel, GUILayout.Width(55f));
            GUILayout.Space(100f);
            EditorGUILayout.EndHorizontal();
        }

        static void DrawRow(in TweenEngine.DebugInfo info) {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(KindLabel(info), GUILayout.Width(70f));
            DrawTargetField(info, 150f);
            GUILayout.Label(info.Property == PropertyKind.None ? "(value)" : info.Property.ToString(), GUILayout.Width(120f));
            DrawProgressBar(info);
            GUILayout.Label(CycleLabel(info), GUILayout.Width(55f));
            DrawControls(info);
            EditorGUILayout.EndHorizontal();
        }

        static string KindLabel(in TweenEngine.DebugInfo info) {
            if (info.IsSequence) { return info.OwnedBySequence ? "Seq └" : "Sequence"; }
            return info.OwnedBySequence ? "Tween └" : "Tween";
        }

        static void DrawTargetField(in TweenEngine.DebugInfo info, float width) {
            if (info.Target != null) {
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.ObjectField(info.Target, typeof(Object), true, GUILayout.Width(width));
                }
            } else {
                GUILayout.Label(info.IsSequence ? "(timeline)" : "(no target)", GUILayout.Width(width));
            }
        }

        static void DrawProgressBar(in TweenEngine.DebugInfo info) {
            float length = Mathf.Max(info.CycleLength, 0.0001f);
            float progress = Mathf.Clamp01(info.Elapsed / length);
            Rect rect = GUILayoutUtility.GetRect(60f, 16f, GUILayout.ExpandWidth(true));
            string label = info.Paused ? "paused" : (progress * 100f).ToString("0") + "%";
            EditorGUI.ProgressBar(rect, progress, label);
        }

        static string CycleLabel(in TweenEngine.DebugInfo info) {
            string total = info.Cycles < 0 ? "∞" : info.Cycles.ToString();
            return info.CyclesDone + "/" + total;
        }

        static void DrawControls(in TweenEngine.DebugInfo info) {
            using (new EditorGUI.DisabledScope(info.OwnedBySequence)) {
                Texture pauseIcon = info.Paused ? RavenEditorIcons.Play : RavenEditorIcons.Pause;
                string pauseTip = info.Paused ? "Resume" : "Pause";
                if (IconButton(pauseIcon, pauseTip, EditorStyles.miniButtonLeft)) {
                    TweenEngine.SetPaused(info.Index, info.Version, !info.Paused);
                }
                if (IconButton(RavenEditorIcons.Complete, "Complete: jump to the end value", EditorStyles.miniButtonMid)) {
                    TweenEngine.Kill(info.Index, info.Version, true);
                }
                if (IconButton(RavenEditorIcons.Kill, "Kill: stop where it is", EditorStyles.miniButtonRight)) {
                    TweenEngine.Kill(info.Index, info.Version, false);
                }
            }
        }

        static bool IconButton(Texture icon, string tooltip, GUIStyle style) {
            return GUILayout.Button(new GUIContent(icon, tooltip), style, GUILayout.Width(30f), GUILayout.Height(18f));
        }
    }
}
