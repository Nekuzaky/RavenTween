using System.IO;
using RavenTween;
using RavenTween.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace RavenTweenDemo {
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
                    EditorWindow.GetWindow<RavenSequenceEditorWindow>().Close();
                    _motionObject = BuildMotionObject();
                    Selection.activeObject = _motionObject;
                    break;
                case 5:
                    Capture(GetInspector(), "designer_motion_inspector.png");
                    MotionLibraryWindow.Open();
                    EditorWindow.GetWindow<MotionLibraryWindow>().position = new Rect(600f, 60f, 560f, 760f);
                    break;
                case 6:
                    Capture(EditorWindow.GetWindow<MotionLibraryWindow>(), "designer_motion_library.png");
                    break;
                case 7:
                    EditorWindow.GetWindow<MotionLibraryWindow>().Close();
                    RestoreInspector();
                    EditorApplication.update -= DesignerStage;
                    EditorApplication.Exit(0);
                    break;
                default:
                    break;
            }
        }

        static GameObject[] _shots;

        public static void RunInspectors() {
            Directory.CreateDirectory(Dir);
            EditorSceneManager.OpenScene("Assets/Demo/RavenDemo.unity");
            _shots = new[] { BuildFeedbackObject(), BuildShowHideObject(), BuildStaggerObject(), BuildPathObject(), BuildMotionObject() };
            _next = EditorApplication.timeSinceStartup + 4.0;
            _step = 0;
            EditorApplication.update += InspectorStage;
        }

        static readonly string[] ShotNames = { "inspector_feedback.png", "inspector_show_hide.png", "inspector_stagger.png", "inspector_path.png" };

        static void InspectorStage() {
            if (EditorApplication.timeSinceStartup < _next) { return; }
            _next = EditorApplication.timeSinceStartup + 1.5;
            _step++;
            int shot = _step - 2;
            if (_step == 1) {
                PlaceInspector(new Rect(80f, 60f, 470f, 900f));
                Selection.activeObject = _shots[0];
            } else if (shot < ShotNames.Length) {
                Capture(GetInspector(), ShotNames[shot]);
                Selection.activeObject = _shots[shot + 1];
            } else if (shot == ShotNames.Length) {
                MotionLibraryWindow.Open();
                var library = EditorWindow.GetWindow<MotionLibraryWindow>();
                library.position = new Rect(600f, 60f, 560f, 820f);
                var selected = typeof(MotionLibraryWindow).GetField("_selected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (selected != null) { selected.SetValue(library, MotionPreset.Tada); }
            } else if (shot == ShotNames.Length + 1) {
                Capture(EditorWindow.GetWindow<MotionLibraryWindow>(), "motion_library_selected.png");
            } else {
                EditorWindow.GetWindow<MotionLibraryWindow>().Close();
                RestoreInspector();
                EditorApplication.update -= InspectorStage;
                EditorApplication.Exit(0);
            }
        }

        static void Fold(GameObject go) {
            foreach (Component component in go.GetComponents<Component>()) {
                if (component is Transform || component is MeshFilter || component is Renderer || component is CanvasRenderer
                    || component is UnityEngine.UI.Image || component is CanvasGroup || component is Collider || component is UnityEngine.UI.LayoutGroup) {
                    InternalEditorUtility.SetIsInspectorExpanded(component, false);
                }
            }
        }

        static GameObject BuildFeedbackObject() {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(2.5f, 0f, 0f);
            var feedback = enemy.AddComponent<RavenFeedback>();
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.Punch, target = enemy.transform, strength = 1.2f, duration = 0.35f });
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.ColorFlash, target = enemy.transform, color = Color.white, strength = 0.9f, duration = 0.25f });
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.CameraShake, camera = Camera.main, strength = 0.4f, duration = 0.3f });
            feedback.Actions.Add(new FeedbackAction { kind = FeedbackAction.Kind.HitStop, timeScale = 0.05f, duration = 0.08f });
            feedback.Cooldown = 0.1f;
            Fold(enemy);
            Expand(feedback, "actions", 1);
            return enemy;
        }

        static void Expand(Object target, string list, int elements) {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(list);
            if (property == null) { return; }
            property.isExpanded = true;
            for (int i = 0; i < property.arraySize; i++) { property.GetArrayElementAtIndex(i).isExpanded = i < elements; }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform _canvas;

        static RectTransform Canvas() {
            if (_canvas != null) { return _canvas; }
            var go = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas = (RectTransform)go.transform;
            return _canvas;
        }

        static GameObject Panel(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject BuildShowHideObject() {
            GameObject pause = Panel("Pause Menu", Canvas());
            var showHide = pause.AddComponent<RavenShowHide>();
            showHide.ShowMotion = MotionPreset.PopIn;
            showHide.HideMotion = MotionPreset.ZoomOut;
            Fold(pause);
            return pause;
        }

        static GameObject BuildStaggerObject() {
            var menu = new GameObject("Main Menu", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
            menu.transform.SetParent(Canvas(), false);
            string[] items = { "Continue", "New Game", "Options", "Quit" };
            for (int i = 0; i < items.Length; i++) { Panel(items[i], menu.transform); }
            var stagger = menu.AddComponent<RavenStagger>();
            stagger.Preset = MotionPreset.SlideInLeft;
            stagger.Interval = 0.06f;
            Fold(menu);
            return menu;
        }

        static GameObject BuildPathObject() {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Moving Platform";
            platform.transform.position = new Vector3(-3f, -1f, 0f);
            platform.transform.localScale = new Vector3(2f, 0.3f, 1f);
            var path = platform.AddComponent<RavenPath>();
            path.Points.Clear();
            path.Points.Add(new Vector3(2f, 1.5f, 0f));
            path.Points.Add(new Vector3(4f, 0f, 0f));
            path.Looping = RavenPath.PathLoop.BackAndForth;
            Fold(platform);
            Expand(path, "points", 0);
            return platform;
        }

        static GameObject _motionObject;

        static GameObject BuildMotionObject() {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Gold Coin";
            coin.transform.position = new Vector3(0f, 1.5f, 0f);
            Object.DestroyImmediate(coin.GetComponent<Collider>());
            InternalEditorUtility.SetIsInspectorExpanded(coin.GetComponent<MeshFilter>(), false);
            InternalEditorUtility.SetIsInspectorExpanded(coin.GetComponent<MeshRenderer>(), false);
            var motion = coin.AddComponent<RavenMotion>();
            motion.Preset = MotionPreset.Float;
            return coin;
        }

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

        static Rect _inspectorHome;

        static void RestoreInspector() {
            if (_inspectorHome.width > 0f) { GetInspector().position = _inspectorHome; }
        }

        static void PlaceInspector(Rect rect) {
            EditorWindow inspector = GetInspector();
            if (_inspectorHome.width <= 0f) { _inspectorHome = inspector.position; }
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
