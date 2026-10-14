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

        // ----- Designer workflow captures (no Play Mode) -----
        // Launched via -executeMethod RavenTweenDemo.RavenUIScreenshots.RunDesigner.

        static RavenSequencePlayer _showcase;

        public static void RunDesigner() {
            Directory.CreateDirectory(Dir);
            EditorSceneManager.OpenScene("Assets/Demo/RavenDemo.unity");
            _showcase = BuildShowcase();
            _next = EditorApplication.timeSinceStartup + 4.0;
            _step = 0;
            EditorApplication.update += DesignerStage;
        }

        static void DesignerStage() {
            if (EditorApplication.timeSinceStartup < _next) { return; }
            _next = EditorApplication.timeSinceStartup + 1.5;
            _step++;
            switch (_step) {
                case 1:
                    PlaceInspector(new Rect(80f, 80f, 470f, 640f));
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<TweenTemplate>("Assets/Demo/PulseScale.asset");
                    break;
                case 2:
                    Capture(GetInspector(), "designer_template.png");
                    Selection.activeObject = _showcase.gameObject;
                    break;
                case 3:
                    Capture(GetInspector(), "designer_sequence_player.png");
                    RavenSequenceEditorWindow.Open(_showcase);
                    EditorWindow.GetWindow<RavenSequenceEditorWindow>().position = new Rect(120f, 160f, 1000f, 330f);
                    break;
                case 4:
                    Capture(EditorWindow.GetWindow<RavenSequenceEditorWindow>(), "designer_sequence_editor.png");
                    break;
                case 5:
                    EditorApplication.update -= DesignerStage;
                    EditorApplication.Exit(0);
                    break;
                default:
                    break;
            }
        }

        // A five-step choreography built in memory (never saved), so the timeline has something to show.
        static RavenSequencePlayer BuildShowcase() {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Reward Chest";
            crate.transform.position = new Vector3(-2.5f, 0f, 0f);
            var player = crate.AddComponent<RavenSequencePlayer>();
            Transform t = crate.transform;
            AddStep(player, RavenSequencePlayer.StepMode.Chain, 0f, t, Template("Rise", PropertyKind.LocalPositionY, new Vector4(1.2f, 0f, 0f, 0f), 0.6f, Ease.OutBack));
            AddStep(player, RavenSequencePlayer.StepMode.Group, 0f, t, Template("Spin", PropertyKind.LocalEulerAngles, new Vector4(0f, 360f, 0f, 0f), 0.9f, Ease.InOutCubic));
            AddStep(player, RavenSequencePlayer.StepMode.Insert, 0.35f, t, Template("Pop", PropertyKind.LocalScale, new Vector4(1.4f, 1.4f, 1.4f, 0f), 0.3f, Ease.OutQuad, 2, CycleMode.Yoyo));
            AddStep(player, RavenSequencePlayer.StepMode.Chain, 0f, t, Template("Hover", PropertyKind.LocalPositionY, new Vector4(1.5f, 0f, 0f, 0f), 0.5f, Ease.InOutSine, 2, CycleMode.Yoyo));
            AddStep(player, RavenSequencePlayer.StepMode.Chain, 0f, t, Template("Land", PropertyKind.LocalPositionY, Vector4.zero, 0.45f, Ease.OutBounce));
            return player;
        }

        static TweenTemplate Template(string name, PropertyKind property, Vector4 end, float duration, Ease ease, int cycles = 1, CycleMode mode = CycleMode.Restart) {
            TweenTemplate template = ScriptableObject.CreateInstance<TweenTemplate>();
            template.name = name;
            template.property = property;
            template.endValue = end;
            template.settings = TweenParams.Default;
            template.settings.duration = duration;
            template.settings.ease = ease;
            template.settings.cycles = cycles;
            template.settings.cycleMode = mode;
            return template;
        }

        static void AddStep(RavenSequencePlayer player, RavenSequencePlayer.StepMode mode, float insertTime, Object target, TweenTemplate template) {
            player.Steps.Add(new RavenSequencePlayer.Step { mode = mode, insertTime = insertTime, target = target, template = template });
        }

        static void PlaceInspector(Rect rect) {
            EditorWindow inspector = GetInspector();
            inspector.position = rect;
            inspector.Focus();
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
