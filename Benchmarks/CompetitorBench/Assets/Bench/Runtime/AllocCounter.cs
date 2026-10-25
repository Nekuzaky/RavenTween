using System;
using UnityEngine.Scripting;

namespace RavenBench {
    static class AllocCounter {
        static bool _perThread;
        static bool _open;
        static object _sink;

        public static string Method {
            get { return _perThread ? "GC.GetAllocatedBytesForCurrentThread" : "GC disabled + GC.GetTotalMemory"; }
        }

        public static void Detect() {
            const int Size = 1 << 20;
            long before = ThreadBytes();
            _sink = new byte[Size];
            long after = ThreadBytes();
            _sink = null;
            _perThread = after - before >= Size;
        }

        static long ThreadBytes() {
            try {
                return GC.GetAllocatedBytesForCurrentThread();
            } catch (Exception) {
                return 0;
            }
        }

        public static long Open() {
            if (!_perThread && !_open) {
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
                _open = true;
            }
            return Read();
        }

        public static long Close(long start) {
            long bytes = Math.Max(Read() - start, 0);
            if (_open) {
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                _open = false;
            }
            return bytes;
        }

        static long Read() {
            return _perThread ? GC.GetAllocatedBytesForCurrentThread() : GC.GetTotalMemory(false);
        }
    }
}
