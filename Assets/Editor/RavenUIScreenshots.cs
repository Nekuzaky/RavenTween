using System.IO;
using RavenTween;
using RavenTween.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace RavenTweenDemo {
    /// <summary>
    /// Self-driving capture of the package UI: opens the demo scene, selects objects,
    /// screenshots the Inspector and the Monitor window (in Play Mode), then exits.
    /// Launched via -executeMethod RavenTweenDemo.RavenUIScreenshots.Run.
    /// </summary>
    [InitializeOnLoad]
    public static class RavenUIScreenshots {
        const string StageKey = "RavenShots.Stage";
        const string Dir = "Screenshots";

        static double _next;
        static int _step;

        static RavenUIScreenshots() {
            if (SessionState.GetInt(StageKey, 0) == 2) {
                _step = 0;
                _next = EditorApplication.timeSinceStartup + 3.0;
                EditorApplication.update += PlayStage;
            }
        }

        public static void Run() {
            Directory.CreateDirectory(Dir);
            SessionState.SetInt(StageKey, 1);
            EditorSceneManager.OpenScene("Assets/Demo/RavenDemo.unity");
            _next = EditorApplication.timeSinceStartup + 4.0;
            _step = 0;
            EditorApplication.update += EditStage;
        }

        static void EditStage() {
            if (EditorApplication.timeSinceStartup < _next) { return; }
            _next = EditorApplication.timeSinceStartup + 1.5;
            _step++;
            switch (_step) {
                case 1:
                    Selection.activeObject = GameObject.Find("Pulsing Cube");
                    FocusInspector();
                    break;
                case 2:
                    Capture(GetInspector(), "inspector_raven_animator.png");
                    Selection.activeObject = GameObject.Find("Floating Sphere");
                    break;
                case 3:
                    Capture(GetInspector(), "inspector_sequence_player.png");
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<TweenTemplate>("Assets/Demo/PulseScale.asset");
                    break;
                case 4:
                    Capture(GetInspector(), "inspector_tween_template.png");
                    break;
                case 5:
                    RavenMonitorWindow.Open();
                    PositionMonitor();
                    break;
                case 6:
                    EditorApplication.update -= EditStage;
                    SessionState.SetInt(StageKey, 2);
                    EditorApplication.EnterPlaymode();
                    break;
                default:
                    break;
            }
        }

        static void PlayStage() {
            if (!EditorApplication.isPlaying) { return; }
            if (EditorApplication.timeSinceStartup < _next) { return; }
            _next = EditorApplication.timeSinceStartup + 2.0;
            _step++;
            switch (_step) {
                case 1:
                    RavenMonitorWindow.Open();
                    PositionMonitor();
                    break;
                case 2:
                    Capture(EditorWindow.GetWindow<RavenMonitorWindow>(), "monitor_play_mode.png");
                    break;
                case 3:
                    SessionState.SetInt(StageKey, 0);
                    EditorApplication.update -= PlayStage;
                    EditorApplication.Exit(0);
                    break;
                default:
                    break;
            }
        }

        static void PositionMonitor() {
            var window = EditorWindow.GetWindow<RavenMonitorWindow>();
            window.position = new Rect(300f, 240f, 900f, 360f);
            window.Focus();
        }

        static EditorWindow GetInspector() {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            return EditorWindow.GetWindow(type);
        }

        static void FocusInspector() {
            EditorWindow inspector = GetInspector();
            inspector.Focus();
        }

        static void Capture(EditorWindow window, string fileName) {
            if (window == null) { return; }
            window.Focus();
            window.Repaint();
            Rect rect = window.position;
            int width = (int)rect.width;
            int height = (int)rect.height;
            Color[] pixels = InternalEditorUtility.ReadScreenPixel(new Vector2(rect.x, rect.y), width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.SetPixels(pixels);
            File.WriteAllBytes(Path.Combine(Dir, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log("Captured " + fileName);
        }
    }
}
