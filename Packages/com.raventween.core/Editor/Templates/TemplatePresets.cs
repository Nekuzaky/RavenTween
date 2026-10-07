using System;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Ready-made template settings, applied from the Tween Template inspector.</summary>
    static class TemplatePresets {
        public struct Preset {
            public string Menu;
            public Action<TweenTemplate> Apply;
        }

        public static readonly Preset[] All = {
            Make("Scale/Pop In", PropertyKind.LocalScale, Vector3.one, Vector3.zero, 0.35f, Ease.OutBack),
            Make("Scale/Pop Out", PropertyKind.LocalScale, Vector3.zero, null, 0.25f, Ease.InBack),
            Make("Scale/Pulse (loop)", PropertyKind.LocalScale, Vector3.one * 1.1f, null, 0.5f, Ease.InOutSine, -1, CycleMode.Yoyo),
            Make("Scale/Squash", PropertyKind.LocalScale, new Vector3(1.25f, 0.75f, 1.25f), null, 0.12f, Ease.OutQuad, 2, CycleMode.Yoyo),
            Make("Fade/Fade In (Canvas Group)", PropertyKind.CanvasGroupAlpha, new Vector4(1f, 0, 0, 0), new Vector4(0f, 0, 0, 0), 0.3f, Ease.OutQuad),
            Make("Fade/Fade Out (Canvas Group)", PropertyKind.CanvasGroupAlpha, new Vector4(0f, 0, 0, 0), null, 0.25f, Ease.InQuad),
            Make("Fade/Blink (Graphic)", PropertyKind.GraphicAlpha, new Vector4(0.15f, 0, 0, 0), null, 0.15f, Ease.Linear, 6, CycleMode.Yoyo),
            Make("Move/Slide In From Left (UI)", PropertyKind.AnchoredPosition, Vector2.zero, new Vector2(-800f, 0f), 0.5f, Ease.OutCubic),
            Make("Move/Slide In From Bottom (UI)", PropertyKind.AnchoredPosition, Vector2.zero, new Vector2(0f, -600f), 0.5f, Ease.OutCubic),
            Make("Move/Bounce Drop", PropertyKind.LocalPosition, Vector3.zero, new Vector3(0f, 3f, 0f), 0.8f, Ease.OutBounce),
            Make("Rotate/Spin (loop)", PropertyKind.LocalEulerAngles, new Vector3(0f, 360f, 0f), Vector3.zero, 1.5f, Ease.Linear, -1, CycleMode.Restart),
            Make("Rotate/Wobble", PropertyKind.LocalEulerAngles, new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, -8f), 0.15f, Ease.InOutSine, 4, CycleMode.Yoyo),
            Make("Camera/Zoom In", PropertyKind.CameraFieldOfView, new Vector4(40f, 0, 0, 0), null, 0.6f, Ease.InOutSine),
            Make("Light/Flicker", PropertyKind.LightIntensity, new Vector4(0.3f, 0, 0, 0), null, 0.08f, Ease.Linear, 8, CycleMode.Yoyo),
            Make("Audio/Fade Out", PropertyKind.AudioVolume, new Vector4(0f, 0, 0, 0), null, 1f, Ease.InQuad),
        };

        static Preset Make(string menu, PropertyKind property, Vector4 end, Vector4? from, float duration,
                           Ease ease, int cycles = 1, CycleMode mode = CycleMode.Restart) {
            Debug.Assert(!string.IsNullOrEmpty(menu), "Presets need a menu path.");
            Debug.Assert(duration > 0f, "Presets need a positive duration.");
            return new Preset {
                Menu = menu,
                Apply = template => {
                    template.property = property;
                    template.endValue = end;
                    template.useExplicitFrom = from.HasValue;
                    template.fromValue = from ?? Vector4.zero;
                    template.settings = TweenParams.Default;
                    template.settings.duration = duration;
                    template.settings.ease = ease;
                    template.settings.cycles = cycles;
                    template.settings.cycleMode = mode;
                }
            };
        }
    }
}
