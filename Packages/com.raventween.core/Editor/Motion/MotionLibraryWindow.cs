using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween.Editor {
    /// <summary>
    /// Every motion on one page. Hover a motion to see it on the animated card, click it to watch
    /// it play on the selected objects (outside Play Mode, nothing is changed), then add it as a
    /// Raven Motion in one click. Saved Motion Styles appear at the top.
    /// </summary>
    public sealed class MotionLibraryWindow : EditorWindow {
        const float ButtonHeight = 30f;
        const float MinButtonWidth = 132f;
        const float StageHeight = 96f;
        const double FrameInterval = 1.0 / 30.0;

        static readonly Color EntranceColor = new Color(0.36f, 0.78f, 0.47f);
        static readonly Color ExitColor = new Color(0.9f, 0.42f, 0.42f);
        static readonly Color AttentionColor = new Color(0.96f, 0.7f, 0.25f);
        static readonly Color LoopColor = new Color(0.4f, 0.62f, 0.98f);
        static readonly Color StyleColor = new Color(0.72f, 0.5f, 0.95f);

        static GUIContent[] TriggerLabels { get { return MotionPresetDrawer.TriggerLabels; } }

        readonly List<Transform> _targets = new List<Transform>(8);
        readonly List<MotionStyle> _styles = new List<MotionStyle>(8);
        readonly List<MotionPreset> _row = new List<MotionPreset>(16);
        MotionStage _stage;
        MotionPreset _selected = MotionPreset.PopIn;
        MotionStyle _selectedStyle;
        // The tile under the pointer, kept while the pointer crosses the gaps between tiles or
        // its tooltip opens, so the card does not flip back and forth; cleared when it leaves.
        MotionPreset _hovered;
        MotionStyle _hoveredStyle;
        MotionTrigger _trigger = MotionTrigger.OnEnable;
        float _intensity = 1f;
        float _duration;
        float _interval = 0.06f;
        string _search = "";
        Vector2 _scroll;
        double _lastFrame;
        GUIStyle _tile;
        GUIStyle _header;

        [MenuItem("Tools/RavenTween/Motion Library", false, 0)]
        public static void Open() {
            var window = GetWindow<MotionLibraryWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.raventween.core/Editor/Icons/icon_animator.png");
            window.titleContent = new GUIContent("Motion Library", icon);
            window.minSize = new Vector2(360f, 480f);
            window.Show();
        }

        [MenuItem("GameObject/RavenTween/Add Motion…", false, 10)]
        static void OpenFromHierarchy() { Open(); }

        void OnEnable() {
            wantsMouseMove = true;
            _stage = new MotionStage();
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += Animate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            RefreshStyles();
        }

        void OnDisable() {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= Animate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            MotionPreview.Stop(this);
            if (_stage != null) { _stage.Dispose(); }
            _stage = null;
        }

        // Entering or leaving Play Mode may reset the engine (no domain reload): the card's
        // motion handle must never point at a gameplay tween, so the card starts over.
        void OnPlayModeChanged(PlayModeStateChange change) {
            if (change != PlayModeStateChange.ExitingEditMode && change != PlayModeStateChange.ExitingPlayMode) { return; }
            if (_stage != null) { _stage.Dispose(); }
            _stage = new MotionStage();
        }

        void OnProjectChange() {
            RefreshStyles();
            Repaint();
        }

        void OnSelectionChanged() {
            MotionPreview.Stop(this);
            Repaint();
        }

        // The card animates at 30 frames per second while the window is on screen.
        void Animate() {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastFrame < FrameInterval) { return; }
            _lastFrame = now;
            Repaint();
        }

        void OnGUI() {
            EnsureStyles();
            CollectTargets();
            if (Event.current.type == EventType.MouseLeaveWindow) { ClearHover(); }
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawStyles();
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
            EditorGUILayout.LabelField("Hover a motion to see it on the card. Click it to play it on the selected objects, right in the scene. Nothing is changed until you add it.", EditorStyles.wordWrappedMiniLabel);
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
                if (GUILayout.Button(new GUIContent("Show / Hide", "Adds a Raven Show Hide: call Show, Hide or Toggle from buttons. For panels and popups."), EditorStyles.miniButtonMid)) { AddShowHide(); }
                if (GUILayout.Button(new GUIContent("Idle Float", "A gentle float that never stops. Pickups, menus, collectibles."), EditorStyles.miniButtonMid)) { QuickMotion(MotionPreset.Float, MotionTrigger.OnEnable); }
                if (GUILayout.Button(new GUIContent("Pop In On Show", "Pops in every time the object is enabled."), EditorStyles.miniButtonRight)) { QuickMotion(MotionPreset.PopIn, MotionTrigger.OnEnable); }
                EditorGUILayout.EndHorizontal();
            }
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space(4f);
        }

        // ----- Grid -----

        void DrawStyles() {
            if (_styles.Count == 0) { return; }
            DrawSectionHeader("Your Styles", "Saved Motion Styles, shared by the whole project.", StyleColor);
            float width = TileWidth(out int columns);
            int shown = 0;
            for (int i = 0; i < _styles.Count; i++) {
                MotionStyle style = _styles[i];
                if (style == null || !MatchesText(style.name)) { continue; }
                if (shown % columns == 0) { EditorGUILayout.BeginHorizontal(); }
                DrawStyleTile(style, width);
                shown++;
                if (shown % columns == 0) { GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); }
            }
            if (shown % columns != 0) { GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); }
            EditorGUILayout.Space(8f);
        }

        void DrawStyleTile(MotionStyle style, float width) {
            Color previous = GUI.backgroundColor;
            if (style == _selectedStyle) { GUI.backgroundColor = StyleColor; }
            string tip = MotionLibrary.DisplayName(style.Preset) + (string.IsNullOrEmpty(style.Notes) ? "" : " — " + style.Notes);
            if (GUILayout.Button(new GUIContent(style.name, tip), _tile, GUILayout.Height(ButtonHeight), GUILayout.Width(width))) { SelectStyle(style); }
            if (IsHovered(GUILayoutUtility.GetLastRect())) { _hoveredStyle = style; _hovered = MotionPreset.None; }
            GUI.backgroundColor = previous;
        }

        void DrawCategory(MotionCategory category, string title, string subtitle, Color color) {
            _row.Clear();
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                if (MotionLibrary.Category(preset) == category && Matches(preset)) { _row.Add(preset); }
            }
            if (_row.Count == 0) { return; }
            DrawSectionHeader(title, subtitle, color);
            // Every tile gets the same width, so a short last row lines up with the rows above.
            float width = TileWidth(out int columns);
            for (int start = 0; start < _row.Count; start += columns) {
                EditorGUILayout.BeginHorizontal();
                int end = Mathf.Min(start + columns, _row.Count);
                for (int i = start; i < end; i++) { DrawTile(_row[i], color, width); }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(8f);
        }

        void DrawSectionHeader(string title, string subtitle, Color color) {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + 3f, 4f, headerRect.height - 6f), color);
            GUI.Label(new Rect(headerRect.x + 10f, headerRect.y, headerRect.width - 10f, headerRect.height), title + "   <size=10><color=#999999>" + subtitle + "</color></size>", _header);
        }

        float TileWidth(out int columns) {
            float available = position.width - 28f;
            columns = Mathf.Max(2, Mathf.FloorToInt(available / MinButtonWidth));
            return Mathf.Floor((available - (columns - 1) * 4f) / columns);
        }

        void DrawTile(MotionPreset preset, Color color, float width) {
            Color previous = GUI.backgroundColor;
            if (preset == _selected && _selectedStyle == null) { GUI.backgroundColor = color; }
            var content = new GUIContent(MotionLibrary.DisplayName(preset), MotionLibrary.Description(preset));
            if (GUILayout.Button(content, _tile, GUILayout.Height(ButtonHeight), GUILayout.Width(width))) { Select(preset); }
            if (IsHovered(GUILayoutUtility.GetLastRect())) { _hovered = preset; _hoveredStyle = null; }
            GUI.backgroundColor = previous;
        }

        // Rects are only real outside the Layout pass.
        static bool IsHovered(Rect tile) {
            Event e = Event.current;
            return e.type != EventType.Layout && tile.Contains(e.mousePosition);
        }

        void ClearHover() {
            _hovered = MotionPreset.None;
            _hoveredStyle = null;
        }

        bool Matches(MotionPreset preset) {
            if (string.IsNullOrEmpty(_search)) { return true; }
            return MatchesText(MotionLibrary.DisplayName(preset)) || MatchesText(MotionLibrary.Description(preset));
        }

        bool MatchesText(string text) {
            return string.IsNullOrEmpty(_search) || text.IndexOf(_search.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void Select(MotionPreset preset) {
            bool categoryChanged = MotionLibrary.Category(preset) != MotionLibrary.Category(CurrentPreset());
            _selected = preset;
            _selectedStyle = null;
            if (categoryChanged) { _trigger = DefaultTrigger(preset); }
            Preview();
        }

        void SelectStyle(MotionStyle style) {
            bool categoryChanged = MotionLibrary.Category(style.Preset) != MotionLibrary.Category(CurrentPreset());
            _selectedStyle = style;
            if (categoryChanged) { _trigger = DefaultTrigger(style.Preset); }
            Preview();
        }

        MotionPreset CurrentPreset() { return _selectedStyle != null ? _selectedStyle.Preset : _selected; }

        // ----- Footer -----

        void DrawFooter() {
            Rect footer = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            // Pointing at the settings shows the motion being tuned, not the last tile hovered.
            if (IsHovered(footer)) { ClearHover(); }
            DrawStage();
            MotionPreset current = CurrentPreset();
            string title = _selectedStyle != null ? _selectedStyle.name + "  ·  " + MotionLibrary.DisplayName(current) : MotionLibrary.DisplayName(current) + "  ·  " + MotionLibrary.Category(current);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(MotionLibrary.Description(current), EditorStyles.wordWrappedMiniLabel);
            if (_selectedStyle == null) { DrawTuning(current); }
            else if (GUILayout.Button("Edit this style", EditorStyles.miniButton)) { Selection.activeObject = _selectedStyle; }
            DrawActions();
            EditorGUILayout.EndVertical();
        }

        void DrawStage() {
            Rect area = GUILayoutUtility.GetRect(10f, StageHeight, GUILayout.ExpandWidth(true));
            MotionPreset shownPreset;
            MotionSettings shownSettings;
            Color color;
            if (_hoveredStyle != null) { shownPreset = _hoveredStyle.Preset; shownSettings = _hoveredStyle.Settings; color = StyleColor; }
            else if (_hovered != MotionPreset.None) { shownPreset = _hovered; shownSettings = Settings(); color = CategoryColor(_hovered); }
            else if (_selectedStyle != null) { shownPreset = _selectedStyle.Preset; shownSettings = _selectedStyle.Settings; color = StyleColor; }
            else { shownPreset = _selected; shownSettings = Settings(); color = CategoryColor(_selected); }
            if (Event.current.type == EventType.Repaint && _stage != null) {
                _stage.Show(shownPreset, shownSettings);
                _stage.Draw(area, color);
            }
        }

        void DrawTuning(MotionPreset current) {
            EditorGUI.BeginChangeCheck();
            _intensity = EditorGUILayout.Slider(new GUIContent("Intensity", "0.5 = subtle, 1 = as designed, 2 = exaggerated."), _intensity, 0.1f, 3f);
            float auto = MotionLibrary.DefaultDuration(current);
            _duration = EditorGUILayout.Slider(new GUIContent("Duration (s)", "0 = automatic (" + auto.ToString("0.##") + " s)."), _duration, 0f, 4f);
            if (_targets.Count > 1 || HasChildren()) {
                _interval = EditorGUILayout.Slider(new GUIContent("Stagger (s)", "Delay between objects in a cascade."), _interval, 0f, 0.5f);
            }
            if (EditorGUI.EndChangeCheck() && MotionPreview.IsPlaying(this)) { Preview(); }
        }

        void DrawActions() {
            using (new EditorGUI.DisabledScope(_targets.Count == 0)) {
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                    if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Play, "Preview", "Play it on the selection (Edit Mode)"), GUILayout.Height(24f))) { Preview(); }
                }
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
            if (_selectedStyle == null && GUILayout.Button(new GUIContent("Save as style…", "Saves this motion and its settings as a Motion Style asset the whole team can reuse."), EditorStyles.miniButton)) { SaveStyle(); }
        }

        static string Plural(int count) {
            return count <= 1 ? "selected object" : count + " objects";
        }

        static Color CategoryColor(MotionPreset preset) {
            switch (MotionLibrary.Category(preset)) {
                case MotionCategory.Entrance: return EntranceColor;
                case MotionCategory.Exit: return ExitColor;
                case MotionCategory.Attention: return AttentionColor;
                default: return LoopColor;
            }
        }

        // ----- Actions -----

        // Scene previews are an Edit Mode tool: in Play Mode the game owns the objects.
        void Preview() {
            if (_targets.Count == 0 || Application.isPlaying) { return; }
            MotionSettings settings = _selectedStyle != null ? _selectedStyle.Settings : Settings();
            MotionPreview.Play(this, _targets, CurrentPreset(), settings, _interval);
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
                if (_selectedStyle != null) {
                    so.FindProperty("style").objectReferenceValue = _selectedStyle;
                    so.FindProperty("preset").intValue = (int)_selectedStyle.Preset;
                } else {
                    so.FindProperty("preset").intValue = (int)_selected;
                    so.FindProperty("settings.intensity").floatValue = _intensity;
                    so.FindProperty("settings.duration").floatValue = _duration;
                }
                so.FindProperty("trigger").intValue = (int)_trigger;
                if (MotionLibrary.Category(CurrentPreset()) == MotionCategory.Exit) {
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

        void AddShowHide() {
            MotionPreview.Stop(this);
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].GetComponent<RavenShowHide>() == null) { Undo.AddComponent<RavenShowHide>(_targets[i].gameObject); }
            }
        }

        void QuickMotion(MotionPreset preset, MotionTrigger trigger) {
            _selected = preset;
            _selectedStyle = null;
            _trigger = trigger;
            AddMotion();
        }

        void AddStagger() {
            MotionPreview.Stop(this);
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i].childCount == 0) { continue; }
                RavenStagger stagger = Undo.AddComponent<RavenStagger>(_targets[i].gameObject);
                var so = new SerializedObject(stagger);
                if (_selectedStyle != null) {
                    so.FindProperty("style").objectReferenceValue = _selectedStyle;
                } else {
                    so.FindProperty("preset").intValue = (int)_selected;
                    so.FindProperty("settings.intensity").floatValue = _intensity;
                    so.FindProperty("settings.duration").floatValue = _duration;
                }
                so.FindProperty("interval").floatValue = _interval;
                so.FindProperty("trigger").intValue = (int)(_trigger == MotionTrigger.OnStart ? MotionTrigger.OnStart
                    : _trigger == MotionTrigger.Manual ? MotionTrigger.Manual : MotionTrigger.OnEnable);
                so.ApplyModifiedProperties();
            }
        }

        void SaveStyle() {
            string path = EditorUtility.SaveFilePanelInProject("Save Motion Style", MotionLibrary.DisplayName(_selected), "asset",
                "Name the style, e.g. \"Menu Pop In\". Every Raven Motion using it follows its changes.");
            if (string.IsNullOrEmpty(path)) { return; }
            MotionStyle style = CreateInstance<MotionStyle>();
            style.Preset = _selected;
            style.Settings = Settings();
            AssetDatabase.CreateAsset(style, path);
            AssetDatabase.SaveAssets();
            RefreshStyles();
            SelectStyle(style);
            EditorGUIUtility.PingObject(style);
            GUIUtility.ExitGUI(); // The modal dialog broke this frame's layout.
        }

        // ----- Helpers -----

        void RefreshStyles() {
            _styles.Clear();
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(MotionStyle).Name);
            for (int i = 0; i < guids.Length; i++) {
                var style = AssetDatabase.LoadAssetAtPath<MotionStyle>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (style != null) { _styles.Add(style); }
            }
            if (_selectedStyle != null && !_styles.Contains(_selectedStyle)) { _selectedStyle = null; }
        }

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
