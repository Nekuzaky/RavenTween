using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// It spawns a few cubes and shows typical gameplay motion: hops, spins and patrols.
    /// </summary>
    public sealed class GameplayDemo : MonoBehaviour {
        void Start() {
            SetupCamera();
            SpawnHopper(new Vector3(-3f, 0f, 0f), Color.cyan);
            SpawnSpinner(new Vector3(0f, 0f, 0f), Color.yellow);
            SpawnPatroller(new Vector3(3f, 0f, 0f), Color.magenta);
        }

        static void SetupCamera() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
        }

        static Transform SpawnCube(Vector3 position, Color color, string label) {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.position = position;
            cube.GetComponent<Renderer>().material.color = color;
            return cube.transform;
        }

        static void SpawnHopper(Vector3 position, Color color) {
            Transform cube = SpawnCube(position, color, "Hopper");
            // Endless hop: up with OutQuad, down with InQuad, via yoyo cycles.
            Raven.Position(cube, position + Vector3.up * 2f, 0.4f)
                .Ease(Ease.OutQuad)
                .Infinite(CycleMode.Yoyo);
        }

        static void SpawnSpinner(Vector3 position, Color color) {
            Transform cube = SpawnCube(position, color, "Spinner");
            // Relative-style spin: each cycle adds a half turn, restarting from the captured value.
            Raven.LocalEulerAngles(cube, new Vector3(0f, 180f, 0f), 1f)
                .Ease(Ease.InOutCubic)
                .Infinite(CycleMode.Restart);
        }

        static void SpawnPatroller(Vector3 position, Color color) {
            Transform cube = SpawnCube(position, color, "Patroller");
            // A patrol loop built as a sequence: move right, pause, move back, squash on arrival.
            Raven.Sequence()
                .Chain(Raven.Position(cube, position + Vector3.right * 2f, 1f).Ease(Ease.InOutSine))
                .ChainDelay(0.3f)
                .Chain(Raven.Position(cube, position, 1f).Ease(Ease.InOutSine))
                .Group(Raven.Scale(cube, new Vector3(1.2f, 0.8f, 1.2f), 0.5f).Cycles(2, CycleMode.Yoyo))
                .Infinite();
        }
    }
}
