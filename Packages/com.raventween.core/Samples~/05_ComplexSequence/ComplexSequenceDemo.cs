using System.Threading;
using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    public sealed class ComplexSequenceDemo : MonoBehaviour {
        Transform _left;
        Transform _center;
        Transform _right;
        Sequence _intro;
        Tween _gap;

        void Start() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0f, 1f, -8f);
            _left = Spawn(new Vector3(-6f, 0f, 0f), Color.cyan);
            _center = Spawn(new Vector3(0f, 6f, 0f), Color.yellow);
            _right = Spawn(new Vector3(6f, 0f, 0f), Color.magenta);
            RunForever(destroyCancellationToken);
        }

        async void RunForever(CancellationToken destroyed) {
            while (!destroyed.IsCancellationRequested) {
                ResetPositions();
                _intro = BuildIntro().WithCancellation(destroyed);
                await _intro;
                if (destroyed.IsCancellationRequested) { return; }
                _gap = Raven.Delay(0.8f).WithCancellation(destroyed);
                await _gap;
            }
        }

        void OnEnable() {
            _intro.Resume();
            _gap.Resume();
        }

        void OnDisable() {
            _intro.Pause();
            _gap.Pause();
        }

        void ResetPositions() {
            _left.position = new Vector3(-6f, 0f, 0f);
            _center.position = new Vector3(0f, 6f, 0f);
            _right.position = new Vector3(6f, 0f, 0f);
            _left.localScale = _center.localScale = _right.localScale = Vector3.one;
        }

        Sequence BuildIntro() {
            Sequence celebration = Raven.Sequence()
                .Chain(Raven.Scale(_center, 1.5f, 0.25f).Ease(Ease.OutBack))
                .Group(Raven.Scale(_left, 1.3f, 0.25f).Ease(Ease.OutBack))
                .Group(Raven.Scale(_right, 1.3f, 0.25f).Ease(Ease.OutBack))
                .Chain(Raven.Scale(_center, 1f, 0.4f).Ease(Ease.OutBounce))
                .Group(Raven.Scale(_left, 1f, 0.4f).Ease(Ease.OutBounce))
                .Group(Raven.Scale(_right, 1f, 0.4f).Ease(Ease.OutBounce));

            return Raven.Sequence()
                .Chain(Raven.Position(_left, new Vector3(-2f, 0f, 0f), 0.8f).Ease(Ease.OutCubic))
                .Group(Raven.Position(_right, new Vector3(2f, 0f, 0f), 0.8f).Ease(Ease.OutCubic))
                .Insert(0.5f, Raven.Position(_center, Vector3.zero, 0.6f).Ease(Ease.OutBounce))
                .Chain(celebration);
        }

        static Transform Spawn(Vector3 position, Color color) {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.GetComponent<Renderer>().material.color = color;
            return cube.transform;
        }
    }
}
