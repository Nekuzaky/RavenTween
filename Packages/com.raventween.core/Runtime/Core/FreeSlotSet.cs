using System;
using UnityEngine;

namespace RavenTween {
    sealed class FreeSlotSet {
        const ulong DeBruijn = 0x03F79D71B4CB0A89UL;
        static readonly int[] BitIndex = BuildBitIndex();

        ulong[] _words = new ulong[1];
        ulong[] _summary = new ulong[1];
        int _cursor;
        int _count;

        public int Count { get { return _count; } }

        static int[] BuildBitIndex() {
            var table = new int[64];
            for (int i = 0; i < 64; i++) { table[unchecked((1UL << i) * DeBruijn) >> 58] = i; }
            return table;
        }

        static int LowestBit(ulong value) {
            Debug.Assert(value != 0UL, "A word with no bit set has no lowest bit.");
            return BitIndex[unchecked((value & (0UL - value)) * DeBruijn) >> 58];
        }

        public void Clear() {
            Array.Clear(_words, 0, _words.Length);
            Array.Clear(_summary, 0, _summary.Length);
            _cursor = 0;
            _count = 0;
        }

        public void Add(int index) {
            Debug.Assert(index >= 0, "Slot indices are never negative.");
            int word = index >> 6;
            EnsureWords(word + 1);
            ulong bit = 1UL << (index & 63);
            if ((_words[word] & bit) != 0UL) { return; }
            _words[word] |= bit;
            int group = word >> 6;
            _summary[group] |= 1UL << (word & 63);
            if (group < _cursor) { _cursor = group; }
            _count++;
        }

        public int TakeLowest() {
            if (_count == 0) { return -1; }
            while (_summary[_cursor] == 0UL) { _cursor++; }
            ulong groupBits = _summary[_cursor];
            int word = (_cursor << 6) + LowestBit(groupBits);
            ulong bits = _words[word];
            int index = (word << 6) + LowestBit(bits);
            bits &= bits - 1UL;
            _words[word] = bits;
            if (bits == 0UL) { _summary[_cursor] = groupBits & (groupBits - 1UL); }
            _count--;
            return index;
        }

        void EnsureWords(int words) {
            if (words > _words.Length) { Array.Resize(ref _words, Mathf.Max(words, _words.Length * 2)); }
            int groups = (_words.Length + 63) >> 6;
            if (groups > _summary.Length) { Array.Resize(ref _summary, groups); }
        }
    }
}
