using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RavenTween {
    public static class VolumeTweens {
        struct Flashing {
            public Tween Tween;
            public float Rest;
        }

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

        public static Tween FlashWeight(this Volume target, float peak, float duration) {
            Debug.Assert(target != null, "Weight flash needs a live Volume.");
            float rest = target.weight;
            if (Flashes.TryGetValue(target, out Flashing running) && running.Tween.IsAlive) {
                running.Tween.Stop();
                rest = running.Rest;
            }
            Tween flash = Raven.CustomTo(target, v => v.weight, Mathf.Clamp01(Mathf.Max(peak, rest)), duration * 0.5f, (v, w) => v.weight = w)
                .From(rest).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo);
            if (Flashes.Count > 16) { DropFinished(); }
            Flashes[target] = new Flashing { Tween = flash, Rest = rest };
            return flash;
        }

        static readonly List<Volume> Finished = new List<Volume>(8);

        static void DropFinished() {
            foreach (KeyValuePair<Volume, Flashing> entry in Flashes) {
                if (entry.Key == null || !entry.Value.Tween.IsAlive) { Finished.Add(entry.Key); }
            }
            for (int i = 0; i < Finished.Count; i++) { Flashes.Remove(Finished[i]); }
            Finished.Clear();
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
