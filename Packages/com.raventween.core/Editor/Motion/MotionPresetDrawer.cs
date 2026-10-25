using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    [CustomPropertyDrawer(typeof(MotionPreset))]
    public sealed class MotionPresetDrawer : PropertyDrawer {
        internal static readonly GUIContent[] TriggerLabels = {
            new GUIContent("When enabled", "Every time the object is enabled, and when the scene starts."),
            new GUIContent("When the scene starts", "Once, in Start."),
            new GUIContent("On click", "A UI Button's click, or a pointer click on the object."),
            new GUIContent("On hover", "When the pointer moves over the object."),
            new GUIContent("When the pointer leaves", "When the pointer moves off the object."),
            new GUIContent("On press", "When the pointer is pressed on the object."),
            new GUIContent("On release", "When the pointer is released after a press."),
            new GUIContent("When something enters (trigger)", "When a collider enters this object's trigger collider."),
            new GUIContent("When something leaves (trigger)", "When a collider leaves this object's trigger collider."),
            new GUIContent("On collision", "When something hits this object's collider."),
            new GUIContent("Manually (event or script)", "Call Play() from a UnityEvent, such as another button's On Click.")
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            Rect field = EditorGUI.PrefixLabel(position, label);
            var current = (MotionPreset)property.intValue;
            if (EditorGUI.DropdownButton(field, ButtonContent(current, property.hasMultipleDifferentValues), FocusType.Keyboard)) {
                SerializedObject owner = property.serializedObject;
                string path = property.propertyPath;
                ShowMenu(field, current, chosen => {
                    owner.Update();
                    owner.FindProperty(path).intValue = (int)chosen;
                    owner.ApplyModifiedProperties();
                });
            }
            EditorGUI.EndProperty();
        }

        internal static GUIContent ButtonContent(MotionPreset preset, bool mixed) {
            if (mixed) { return new GUIContent("—"); }
            if (preset == MotionPreset.None) { return new GUIContent("None"); }
            string text = MotionLibrary.DisplayName(preset) + "   ·   " + MotionLibrary.Category(preset);
            return new GUIContent(text, MotionLibrary.Description(preset));
        }

        internal static void ShowMenu(Rect below, MotionPreset current, System.Action<MotionPreset> chosen) {
            var menu = new GenericMenu();
            for (int i = 0; i < MotionLibrary.All.Length; i++) {
                MotionPreset preset = MotionLibrary.All[i];
                string path = Plural(MotionLibrary.Category(preset)) + "/" + MotionLibrary.DisplayName(preset);
                menu.AddItem(new GUIContent(path, MotionLibrary.Description(preset)), preset == current, () => chosen(preset));
            }
            menu.DropDown(below);
        }

        internal static string Plural(MotionCategory category) {
            switch (category) {
                case MotionCategory.Entrance: return "Entrances";
                case MotionCategory.Exit: return "Exits";
                case MotionCategory.Attention: return "Attention";
                default: return "Loops";
            }
        }
    }
}
