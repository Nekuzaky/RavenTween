using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenBench {
    public enum BenchScenario {
        Tweens,
        Sequences
    }

    public sealed class BenchTargets {
        public const float TweenDuration = 20f;
        public const float StepDuration = 2f;

        public readonly Transform[] Transforms;
        public readonly Vector3[] PointA;
        public readonly Vector3[] PointB;
        public readonly Vector3[] PointC;

        public BenchTargets(int count) {
            Transforms = new Transform[count];
            PointA = new Vector3[count];
            PointB = new Vector3[count];
            PointC = new Vector3[count];
            for (int i = 0; i < count; i++) {
                var go = new GameObject("Target");
                Transforms[i] = go.transform;
                float angle = i * 0.618034f * Mathf.PI * 2f;
                float radius = 5f + (i % 97) * 0.1f;
                PointA[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                PointB[i] = new Vector3(-PointA[i].y, PointA[i].x, 2f);
                PointC[i] = new Vector3(-PointA[i].x, -PointA[i].y, -2f);
            }
        }

        public void ResetPositions() {
            for (int i = 0; i < Transforms.Length; i++) {
                Transforms[i].position = Vector3.zero;
            }
        }

        public int CountMoved(int count) {
            int moved = 0;
            for (int i = 0; i < count; i++) {
                if (Transforms[i].position.sqrMagnitude > 1e-6f) { moved++; }
            }
            return moved;
        }
    }

    public abstract class BenchLibrary {
        public abstract string Name { get; }

        public virtual string Version {
            get { return string.Empty; }
        }

        public abstract void Prepare(BenchScenario scenario, int count);

        public abstract void StartTweens(BenchTargets targets, int count);

        public abstract void StartSequences(BenchTargets targets, int count);

        public abstract void StopAll();
    }

    public static class BenchRegistry {
        static readonly List<BenchLibrary> Registered = new List<BenchLibrary>();

        public static IReadOnlyList<BenchLibrary> Libraries {
            get { return Registered; }
        }

        public static void Register(BenchLibrary library) {
            for (int i = 0; i < Registered.Count; i++) {
                if (string.Equals(Registered[i].Name, library.Name, StringComparison.OrdinalIgnoreCase)) { return; }
            }
            Registered.Add(library);
            Registered.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        public static BenchLibrary Find(string name) {
            for (int i = 0; i < Registered.Count; i++) {
                if (string.Equals(Registered[i].Name, name, StringComparison.OrdinalIgnoreCase)) { return Registered[i]; }
            }
            return null;
        }
    }
}
