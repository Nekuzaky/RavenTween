using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.Editor {
    /// <summary>
    /// Every motion on one page. Select objects in the scene, click a motion to watch it play on
    /// them (outside Play Mode, nothing is changed), then add it as a Raven Motion in one click.
    /// </summary>
    public sealed class MotionLibraryWindow : EditorWindow {
        const float ButtonHeight = 30f;
        const float MinButtonWidth = 132f;

        static readonly Color EntranceColor = new Color(0.36f, 0.78f, 0.47f);
        static readonly Color ExitColor = new Color(0.9f, 0.42f, 0.42f);
        static readonly Color AttentionColor = new Color(0.96f, 0.7f, 0.25f);
        static readonly Color LoopColor = new Color(0.4f, 0.62f, 0.98f);

        static readonly GUIContent[] TriggerLabels = {
            new GUIContent("When enabled"), new GUIContent("When the scene starts"), new GUIContent("On click"),
            new GUIContent("On hover"), new GUIContent("When the pointer leaves"), new GUIContent("On press"),
            new GUIContent("On release"), new GUIContent("When something enters (trigger)"),
            new GUIContent("When something leaves (trigger)"), new GUIContent("On collision"),
            new GUIContent("Manually (event or script)")
        };

        readonly List<Transform> _targets = new List<Transform>(8);
        MotionPreset _selected = MotionPreset.PopIn;
        MotionTrigger _trigger = MotionTrigger.OnEnable;
        float _intensity = 1f;
        float _duration;
        float _interval = 0.06f;
        string _search = "";
        Vector2 _scroll;
        GUIStyle _tile;
        GUIStyle _header;

        [MenuItem("Tools/RavenTween/Motion Library", false, 0)]
        public static void Open() {
            var window = GetWindow<MotionLibraryWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.raventween.core/Editor/Icons/icon_animator.png");
            window.titleContent = new GUIContent("Motion Library", icon);
            window.minSize = new Vector2(360f, 420f);
            window.Show();
        }

        [MenuItem("GameObject/RavenTween/Add Motion…", false, 10)]
        static void OpenFromHierarchy() { Open(); }

        void OnEnable() {
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDisable() {
            Selection.selectionChanged -= OnSelectionChanged;
            MotionPreview.Stop(this);
        }

        void OnSelectionChanged() {
            MotionPreview.Stop(this);
            Repaint();
        }

        void OnGUI() {
            EnsureStyles();
            CollectTargets();
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCategory(MotionCategory.Entrance, "Entrances", "Make something appear.", EntranceColor);
            DrawCategory(MotionCategory.Attention, "Attention", "Make something noticed, then settle back.", AttentionColor);
            DrawCategory(MotionCategory.Loop, "Loops", "Keep something alive while it is on screen.", LoopColor);
            DrawCategory(MotionCategory.Exit, "Exits", "Make something disappear.", ExitColor);
            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        // ----- Header -----

        void DrawHeader() {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Pick a motion: it plays on the selected objects, right in the scene. Nothing is changed until you add it.", EditorStyles.wordWrappedMiniLabel);
            if (_targets.Count == 0) {
                EditorGUILayout.HelpBox("Select one or more objects in the Hierarchy to try motions on them.", MessageType.Info);
            } else {
                string names = _targets[0].name + (_targets.Count > 1 ? "  (+" + (_targets.Count - 1) + ")" : "");
                EditorGUILayout.LabelField("Trying on", names, EditorStyles.boldLabel);
            }
            using (new EditorGUI.DisabledScope(_targets.Count == 0)) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Quick add", EditorStyles.miniBoldLabel, GUILayout.Width(62f));
                if (GUILayout.Button(new GUIContent("Button Feel", "Grows on hover, sinks when pressed, springs back. For any button."), EditorStyles.miniButtonLeft)) { AddButtonFeel(); }
                if (GUILayout.Button(new GUIContent("Idle Float", "A gentle float that never stops. Pickups, menus, collectibles."), EditorStyles.miniButtonMid)) { QuickMotion(MotionPreset.Float, MotionTrigger.OnEnable); }
                if (GUILayout.Button(new GUIContent("Pop In On Show", "Pops in every time the object is enabled."), EditorStyles.miniButtonRight)) { QuickMotion(MotionPreset.PopIn, MotionTrigger.OnEnable); }
                EditorGUILayout.EndHorizontal();
            }
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space(4f);
        }

        // ----- Grid -----

        void DrawCategory(MotionCategory category, string title, string subtitle, Color color) {
            var presets = new List<MotionPreset>(16);
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                if (MotionLibrary.Category(preset) == category && Matches(preset)) { presets.Add(preset); }
            }
            if (presets.Count == 0) { return; }
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + 3f, 4f, headerRect.height - 6f), color);
            GUI.Label(new Rect(headerRect.x + 10f, headerRect.y, headerRect.width - 10f, headerRect.height), title + "   <size=10><color=#999999>" + subtitle + "</color></size>", _header);
            int columns = Mathf.Max(2, Mathf.FloorToInt((position.width - 24f) / MinButtonWidth));
            for (int start = 0; start < presets.Count; start += columns) {
                EditorGUILayout.BeginHorizontal();
                for (int i = start; i < start + columns; i++) {
                    if (i < presets.Count) { DrawTile(presets[i], color); }
                    else { GUILayout.FlexibleSpace(); }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(8f);
        }

        void DrawTile(MotionPreset preset, Color color) {
            Color previous = GUI.backgroundColor;
            if (preset == _selected) { GUI.backgroundColor = color; }
            var content = new GUIContent(MotionLibrary.DisplayName(preset), MotionLibrary.Description(preset));
            if (GUILayout.Button(content, _tile, GUILayout.Height(ButtonHeight), GUILayout.MinWidth(60f))) { Select(preset); }
            GUI.backgroundColor = previous;
        }

        bool Matches(MotionPreset preset) {
            if (string.IsNullOrEmpty(_search)) { return true; }
            string needle = _search.Trim();
            return MotionLibrary.DisplayName(preset).IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0
                || MotionLibrary.Description(preset).IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void Select(MotionPreset preset) {
            bool categoryChanged = MotionLibrary.Category(preset) != MotionLibrary.Category(_selected);
            _selected = preset;
            if (categoryChanged) { _trigger = DefaultTrigger(preset); }
            Preview();
        }

        // ----- Footer -----

        void DrawFooter() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MotionLibrary.DisplayName(_selected) + "  ·  " + MotionLibrary.Category(_selected), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(MotionLibrary.Description(_selected), EditorStyles.wordWrappedMiniLabel);
            EditorGUI.BeginChangeCheck();
            _intensity = EditorGUILayout.Slider(new GUIContent("Intensity", "0.5 = subtle, 1 = as designed, 2 = exaggerated."), _intensity, 0.1f, 3f);
            float auto = MotionLibrary.DefaultDuration(_selected);
            _duration = EditorGUILayout.Slider(new GUIContent("Duration (s)", "0 = automatic (" + auto.ToString("0.##") + " s)."), _duration, 0f, 4f);
            if (_targets.Count > 1 || HasChildren()) {
                _interval = EditorGUILayout.Slider(new GUIContent("Stagger (s)", "Delay between objects in a cascade."), _interval, 0f, 0.5f);
            }
            if (EditorGUI.EndChangeCheck() && MotionPreview.IsPlaying(this)) { Preview(); }
            using (new EditorGUI.DisabledScope(_targets.Count == 0)) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Play, "Preview", "Play it on the selection"), GUILayout.Height(24f))) { Preview(); }
                using (new EditorGUI.DisabledScope(!MotionPreview.IsPlaying(this))) {
                    if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Stop, "Reset", "Put the objects back"), GUILayout.Height(24f))) { MotionPreview.Stop(this); }
                }
                EditorGUILayout.EndHorizontal();
                _trigger = (MotionTrigger)EditorGUILayout.Popup(new GUIContent("Starts"), (int)_trigger, TriggerLabels);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Add to " + Plural(_targets.Count), "Adds a Raven Motion with these settings."), GUILayout.Height(26f))) { AddMotion(); }
                using (new EditorGUI.DisabledScope(!HasChildren())) {
                    if (GUILayout.Button(new GUIContent("Cascade children", "Adds a Raven Stagger: the motion plays on each child, one after another."), GUILayout.Height(26f))) { AddStagger(); }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        static string Plural(int count) {
            return count <= 1 ? "selected object" : count + " objects";
        }

        // ----- Actions -----

        void Preview() {
            if (_targets.Count == 0) { return; }
            MotionPreview.Play(this, _targets, _selected, Settings(), _interval);
        }

        MotionSettings Settings() {
            MotionSettings settings = MotionSettings.Default;
            settings.intensity = _intensity;
            settings.duration = _duration;
            return settings;
        }

        void AddMotion() {
            MotionPreview.Stop(this);
            var added = new List<Object>(_targets.Count);
            for (int i = 0; i < _targets.Count; i++) {
                RavenMotion motion = Undo.AddComponent<RavenMotion>(_targets[i].gameObject);
                var so = new SerializedObject(motion);
                so.FindProperty("preset").intValue = (int)_selected;
                so.FindProperty("trigger").intValue = (int)_trigger;
                so.FindProperty("settings.intensity").floatValue = _intensity;
                so.FindProperty("settings.duration").floatValue = _duration;
                if (MotionLibrary.Category(_selected) == MotionCategory.Exit) {
                    so.FindProperty("whenFinished").intValue = (int)MotionFinish.Deactivate;
                }
                so.ApplyModifiedProperties();
                added.Add(motion);
            }
            if (added.Count > 0) { EditorGUIUtility.PingObject(added[0]); }
        }

        void AddButtonFeel() {
            MotionPreview.Stop(this);
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].GetComponent<RavenButtonFeel>() == null) { Undo.AddComponent<RavenButtonFeel>(_targets[i].gameObject); }
            }
        }

        void QuickMotion(MotionPreset preset, MotionTrigger trigger) {
            _selected = preset;
            _trigger = trigger;
            AddMotion();
        }

        void AddStagger() {
            MotionPreview.Stop(this);
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].childCount == 0) { continue; }
                RavenStagger stagger = Undo.AddComponent<RavenStagger>(_targets[i].gameObject);
                var so = new SerializedObject(stagger);
                so.FindProperty("preset").intValue = (int)_selected;
                so.FindProperty("interval").floatValue = _interval;
                so.FindProperty("settings.intensity").floatValue = _intensity;
                so.FindProperty("settings.duration").floatValue = _duration;
                so.FindProperty("trigger").intValue = (int)(_trigger == MotionTrigger.OnStart ? MotionTrigger.OnStart
                    : _trigger == MotionTrigger.Manual ? MotionTrigger.Manual : MotionTrigger.OnEnable);
                so.ApplyModifiedProperties();
            }
        }

        // ----- Helpers -----

        // Top-level selected scene objects only (a selected child of a selected parent would play twice).
        void CollectTargets() {
            _targets.Clear();
            Transform[] selected = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            for (int i = 0; i < selected.Length; i++) {
                if (!EditorUtility.IsPersistent(selected[i])) { _targets.Add(selected[i]); }
            }
        }

        bool HasChildren() {
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].childCount > 0) { return true; }
            }
            return false;
        }

        MotionTrigger DefaultTrigger(MotionPreset preset) {
            switch (MotionLibrary.Category(preset)) {
                case MotionCategory.Exit: return MotionTrigger.Manual;
                case MotionCategory.Attention: return SelectionHasButton() ? MotionTrigger.OnClick : MotionTrigger.Manual;
                default: return MotionTrigger.OnEnable;
            }
        }

        bool SelectionHasButton() {
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].GetComponent<Selectable>() != null) { return true; }
            }
            return false;
        }

        void EnsureStyles() {
            if (_tile != null) { return; }
            _tile = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = true };
            _header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft };
        }
    }
}
