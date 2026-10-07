using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RavenTween.Editor {
    /// <summary>
    /// Plays or scrubs one tween or sequence outside Play Mode, then puts every touched value
    /// back. Values are always restored before entering Play Mode and before script reloads,
    /// so a preview can never leave objects modified in the scene.
    /// </summary>
    [InitializeOnLoad]
    static class EditorTweenPreview {
        struct Snapshot {
            public Object Target;
            public PropertyKind Property;
            public int PropertyId;
            public TweenValue Value;
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
            EditorSceneManagerHooks.Register(End);
        }

        /// <summary>Remembers a value so it can be restored. Call before building the preview.</summary>
        public static void Record(Object target, PropertyKind property, int propertyId) {
            if (target == null || property == PropertyKind.None) { return; }
            for (int i = 0; i < Snapshots.Count; i++) {
                Snapshot s = Snapshots[i];
                if (s.Target == target && s.Property == property && s.PropertyId == propertyId) { return; }
            }
            Snapshots.Add(new Snapshot {
                Target = target, Property = property, PropertyId = propertyId,
                Value = PropertyAccessor.Read(property, target, propertyId)
            });
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
        public static void End() {
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
                if (s.Target != null) { PropertyAccessor.Write(s.Property, s.Target, s.PropertyId, s.Value); }
            }
        }

        static void NotifyChanged() {
            SceneView.RepaintAll();
            if (Changed != null) { Changed(); }
        }
    }

    /// <summary>Ends previews when the open scene changes, without a hard dependency in the type above.</summary>
    static class EditorSceneManagerHooks {
        public static void Register(Action onSceneChange) {
            Debug.Assert(onSceneChange != null, "Scene hook needs a callback.");
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += (scene, removing) => onSceneChange();
        }
    }
}
