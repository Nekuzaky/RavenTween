using System.Collections.Generic;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Plays motions on scene objects outside Play Mode through <see cref="EditorTweenPreview"/>,
    /// which puts every touched value back afterwards.
    /// </summary>
    static class MotionPreview {
        /// <summary>Previews <paramref name="preset"/> on each target, <paramref name="interval"/> seconds apart.</summary>
        public static void Play(object owner, IList<Transform> targets, MotionPreset preset, MotionSettings settings, float interval) {
            Debug.Assert(owner != null && targets != null, "A motion preview needs an owner and targets.");
            EditorTweenPreview.End();
            if (preset == MotionPreset.None || targets.Count == 0 || Application.isPlaying) { return; }
            settings.delay = 0f;
            bool loop = MotionLibrary.Category(preset) == MotionCategory.Loop;
            for (int i = 0; i < targets.Count; i++) { Record(MotionTarget.Resolve(targets[i])); }
            Sequence preview = loop ? BuildLoop(targets[0], preset, settings) : BuildCascade(targets, preset, settings, interval);
            if (!preview.IsAlive) { EditorTweenPreview.End(); return; }
            float length = loop ? preview.Duration : preview.DurationTotal;
            EditorTweenPreview.Begin(owner, preview, length, loop);
            EditorTweenPreview.Play();
        }

        public static void Stop(object owner) {
            if (EditorTweenPreview.Owner == owner || owner == null) { EditorTweenPreview.End(); }
        }

        public static bool IsPlaying(object owner) {
            return EditorTweenPreview.Owner == owner && EditorTweenPreview.IsActive;
        }

        // A loop can't sit inside another sequence, so loops preview on the first target only.
        static Sequence BuildLoop(Transform target, MotionPreset preset, MotionSettings settings) {
            MotionTarget resolved = MotionTarget.Resolve(target);
            return MotionBuilder.Build(resolved, preset, settings, MotionRest.Capture(resolved), out _);
        }

        static Sequence BuildCascade(IList<Transform> targets, MotionPreset preset, MotionSettings settings, float interval) {
            Sequence root = Raven.Sequence();
            for (int i = 0; i < targets.Count; i++) {
                MotionTarget resolved = MotionTarget.Resolve(targets[i]);
                if (!resolved.IsValid) { continue; }
                Sequence child = MotionBuilder.Build(resolved, preset, settings, MotionRest.Capture(resolved), out _);
                if (child.IsAlive) { root.Insert(i * interval, child); }
            }
            return root;
        }

        static void Record(in MotionTarget target) {
            if (!target.IsValid) { return; }
            if (target.Rect != null) { EditorTweenPreview.Record(target.Rect, PropertyKind.AnchoredPosition, 0); }
            else { EditorTweenPreview.Record(target.Transform, PropertyKind.LocalPosition, 0); }
            EditorTweenPreview.Record(target.Transform, PropertyKind.LocalRotation, 0);
            EditorTweenPreview.Record(target.Transform, PropertyKind.LocalScale, 0);
            if (target.Group != null) { EditorTweenPreview.Record(target.Group, PropertyKind.CanvasGroupAlpha, 0); }
            if (target.Graphic != null) { EditorTweenPreview.Record(target.Graphic, PropertyKind.GraphicColor, 0); }
            if (target.Sprite != null) { EditorTweenPreview.Record(target.Sprite, PropertyKind.SpriteColor, 0); }
            if (target.Renderer != null) { EditorTweenPreview.Record(target.Renderer, PropertyKind.PropertyBlockColor, target.ColorId); }
        }
    }
}
