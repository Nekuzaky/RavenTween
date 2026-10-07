using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Animates material properties on a grid of spheres using shader property IDs.
    /// </summary>
    public sealed class MaterialsDemo : MonoBehaviour {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP lit shader.

        void Start() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(1.5f, 1.5f, -6f);

            for (int x = 0; x < 4; x++) {
                for (int y = 0; y < 4; y++) {
                    SpawnSphere(x, y);
                }
            }
        }

        static void SpawnSphere(int x, int y) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(x, y, 0f);
            Material material = sphere.GetComponent<Renderer>().material; // Instance, safe to tween.
            int propertyId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            Color from = Color.HSVToRGB((x * 4 + y) / 16f, 0.8f, 1f);
            Color to = Color.HSVToRGB(((x * 4 + y) / 16f + 0.5f) % 1f, 0.8f, 1f);
            // Stagger each sphere with a delay so the grid shimmers as a wave.
            Raven.MaterialColor(material, propertyId, to, 1.2f)
                .From(from)
                .Delay((x + y) * 0.1f)
                .Ease(Ease.InOutSine)
                .Infinite(CycleMode.Yoyo);
        }
    }
}
