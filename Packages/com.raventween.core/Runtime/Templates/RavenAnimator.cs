using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    [AddComponentMenu("RavenTween/Raven Animator")]
    public sealed class RavenAnimator : MonoBehaviour {
        [Serializable]
        public struct Entry {
            [Tooltip("Component or asset the template animates (Transform, CanvasGroup, Material, ...).")]
            public UnityEngine.Object target;
            public TweenTemplate template;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        [SerializeField] bool playOnEnable;
        [SerializeField] UnityEvent onAllComplete = new UnityEvent();

        readonly List<Tween> _live = new List<Tween>();
        int _pending;

        public List<Entry> Entries { get { return entries; } }

        public UnityEvent OnAllComplete { get { return onAllComplete; } }

        void OnEnable() {
            if (playOnEnable) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        public void Play() {
            Stop();
            _pending = 0;
            for (int i = 0; i < entries.Count; i++) {
                Entry entry = entries[i];
                if (entry.template == null || entry.target == null) {
                    Debug.LogWarning("RavenTween: RavenAnimator entry " + i + " is incomplete and was skipped.", this);
                    continue;
                }
                Tween tween = entry.template.Play(entry.target);
                if (!tween.IsAlive) { continue; }
                _pending++;
                tween.OnComplete(HandleEntryEnd).OnKill(HandleEntryEnd).OnTargetDestroyed(HandleEntryEnd);
                _live.Add(tween);
            }
            if (_pending == 0) { onAllComplete.Invoke(); }
        }

        public void Stop() {
            _stopping = true;
            try {
                for (int i = 0; i < _live.Count; i++) { _live[i].Stop(); }
            } finally {
                _stopping = false;
            }
            _live.Clear();
            _pending = 0;
        }

        public void CompleteNow() {
            if (_live.Count == 0) { return; }
            Tween[] snapshot = _live.ToArray();
            for (int i = 0; i < snapshot.Length; i++) { snapshot[i].Complete(); }
        }

        bool _stopping;

        void HandleEntryEnd() {
            if (_stopping || _pending <= 0) { return; }
            _pending--;
            if (_pending > 0) { return; }
            _live.Clear();
            onAllComplete.Invoke();
        }
    }
}
