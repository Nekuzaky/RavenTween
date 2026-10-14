using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Draws a feedback action with only the fields its kind uses.</summary>
    [CustomPropertyDrawer(typeof(FeedbackAction))]
    public sealed class FeedbackActionDrawer : PropertyDrawer {
        const float Gap = 2f;
        static readonly List<string> Fields = new List<string>(8);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUIUtility.singleLineHeight + Gap;
            if (!property.isExpanded) { return height; }
            CollectFields(property);
            for (int i = 0; i < Fields.Count; i++) {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(Fields[i]), true) + Gap;
            }
            return height + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(new Rect(line.x, line.y, 14f, line.height), property.isExpanded, GUIContent.none, true);
            Rect kindRect = new Rect(line.x + 14f, line.y, line.width * 0.55f - 14f, line.height);
            Rect delayRect = new Rect(line.x + line.width * 0.55f + 4f, line.y, line.width * 0.45f - 4f, line.height);
            EditorGUI.PropertyField(kindRect, property.FindPropertyRelative("kind"), GUIContent.none);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40f;
            EditorGUI.PropertyField(delayRect, property.FindPropertyRelative("delay"), new GUIContent("after", "Seconds after the feedback starts."));
            EditorGUIUtility.labelWidth = labelWidth;
            if (property.isExpanded) { DrawFields(position, line, property); }
            EditorGUI.EndProperty();
        }

        static void DrawFields(Rect position, Rect line, SerializedProperty property) {
            CollectFields(property);
            float y = line.yMax + Gap;
            EditorGUI.indentLevel++;
            for (int i = 0; i < Fields.Count; i++) {
                SerializedProperty field = property.FindPropertyRelative(Fields[i]);
                float h = EditorGUI.GetPropertyHeight(field, true);
                var rect = new Rect(position.x, y, position.width, h);
                if (Fields[i] == "motion") { DrawMotion(rect, field); }
                else if (Fields[i] == "particles") { DrawParticles(rect, field); }
                else { EditorGUI.PropertyField(rect, field, Label(Fields[i], property), true); }
                y += h + Gap;
            }
            EditorGUI.indentLevel--;
        }

        static void DrawMotion(Rect rect, SerializedProperty field) {
            Rect button = EditorGUI.PrefixLabel(rect, new GUIContent("Motion"));
            var current = (MotionPreset)field.intValue;
            if (EditorGUI.DropdownButton(button, MotionPresetDrawer.ButtonContent(current, field.hasMultipleDifferentValues), FocusType.Keyboard)) {
                SerializedObject owner = field.serializedObject;
                string path = field.propertyPath;
                MotionPresetDrawer.ShowMenu(button, current, chosen => {
                    owner.Update();
                    owner.FindProperty(path).intValue = (int)chosen;
                    owner.ApplyModifiedProperties();
                });
            }
        }

        static void DrawParticles(Rect rect, SerializedProperty field) {
#if RAVEN_MODULE_PARTICLES
            field.objectReferenceValue = EditorGUI.ObjectField(rect, "Particles", field.objectReferenceValue as ParticleSystem, typeof(ParticleSystem), true);
#else
            EditorGUI.LabelField(rect, "Particles", "The Particle System module is disabled in this project.");
#endif
        }

        static GUIContent Label(string field, SerializedProperty property) {
            var kind = (FeedbackAction.Kind)property.FindPropertyRelative("kind").intValue;
            switch (field) {
                case "target": return new GUIContent(kind == FeedbackAction.Kind.SetActive ? "Object" : "Animate", "Empty = this object.");
                case "strength": return new GUIContent(kind == FeedbackAction.Kind.ColorFlash ? "Strength (0-1)" : "Strength", "1 = a solid hit, 0.3 = light, 2 = huge.");
                case "duration": return new GUIContent("Duration (s)");
                case "timeScale": return new GUIContent("Slow Down To", "Time.timeScale while it lasts (0.05 = almost frozen, 0.3 = slow motion).");
                case "direction": return new GUIContent(kind == FeedbackAction.Kind.Twist ? "Axis (degrees)" : "Direction", kind == FeedbackAction.Kind.Twist ? "Degrees around each axis, times the strength." : "Local offset of the kick, times the strength.");
                case "parameter": return new GUIContent("Trigger", "Name of the Animator trigger parameter.");
                case "lifetime": return new GUIContent("Destroy After (s)", "0 = keep the spawned object.");
                case "chance": return new GUIContent("Chance", "1 = always, 0.3 = three times in ten.");
                case "variation": return new GUIContent("Variation", "Random change of the strength each time, up to this fraction more or less.");
                case "active": return new GUIContent("Turn On", "Ticked: turn the object on. Unticked: off.");
                case "onPlay": return new GUIContent("On Play");
                default: return new GUIContent(ObjectNames.NicifyVariableName(field));
            }
        }

        static void CollectFields(SerializedProperty property) {
            Fields.Clear();
            switch ((FeedbackAction.Kind)property.FindPropertyRelative("kind").intValue) {
                case FeedbackAction.Kind.Motion: Fields.Add("target"); Fields.Add("motion"); Fields.Add("settings"); break;
                case FeedbackAction.Kind.CameraShake: Fields.Add("camera"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.Punch: Fields.Add("target"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.ColorFlash: Fields.Add("target"); Fields.Add("color"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.Sound: Fields.Add("clip"); Fields.Add("source"); Fields.Add("volume"); Fields.Add("pitchMin"); Fields.Add("pitchMax"); break;
                case FeedbackAction.Kind.HitStop: Fields.Add("timeScale"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.Particles: Fields.Add("particles"); break;
                case FeedbackAction.Kind.SetActive: Fields.Add("target"); Fields.Add("active"); break;
                case FeedbackAction.Kind.LightFlash: Fields.Add("light"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.FieldOfView: Fields.Add("camera"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.Nudge: Fields.Add("target"); Fields.Add("direction"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.Twist: Fields.Add("target"); Fields.Add("direction"); Fields.Add("strength"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.SlowMotion: Fields.Add("timeScale"); Fields.Add("duration"); break;
                case FeedbackAction.Kind.AnimatorTrigger: Fields.Add("target"); Fields.Add("parameter"); break;
                case FeedbackAction.Kind.Spawn: Fields.Add("target"); Fields.Add("prefab"); Fields.Add("lifetime"); break;
                default: Fields.Add("onPlay"); break;
            }
            Fields.Add("chance");
            if (Fields.Contains("strength")) { Fields.Add("variation"); }
        }
    }

    /// <summary>Raven Feedback inspector: the action list, quick-add buttons and a Play button.</summary>
    [CustomEditor(typeof(RavenFeedback))]
    public sealed class RavenFeedbackEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            SerializedProperty triggerProperty = serializedObject.FindProperty("trigger");
            EditorGUI.showMixedValue = triggerProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUILayout.Popup(new GUIContent("Starts", "What plays the feedback. Manually = call Play() from any UnityEvent."), triggerProperty.intValue, MotionPresetDrawer.TriggerLabels);
            if (EditorGUI.EndChangeCheck()) { triggerProperty.intValue = chosen; }
            EditorGUI.showMixedValue = false;
            var trigger = (MotionTrigger)triggerProperty.intValue;
            if (MotionTriggers.IsPhysics(trigger)) { EditorGUILayout.PropertyField(serializedObject.FindProperty("filterTag"), new GUIContent("Only Tag")); }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("actions"), true);
            DrawQuickAdd();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Play, "Play", "Play the feedback now"), GUILayout.Height(24f))) {
                    for (int i = 0; i < targets.Length; i++) { ((RavenFeedback)targets[i]).Play(); }
                }
            }
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Feedbacks shake cameras, play sounds and change time, so they are tried in Play Mode.", MessageType.None);
            }
        }

        void DrawQuickAdd() {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add", EditorStyles.miniBoldLabel, GUILayout.Width(28f));
            if (GUILayout.Button("Hit", EditorStyles.miniButtonLeft)) { AddHit(); }
            if (GUILayout.Button("Pickup", EditorStyles.miniButtonMid)) { AddPickup(); }
            if (GUILayout.Button("Damage", EditorStyles.miniButtonRight)) { AddDamage(); }
            EditorGUILayout.EndHorizontal();
        }

        // Ready-made recipes: a starting point anyone can tweak.
        void AddHit() {
            Add(FeedbackAction.Kind.CameraShake, 0f, a => { a.FindPropertyRelative("strength").floatValue = 0.6f; a.FindPropertyRelative("duration").floatValue = 0.25f; });
            Add(FeedbackAction.Kind.HitStop, 0f, a => { a.FindPropertyRelative("timeScale").floatValue = 0.05f; a.FindPropertyRelative("duration").floatValue = 0.07f; });
            Add(FeedbackAction.Kind.Punch, 0f, a => { a.FindPropertyRelative("strength").floatValue = 1f; a.FindPropertyRelative("duration").floatValue = 0.3f; });
        }

        void AddPickup() {
            Add(FeedbackAction.Kind.Motion, 0f, a => a.FindPropertyRelative("motion").intValue = (int)MotionPreset.PopOut);
            Add(FeedbackAction.Kind.Sound, 0f, a => { });
        }

        void AddDamage() {
            Add(FeedbackAction.Kind.ColorFlash, 0f, a => { a.FindPropertyRelative("strength").floatValue = 1f; a.FindPropertyRelative("duration").floatValue = 0.35f; });
            Add(FeedbackAction.Kind.Motion, 0f, a => a.FindPropertyRelative("motion").intValue = (int)MotionPreset.Shake);
            Add(FeedbackAction.Kind.CameraShake, 0f, a => { a.FindPropertyRelative("strength").floatValue = 0.35f; a.FindPropertyRelative("duration").floatValue = 0.2f; });
        }

        void Add(FeedbackAction.Kind kind, float delay, System.Action<SerializedProperty> setup) {
            SerializedProperty list = serializedObject.FindProperty("actions");
            list.arraySize++;
            SerializedProperty action = list.GetArrayElementAtIndex(list.arraySize - 1);
            ResetAction(action);
            action.FindPropertyRelative("kind").intValue = (int)kind;
            action.FindPropertyRelative("delay").floatValue = delay;
            action.isExpanded = true;
            setup(action);
        }

        // A new array element copies the previous one; start every recipe from the defaults instead.
        static void ResetAction(SerializedProperty action) {
            action.FindPropertyRelative("target").objectReferenceValue = null;
            action.FindPropertyRelative("motion").intValue = (int)MotionPreset.Pulse;
            action.FindPropertyRelative("camera").objectReferenceValue = null;
            action.FindPropertyRelative("strength").floatValue = 1f;
            action.FindPropertyRelative("duration").floatValue = 0.3f;
            action.FindPropertyRelative("color").colorValue = MotionSettings.DefaultTint;
            action.FindPropertyRelative("clip").objectReferenceValue = null;
            action.FindPropertyRelative("source").objectReferenceValue = null;
            action.FindPropertyRelative("volume").floatValue = 1f;
            action.FindPropertyRelative("pitchMin").floatValue = 1f;
            action.FindPropertyRelative("pitchMax").floatValue = 1f;
            action.FindPropertyRelative("timeScale").floatValue = 0.05f;
            action.FindPropertyRelative("particles").objectReferenceValue = null;
            action.FindPropertyRelative("active").boolValue = true;
            action.FindPropertyRelative("light").objectReferenceValue = null;
            action.FindPropertyRelative("direction").vector3Value = new Vector3(0f, 0.2f, 0f);
            action.FindPropertyRelative("parameter").stringValue = "";
            action.FindPropertyRelative("prefab").objectReferenceValue = null;
            action.FindPropertyRelative("lifetime").floatValue = 2f;
            action.FindPropertyRelative("chance").floatValue = 1f;
            action.FindPropertyRelative("variation").floatValue = 0f;
            SerializedProperty calls = action.FindPropertyRelative("onPlay.m_PersistentCalls.m_Calls");
            if (calls != null) { calls.arraySize = 0; }
            SerializedProperty settings = action.FindPropertyRelative("settings");
            settings.FindPropertyRelative("duration").floatValue = 0f;
            settings.FindPropertyRelative("intensity").floatValue = 1f;
            settings.FindPropertyRelative("delay").floatValue = 0f;
            settings.FindPropertyRelative("distance").floatValue = 0f;
            settings.FindPropertyRelative("repeat").intValue = 1;
            settings.FindPropertyRelative("useUnscaledTime").boolValue = false;
            settings.FindPropertyRelative("color").colorValue = MotionSettings.DefaultTint;
        }
    }
}
