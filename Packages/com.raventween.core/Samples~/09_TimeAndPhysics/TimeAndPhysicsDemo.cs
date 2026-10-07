using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// A kinematic platform carries a crate (Rigidbody tweens in FixedUpdate), a clock ticks with
    /// Incremental cycles, a beacon flashes through timeline callbacks and a property block.
    /// Press Space for a hit stop with a camera shake.
    /// </summary>
    public sealed class TimeAndPhysicsDemo : MonoBehaviour {
        Camera _camera;

        void Start() {
            _camera = SetupCamera();
            BuildPlatform();
            BuildClock(new Vector3(-4f, 2.5f, 2f));
            BuildBeacon(new Vector3(4f, 1.5f, 2f));
        }

        static Camera SetupCamera() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0f, 3f, -10f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            return camera;
        }

        // The platform moves through the physics engine, so the crate on top rides along.
        static void BuildPlatform() {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.position = new Vector3(-3f, 0f, 0f);
            platform.transform.localScale = new Vector3(3f, 0.3f, 2f);
            var body = platform.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.TweenMovePosition(new Vector3(3f, 0f, 0f), 2.5f)
                .Ease(Ease.InOutSine)
                .Cycles(-1, CycleMode.PingPong);

            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.position = new Vector3(-3f, 1f, 0f);
            crate.transform.localScale = Vector3.one * 0.6f;
            crate.AddComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Each one-second cycle snaps the hand 30 degrees further, with a small overshoot.
        static void BuildClock(Vector3 position) {
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Clock";
            face.transform.position = position;
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);

            var pivot = new GameObject("Hand Pivot").transform;
            pivot.position = position + new Vector3(0f, 0f, -0.1f);
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.transform.SetParent(pivot, false);
            hand.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            hand.transform.localScale = new Vector3(0.08f, 0.7f, 0.05f);

            Raven.LocalEulerAngles(pivot, new Vector3(0f, 0f, -30f), 1f)
                .Ease(t => Easing.Overshoot(2f).Evaluate(Mathf.Clamp01(t * 4f)))
                .Cycles(-1, CycleMode.Incremental);
        }

        // A looping timeline: grow, flash on a callback, shrink. The flash uses a property block,
        // so the shared material stays untouched.
        static void BuildBeacon(Vector3 position) {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Beacon";
            beacon.transform.position = position;
            var renderer = beacon.GetComponent<Renderer>();
            Raven.Sequence()
                .Chain(Raven.Scale(beacon.transform, 1.4f, 0.4f).Ease(Ease.OutBack))
                .ChainCallback(renderer, Flash)
                .Chain(Raven.Scale(beacon.transform, 1f, 0.6f).Ease(Ease.InOutSine))
                .ChainDelay(0.5f)
                .Infinite();
        }

        static void Flash(Renderer renderer) {
            string property = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Raven.PropertyBlockColor(renderer, property, new Color(1f, 0.55f, 0.1f), 0.08f).Cycles(2, CycleMode.Yoyo);
        }

        void Update() {
            if (SpacePressed()) { HitStop(); }
        }

        static bool SpacePressed() {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        Tween _slowDown;
        Tween _recover;

        // Drop to 5% speed for a moment, then ease back, while the camera shakes in real time.
        void HitStop() {
            _slowDown.Stop();
            _recover.Stop();
            _slowDown = Raven.GlobalTimeScale(0.05f, 0.04f).OnComplete(this, self => self._recover = Raven.GlobalTimeScale(1f, 0.35f));
            Raven.ShakePosition(_camera.transform, new Vector3(0.15f, 0.15f, 0f), 0.4f, 18f).UnscaledTime();
        }

        void OnDestroy() {
            Time.timeScale = 1f;
        }
    }
}
