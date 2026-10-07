using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Live view of every running tween and sequence: search, filters, sorting, an activity graph,
    /// a GC readout, expandable sequence timelines, and per-row pause / complete / kill.
    /// </summary>
    public sealed class RavenMonitorWindow : EditorWindow {
        enum KindFilter { All, Tweens, Sequences }
        enum SortMode { Creation, Progress, Target, Property }

        const int HistoryLength = 240;
        const float GraphHeight = 46f;
        static readonly Color GraphLine = new Color(0.43f, 0.69f, 1f);
        static readonly Color GraphFill = new Color(0.43f, 0.69f, 1f, 0.12f);

        readonly List<TweenEngine.DebugInfo> _snapshot = new List<TweenEngine.DebugInfo>(64);
        readonly List<TweenEngine.DebugInfo> _visible = new List<TweenEngine.DebugInfo>(64);
        readonly List<TweenEngine.DebugSequenceItem> _items = new List<TweenEngine.DebugSequenceItem>(16);
        readonly HashSet<long> _expanded = new HashSet<long>();
        readonly float[] _history = new float[HistoryLength];
        readonly Vector3[] _graphPoints = new Vector3[HistoryLength];
        static readonly Vector3[] QuadBuffer = new Vector3[4];
        int _historyHead;
        int _peak;
        double _lastSample;
        Vector2 _scroll;
        string _search = "";
        KindFilter _kind;
        SortMode _sort;
        bool _showChildren;
        bool _pausedOnly;
        ProfilerRecorder _gcRecorder;

        [MenuItem("Tools/RavenTween/Monitor")]
        public static void Open() {
            var window = GetWindow<RavenMonitorWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.raventween.core/Editor/Icons/raventween_icon_small.png");
            window.titleContent = new GUIContent("Raven Monitor", icon);
            window.minSize = new Vector2(640f, 260f);
            window.Show();
        }

        [MenuItem("Tools/RavenTween/Documentation")]
        public static void OpenDocumentation() {
            Application.OpenURL("https://nekuzaky.com/docs/raventween");
        }

        void OnEnable() {
            EditorApplication.update += OnEditorUpdate;
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        void OnDisable() {
            EditorApplication.update -= OnEditorUpdate;
            _gcRecorder.Dispose();
        }

        void OnEditorUpdate() {
            if (!Application.isPlaying) { return; }
            if (EditorApplication.timeSinceStartup - _lastSample >= 0.05) {
                _lastSample = EditorApplication.timeSinceStartup;
                int alive = Raven.AliveCount;
                _history[_historyHead] = alive;
                _historyHead = (_historyHead + 1) % HistoryLength;
                if (alive > _peak) { _peak = alive; }
            }
            Repaint();
        }

        void OnGUI() {
            DrawToolbar();
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Enter Play Mode to monitor live tweens. Use the Tween Template inspector or Tools ▸ RavenTween ▸ Sequence Editor to preview in Edit Mode.", MessageType.Info);
                return;
            }
            TweenEngine.CollectDebugInfo(_snapshot);
            DrawStats();
            BuildVisibleList();
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _visible.Count; i++) { DrawRow(_visible[i]); }
            if (_visible.Count == 0) {
                EditorGUILayout.LabelField(_snapshot.Count == 0 ? "No live tweens." : "Nothing matches the filters.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        // ----- Toolbar & stats -----

        void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f), GUILayout.MaxWidth(220f));
            _kind = (KindFilter)EditorGUILayout.EnumPopup(_kind, EditorStyles.toolbarDropDown, GUILayout.Width(84f));
            GUILayout.Label("Sort", EditorStyles.miniLabel, GUILayout.Width(26f));
            _sort = (SortMode)EditorGUILayout.EnumPopup(_sort, EditorStyles.toolbarDropDown, GUILayout.Width(80f));
            _pausedOnly = GUILayout.Toggle(_pausedOnly, "Paused", EditorStyles.toolbarButton, GUILayout.Width(52f));
            _showChildren = GUILayout.Toggle(_showChildren, "Children", EditorStyles.toolbarButton, GUILayout.Width(60f));
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.CompleteAll, "Complete All", "Jump everything to its end values"), EditorStyles.toolbarButton, GUILayout.Width(104f))) { Raven.CompleteAll(); }
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Stop, "Stop All", "Kill everything where it is"), EditorStyles.toolbarButton, GUILayout.Width(80f))) { Raven.StopAll(); }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawStats() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(230f));
            int tweens = 0, sequences = 0, paused = 0;
            for (int i = 0; i < _snapshot.Count; i++) {
                if (_snapshot[i].IsSequence) { sequences++; } else { tweens++; }
                if (_snapshot[i].Paused) { paused++; }
            }
            Vector2 iconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(14f, 14f));
            GUILayout.Label(RavenEditorIcons.Content(RavenEditorIcons.Activity, "Alive " + Raven.AliveCount + "   (peak " + _peak + ")", "Live tweens and sequences"), EditorStyles.boldLabel);
            EditorGUIUtility.SetIconSize(iconSize);
            GUILayout.Label(tweens + " tweens · " + sequences + " sequences · " + paused + " paused", EditorStyles.miniLabel);
            string gc = _gcRecorder.Valid ? FormatBytes(_gcRecorder.LastValue) : "n/a";
            GUILayout.Label(new GUIContent("GC this frame: " + gc, "Whole game, not only tweens. RavenTween itself allocates 0 B per frame."), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Time Scale", EditorStyles.miniLabel, GUILayout.Width(62f));
            Raven.TimeScale = GUILayout.HorizontalSlider(Raven.TimeScale, 0f, 2f, GUILayout.Width(110f));
            GUILayout.Label(Raven.TimeScale.ToString("0.00"), EditorStyles.miniLabel, GUILayout.Width(32f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            DrawGraph(GUILayoutUtility.GetRect(10f, GraphHeight, GUILayout.ExpandWidth(true), GUILayout.Height(GraphHeight)));
            EditorGUILayout.EndHorizontal();
        }

        void DrawGraph(Rect rect) {
            if (Event.current.type != EventType.Repaint) { return; }
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.2f));
            float max = Mathf.Max(_peak, 1);
            for (int i = 0; i < HistoryLength; i++) {
                float v = _history[(_historyHead + i) % HistoryLength];
                _graphPoints[i] = new Vector3(rect.x + rect.width * i / (HistoryLength - 1f), rect.yMax - 2f - (rect.height - 4f) * v / max, 0f);
            }
            Handles.color = GraphFill;
            for (int i = 1; i < HistoryLength; i++) {
                Vector3 a = _graphPoints[i - 1], b = _graphPoints[i];
                QuadBuffer[0] = a;
                QuadBuffer[1] = b;
                QuadBuffer[2] = new Vector3(b.x, rect.yMax, 0f);
                QuadBuffer[3] = new Vector3(a.x, rect.yMax, 0f);
                Handles.DrawAAConvexPolygon(QuadBuffer);
            }
            Handles.color = GraphLine;
            Handles.DrawAAPolyLine(2f, _graphPoints);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 160f, 14f), "Alive, last 12 s", EditorStyles.miniLabel);
        }

        static string FormatBytes(long bytes) {
            if (bytes < 1024) { return bytes + " B"; }
            return (bytes / 1024f).ToString("0.0") + " KB";
        }

        // ----- Filtering & sorting -----

        void BuildVisibleList() {
            _visible.Clear();
            for (int i = 0; i < _snapshot.Count; i++) {
                TweenEngine.DebugInfo info = _snapshot[i];
                if (info.OwnedBySequence && !_showChildren) { continue; }
                if (_kind == KindFilter.Tweens && info.IsSequence) { continue; }
                if (_kind == KindFilter.Sequences && !info.IsSequence) { continue; }
                if (_pausedOnly && !info.Paused) { continue; }
                if (!MatchesSearch(info)) { continue; }
                _visible.Add(info);
            }
            if (_sort != SortMode.Creation) { _visible.Sort(Compare); }
        }

        bool MatchesSearch(in TweenEngine.DebugInfo info) {
            if (string.IsNullOrEmpty(_search)) { return true; }
            string needle = _search.Trim();
            string target = info.Target != null ? info.Target.name : "";
            return target.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0
                || info.Property.ToString().IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int Compare(TweenEngine.DebugInfo a, TweenEngine.DebugInfo b) {
            switch (_sort) {
                case SortMode.Progress: return Progress(b).CompareTo(Progress(a));
                case SortMode.Target: return string.CompareOrdinal(a.Target != null ? a.Target.name : "~", b.Target != null ? b.Target.name : "~");
                case SortMode.Property: return a.Property.CompareTo(b.Property);
                default: return a.Index.CompareTo(b.Index);
            }
        }

        static float Progress(in TweenEngine.DebugInfo info) {
            return Mathf.Clamp01(info.Elapsed / Mathf.Max(info.CycleLength, 0.0001f));
        }

        // ----- Rows -----

        static void DrawHeader() {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.Label("Kind", EditorStyles.miniBoldLabel, GUILayout.Width(64f));
            GUILayout.Label("Target", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            GUILayout.Label("Property", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            GUILayout.Label("Progress", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label("Cycles", EditorStyles.miniBoldLabel, GUILayout.Width(50f));
            GUILayout.Space(100f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawRow(in TweenEngine.DebugInfo info) {
            long key = ((long)info.Index << 32) | info.Version;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            bool expanded = _expanded.Contains(key);
            Rect fold = GUILayoutUtility.GetRect(14f, 16f, GUILayout.Width(14f));
            if (info.IsSequence) {
                bool next = EditorGUI.Foldout(fold, expanded, GUIContent.none, true);
                if (next != expanded) { if (next) { _expanded.Add(key); } else { _expanded.Remove(key); } }
            }
            GUILayout.Label(info.IsSequence ? (info.OwnedBySequence ? "Seq └" : "Sequence") : (info.OwnedBySequence ? "Tween └" : "Tween"), GUILayout.Width(64f));
            DrawTarget(info.Target, info.IsSequence);
            GUILayout.Label(info.Property == PropertyKind.None ? "(value)" : info.Property.ToString(), GUILayout.Width(120f));
            Rect bar = GUILayoutUtility.GetRect(60f, 16f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, Progress(info), info.Paused ? "paused" : (Progress(info) * 100f).ToString("0") + "%");
            GUILayout.Label(info.CyclesDone + "/" + (info.Cycles < 0 ? "∞" : info.Cycles.ToString()), GUILayout.Width(50f));
            DrawControls(info);
            EditorGUILayout.EndHorizontal();
            if (info.IsSequence && expanded) { DrawSequenceTimeline(info); }
            EditorGUILayout.EndVertical();
        }

        static void DrawTarget(Object target, bool isSequence) {
            if (target == null) {
                GUILayout.Label(isSequence ? "(timeline)" : "(no target)", EditorStyles.miniLabel, GUILayout.Width(150f));
                return;
            }
            if (GUILayout.Button(new GUIContent(target.name, "Click to select"), EditorStyles.linkLabel, GUILayout.Width(150f))) {
                EditorGUIUtility.PingObject(target);
                Selection.activeObject = target;
            }
        }

        void DrawSequenceTimeline(in TweenEngine.DebugInfo info) {
            if (!TweenEngine.CollectSequenceItems(info.Index, info.Version, _items) || _items.Count == 0) { return; }
            float length = Mathf.Max(info.CycleLength, 0.0001f);
            for (int i = 0; i < _items.Count; i++) {
                TweenEngine.DebugSequenceItem item = _items[i];
                Rect row = GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true));
                Rect lane = new Rect(row.x + 200f, row.y + 2f, row.width - 204f, row.height - 4f);
                string label = (item.Target != null ? item.Target.name : "(value)") + " · " + (item.IsSequence ? "Sequence" : item.Property.ToString());
                GUI.Label(new Rect(row.x + 18f, row.y - 1f, 180f, row.height), label, EditorStyles.miniLabel);
                if (Event.current.type != EventType.Repaint) { continue; }
                EditorGUI.DrawRect(lane, new Color(0f, 0f, 0f, 0.2f));
                float x0 = lane.x + lane.width * Mathf.Clamp01(item.StartTime / length);
                float x1 = lane.x + lane.width * Mathf.Clamp01((item.StartTime + item.Duration) / length);
                EditorGUI.DrawRect(new Rect(x0, lane.y, Mathf.Max(x1 - x0, 2f), lane.height), GraphLine);
                float head = lane.x + lane.width * Progress(info);
                EditorGUI.DrawRect(new Rect(head, lane.y - 1f, 1f, lane.height + 2f), Color.white);
            }
        }

        static void DrawControls(in TweenEngine.DebugInfo info) {
            using (new EditorGUI.DisabledScope(info.OwnedBySequence)) {
                Texture pauseIcon = info.Paused ? RavenEditorIcons.Play : RavenEditorIcons.Pause;
                if (IconButton(pauseIcon, info.Paused ? "Resume" : "Pause", EditorStyles.miniButtonLeft)) {
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
