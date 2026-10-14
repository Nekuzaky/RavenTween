using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RavenTween {
    /// <summary>
    /// Post-processing for URP and HDRP: <c>volume.TweenWeight(1f, 0.3f)</c> fades a Volume's whole
    /// effect (a red vignette when hit, a blur on pause). Also serves the Volume Flash action of
    /// Raven Feedback.
    /// </summary>
    public static class VolumeTweens {
        struct Flashing {
            public Tween Tween;
            public float Rest;
        }

        // A volume flashing now keeps the weight it rests at, so rapid repeats never drift.
        static readonly Dictionary<Volume, Flashing> Flashes = new Dictionary<Volume, Flashing>(4);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            Flashes.Clear();
            FeedbackHooks.VolumeFlash = Flash;
        }

        public static Tween TweenWeight(this Volume target, float to, float duration) {
            Debug.Assert(target != null, "Weight tween needs a live Volume.");
            return Raven.CustomTo(target, v => v.weight, Mathf.Clamp01(to), duration, (v, w) => v.weight = w);
        }

        /// <summary>Raises the weight to <paramref name="peak"/> and brings it back, in <paramref name="duration"/> seconds.</summary>
        public static Tween FlashWeight(this Volume target, float peak, float duration) {
            Debug.Assert(target != null, "Weight flash needs a live Volume.");
            float rest = target.weight;
            if (Flashes.TryGetValue(target, out Flashing running) && running.Tween.IsAlive) {
                running.Tween.Stop();
                rest = running.Rest;
            }
            Tween flash = Raven.CustomTo(target, v => v.weight, Mathf.Clamp01(Mathf.Max(peak, rest)), duration * 0.5f, (v, w) => v.weight = w)
                .From(rest).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo);
            if (Flashes.Count > 16) { Flashes.Clear(); }
            Flashes[target] = new Flashing { Tween = flash, Rest = rest };
            return flash;
        }

        static void Flash(FeedbackAction action, float power) {
            var volume = action.effect as Volume;
            if (volume == null && action.effect != null) { volume = action.effect.GetComponent<Volume>(); }
            if (volume == null) {
                Debug.LogWarning("RavenTween: Volume Flash needs a Volume in its Volume field.");
                return;
            }
            volume.FlashWeight(power, action.duration);
        }
    }
}
