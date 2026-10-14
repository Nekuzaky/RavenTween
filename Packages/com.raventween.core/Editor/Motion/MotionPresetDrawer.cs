using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Shows every MotionPreset field as a menu grouped by family, with descriptions as tooltips.</summary>
    [CustomPropertyDrawer(typeof(MotionPreset))]
    public sealed class MotionPresetDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            Rect field = EditorGUI.PrefixLabel(position, label);
            var current = (MotionPreset)property.intValue;
            if (EditorGUI.DropdownButton(field, ButtonContent(current, property.hasMultipleDifferentValues), FocusType.Keyboard)) {
                // The menu answers after this OnGUI: keep the object and path, not the property.
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

        /// <summary>The grouped preset menu, shared by the drawer, the Raven Motion inspector and the library.</summary>
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
