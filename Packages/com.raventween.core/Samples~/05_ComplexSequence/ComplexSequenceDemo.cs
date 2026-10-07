using System.Collections;
using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Builds a nested sequence: a staged "intro" where three cubes enter, regroup and
    /// celebrate. A coroutine waits for it to finish, pauses, and plays it again.
    /// </summary>
    public sealed class ComplexSequenceDemo : MonoBehaviour {
        Transform _left;
        Transform _center;
        Transform _right;

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
            StartCoroutine(RunForever());
        }

        IEnumerator RunForever() {
            while (true) {
                ResetPositions();
                Sequence intro = BuildIntro();
                // A sequence can be awaited from a coroutine...
                yield return intro.ToYieldInstruction();
                // ...or from async code; here we just pause between runs.
                yield return Raven.Delay(0.8f).ToYieldInstruction();
            }
        }

        void ResetPositions() {
            _left.position = new Vector3(-6f, 0f, 0f);
            _center.position = new Vector3(0f, 6f, 0f);
            _right.position = new Vector3(6f, 0f, 0f);
            _left.localScale = _center.localScale = _right.localScale = Vector3.one;
        }

        Sequence BuildIntro() {
            // Nested sequence: the celebration is its own unit, chained after the entries.
            Sequence celebration = Raven.Sequence()
                .Chain(Raven.Scale(_center, 1.5f, 0.25f).Ease(Ease.OutBack))
                .Group(Raven.Scale(_left, 1.3f, 0.25f).Ease(Ease.OutBack))
                .Group(Raven.Scale(_right, 1.3f, 0.25f).Ease(Ease.OutBack))
                .Chain(Raven.Scale(_center, 1f, 0.4f).Ease(Ease.OutBounce))
                .Group(Raven.Scale(_left, 1f, 0.4f).Ease(Ease.OutBounce))
                .Group(Raven.Scale(_right, 1f, 0.4f).Ease(Ease.OutBounce));

            return Raven.Sequence()
                // The two side cubes slide in together.
                .Chain(Raven.Position(_left, new Vector3(-2f, 0f, 0f), 0.8f).Ease(Ease.OutCubic))
                .Group(Raven.Position(_right, new Vector3(2f, 0f, 0f), 0.8f).Ease(Ease.OutCubic))
                // The center cube drops in slightly before the slides end.
                .Insert(0.5f, Raven.Position(_center, Vector3.zero, 0.6f).Ease(Ease.OutBounce))
                // Then everyone celebrates.
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
