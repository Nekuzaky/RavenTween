using UnityEditor;
using UnityEngine;

namespace RavenTween.Editor {
    /// <summary>Small, non-modal prompt offering a newer RavenTween release.</summary>
    sealed class RavenUpdateWindow : EditorWindow {
        UpdateLogic.ReleaseInfo _release;
        string _installedVersion;
        string _identifier;
        InstallSource _source;

        public static void Open(UpdateLogic.ReleaseInfo release, InstalledPackage installed, string identifier) {
            Debug.Assert(release != null, "Update window needs release information.");
            Debug.Assert(installed != null, "Update window needs the installed package.");
            var window = CreateInstance<RavenUpdateWindow>();
            window._release = release;
            window._installedVersion = installed.Version;
            window._identifier = identifier;
            window._source = installed.Source;
            var icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.raventween.core/Editor/Icons/raventween_icon_small.png");
            window.titleContent = new GUIContent("RavenTween Update", icon);
            window.minSize = window.maxSize = new Vector2(420f, 190f);
            window.ShowUtility();
        }

        void OnGUI() {
            if (_release == null) { Close(); return; }
            GUILayout.Space(10f);
            EditorGUILayout.LabelField(_release.name ?? _release.tag_name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("A new version is available. You have " + _installedVersion + ".", EditorStyles.wordWrappedLabel);
            GUILayout.Space(6f);
            if (_identifier == null) { DrawManualHint(); }
            GUILayout.FlexibleSpace();
            DrawButtons();
            GUILayout.Space(10f);
        }

        void DrawManualHint() {
            string hint = _source == InstallSource.Embedded || _source == InstallSource.Local
                ? "This project uses an editable copy of the package, so it isn't updated automatically."
                : "This installation can't be updated from here. Update it from the Package Manager.";
            EditorGUILayout.HelpBox(hint, MessageType.Info);
        }

        void DrawButtons() {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_identifier == null)) {
                if (GUILayout.Button(RavenEditorIcons.Content(RavenEditorIcons.Play, "Update now", "Install " + _release.tag_name), GUILayout.Height(26f))) {
                    RavenUpdateChecker.Install(_identifier, _release.tag_name);
                    Close();
                }
            }
            if (GUILayout.Button("What's new", GUILayout.Height(26f))) {
                Application.OpenURL(string.IsNullOrEmpty(_release.html_url) ? UpdateLogic.ReleasesPage : _release.html_url);
            }
            if (GUILayout.Button("Skip this version", GUILayout.Height(26f))) {
                RavenUpdateChecker.Skip(_release.tag_name);
                Close();
            }
            if (GUILayout.Button("Later", GUILayout.Height(26f))) { Close(); }
            EditorGUILayout.EndHorizontal();
        }
    }
}
