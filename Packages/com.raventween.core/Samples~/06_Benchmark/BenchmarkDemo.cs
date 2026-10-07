using RavenTween;
using Unity.Profiling;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Spawns thousands of animated cubes and shows frame time and GC allocations per frame.
    /// Use the +/- keys to add or remove batches. Build a Development player for real numbers.
    /// </summary>
    public sealed class BenchmarkDemo : MonoBehaviour {
        [SerializeField, Min(100)] int batchSize = 1000;
        [SerializeField, Min(0)] int initialBatches = 5;

        ProfilerRecorder _gcAllocRecorder;
        float _smoothedFrameMs;
        int _cubeCount;
        Mesh _mesh;
        Material _material;

        void OnEnable() {
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        void OnDisable() {
            _gcAllocRecorder.Dispose();
        }

        void Start() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0f, 0f, -60f);
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _mesh = probe.GetComponent<MeshFilter>().sharedMesh;
            _material = probe.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(probe);
            for (int i = 0; i < initialBatches; i++) { SpawnBatch(); }
        }

        void SpawnBatch() {
            for (int i = 0; i < batchSize; i++) {
                var cube = new GameObject("Cube");
                cube.AddComponent<MeshFilter>().sharedMesh = _mesh;
                cube.AddComponent<MeshRenderer>().sharedMaterial = _material;
                Transform t = cube.transform;
                t.localScale = Vector3.one * 0.3f;
                t.position = Random.insideUnitSphere * 25f;
                // Each cube runs two independent infinite tweens: position and rotation.
                Raven.Position(t, Random.insideUnitSphere * 25f, Random.Range(1f, 3f)).Ease(Ease.InOutSine).Infinite();
                Raven.LocalRotation(t, Random.rotation, Random.Range(1f, 3f)).Infinite();
                _cubeCount++;
            }
        }

        void Update() {
            _smoothedFrameMs = Mathf.Lerp(_smoothedFrameMs, Time.unscaledDeltaTime * 1000f, 0.05f);
            if (PlusPressed()) { SpawnBatch(); }
            if (MinusPressed()) { Raven.StopAll(); }
        }

        static bool PlusPressed() {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.numpadPlusKey.wasPressedThisFrame || keyboard.equalsKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals);
#endif
        }

        static bool MinusPressed() {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.numpadMinusKey.wasPressedThisFrame || keyboard.minusKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus);
#endif
        }

        void OnGUI() {
            const int width = 340;
            GUILayout.BeginArea(new Rect(10, 10, width, 140), GUI.skin.box);
            GUILayout.Label("RavenTween benchmark");
            GUILayout.Label("Cubes: " + _cubeCount + "   Live tweens: " + Raven.AliveCount);
            GUILayout.Label("Frame: " + _smoothedFrameMs.ToString("0.00") + " ms  (" + (1000f / Mathf.Max(_smoothedFrameMs, 0.01f)).ToString("0") + " FPS)");
            GUILayout.Label("GC alloc this frame: " + (_gcAllocRecorder.Valid ? _gcAllocRecorder.LastValue + " B" : "n/a") + "  (OnGUI itself allocates)");
            GUILayout.Label("[+] add " + batchSize + " cubes   [-] stop all tweens");
            GUILayout.EndArea();
        }
    }
}
