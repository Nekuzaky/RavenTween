using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RavenBench {
    public sealed class BenchRunner : MonoBehaviour {
        enum Phase {
            Settle,
            JitWarmup,
            Baseline,
            Start,
            Warmup,
            Measure,
            Finish
        }

        const int JitWarmupCount = 64;
        const int JitWarmupFrames = 30;
        const int SettleFrames = 30;
        const int MaxSamples = 200000;
        const double BaselineSeconds = 2.0;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        BenchLibrary _library;
        BenchScenario _scenario;
        BenchTargets _targets;
        int _count;
        int _rep;
        double _warmupSeconds;
        double _measureSeconds;
        string _outPath;

        Phase _phase;
        Phase _afterSettle;
        int _frames;
        readonly Stopwatch _clock = new Stopwatch();
        long _allocStart;
        int _gcStart;

        Stats _baseline;
        double _startMs;
        long _startAlloc;

        struct Stats {
            public int Frames;
            public double Average;
            public double Median;
            public double P95;
            public double Max;
            public long Bytes;
            public int Collections;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() {
            var go = new GameObject("BenchRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<BenchRunner>();
        }

        void Start() {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            try {
                if (!Configure()) {
                    enabled = false;
                    return;
                }
            } catch (Exception exception) {
                Fail(exception);
            }
        }

        bool Configure() {
            string listPath = Arg("-benchList", null);
            string envPath = Arg("-benchEnv", null);
            if (envPath != null) { WriteEnvironment(envPath); }
            if (listPath != null) {
                WriteLibraries(listPath);
                Quit(0);
                return false;
            }
            string libraryName = Arg("-benchLibrary", null);
            if (libraryName == null) {
                Debug.Log("RavenBench: no -benchLibrary argument. Registered: " + LibraryNames());
                Quit(0);
                return false;
            }
            _library = BenchRegistry.Find(libraryName);
            if (_library == null) { throw new ArgumentException("Unknown library '" + libraryName + "'. Registered: " + LibraryNames()); }
            _scenario = (BenchScenario)Enum.Parse(typeof(BenchScenario), Arg("-benchScenario", "Tweens"), true);
            _count = int.Parse(Arg("-benchCount", "1000"), Inv);
            _rep = int.Parse(Arg("-benchRep", "1"), Inv);
            _warmupSeconds = double.Parse(Arg("-benchWarmup", "1"), Inv);
            _measureSeconds = double.Parse(Arg("-benchMeasure", "4"), Inv);
            _outPath = Arg("-benchOut", Path.Combine(Application.persistentDataPath, "raw.csv"));

            _targets = new BenchTargets(_count);
            _library.Prepare(_scenario, _count);
            AllocCounter.Detect();
            StartLibrary(Mathf.Min(JitWarmupCount, _count));
            _frames = 0;
            _phase = Phase.JitWarmup;
            return true;
        }

        void Update() {
            try {
                Tick();
            } catch (Exception exception) {
                Fail(exception);
            }
        }

        void Tick() {
            _frames++;
            switch (_phase) {
                case Phase.JitWarmup:
                    if (_frames < JitWarmupFrames) { return; }
                    _library.StopAll();
                    _targets.ResetPositions();
                    FrameClock.Install();
                    Settle(Phase.Baseline);
                    return;
                case Phase.Settle:
                    if (_frames < SettleFrames) { return; }
                    _phase = _afterSettle;
                    _frames = 0;
                    if (_phase == Phase.Baseline) { BeginMeasure(); }
                    return;
                case Phase.Baseline:
                    if (_clock.Elapsed.TotalSeconds < BaselineSeconds) { return; }
                    _baseline = EndMeasure();
                    Settle(Phase.Start);
                    return;
                case Phase.Start:
                    StartScenario();
                    _clock.Reset();
                    _clock.Start();
                    _phase = Phase.Warmup;
                    return;
                case Phase.Warmup:
                    if (_clock.Elapsed.TotalSeconds < _warmupSeconds) { return; }
                    BeginMeasure();
                    _phase = Phase.Measure;
                    return;
                case Phase.Measure:
                    if (_clock.Elapsed.TotalSeconds < _measureSeconds) { return; }
                    Stats run = EndMeasure();
                    int moved = _targets.CountMoved(_count);
                    _library.StopAll();
                    WriteRow(run, moved);
                    _phase = Phase.Finish;
                    Quit(0);
                    return;
            }
        }

        void Settle(Phase next) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _afterSettle = next;
            _frames = 0;
            _phase = Phase.Settle;
        }

        void StartLibrary(int count) {
            if (_scenario == BenchScenario.Tweens) { _library.StartTweens(_targets, count); }
            else { _library.StartSequences(_targets, count); }
        }

        void StartScenario() {
            long alloc = AllocCounter.Open();
            long begin = Stopwatch.GetTimestamp();
            StartLibrary(_count);
            long end = Stopwatch.GetTimestamp();
            _startAlloc = AllocCounter.Close(alloc);
            _startMs = (end - begin) * 1000.0 / Stopwatch.Frequency;
        }

        void BeginMeasure() {
            GC.Collect();
            _gcStart = GC.CollectionCount(0);
            _allocStart = AllocCounter.Open();
            FrameClock.StartRecording(MaxSamples);
            _clock.Reset();
            _clock.Start();
        }

        Stats EndMeasure() {
            FrameClock.StopRecording();
            long bytes = AllocCounter.Close(_allocStart);
            int collections = GC.CollectionCount(0) - _gcStart;
            int n = FrameClock.Count;
            if (n == 0) { throw new InvalidOperationException("No frame was recorded: the frame clock is not in the player loop."); }
            var sorted = new double[n];
            Array.Copy(FrameClock.Samples, sorted, n);
            Array.Sort(sorted);
            double sum = 0;
            for (int i = 0; i < n; i++) { sum += sorted[i]; }
            return new Stats {
                Frames = n,
                Average = sum / n,
                Median = Percentile(sorted, 0.5),
                P95 = Percentile(sorted, 0.95),
                Max = sorted[n - 1],
                Bytes = bytes,
                Collections = collections
            };
        }

        static double Percentile(double[] sorted, double p) {
            int index = (int)Math.Ceiling(p * sorted.Length) - 1;
            return sorted[Mathf.Clamp(index, 0, sorted.Length - 1)];
        }

        void WriteRow(Stats run, int moved) {
            bool header = !File.Exists(_outPath);
            var sb = new StringBuilder();
            if (header) {
                sb.AppendLine("rep,scenario,count,library,backend,alloc_method,start_ms,start_alloc_bytes,baseline_frames,baseline_avg_ms,baseline_median_ms,baseline_p95_ms,frames,frame_avg_ms,frame_median_ms,frame_p95_ms,frame_max_ms,update_avg_ms,update_p95_ms,gc_bytes_per_frame,gc_collections,moved");
            }
            sb.Append(_rep.ToString(Inv)).Append(',');
            sb.Append(_scenario).Append(',');
            sb.Append(_count.ToString(Inv)).Append(',');
            sb.Append(_library.Name).Append(',');
            sb.Append(Backend()).Append(',');
            sb.Append(AllocCounter.Method).Append(',');
            sb.Append(F(_startMs)).Append(',');
            sb.Append(_startAlloc.ToString(Inv)).Append(',');
            sb.Append(_baseline.Frames.ToString(Inv)).Append(',');
            sb.Append(F(_baseline.Average)).Append(',');
            sb.Append(F(_baseline.Median)).Append(',');
            sb.Append(F(_baseline.P95)).Append(',');
            sb.Append(run.Frames.ToString(Inv)).Append(',');
            sb.Append(F(run.Average)).Append(',');
            sb.Append(F(run.Median)).Append(',');
            sb.Append(F(run.P95)).Append(',');
            sb.Append(F(run.Max)).Append(',');
            sb.Append(F(run.Average - _baseline.Average)).Append(',');
            sb.Append(F(run.P95 - _baseline.Median)).Append(',');
            sb.Append(F((double)run.Bytes / (run.Frames + 1))).Append(',');
            sb.Append(run.Collections.ToString(Inv)).Append(',');
            sb.Append(moved.ToString(Inv));
            sb.AppendLine();
            File.AppendAllText(_outPath, sb.ToString());
        }

        static void WriteLibraries(string path) {
            var sb = new StringBuilder();
            sb.AppendLine("library,version");
            for (int i = 0; i < BenchRegistry.Libraries.Count; i++) {
                BenchLibrary library = BenchRegistry.Libraries[i];
                sb.Append(library.Name).Append(',').Append(library.Version).AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        static void WriteEnvironment(string path) {
            AllocCounter.Detect();
            var sb = new StringBuilder();
            sb.AppendLine("key,value");
            sb.Append("unity_version,").AppendLine(Application.unityVersion);
            sb.Append("backend,").AppendLine(Backend());
            sb.Append("development_build,").AppendLine(Debug.isDebugBuild ? "true" : "false");
            sb.Append("cpu,").AppendLine(SystemInfo.processorType.Replace(',', ' ').Trim());
            sb.Append("logical_cores,").AppendLine(SystemInfo.processorCount.ToString(Inv));
            sb.Append("os,").AppendLine(SystemInfo.operatingSystem.Replace(',', ' '));
            sb.Append("graphics,").AppendLine(SystemInfo.graphicsDeviceType.ToString());
            sb.Append("alloc_method,").AppendLine(AllocCounter.Method);
            File.WriteAllText(path, sb.ToString());
        }

        static string Backend() {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }

        static string LibraryNames() {
            var sb = new StringBuilder();
            for (int i = 0; i < BenchRegistry.Libraries.Count; i++) {
                if (i > 0) { sb.Append(", "); }
                sb.Append(BenchRegistry.Libraries[i].Name);
            }
            return sb.ToString();
        }

        static string F(double value) {
            return value.ToString("0.0000", Inv);
        }

        static string Arg(string name, string fallback) {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { return args[i + 1]; }
            }
            return fallback;
        }

        void Fail(Exception exception) {
            Debug.LogException(exception);
            enabled = false;
            Quit(2);
        }

        static void Quit(int code) {
#if UNITY_EDITOR
            Debug.Log("RavenBench: finished with code " + code);
#else
            Application.Quit(code);
#endif
        }
    }
}
