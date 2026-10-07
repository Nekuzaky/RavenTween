using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Draws an easing curve, with an optional playhead, in any inspector rect.</summary>
    static class EaseCurveView {
        const int Samples = 96;
        static readonly Vector3[] Points = new Vector3[Samples + 1];
        static readonly Color Background = new Color(0.12f, 0.13f, 0.17f);
        static readonly Color Grid = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color Curve = new Color(0.43f, 0.69f, 1f);

        /// <summary>Evaluates the template's easing at t in [0, 1].</summary>
        public static float Evaluate(Ease ease, AnimationCurve custom, float t) {
            if (ease != Ease.Custom) { return EaseUtility.Evaluate(ease, t); }
            return custom != null ? custom.Evaluate(t) : t;
        }

        /// <summary>Draws the curve; <paramref name="playhead"/> in [0, 1], or negative for none.</summary>
        public static void Draw(Rect rect, Ease ease, AnimationCurve custom, float playhead) {
            Debug.Assert(rect.width > 0f && rect.height > 0f, "Curve view needs a visible rect.");
            if (Event.current.type != EventType.Repaint) { return; }
            EditorGUI.DrawRect(rect, Background);
            ComputeRange(ease, custom, out float lo, out float hi);
            Rect plot = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);
            DrawGuides(plot, lo, hi);
            for (int i = 0; i <= Samples; i++) {
                float t = i / (float)Samples;
                Points[i] = ToPoint(plot, t, Evaluate(ease, custom, t), lo, hi);
            }
            Handles.color = Curve;
            Handles.DrawAAPolyLine(2.5f, Points);
            if (playhead >= 0f) { DrawPlayhead(plot, ease, custom, Mathf.Clamp01(playhead), lo, hi); }
        }

        static void ComputeRange(Ease ease, AnimationCurve custom, out float lo, out float hi) {
            lo = 0f;
            hi = 1f;
            for (int i = 0; i <= Samples; i++) {
                float v = Evaluate(ease, custom, i / (float)Samples);
                if (v < lo) { lo = v; }
                if (v > hi) { hi = v; }
            }
            float pad = (hi - lo) * 0.06f;
            lo -= pad;
            hi += pad;
        }

        static void DrawGuides(Rect plot, float lo, float hi) {
            Handles.color = Grid;
            Vector3 a0 = ToPoint(plot, 0f, 0f, lo, hi), a1 = ToPoint(plot, 1f, 0f, lo, hi);
            Vector3 b0 = ToPoint(plot, 0f, 1f, lo, hi), b1 = ToPoint(plot, 1f, 1f, lo, hi);
            Handles.DrawLine(a0, a1);
            Handles.DrawLine(b0, b1);
            Handles.DrawDottedLine(a0, b1, 3f);
        }

        static void DrawPlayhead(Rect plot, Ease ease, AnimationCurve custom, float t, float lo, float hi) {
            Vector3 p = ToPoint(plot, t, Evaluate(ease, custom, t), lo, hi);
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.DrawLine(new Vector3(p.x, plot.yMin), new Vector3(p.x, plot.yMax));
            EditorGUI.DrawRect(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), Color.white);
        }

        static Vector3 ToPoint(Rect plot, float t, float v, float lo, float hi) {
            float y = plot.yMax - (v - lo) / Mathf.Max(hi - lo, 0.0001f) * plot.height;
            return new Vector3(plot.x + t * plot.width, y, 0f);
        }
    }
}
