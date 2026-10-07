using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Compact inspector for TweenParams: the curve field only shows for custom easing.</summary>
    [CustomPropertyDrawer(typeof(TweenParams))]
    public sealed class TweenParamsDrawer : PropertyDrawer {
        const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Debug.Assert(property != null, "Drawer received a null property.");
            EditorGUI.BeginProperty(position, label, property);
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (property.isExpanded) {
                EditorGUI.indentLevel++;
                line = DrawField(line, property, "duration");
                line = DrawField(line, property, "startDelay");
                line = DrawField(line, property, "ease");
                if (IsCustomEase(property)) { line = DrawField(line, property, "customCurve"); }
                line = DrawField(line, property, "cycles");
                line = DrawField(line, property, "cycleMode");
                DrawField(line, property, "useUnscaledTime");
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            Debug.Assert(property != null, "Drawer received a null property.");
            if (!property.isExpanded) { return EditorGUIUtility.singleLineHeight; }
            int lines = IsCustomEase(property) ? 8 : 7;
            return lines * (EditorGUIUtility.singleLineHeight + Spacing);
        }

        static bool IsCustomEase(SerializedProperty property) {
            SerializedProperty ease = property.FindPropertyRelative("ease");
            return ease != null && ease.enumValueIndex >= 0 && (Ease)ease.intValue == Ease.Custom;
        }

        static Rect DrawField(Rect previous, SerializedProperty root, string name) {
            SerializedProperty field = root.FindPropertyRelative(name);
            Debug.Assert(field != null, "TweenParams is missing expected field: " + name);
            Rect line = new Rect(previous.x, previous.yMax + Spacing, previous.width, EditorGUIUtility.singleLineHeight);
            if (field != null) { EditorGUI.PropertyField(line, field); }
            return line;
        }
    }
}
