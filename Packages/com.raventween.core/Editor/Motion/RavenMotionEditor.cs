using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenTween.Editor {
    /// <summary>
    /// Raven Motion inspector: a grouped preset picker with a plain-language description,
    /// triggers named the way people think of them, settings that only show when they matter,
    /// warnings with one-click fixes, and a preview in the scene outside Play Mode.
    /// </summary>
    [CustomEditor(typeof(RavenMotion)), CanEditMultipleObjects]
    public sealed class RavenMotionEditor : UnityEditor.Editor {
        static readonly GUIContent[] TriggerLabels = {
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

        SerializedProperty _style;
        SerializedProperty _preset;
        SerializedProperty _trigger;
        SerializedProperty _target;
        SerializedProperty _duration;
        SerializedProperty _intensity;
        SerializedProperty _delay;
        SerializedProperty _distance;
        SerializedProperty _repeat;
        SerializedProperty _unscaled;
        SerializedProperty _color;
        SerializedProperty _thenPlay;
        SerializedProperty _filterTag;
        SerializedProperty _whenFinished;
        SerializedProperty _onComplete;

        void OnEnable() {
            _style = serializedObject.FindProperty("style");
            _preset = serializedObject.FindProperty("preset");
            _trigger = serializedObject.FindProperty("trigger");
            _target = serializedObject.FindProperty("target");
            SerializedProperty settings = serializedObject.FindProperty("settings");
            _duration = settings.FindPropertyRelative("duration");
            _intensity = settings.FindPropertyRelative("intensity");
            _delay = settings.FindPropertyRelative("delay");
            _distance = settings.FindPropertyRelative("distance");
            _repeat = settings.FindPropertyRelative("repeat");
            _unscaled = settings.FindPropertyRelative("useUnscaledTime");
            _color = settings.FindPropertyRelative("color");
            _thenPlay = serializedObject.FindProperty("thenPlay");
            _filterTag = serializedObject.FindProperty("filterTag");
            _whenFinished = serializedObject.FindProperty("whenFinished");
            _onComplete = serializedObject.FindProperty("onComplete");
        }

        void OnDisable() {
            MotionPreview.Stop(this);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            var style = _style.objectReferenceValue as MotionStyle;
            var preset = style != null ? style.Preset : (MotionPreset)_preset.intValue;
            EditorGUILayout.PropertyField(_style, new GUIContent("Style (optional)", "A saved Motion Style replaces the motion and feel below. Shared by every object that uses it."));
            if (style != null && !_style.hasMultipleDifferentValues) { DrawStyleSummary(style); }
            else { DrawPreset(preset); }
            DrawTrigger();
            EditorGUILayout.PropertyField(_target, new GUIContent("Animate", "Object to animate. Empty = this object."));
            EditorGUILayout.Space(4f);
            if (style == null) { DrawFeel(preset); }
            DrawFinish(preset);
            EditorGUILayout.PropertyField(_thenPlay, new GUIContent("Then Play", "Another Raven Motion to start when this one ends, e.g. Pop In then Float. Set its start to Manually."));
            EditorGUILayout.PropertyField(_onComplete);
            serializedObject.ApplyModifiedProperties();
            DrawWarnings(preset);
            DrawPreviewBar();
        }

        // ----- Sections -----

        static void DrawStyleSummary(MotionStyle style) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MotionLibrary.DisplayName(style.Preset) + "  ·  " + MotionLibrary.Category(style.Preset), EditorStyles.boldLabel);
            if (GUILayout.Button("Edit Style", EditorStyles.miniButton, GUILayout.Width(80f))) { Selection.activeObject = style; }
            EditorGUILayout.EndHorizontal();
            string notes = string.IsNullOrEmpty(style.Notes) ? MotionLibrary.Description(style.Preset) : style.Notes;
            EditorGUILayout.LabelField(notes, EditorStyles.wordWrappedMiniLabel);
        }

        void DrawPreset(MotionPreset preset) {
            EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 24f);
            if (EditorGUI.DropdownButton(rect, MotionPresetDrawer.ButtonContent(preset, _preset.hasMultipleDifferentValues), FocusType.Keyboard)) {
                SerializedObject owner = serializedObject;
                MotionPresetDrawer.ShowMenu(rect, preset, chosen => {
                    owner.Update();
                    owner.FindProperty("preset").intValue = (int)chosen;
                    owner.ApplyModifiedProperties();
                    MotionPreview.Stop(this);
                });
            }
            if (!_preset.hasMultipleDifferentValues && preset != MotionPreset.None) {
                EditorGUILayout.LabelField(MotionLibrary.Description(preset), EditorStyles.wordWrappedMiniLabel);
            }
            if (GUILayout.Button("Browse all motions…", EditorStyles.miniButton)) { MotionLibraryWindow.Open(); }
        }

        void DrawTrigger() {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Starts", EditorStyles.boldLabel);
            EditorGUI.showMixedValue = _trigger.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int index = EditorGUILayout.Popup(new GUIContent("When"), _trigger.intValue, TriggerLabels);
            if (EditorGUI.EndChangeCheck()) { _trigger.intValue = index; }
            EditorGUI.showMixedValue = false;
            var trigger = (MotionTrigger)_trigger.intValue;
            if (trigger >= MotionTrigger.OnTriggerEnter && trigger <= MotionTrigger.OnCollisionEnter) {
                EditorGUILayout.PropertyField(_filterTag, new GUIContent("Only Tag", "Only objects with this tag start the motion. Empty = any object."));
            }
        }

        void DrawFeel(MotionPreset preset) {
            EditorGUILayout.LabelField("Feel", EditorStyles.boldLabel);
            float auto = MotionLibrary.DefaultDuration(preset);
            AutoFloat(_duration, "Duration (s)", "Auto (" + auto.ToString("0.##") + " s)", auto);
            EditorGUILayout.Slider(_intensity, 0.1f, 3f, new GUIContent("Intensity", "0.5 = subtle, 1 = as designed, 2 = exaggerated."));
            EditorGUILayout.PropertyField(_delay, new GUIContent("Delay (s)"));
            if (MotionLibrary.UsesDistance(preset)) {
                AutoFloat(_distance, "Distance", "Auto (from the object's size)", 100f);
            }
            if (MotionLibrary.Category(preset) == MotionCategory.Attention) {
                EditorGUILayout.PropertyField(_repeat, new GUIContent("Repeat", "How many times in a row."));
            }
            if (MotionLibrary.NeedsColor(preset)) {
                EditorGUILayout.PropertyField(_color, new GUIContent("Color", "The color it flashes: red for damage, yellow for warnings, white for highlights."));
            }
            EditorGUILayout.PropertyField(_unscaled, new GUIContent("Play While Paused", "Keep playing when Time.timeScale is 0 (pause menus)."));
        }

        // A float where 0 means automatic, shown as a checkbox plus the value.
        static void AutoFloat(SerializedProperty property, string label, string autoLabel, float suggested) {
            EditorGUILayout.BeginHorizontal();
            bool isAuto = property.floatValue <= 0f && !property.hasMultipleDifferentValues;
            bool wantAuto = EditorGUILayout.ToggleLeft(new GUIContent("Auto " + label.ToLowerInvariant()), isAuto, GUILayout.Width(EditorGUIUtility.labelWidth));
            if (wantAuto != isAuto) { property.floatValue = wantAuto ? 0f : suggested; }
            if (wantAuto) { EditorGUILayout.LabelField(autoLabel, EditorStyles.miniLabel); }
            else { property.floatValue = Mathf.Max(EditorGUILayout.FloatField(property.floatValue), 0.01f); }
            EditorGUILayout.EndHorizontal();
        }

        void DrawFinish(MotionPreset preset) {
            bool exit = MotionLibrary.Category(preset) == MotionCategory.Exit;
            if (!exit && _whenFinished.intValue == (int)MotionFinish.Nothing) { return; }
            EditorGUILayout.PropertyField(_whenFinished, new GUIContent("When Finished", "Deactivate is typical after an exit, so the hidden object stops taking clicks."));
        }

        // ----- Warnings with fixes -----

        void DrawWarnings(MotionPreset preset) {
            if (targets.Length != 1) { return; }
            var motion = (RavenMotion)target;
            preset = motion.EffectivePreset;
            Transform animated = motion.AnimatedTransform;
            MotionTarget resolved = MotionTarget.Resolve(animated);
            var trigger = motion.Trigger;
            if (MotionLibrary.NeedsAlpha(preset) && !resolved.HasAlpha) { WarnNoAlpha(animated); }
            if (MotionLibrary.NeedsColor(preset) && !resolved.HasColor) {
                EditorGUILayout.HelpBox("Nothing here has a color (an Image, a Text, a Sprite Renderer or a renderer whose material has _BaseColor or _Color). The motion pulses instead.", MessageType.Warning);
            }
            if (motion.ThenPlay != null && motion.ThenPlay != motion && motion.ThenPlay.Trigger != MotionTrigger.Manual) {
                EditorGUILayout.HelpBox("'" + motion.ThenPlay.EffectivePreset + "' also starts on its own trigger. Set its start to Manually so it only plays after this one.", MessageType.Info);
            }
            if (MotionLibrary.MovesPosition(preset) && resolved.Rect != null && animated.parent != null && animated.parent.GetComponent<LayoutGroup>() != null) {
                EditorGUILayout.HelpBox("This object sits in a Layout Group, which places it every time the layout changes. Prefer motions that don't move it (fades, pops, pulses).", MessageType.Info);
            }
            if (trigger >= MotionTrigger.OnClick && trigger <= MotionTrigger.OnPointerUp) { WarnPointer(motion); }
            if (trigger >= MotionTrigger.OnTriggerEnter && trigger <= MotionTrigger.OnCollisionEnter) { WarnPhysics(motion, trigger); }
            if (MotionLibrary.Category(preset) == MotionCategory.Exit && trigger == MotionTrigger.OnEnable) {
                EditorGUILayout.HelpBox("An exit that plays when the object is enabled hides it right away. Usually an exit is started Manually, e.g. from a Close button's On Click.", MessageType.Info);
            }
        }

        static void WarnNoAlpha(Transform animated) {
            bool ui = animated is RectTransform;
            string text = ui
                ? "Nothing here can fade. Add a Canvas Group to fade this object and its children; until then the motion scales instead."
                : "Nothing here can fade (only UI, Canvas Groups and Sprite Renderers can). The motion scales instead.";
            EditorGUILayout.HelpBox(text, MessageType.Warning);
            if (ui && GUILayout.Button("Add Canvas Group")) { Undo.AddComponent<CanvasGroup>(animated.gameObject); }
        }

        static void WarnPointer(RavenMotion motion) {
            if (!SceneHasEventSystem()) {
                EditorGUILayout.HelpBox("Clicks and hovers need an Event System in the scene.", MessageType.Warning);
                if (GUILayout.Button("Create Event System")) { CreateEventSystem(); }
            }
            bool ui = motion.transform is RectTransform;
            if (ui && motion.GetComponent<Graphic>() == null) {
                EditorGUILayout.HelpBox("A UI object receives clicks through a Graphic with Raycast Target on (an Image, a Text…).", MessageType.Warning);
            }
            if (!ui && !HasAnyCollider(motion.gameObject)) {
                EditorGUILayout.HelpBox("A scene object receives clicks through a Collider, and the camera needs a Physics Raycaster (Physics 2D Raycaster for 2D).", MessageType.Warning);
                DrawAddColliderButtons(motion.gameObject, false);
            }
        }

        static bool SceneHasEventSystem() {
#if UNITY_2022_2_OR_NEWER
            return Object.FindAnyObjectByType<EventSystem>() != null;
#else
            return Object.FindObjectOfType<EventSystem>() != null;
#endif
        }

        static bool HasAnyCollider(GameObject go) {
#if RAVEN_MODULE_PHYSICS
            if (go.GetComponent<Collider>() != null) { return true; }
#endif
#if RAVEN_MODULE_PHYSICS2D
            if (go.GetComponent<Collider2D>() != null) { return true; }
#endif
            return false;
        }

        static bool ColliderIsTrigger(GameObject go) {
#if RAVEN_MODULE_PHYSICS
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) { return collider.isTrigger; }
#endif
#if RAVEN_MODULE_PHYSICS2D
            Collider2D collider2D = go.GetComponent<Collider2D>();
            if (collider2D != null) { return collider2D.isTrigger; }
#endif
            return false;
        }

        static void DrawAddColliderButtons(GameObject go, bool asTrigger) {
            EditorGUILayout.BeginHorizontal();
#if RAVEN_MODULE_PHYSICS
            if (GUILayout.Button("Add Box Collider")) { Undo.AddComponent<BoxCollider>(go).isTrigger = asTrigger; }
#endif
#if RAVEN_MODULE_PHYSICS2D
            if (GUILayout.Button("Add Box Collider 2D")) { Undo.AddComponent<BoxCollider2D>(go).isTrigger = asTrigger; }
#endif
            EditorGUILayout.EndHorizontal();
        }

        static void WarnPhysics(RavenMotion motion, MotionTrigger trigger) {
            if (!HasAnyCollider(motion.gameObject)) {
                EditorGUILayout.HelpBox("This needs a Collider on this object.", MessageType.Warning);
                DrawAddColliderButtons(motion.gameObject, trigger != MotionTrigger.OnCollisionEnter);
                return;
            }
            if (trigger != MotionTrigger.OnCollisionEnter && !ColliderIsTrigger(motion.gameObject)) {
                EditorGUILayout.HelpBox("The collider is not a trigger, so nothing can enter it. Tick Is Trigger on it.", MessageType.Warning);
            }
            EditorGUILayout.HelpBox("Unity reports triggers and collisions only when one of the two objects has a Rigidbody.", MessageType.None);
        }

        // Unity's own menu item picks the input module that matches the project (Input System or legacy).
        static void CreateEventSystem() {
            EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");
        }

        // ----- Preview -----

        void DrawPreviewBar() {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying) {
                if (Button(RavenEditorIcons.Play, "Play", "Play the motion now")) { ForEachMotion(m => m.Play()); }
                if (Button(RavenEditorIcons.Stop, "Stop", "Stop and return to the rest pose")) { ForEachMotion(m => m.Stop()); }
                if (Button(RavenEditorIcons.Complete, "Complete", "Jump to the end")) { ForEachMotion(m => m.Complete()); }
            } else {
                bool playing = MotionPreview.IsPlaying(this);
                if (Button(RavenEditorIcons.Play, playing ? "Replay" : "Preview", "Play the motion on the object in the scene. Everything is put back afterwards.")) { StartPreview(); }
                using (new EditorGUI.DisabledScope(!playing)) {
                    if (Button(RavenEditorIcons.Stop, "Reset", "Stop the preview and put the object back")) { MotionPreview.Stop(this); }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void StartPreview() {
            var motion = (RavenMotion)target;
            var list = new List<Transform>(targets.Length);
            for (int i = 0; i < targets.Length; i++) { list.Add(((RavenMotion)targets[i]).AnimatedTransform); }
            MotionPreview.Play(this, list, motion.EffectivePreset, motion.EffectiveSettings, 0f);
        }

        void ForEachMotion(System.Action<RavenMotion> action) {
            for (int i = 0; i < targets.Length; i++) { action((RavenMotion)targets[i]); }
        }

        static bool Button(Texture icon, string label, string tooltip) {
            return GUILayout.Button(RavenEditorIcons.Content(icon, label, tooltip), GUILayout.Height(24f));
        }
    }
}
