using UnityEngine;

namespace RavenTween {
    /// <summary>
    /// A motion with its settings saved as an asset: your team's own "Pop In", tuned once and
    /// shared by every Raven Motion, Raven Stagger and Raven Show Hide that uses it. Changing the
    /// asset changes them all. Create one with Create ▸ RavenTween ▸ Motion Style, or with
    /// "Save as style" in the Motion Library.
    /// </summary>
    [CreateAssetMenu(menuName = "RavenTween/Motion Style", fileName = "New Motion Style", order = 101)]
    public sealed class MotionStyle : ScriptableObject {
        [SerializeField] MotionPreset preset = MotionPreset.PopIn;
        [SerializeField] MotionSettings settings = MotionSettings.Default;
        [Tooltip("What this style is for, so the team knows when to use it.")]
        [SerializeField, TextArea(2, 4)] string notes = "";

        public MotionPreset Preset { get { return preset; } set { preset = value; } }
        public MotionSettings Settings { get { return settings; } set { settings = value; } }
        public string Notes { get { return notes; } set { notes = value ?? ""; } }

        /// <summary>Plays this style on <paramref name="target"/> around its current pose.</summary>
        public Sequence Play(Component target) {
            return Raven.Motion(target, preset, settings);
        }
    }

    /// <summary>
    /// Implemented by components that start on pointer events (clicks, hovers). The pointer relay
    /// forwards each event to every such component on its GameObject.
    /// </summary>
    public interface IMotionTriggerable {
        void HandleTrigger(MotionTrigger trigger);
    }

    /// <summary>Trigger helpers shared by the no-code components.</summary>
    public static class MotionTriggers {
        public static bool IsPointer(MotionTrigger trigger) {
            return trigger >= MotionTrigger.OnClick && trigger <= MotionTrigger.OnPointerUp;
        }

        public static bool IsPhysics(MotionTrigger trigger) {
            return trigger >= MotionTrigger.OnTriggerEnter && trigger <= MotionTrigger.OnCollisionEnter;
        }

        /// <summary>
        /// Adds the relay <paramref name="trigger"/> needs to <paramref name="go"/>: hover, press or
        /// click. Each relay only takes its own events, so it never steals the others from parents.
        /// </summary>
        public static void EnsurePointerRelay(GameObject go, MotionTrigger trigger) {
            if (go == null || !IsPointer(trigger)) { return; }
            if (trigger == MotionTrigger.OnClick) { Ensure<RavenClickRelay>(go); }
            else if (trigger == MotionTrigger.OnPointerEnter || trigger == MotionTrigger.OnPointerExit) { Ensure<RavenHoverRelay>(go); }
            else { Ensure<RavenPressRelay>(go); }
        }

        static void Ensure<T>(GameObject go) where T : Component {
            if (go.GetComponent<T>() == null) { go.AddComponent<T>(); }
        }

        /// <summary>True when <paramref name="other"/> passes a tag filter (empty = any object).</summary>
        public static bool PassesTag(string filterTag, GameObject other) {
            return string.IsNullOrEmpty(filterTag) || (other != null && other.CompareTag(filterTag));
        }
    }
}
