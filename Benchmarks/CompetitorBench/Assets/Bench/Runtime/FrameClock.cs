using System;
using System.Diagnostics;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RavenBench {
    static class FrameClock {
        struct FrameBegin { }

        struct FrameEnd { }

        static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
        static long _begin;
        static bool _began;
        static bool _recording;
        static bool _skip;
        static double[] _samples = new double[0];
        static int _count;

        public static double LastFrameMs { get; private set; }

        public static int Count {
            get { return _count; }
        }

        public static double[] Samples {
            get { return _samples; }
        }

        public static void Install() {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] top = root.subSystemList;
            bool begin = false;
            bool end = false;
            for (int i = 0; i < top.Length; i++) {
                if (top[i].type == typeof(Initialization)) {
                    top[i].subSystemList = Insert(top[i].subSystemList, 0, new PlayerLoopSystem { type = typeof(FrameBegin), updateDelegate = OnBegin });
                    begin = true;
                } else if (top[i].type == typeof(PostLateUpdate)) {
                    PlayerLoopSystem[] children = top[i].subSystemList;
                    int at = children == null ? 0 : children.Length;
                    top[i].subSystemList = Insert(children, at, new PlayerLoopSystem { type = typeof(FrameEnd), updateDelegate = OnEnd });
                    end = true;
                }
            }
            if (!begin || !end) { throw new InvalidOperationException("Player loop has no Initialization or PostLateUpdate phase."); }
            PlayerLoop.SetPlayerLoop(root);
        }

        static PlayerLoopSystem[] Insert(PlayerLoopSystem[] source, int index, PlayerLoopSystem system) {
            int length = source == null ? 0 : source.Length;
            var result = new PlayerLoopSystem[length + 1];
            for (int i = 0, j = 0; i < result.Length; i++) {
                result[i] = i == index ? system : source[j++];
            }
            return result;
        }

        public static void StartRecording(int capacity) {
            if (_samples.Length < capacity) { _samples = new double[capacity]; }
            _count = 0;
            _skip = true;
            _recording = true;
        }

        public static void StopRecording() {
            _recording = false;
        }

        static void OnBegin() {
            _begin = Stopwatch.GetTimestamp();
            _began = true;
        }

        static void OnEnd() {
            if (!_began) { return; }
            double ms = (Stopwatch.GetTimestamp() - _begin) * TicksToMs;
            LastFrameMs = ms;
            if (_skip) {
                _skip = false;
                return;
            }
            if (_recording && _count < _samples.Length) {
                _samples[_count++] = ms;
            }
        }
    }
}
