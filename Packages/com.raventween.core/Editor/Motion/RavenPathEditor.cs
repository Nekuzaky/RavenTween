using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    [CustomEditor(typeof(RavenPath))]
    public sealed class RavenPathEditor : UnityEditor.Editor {
        static readonly Color PathColor = new Color(0.4f, 0.75f, 1f, 0.95f);
        static readonly Color StartColor = new Color(0.36f, 0.85f, 0.47f);
        readonly List<Vector3> _polyline = new List<Vector3>(128);

        void OnDisable() {
            if (EditorTweenPreview.Owner == (object)this) { EditorTweenPreview.End(); }
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            var path = (RavenPath)target;
            EditorGUILayout.LabelField("Length " + path.Length.ToString("0.##") + " units  ·  one trip " + path.TripDuration.ToString("0.##") + " s", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point")) { AddPoint(path); }
            using (new EditorGUI.DisabledScope(path.Points.Count <= 1)) {
                if (GUILayout.Button("Remove Last Point")) { RemoveLast(path); }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Drag the points in the Scene view. The path starts where the object stands.", MessageType.None);
            DrawPreviewBar(path);
        }

        void DrawPreviewBar(RavenPath path) {
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying) {
                if (Button(RavenEditorIcons.Play, "Play")) { path.Play(); }
                if (Button(RavenEditorIcons.Stop, "Back to Start")) { path.ResetToStart(); }
            } else {
                bool previewing = EditorTweenPreview.Owner == (object)this && EditorTweenPreview.IsActive;
                if (Button(RavenEditorIcons.Play, previewing ? "Replay" : "Preview")) { Preview(path); }
                using (new EditorGUI.DisabledScope(!previewing)) {
                    if (Button(RavenEditorIcons.Stop, "Reset")) { EditorTweenPreview.End(); }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void Preview(RavenPath path) {
            EditorTweenPreview.End();
            path.ForgetOrigin();
            EditorTweenPreview.Record(path.transform, PropertyKind.LocalPosition, 0);
            EditorTweenPreview.Record(path.transform, PropertyKind.LocalRotation, 0);
            Tween trip = path.CreateTween();
            if (!trip.IsAlive) { EditorTweenPreview.End(); return; }
            bool loops = path.Looping != RavenPath.PathLoop.Once;
            if (path.Looping == RavenPath.PathLoop.BackAndForth) { trip.Cycles(2, CycleMode.Yoyo); }
            float length = path.Looping == RavenPath.PathLoop.BackAndForth ? path.TripDuration * 2f : path.TripDuration;
            EditorTweenPreview.Begin(this, trip, length, loops);
            EditorTweenPreview.Play();
        }

        void OnSceneGUI() {
            var path = (RavenPath)target;
            if (path.Points.Count == 0) { return; }
            bool previewing = EditorTweenPreview.Owner == (object)this && EditorTweenPreview.IsActive;
            if (!Application.isPlaying && !previewing) { path.ForgetOrigin(); }
            path.GetWorldPolyline(_polyline);
            Handles.color = PathColor;
            if (_polyline.Count > 1) { Handles.DrawAAPolyLine(4f, _polyline.ToArray()); }
            Vector3 start = path.GetWorldPoint(-1);
            Handles.color = StartColor;
            Handles.SphereHandleCap(0, start, Quaternion.identity, HandleUtility.GetHandleSize(start) * 0.12f, EventType.Repaint);
            bool editable = !Application.isPlaying && !previewing;
            for (int i = 0; i < path.Points.Count; i++) { DrawPoint(path, i, editable); }
        }

        static void DrawPoint(RavenPath path, int index, bool editable) {
            Vector3 world = path.GetWorldPoint(index);
            Handles.color = PathColor;
            Handles.Label(world + Vector3.up * HandleUtility.GetHandleSize(world) * 0.25f, (index + 1).ToString(), EditorStyles.boldLabel);
            if (!editable) {
                Handles.SphereHandleCap(0, world, Quaternion.identity, HandleUtility.GetHandleSize(world) * 0.1f, EventType.Repaint);
                return;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(world, Tools.pivotRotation == PivotRotation.Local ? path.transform.rotation : Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(path, "Move Path Point");
                path.SetWorldPoint(index, moved);
            }
        }

        static void AddPoint(RavenPath path) {
            Undo.RecordObject(path, "Add Path Point");
            List<Vector3> points = path.Points;
            Vector3 last = points.Count > 0 ? points[points.Count - 1] : Vector3.zero;
            Vector3 before = points.Count > 1 ? points[points.Count - 2] : Vector3.zero;
            Vector3 step = last - before;
            if (step.sqrMagnitude < 1e-6f) { step = Vector3.right * 2f; }
            points.Add(last + step);
            EditorUtility.SetDirty(path);
        }

        static void RemoveLast(RavenPath path) {
            Undo.RecordObject(path, "Remove Path Point");
            path.Points.RemoveAt(path.Points.Count - 1);
            EditorUtility.SetDirty(path);
        }

        static bool Button(Texture icon, string label) {
            return GUILayout.Button(RavenEditorIcons.Content(icon, label, label), GUILayout.Height(24f));
        }
    }
}
