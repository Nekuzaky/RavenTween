using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Visual timeline for a Raven Sequence Player: one track per step, drag blocks to retime
    /// them, scrub or play the whole sequence on the scene outside Play Mode.
    /// </summary>
    public sealed class RavenSequenceEditorWindow : EditorWindow {
        const float ListWidth = 420f;
        const float RowHeight = 26f;
        const float RulerHeight = 24f;
        const float SnapStep = 0.05f;
        static readonly Color ChainColor = new Color(0.31f, 0.56f, 0.91f);
        static readonly Color GroupColor = new Color(0.5f, 0.71f, 1f);
        static readonly Color InsertColor = new Color(0.6f, 0.65f, 0.78f);
        static readonly Color TrackColor = new Color(0f, 0f, 0f, 0.15f);
        static readonly Color PlayheadColor = new Color(1f, 0.85f, 0.3f);

        RavenSequencePlayer _player;
        bool _locked;
        bool _loop;
        float _pixelsPerSecond = 160f;
        int _undoGroup;
        Vector2 _scroll;
        readonly List<SequenceLayout.Block> _blocks = new List<SequenceLayout.Block>();
        int _dragIndex = -1;
        float _dragOffset;
        bool _scrubbing;

        [MenuItem("Tools/RavenTween/Sequence Editor")]
        public static void OpenWindow() { Open(Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<RavenSequencePlayer>() : null); }

        public static void Open(RavenSequencePlayer player) {
            var window = GetWindow<RavenSequenceEditorWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.raventween.core/Editor/Icons/icon_sequence_player.png");
            window.titleContent = new GUIContent("Raven Sequence", icon);
            window.minSize = new Vector2(720f, 220f);
            if (player != null) { window._player = player; }
            window.Show();
        }

        void OnEnable() {
            EditorTweenPreview.Changed += Repaint;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable() {
            EditorTweenPreview.Changed -= Repaint;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EndMyPreview();
        }

        void OnSelectionChanged() {
            if (_locked || Selection.activeGameObject == null) { return; }
            var player = Selection.activeGameObject.GetComponent<RavenSequencePlayer>();
            if (player == null || player == _player) { return; }
            EndMyPreview();
            _player = player;
            Repaint();
        }

        void OnUndoRedo() {
            EndMyPreview();
            Repaint();
        }

        bool Mine { get { return EditorTweenPreview.Owner == (object)this; } }

        void EndMyPreview() {
            if (Mine) { EditorTweenPreview.End(); }
        }

        void OnGUI() {
            DrawToolbar();
            if (_player == null) {
                EditorGUILayout.HelpBox("Select a GameObject with a Raven Sequence Player, or open this window from its inspector.", MessageType.Info);
                return;
            }
            float total = SequenceLayout.Compute(_player.Steps, _blocks);
            Rect area = GUILayoutUtility.GetRect(10f, 10000f, 10f, 10000f);
            float width = Mathf.Max(area.width - 16f, ListWidth + TimeToX(total + 2f));
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0f, 0f, width, RulerHeight + RowHeight * (_player.Steps.Count + 1) + 8f));
            DrawRuler(new Rect(ListWidth, 0f, width - ListWidth, RulerHeight), total);
            for (int i = 0; i < _player.Steps.Count; i++) { DrawRow(i, new Rect(0f, RulerHeight + i * RowHeight, width, RowHeight)); }
            DrawAddRow(new Rect(0f, RulerHeight + _player.Steps.Count * RowHeight, width, RowHeight));
            DrawPlayhead(new Rect(ListWidth, 0f, width - ListWidth, RulerHeight + RowHeight * _player.Steps.Count));
            GUI.EndScrollView();
            HandleTimelineInput(new Rect(area.x + ListWidth, area.y, area.width - ListWidth - 16f, area.height), total);
        }

        // ----- Toolbar -----

        void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var picked = (RavenSequencePlayer)EditorGUILayout.ObjectField(_player, typeof(RavenSequencePlayer), true, GUILayout.Width(220f));
            if (picked != _player) { EndMyPreview(); _player = picked; }
            _locked = GUILayout.Toggle(_locked, new GUIContent("Lock", "Keep this player even when the selection changes"), EditorStyles.toolbarButton, GUILayout.Width(44f));
            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(_player == null || EditorApplication.isPlayingOrWillChangePlaymode)) {
                bool playing = Mine && EditorTweenPreview.IsPlaying;
                if (GUILayout.Button(new GUIContent(playing ? RavenEditorIcons.Pause : RavenEditorIcons.Play, playing ? "Pause preview" : "Preview the sequence in the scene"), EditorStyles.toolbarButton, GUILayout.Width(30f))) {
                    TogglePlay(playing);
                }
                if (GUILayout.Button(new GUIContent(RavenEditorIcons.Stop, "Stop and restore the scene"), EditorStyles.toolbarButton, GUILayout.Width(30f))) { EndMyPreview(); }
            }
            _loop = GUILayout.Toggle(_loop, "Loop", EditorStyles.toolbarButton, GUILayout.Width(44f));
            GUILayout.Label(Mine ? string.Format("{0:0.00} / {1:0.00} s", EditorTweenPreview.Time, EditorTweenPreview.Length) : "", EditorStyles.miniLabel, GUILayout.Width(100f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Zoom", EditorStyles.miniLabel);
            _pixelsPerSecond = GUILayout.HorizontalSlider(_pixelsPerSecond, 40f, 600f, GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
        }

        void TogglePlay(bool playing) {
            if (playing) { EditorTweenPreview.Pause(); return; }
            if (!Mine && !StartPreview()) { return; }
            EditorTweenPreview.Play();
        }

        bool StartPreview() {
            EditorTweenPreview.End();
            foreach (RavenSequencePlayer.Step step in _player.Steps) {
                if (step.template == null || step.target == null) { continue; }
                PropertyKind kind = step.template.property;
                int id = kind == PropertyKind.MaterialFloat || kind == PropertyKind.MaterialColor ? Shader.PropertyToID(step.template.materialProperty) : 0;
                EditorTweenPreview.Record(step.target, kind, id);
            }
            Sequence sequence = _player.BuildSequence();
            if (!sequence.IsAlive) { EditorTweenPreview.End(); return false; }
            EditorTweenPreview.Begin(this, sequence, sequence.Duration, _loop);
            return true;
        }

        // ----- Rows -----

        void DrawRow(int index, Rect row) {
            if ((index & 1) == 0) { EditorGUI.DrawRect(row, TrackColor); }
            RavenSequencePlayer.Step step = _player.Steps[index];
            Rect r = new Rect(row.x + 4f, row.y + 4f, 22f, row.height - 8f);
            GUI.Label(r, index.ToString(), EditorStyles.miniLabel);
            r.x += 20f; r.width = 64f;
            var mode = (RavenSequencePlayer.StepMode)EditorGUI.EnumPopup(r, step.mode);
            r.x += 68f; r.width = 146f;
            var stepTarget = EditorGUI.ObjectField(r, step.target, typeof(Object), true);
            r.x += 150f; r.width = 128f;
            var template = (TweenTemplate)EditorGUI.ObjectField(r, step.template, typeof(TweenTemplate), false);
            if (mode != step.mode || stepTarget != step.target || template != step.template) {
                step.mode = mode; step.target = stepTarget; step.template = template;
                SetStep(index, step, "Edit Sequence Step");
            }
            DrawRowButtons(index, new Rect(row.x + ListWidth - 0f, row.y, 0f, row.height));
            DrawBlock(index, row);
        }

        void DrawRowButtons(int index, Rect anchor) {
            Rect b = new Rect(ListWidth - 48f, anchor.y + 4f, 22f, anchor.height - 8f);
            if (GUI.Button(b, new GUIContent("↑", "Move up"), EditorStyles.miniButtonLeft) && index > 0) { Swap(index, index - 1); }
            b.x += 22f;
            if (GUI.Button(b, new GUIContent(RavenEditorIcons.Kill, "Remove step"), EditorStyles.miniButtonRight)) { RemoveStep(index); }
        }

        void DrawAddRow(Rect row) {
            if (GUI.Button(new Rect(row.x + 4f, row.y + 3f, 110f, row.height - 6f), "+ Add Step", EditorStyles.miniButton)) {
                Undo.RecordObject(_player, "Add Sequence Step");
                _player.Steps.Add(new RavenSequencePlayer.Step { mode = RavenSequencePlayer.StepMode.Chain });
                MarkChanged();
            }
        }

        void DrawBlock(int index, Rect row) {
            if (index >= _blocks.Count) { return; }
            SequenceLayout.Block block = _blocks[index];
            RavenSequencePlayer.Step step = _player.Steps[index];
            float x = ListWidth + TimeToX(block.Start);
            float w = Mathf.Max(block.Length * _pixelsPerSecond, 6f);
            Rect bar = new Rect(x, row.y + 4f, w, row.height - 8f);
            Color color = !block.Valid ? new Color(0.4f, 0.4f, 0.4f, 0.5f) : ModeColor(step.mode);
            EditorGUI.DrawRect(bar, color);
            string label = step.template != null ? step.template.name : "(no template)";
            if (block.LoopClamped) { label += "  ↻ loops: plays once here"; }
            GUI.Label(new Rect(bar.x + 4f, bar.y, Mathf.Max(bar.width, 200f), bar.height), new GUIContent(label, BlockTooltip(step, block)), EditorStyles.whiteMiniLabel);
        }

        static string BlockTooltip(RavenSequencePlayer.Step step, SequenceLayout.Block block) {
            if (!block.Valid) { return "Incomplete step: assign a target and a template. Skipped at runtime."; }
            return string.Format("{0} at {1:0.00} s, lasts {2:0.00} s. Drag to retime (becomes Insert). Hold Alt to disable snapping.", step.mode, block.Start, block.Length);
        }

        static Color ModeColor(RavenSequencePlayer.StepMode mode) {
            switch (mode) {
                case RavenSequencePlayer.StepMode.Group: return GroupColor;
                case RavenSequencePlayer.StepMode.Insert: return InsertColor;
                default: return ChainColor;
            }
        }

        // ----- Ruler, playhead, input -----

        void DrawRuler(Rect rect, float total) {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            float step = _pixelsPerSecond >= 300f ? 0.1f : _pixelsPerSecond >= 120f ? 0.25f : 0.5f;
            float end = Mathf.Max(total, 1f) + 1f;
            for (float t = 0f; t <= end + 1e-4f; t += step) {
                float x = rect.x + TimeToX(t);
                bool major = Mathf.Abs(t - Mathf.Round(t)) < 1e-4f;
                EditorGUI.DrawRect(new Rect(x, rect.yMax - (major ? 10f : 5f), 1f, major ? 10f : 5f), new Color(1f, 1f, 1f, 0.35f));
                if (major) { GUI.Label(new Rect(x + 3f, rect.y, 40f, 14f), t.ToString("0") + "s", EditorStyles.miniLabel); }
            }
            float endX = rect.x + TimeToX(total);
            EditorGUI.DrawRect(new Rect(endX, rect.y, 2f, 2000f), new Color(1f, 1f, 1f, 0.25f));
        }

        void DrawPlayhead(Rect rect) {
            if (!Mine) { return; }
            float x = rect.x + TimeToX(EditorTweenPreview.Time);
            EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), PlayheadColor);
        }

        void HandleTimelineInput(Rect timeline, float total) {
            Event e = Event.current;
            if (_player == null || EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            Vector2 local = e.mousePosition - timeline.position + _scroll;
            if (e.type == EventType.MouseDown && e.button == 0 && timeline.Contains(e.mousePosition)) {
                OnMouseDown(local, e);
            } else if (e.type == EventType.MouseDrag && (_dragIndex >= 0 || _scrubbing)) {
                OnMouseDrag(local, e);
            } else if (e.type == EventType.MouseUp && (_dragIndex >= 0 || _scrubbing)) {
                if (_dragIndex >= 0) { Undo.CollapseUndoOperations(_undoGroup); }
                _dragIndex = -1;
                _scrubbing = false;
                e.Use();
            }
        }

        void OnMouseDown(Vector2 local, Event e) {
            if (local.y < RulerHeight) {
                _scrubbing = true;
                Scrub(XToTime(local.x));
                e.Use();
                return;
            }
            int row = Mathf.FloorToInt((local.y - RulerHeight) / RowHeight);
            if (row < 0 || row >= _blocks.Count) { return; }
            float start = TimeToX(_blocks[row].Start);
            float end = start + Mathf.Max(_blocks[row].Length * _pixelsPerSecond, 6f);
            if (local.x < start || local.x > end) { return; }
            _dragIndex = row;
            _dragOffset = local.x - start;
            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            e.Use();
        }

        void OnMouseDrag(Vector2 local, Event e) {
            if (_scrubbing) {
                Scrub(XToTime(local.x));
            } else {
                float time = Mathf.Max(XToTime(local.x - _dragOffset), 0f);
                if (!e.alt) { time = Mathf.Round(time / SnapStep) * SnapStep; }
                RavenSequencePlayer.Step step = _player.Steps[_dragIndex];
                step.mode = RavenSequencePlayer.StepMode.Insert;
                step.insertTime = time;
                SetStep(_dragIndex, step, "Move Sequence Step");
            }
            e.Use();
        }

        void Scrub(float time) {
            if (!Mine && !StartPreview()) { return; }
            EditorTweenPreview.Pause();
            EditorTweenPreview.Seek(time);
        }

        float TimeToX(float time) { return time * _pixelsPerSecond + 8f; }
        float XToTime(float x) { return (x - 8f) / _pixelsPerSecond; }

        // ----- Editing -----

        void SetStep(int index, RavenSequencePlayer.Step step, string undoName) {
            Undo.RecordObject(_player, undoName);
            _player.Steps[index] = step;
            MarkChanged();
        }

        void Swap(int a, int b) {
            Undo.RecordObject(_player, "Reorder Sequence Steps");
            RavenSequencePlayer.Step tmp = _player.Steps[a];
            _player.Steps[a] = _player.Steps[b];
            _player.Steps[b] = tmp;
            MarkChanged();
        }

        void RemoveStep(int index) {
            Undo.RecordObject(_player, "Remove Sequence Step");
            _player.Steps.RemoveAt(index);
            MarkChanged();
        }

        void MarkChanged() {
            EndMyPreview();
            EditorUtility.SetDirty(_player);
            Repaint();
        }
    }
}
