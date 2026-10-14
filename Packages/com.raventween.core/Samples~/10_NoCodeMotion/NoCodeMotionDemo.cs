using RavenTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Everything that moves is driven by the no-code components — Raven Motion, Raven Stagger
    /// and Raven Button Feel — added here by script only so the sample needs no scene file.
    /// In your project, add the same components from the Inspector or the Motion Library.
    /// </summary>
    public sealed class NoCodeMotionDemo : MonoBehaviour {
        static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.16f, 0.96f);
        static readonly Color ButtonColor = new Color(0.24f, 0.46f, 0.95f);

        Font _font;

        void Start() {
            _font = Resources.GetBuiltinResource<Font>(BuiltinFontName());
            EnsureEventSystem();
            BuildCoin();
            Transform canvas = BuildCanvas();
            Text title = MakeText(canvas, "Title", "No code, just motion", 40, new Vector2(0f, 220f));
            // The title rises in when the scene starts.
            AddMotion(title.gameObject, MotionPreset.RiseIn, MotionTrigger.OnEnable);
            GameObject dialog = BuildDialog(canvas);
            BuildMenu(canvas, dialog);
        }

        // A coin that floats and spins forever: two loops on different channels.
        void BuildCoin() {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin";
            coin.transform.position = new Vector3(3.2f, 0.5f, 0f);
            coin.transform.localScale = new Vector3(1f, 0.08f, 1f);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var pivot = new GameObject("Coin Pivot").transform;
            pivot.position = coin.transform.position;
            coin.transform.SetParent(pivot, true);
            AddMotion(pivot.gameObject, MotionPreset.Float, MotionTrigger.OnEnable);
            AddMotion(pivot.gameObject, MotionPreset.Spin, MotionTrigger.OnEnable);
            Camera main = Camera.main;
            if (main != null) { main.transform.position = new Vector3(0f, 0f, -10f); }
        }

        // Three buttons that cascade in, feel springy, and celebrate when clicked.
        void BuildMenu(Transform canvas, GameObject dialog) {
            var menu = new GameObject("Menu", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)menu.transform;
            rect.SetParent(canvas, false);
            rect.sizeDelta = new Vector2(260f, 220f);
            rect.anchoredPosition = new Vector2(0f, 20f);
            var layout = menu.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            menu.SetActive(false); // Raven Stagger plays when the menu is enabled below.
            var stagger = menu.AddComponent<RavenStagger>();
            stagger.Preset = MotionPreset.PopIn;
            stagger.Interval = 0.08f;
            Button play = MakeButton(rect, "Play");
            AddMotion(play.gameObject, MotionPreset.Tada, MotionTrigger.OnClick);
            Button options = MakeButton(rect, "Options");
            AddMotion(options.gameObject, MotionPreset.Jello, MotionTrigger.OnClick);
            Button about = MakeButton(rect, "About");
            about.onClick.AddListener(() => dialog.SetActive(true));
            menu.SetActive(true);
        }

        // A dialog that slides in when shown and slides out (then hides) when closed.
        GameObject BuildDialog(Transform canvas) {
            var dialog = new GameObject("About Dialog", typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)dialog.transform;
            rect.SetParent(canvas, false);
            rect.sizeDelta = new Vector2(420f, 240f);
            rect.anchoredPosition = new Vector2(0f, -160f);
            dialog.GetComponent<Image>().color = PanelColor;
            MakeText(rect, "Text", "Raven Motion · Raven Stagger\nRaven Button Feel", 22, new Vector2(0f, 40f));
            Button close = MakeButton(rect, "Close");
            ((RectTransform)close.transform).anchoredPosition = new Vector2(0f, -70f);
            AddMotion(dialog, MotionPreset.SlideInUp, MotionTrigger.OnEnable);
            RavenMotion exit = AddMotion(dialog, MotionPreset.SlideOutDown, MotionTrigger.Manual);
            exit.WhenFinished = MotionFinish.Deactivate;
            close.onClick.AddListener(exit.Play);
            dialog.SetActive(false);
            return dialog;
        }

        static RavenMotion AddMotion(GameObject go, MotionPreset preset, MotionTrigger trigger) {
            bool wasActive = go.activeSelf;
            go.SetActive(false); // Configure before OnEnable runs.
            var motion = go.AddComponent<RavenMotion>();
            motion.Preset = preset;
            motion.Trigger = trigger;
            go.SetActive(wasActive);
            return motion;
        }

        Button MakeButton(Transform parent, string label) {
            var go = new GameObject(label, typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(220f, 52f);
            go.GetComponent<Image>().color = ButtonColor;
            go.AddComponent<RavenButtonFeel>();
            Text text = MakeText(rect, "Label", label, 24, Vector2.zero);
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        Text MakeText(Transform parent, string name, string content, int size, Vector2 position) {
            var go = new GameObject(name, typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(600f, 80f);
            rect.anchoredPosition = position;
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        static Transform BuildCanvas() {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            return go.transform;
        }

        static void EnsureEventSystem() {
            if (FindEventSystem() != null) { return; }
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static EventSystem FindEventSystem() {
#if UNITY_2022_2_OR_NEWER
            return FindAnyObjectByType<EventSystem>();
#else
            return FindObjectOfType<EventSystem>();
#endif
        }

        static string BuiltinFontName() {
#if UNITY_2022_2_OR_NEWER
            return "LegacyRuntime.ttf";
#else
            return "Arial.ttf";
#endif
        }
    }
}
