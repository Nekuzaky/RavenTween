using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RavenTween.Editor {
    /// <summary>
    /// Plays or scrubs one tween or sequence outside Play Mode, then puts every touched value
    /// back. Values are restored before entering Play Mode, before script reloads, before any
    /// scene, prefab or asset is saved and before quitting, so a preview never ends up saved.
    /// </summary>
    /// <remarks>
    /// Transform values are recorded in local space (anchored space for UI), so restoring a
    /// parent and its child in any order gives back the exact original layout. Editing an
    /// animated object while a preview is shown stops the preview and restores the scene first.
    /// </remarks>
    [InitializeOnLoad]
    static class EditorTweenPreview {
        enum Channel : byte { Property, LocalPosition, AnchoredPosition3D, PropertyBlock }

        struct Snapshot {
            public Object Target;
            public Channel Channel;
            public PropertyKind Property;
            public int PropertyId;
            public TweenValue Value;
            public bool HadBlock;
        }

        static readonly List<Snapshot> Snapshots = new List<Snapshot>(16);
        static int _index = -1;
        static uint _version;
        static double _lastTick;
        static bool _loop;

        /// <summary>Who started the current preview; lets several windows share the service.</summary>
        public static object Owner { get; private set; }
        public static bool IsActive { get { return _index >= 0; } }
        public static bool IsPlaying { get; private set; }
        public static float Time { get; private set; }
        public static float Length { get; private set; }

        /// <summary>Raised whenever the preview time or state changes; repaint on it.</summary>
        public static event Action Changed;

        static EditorTweenPreview() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingEditMode) { End(); }
            };
            AssemblyReloadEvents.beforeAssemblyReload += End;
            EditorApplication.wantsToQuit += () => { End(); return true; };
            EditorSceneManager.sceneClosing += (scene, removing) => End();
            EditorSceneManager.sceneSaving += (scene, path) => End();
            PrefabStage.prefabSaving += root => End();
            PrefabStage.prefabStageClosing += stage => End();
            Undo.postprocessModifications += OnUserModifications;
        }

        /// <summary>True while values are recorded and not yet restored.</summary>
        internal static bool HasSnapshots { get { return Snapshots.Count > 0; } }

        /// <summary>Remembers a value so it can be restored. Call before building the preview.</summary>
        public static void Record(Object target, PropertyKind property, int propertyId) {
            if (target == null || property == PropertyKind.None) { return; }
            if (target is Material material && !material.HasProperty(propertyId)) { return; }
            if (!PropertyAccessor.TargetType(property).IsInstanceOfType(target)) { return; }
            Snapshot snapshot = MakeSnapshot(target, property, propertyId);
            for (int i = 0; i < Snapshots.Count; i++) {
                Snapshot s = Snapshots[i];
                if (s.Target == target && s.Channel == snapshot.Channel && s.Property == snapshot.Property && s.PropertyId == propertyId) { return; }
            }
            Snapshots.Add(snapshot);
        }

        // World-space and euler transform properties are stored as their local equivalents.
        static Snapshot MakeSnapshot(Object target, PropertyKind property, int propertyId) {
            var snapshot = new Snapshot { Target = target, Channel = Channel.Property, Property = property, PropertyId = propertyId };
            switch (property) {
                case PropertyKind.Position:
                case PropertyKind.LocalPosition:
                case PropertyKind.AnchoredPosition:
                case PropertyKind.PositionX:
                case PropertyKind.PositionY:
                case PropertyKind.PositionZ:
                case PropertyKind.LocalPositionX:
                case PropertyKind.LocalPositionY:
                case PropertyKind.LocalPositionZ:
                case PropertyKind.AnchoredPositionX:
                case PropertyKind.AnchoredPositionY:
                    snapshot.Channel = target is RectTransform ? Channel.AnchoredPosition3D : Channel.LocalPosition;
                    snapshot.Property = PropertyKind.LocalPosition;
                    break;
                case PropertyKind.Rotation:
                case PropertyKind.EulerAngles:
                case PropertyKind.LocalEulerAngles:
                    snapshot.Property = PropertyKind.LocalRotation;
                    break;
                case PropertyKind.LocalScaleX:
                case PropertyKind.LocalScaleY:
                case PropertyKind.LocalScaleZ:
                    snapshot.Property = PropertyKind.LocalScale;
                    break;
                case PropertyKind.PropertyBlockFloat:
                case PropertyKind.PropertyBlockColor:
                    snapshot.Channel = Channel.PropertyBlock;
                    snapshot.HadBlock = ((Renderer)target).HasPropertyBlock();
                    break;
            }
            snapshot.Value = ReadSnapshot(snapshot);
            return snapshot;
        }

        static TweenValue ReadSnapshot(in Snapshot s) {
            switch (s.Channel) {
                case Channel.LocalPosition: return new TweenValue(((Transform)s.Target).localPosition);
                case Channel.AnchoredPosition3D: return new TweenValue(((RectTransform)s.Target).anchoredPosition3D);
                default: return PropertyAccessor.Read(s.Property, s.Target, s.PropertyId);
            }
        }

        static void WriteSnapshot(in Snapshot s) {
            switch (s.Channel) {
                case Channel.LocalPosition: ((Transform)s.Target).localPosition = s.Value.Vector3; break;
                case Channel.AnchoredPosition3D: ((RectTransform)s.Target).anchoredPosition3D = s.Value.Vector3; break;
                case Channel.PropertyBlock:
                    // A renderer that had no block before the preview gets none back.
                    if (s.HadBlock) { PropertyAccessor.Write(s.Property, s.Target, s.PropertyId, s.Value); }
                    else { ((Renderer)s.Target).SetPropertyBlock(null); }
                    break;
                default: PropertyAccessor.Write(s.Property, s.Target, s.PropertyId, s.Value); break;
            }
        }

        /// <summary>Takes over a freshly built tween or sequence. Ends any previous preview.</summary>
        public static void Begin(object owner, int index, uint version, float length, bool loop) {
            Debug.Assert(owner != null, "A preview needs an owner.");
            Debug.Assert(length >= 0f, "Preview length cannot be negative.");
            StopTicking();
            _index = index;
            _version = version;
            _loop = loop;
            Owner = owner;
            Length = Mathf.Max(length, 0.0001f);
            Seek(0f);
        }

        public static void Begin(object owner, Tween tween, float length, bool loop) {
            Begin(owner, tween.Index, tween.Version, length, loop);
        }

        public static void Begin(object owner, Sequence sequence, float length, bool loop) {
            Begin(owner, sequence.Index, sequence.Version, length, loop);
        }

        /// <summary>Shows the pose at <paramref name="time"/> seconds.</summary>
        public static void Seek(float time) {
            if (!IsActive) { return; }
            Time = Mathf.Clamp(time, 0f, Length);
            RestoreValues();
            TweenEngine.Seek(_index, _version, Time);
            NotifyChanged();
        }

        public static void Play() {
            if (!IsActive || IsPlaying) { return; }
            if (Time >= Length) { Time = 0f; }
            IsPlaying = true;
            _lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            NotifyChanged();
        }

        public static void Pause() {
            StopTicking();
            NotifyChanged();
        }

        /// <summary>Stops the preview, kills its tweens and restores every recorded value.</summary>
        /// <remarks>Also safe after a failed build: whatever was recorded is restored and forgotten.</remarks>
        public static void End() {
            if (!IsActive && Snapshots.Count == 0 && !IsPlaying) { return; }
            StopTicking();
            if (_index >= 0) { TweenEngine.Kill(_index, _version, false); }
            _index = -1;
            RestoreValues();
            Snapshots.Clear();
            Owner = null;
            Time = 0f;
            NotifyChanged();
        }

        static void Tick() {
            if (!IsActive) { StopTicking(); return; }
            double now = EditorApplication.timeSinceStartup;
            float next = Time + (float)(now - _lastTick);
            _lastTick = now;
            if (next >= Length) {
                if (_loop) { next %= Length; }
                else { Seek(Length); StopTicking(); NotifyChanged(); return; }
            }
            Seek(next);
        }

        static void StopTicking() {
            if (!IsPlaying) { return; }
            IsPlaying = false;
            EditorApplication.update -= Tick;
        }

        static void RestoreValues() {
            for (int i = Snapshots.Count - 1; i >= 0; i--) {
                Snapshot s = Snapshots[i];
                if (s.Target != null) { WriteSnapshot(s); }
            }
        }

        // An edit to an animated object would be recorded on top of a preview pose and then
        // reverted by End. Instead the preview ends first and the edit is not recorded.
        static UndoPropertyModification[] OnUserModifications(UndoPropertyModification[] modifications) {
            if (Snapshots.Count == 0 || !TouchesRecordedTarget(modifications)) { return modifications; }
            var kept = new List<UndoPropertyModification>(modifications.Length);
            for (int i = 0; i < modifications.Length; i++) {
                if (!IsRecorded(TargetOf(modifications[i]))) { kept.Add(modifications[i]); }
            }
            End();
            return kept.ToArray();
        }

        static bool TouchesRecordedTarget(UndoPropertyModification[] modifications) {
            for (int i = 0; i < modifications.Length; i++) {
                if (IsRecorded(TargetOf(modifications[i]))) { return true; }
            }
            return false;
        }

        static Object TargetOf(UndoPropertyModification modification) {
            PropertyModification current = modification.currentValue;
            return current != null ? current.target : null;
        }

        static bool IsRecorded(Object target) {
            if (target == null) { return false; }
            for (int i = 0; i < Snapshots.Count; i++) {
                if (Snapshots[i].Target == target) { return true; }
            }
            return false;
        }

        static void NotifyChanged() {
            SceneView.RepaintAll();
            if (Changed != null) { Changed(); }
        }
    }

    /// <summary>Restores previewed values before any asset (prefab, material, scene) is written to disk.</summary>
    sealed class EditorTweenPreviewSaveGuard : UnityEditor.AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            if (EditorTweenPreview.HasSnapshots) { EditorTweenPreview.End(); }
            return paths;
        }
    }
}
