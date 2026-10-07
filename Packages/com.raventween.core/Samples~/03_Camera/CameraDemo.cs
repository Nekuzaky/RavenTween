using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Cycles the camera between a wide and a zoomed shot while blending the sky color.
    /// Press Space for a quick punch-in zoom.
    /// </summary>
    public sealed class CameraDemo : MonoBehaviour {
        Camera _camera;

        void Start() {
            _camera = Camera.main;
            if (_camera == null) {
                _camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                _camera.tag = "MainCamera";
            }
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.transform.position = new Vector3(0f, 1f, -6f);
            GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Slow breathing zoom plus a day/night-ish background blend, forever.
            Raven.FieldOfView(_camera, 40f, 3f).Ease(Ease.InOutSine).Infinite(CycleMode.Yoyo);
            Raven.BackgroundColor(_camera, new Color(0.05f, 0.05f, 0.2f), 3f)
                .From(new Color(0.5f, 0.7f, 1f))
                .Ease(Ease.InOutSine)
                .Infinite(CycleMode.Yoyo);
        }

        void Update() {
            if (SpacePressed()) { PunchZoom(); }
        }

        static bool SpacePressed() {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        void PunchZoom() {
            // Two quick cycles of yoyo: in and back out.
            Raven.FieldOfView(_camera, _camera.fieldOfView - 15f, 0.12f)
                .Ease(Ease.OutQuad)
                .Cycles(2, CycleMode.Yoyo)
                .UnscaledTime();
        }
    }
}
