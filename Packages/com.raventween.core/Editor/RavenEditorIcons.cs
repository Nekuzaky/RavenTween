using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>
    /// Cached access to the editor glyph set (Bootstrap Icons, MIT - see Third Party Notices).
    /// All lookups go through AssetDatabase once and are cached for the session.
    /// </summary>
    static class RavenEditorIcons {
        const string Root = "Packages/com.raventween.core/Editor/Icons/Glyphs/";

        static Texture _play, _pause, _stop, _complete, _kill, _completeAll, _infinity, _activity;

        public static Texture Play { get { return Cache(ref _play, "bs_play_fill.png"); } }
        public static Texture Pause { get { return Cache(ref _pause, "bs_pause_fill.png"); } }
        public static Texture Stop { get { return Cache(ref _stop, "bs_stop_fill.png"); } }
        public static Texture Complete { get { return Cache(ref _complete, "bs_skip_end_fill.png"); } }
        public static Texture Kill { get { return Cache(ref _kill, "bs_x_lg.png"); } }
        public static Texture CompleteAll { get { return Cache(ref _completeAll, "bs_check2_all.png"); } }
        public static Texture Infinity { get { return Cache(ref _infinity, "bs_infinity.png"); } }
        public static Texture Activity { get { return Cache(ref _activity, "bs_activity.png"); } }

        /// <summary>Builds a button content with icon, label and tooltip.</summary>
        public static GUIContent Content(Texture icon, string label, string tooltip) {
            Debug.Assert(tooltip != null, "Tooltip text is required for icon buttons.");
            return new GUIContent(string.IsNullOrEmpty(label) ? "" : " " + label, icon, tooltip);
        }

        static Texture Cache(ref Texture slot, string file) {
            if (slot == null) { slot = AssetDatabase.LoadAssetAtPath<Texture>(Root + file); }
            Debug.Assert(slot != null, "Missing editor glyph: " + file);
            return slot;
        }
    }
}
