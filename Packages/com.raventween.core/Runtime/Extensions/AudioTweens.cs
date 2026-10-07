using UnityEngine;

namespace RavenTween {
    public static partial class Raven {
        /// <summary>Tweens an AudioSource's volume.</summary>
        public static Tween Volume(AudioSource target, float to, float duration) {
            Debug.Assert(target != null, "Volume tween needs a live AudioSource.");
            Debug.Assert(to >= 0f && to <= 1f, "Volume must be in [0, 1].");
            return CreatePropertyTween(target, PropertyKind.AudioVolume, 0, new TweenValue(Mathf.Clamp01(to)), duration);
        }

        /// <summary>Tweens an AudioSource's pitch.</summary>
        public static Tween Pitch(AudioSource target, float to, float duration) {
            Debug.Assert(target != null, "Pitch tween needs a live AudioSource.");
            Debug.Assert(duration >= 0f, "Duration cannot be negative.");
            return CreatePropertyTween(target, PropertyKind.AudioPitch, 0, new TweenValue(to), duration);
        }
    }

    /// <summary>Extension-method style access to AudioSource tweens.</summary>
    public static class AudioTweens {
        public static Tween TweenVolume(this AudioSource target, float to, float duration) {
            return Raven.Volume(target, to, duration);
        }

        public static Tween TweenPitch(this AudioSource target, float to, float duration) {
            return Raven.Pitch(target, to, duration);
        }
    }
}
