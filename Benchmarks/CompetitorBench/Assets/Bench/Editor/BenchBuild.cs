using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RavenBench.EditorTools {
    public static class BenchBuild {
        const string ScenePath = "Assets/Scenes/Bench.unity";
        const string DOTweenAssembly = "Assets/Plugins/Demigiant/DOTween/DOTween.dll";
        const string DOTweenDefine = "RAVENBENCH_DOTWEEN";

        public static void BuildPlayer() {
            int code = 1;
            try {
                code = Build() ? 0 : 1;
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
            EditorApplication.Exit(code);
        }

        static bool Build() {
            string output = Path.GetFullPath(Arg("-benchBuildPath", "Builds/Windows/RavenBench.exe"));
            bool il2cpp = string.Equals(Arg("-benchBackend", "Mono"), "IL2CPP", StringComparison.OrdinalIgnoreCase);
            EnsureScene();

            NamedBuildTarget target = NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(target, il2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
            PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
            PlayerSettings.productName = "RavenBench";
            PlayerSettings.companyName = "RavenTween";
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 640;
            PlayerSettings.defaultScreenHeight = 360;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SplashScreen.show = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            bool hasDOTween = File.Exists(DOTweenAssembly);
            Debug.Log("RavenBench: building " + (il2cpp ? "IL2CPP" : "Mono") + " release player to " + output + (hasDOTween ? " with DOTween" : " without DOTween (not installed)"));

            var options = new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
                extraScriptingDefines = hasDOTween ? new[] { DOTweenDefine } : new string[0]
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("RavenBench: build " + report.summary.result + ", " + report.summary.totalErrors + " error(s)");
            return report.summary.result == BuildResult.Succeeded;
        }

        static void EnsureScene() {
            if (File.Exists(ScenePath)) { return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static string Arg(string name, string fallback) {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { return args[i + 1]; }
            }
            return fallback;
        }
    }
}
