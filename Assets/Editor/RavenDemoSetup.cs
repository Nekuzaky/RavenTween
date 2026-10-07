using RavenTween;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RavenTweenDemo {
    /// <summary>Builds a small demo scene and template assets used to showcase the inspectors.</summary>
    public static class RavenDemoSetup {
        public static void CreateDemo() {
            if (!AssetDatabase.IsValidFolder("Assets/Demo")) {
                AssetDatabase.CreateFolder("Assets", "Demo");
            }

            TweenTemplate scaleTemplate = ScriptableObject.CreateInstance<TweenTemplate>();
            scaleTemplate.property = PropertyKind.LocalScale;
            scaleTemplate.endValue = new Vector4(1.6f, 1.6f, 1.6f, 0f);
            scaleTemplate.settings = TweenParams.Default;
            scaleTemplate.settings.duration = 0.6f;
            scaleTemplate.settings.ease = Ease.OutBack;
            scaleTemplate.settings.cycles = -1;
            scaleTemplate.settings.cycleMode = CycleMode.Yoyo;
            AssetDatabase.CreateAsset(scaleTemplate, "Assets/Demo/PulseScale.asset");

            TweenTemplate moveTemplate = ScriptableObject.CreateInstance<TweenTemplate>();
            moveTemplate.property = PropertyKind.LocalPosition;
            moveTemplate.endValue = new Vector4(0f, 2f, 0f, 0f);
            moveTemplate.settings = TweenParams.Default;
            moveTemplate.settings.duration = 1.2f;
            moveTemplate.settings.ease = Ease.InOutSine;
            moveTemplate.settings.cycles = -1;
            moveTemplate.settings.cycleMode = CycleMode.Yoyo;
            AssetDatabase.CreateAsset(moveTemplate, "Assets/Demo/FloatUp.asset");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Pulsing Cube";
            var animator = cube.AddComponent<RavenAnimator>();
            animator.Entries.Add(new RavenAnimator.Entry { target = cube.transform, template = scaleTemplate });
            var animatorSo = new SerializedObject(animator);
            animatorSo.FindProperty("playOnEnable").boolValue = true;
            animatorSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Floating Sphere";
            sphere.transform.position = new Vector3(2.5f, 0f, 0f);
            var player = sphere.AddComponent<RavenSequencePlayer>();
            player.Steps.Add(new RavenSequencePlayer.Step {
                mode = RavenSequencePlayer.StepMode.Chain, target = sphere.transform, template = moveTemplate
            });
            player.Steps.Add(new RavenSequencePlayer.Step {
                mode = RavenSequencePlayer.StepMode.Group, target = sphere.transform, template = scaleTemplate
            });
            var playerSo = new SerializedObject(player);
            playerSo.FindProperty("playOnEnable").boolValue = true;
            playerSo.FindProperty("cycles").intValue = -1;
            playerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Demo/RavenDemo.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Raven demo scene created.");
        }
    }
}
