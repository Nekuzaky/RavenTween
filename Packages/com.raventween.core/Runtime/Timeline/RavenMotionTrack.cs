using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>
    /// A Timeline track that plays ready-made motions on the bound object: drop "Pop In",
    /// "Tada" or a Motion Style on a cutscene's timeline, stretch it, scrub it. Every frame is
    /// exact (the motion is sampled at the playhead), and previewing in the editor never leaves
    /// a change in the scene.
    /// </summary>
    [TrackColor(0.36f, 0.62f, 0.98f)]
    [TrackClipType(typeof(RavenMotionClip))]
    [TrackBindingType(typeof(Transform))]
    [DisplayName("RavenTween/Raven Motion Track")]
    public sealed class RavenMotionTrack : TrackAsset {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
            var mixer = ScriptPlayable<RavenMotionMixer>.Create(graph, inputCount);
            RavenMotionMixer behaviour = mixer.GetBehaviour();
            foreach (TimelineClip clip in GetClips()) {
                if (clip.asset is RavenMotionClip motion) { behaviour.AddClip(clip.start, clip.end, clip.clipIn, clip.timeScale, motion); }
            }
            return mixer;
        }

        // Tells Timeline which properties the clips animate, so its editor preview puts them back.
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
            var bound = director.GetGenericBinding(this) as Transform;
            if (bound == null) { return; }
            GameObject go = bound.gameObject;
            if (bound is RectTransform) {
                driver.AddFromName<RectTransform>(go, "m_AnchoredPosition.x");
                driver.AddFromName<RectTransform>(go, "m_AnchoredPosition.y");
            }
            AddVector(driver, go, "m_LocalPosition");
            driver.AddFromName<Transform>(go, "m_LocalRotation.x");
            driver.AddFromName<Transform>(go, "m_LocalRotation.y");
            driver.AddFromName<Transform>(go, "m_LocalRotation.z");
            driver.AddFromName<Transform>(go, "m_LocalRotation.w");
            AddVector(driver, go, "m_LocalScale");
            if (go.GetComponent<CanvasGroup>() != null) { driver.AddFromName<CanvasGroup>(go, "m_Alpha"); }
            if (go.GetComponent<Graphic>() != null) { AddColor<Graphic>(driver, go); }
            if (go.GetComponent<SpriteRenderer>() != null) { AddColor<SpriteRenderer>(driver, go); }
            base.GatherProperties(director, driver);
        }

        static void AddVector(IPropertyCollector driver, GameObject go, string vector) {
            driver.AddFromName<Transform>(go, vector + ".x");
            driver.AddFromName<Transform>(go, vector + ".y");
            driver.AddFromName<Transform>(go, vector + ".z");
        }

        static void AddColor<T>(IPropertyCollector driver, GameObject go) where T : UnityEngine.Component {
            driver.AddFromName<T>(go, "m_Color.r");
            driver.AddFromName<T>(go, "m_Color.g");
            driver.AddFromName<T>(go, "m_Color.b");
            driver.AddFromName<T>(go, "m_Color.a");
        }
    }

    /// <summary>One motion on a Raven Motion Track. Its length defaults to the motion's own timing.</summary>
    [DisplayName("Raven Motion")]
    public sealed class RavenMotionClip : PlayableAsset, ITimelineClipAsset {
        [Tooltip("A saved Motion Style. When set, it replaces the motion and settings below.")]
        public MotionStyle style;
        public MotionPreset preset = MotionPreset.PopIn;
        public MotionSettings settings = MotionSettings.Default;

        public ClipCaps clipCaps { get { return ClipCaps.SpeedMultiplier | ClipCaps.ClipIn; } }

        public MotionPreset EffectivePreset { get { return style != null ? style.Preset : preset; } }
        public MotionSettings EffectiveSettings { get { return style != null ? style.Settings : settings; } }

        /// <summary>One play (or one loop cycle); stretch the clip to repeat a loop or an attention motion.</summary>
        public override double duration {
            get {
                MotionSettings s = EffectiveSettings;
                float one = s.duration > 0f ? s.duration : MotionLibrary.DefaultDuration(EffectivePreset);
                return Mathf.Max(one, 0.05f);
            }
        }

        // The track's mixer does the work; a clip only carries its settings.
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            return Playable.Create(graph);
        }
    }

    /// <summary>
    /// Drives a Raven Motion Track: at every frame it shows the clip under the playhead, the end of
    /// the last clip already passed, or, before the first clip, the object as designed. The
    /// designed pose is read once for the whole track, before any clip moves the object.
    /// </summary>
    public sealed class RavenMotionMixer : PlayableBehaviour {
        struct Entry {
            public double Start;
            public double End;
            public double ClipIn;
            public double Speed;
            public RavenMotionClip Clip;
            public Sequence Motion;
            public MotionChannels Channels;
            public float Cycle;
            public bool Repeats;
        }

        readonly List<Entry> _clips = new List<Entry>(4);
        MotionTarget _target;
        MotionRest _rest;
        bool _hasRest;
        MotionChannels _used;

        internal void AddClip(double start, double end, double clipIn, double speed, RavenMotionClip clip) {
            var entry = new Entry { Start = start, End = end, ClipIn = clipIn, Speed = speed > 0.0 ? speed : 1.0, Clip = clip };
            int at = 0;
            while (at < _clips.Count && _clips[at].Start <= start) { at++; }
            _clips.Insert(at, entry);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            var bound = playerData as Transform;
            if (bound == null || _clips.Count == 0) { return; }
            if (!_hasRest || _target.Transform != bound) { Bind(bound); }
            double time = playable.GetTime();
            // The clip under the playhead; otherwise the clip that ended last, held on its end.
            int active = -1;
            int passed = -1;
            for (int i = 0; i < _clips.Count; i++) {
                Entry clip = _clips[i];
                if (time >= clip.Start && time < clip.End) { active = i; }
                else if (time >= clip.End && (passed < 0 || clip.End >= _clips[passed].End)) { passed = i; }
            }
            int shown = active >= 0 ? active : passed;
            bool hold = active < 0;
            if (shown < 0) { _rest.Apply(_target, _used); return; }
            Entry entry = _clips[shown];
            double local = hold ? entry.End - entry.Start : time - entry.Start;
            Sample(shown, (float)(entry.ClipIn + local * entry.Speed));
        }

        public override void OnPlayableDestroy(Playable playable) {
            for (int i = 0; i < _clips.Count; i++) {
                Entry entry = _clips[i];
                if (entry.Motion.IsAlive) { entry.Motion.Stop(); }
                entry.Motion = default;
                _clips[i] = entry;
            }
            // Outside Play Mode nothing of a preview may stay (a mesh tint lives in a property
            // block Timeline does not track): everything the motions touched is put back.
            if (!Application.isPlaying && _hasRest && _target.IsValid) { _rest.Apply(_target, _used); }
            _hasRest = false;
        }

        void Bind(Transform bound) {
            for (int i = 0; i < _clips.Count; i++) {
                Entry entry = _clips[i];
                if (entry.Motion.IsAlive) { entry.Motion.Stop(); }
                entry.Motion = default;
                _clips[i] = entry;
            }
            _target = MotionTarget.Resolve(bound);
            _rest = MotionRest.Capture(_target);
            _hasRest = true;
            _used = MotionChannels.None;
        }

        // The motion of a clip is an ordinary sequence, built once, paused (the playhead drives
        // it, never the engine) and sought. Loops and attention motions repeat over a longer clip.
        void Sample(int index, float time) {
            Entry entry = _clips[index];
            if (!entry.Motion.IsAlive) {
                entry = Build(entry);
                _clips[index] = entry;
                if (!entry.Motion.IsAlive) { return; }
            }
            float local = entry.Repeats ? Mathf.Repeat(time, entry.Cycle) : Mathf.Min(time, entry.Cycle);
            if (entry.Repeats && time > 0f && local <= 0f) { local = entry.Cycle; }
            _rest.Apply(_target, _used);
            TweenEngine.Seek(entry.Motion.Index, entry.Motion.Version, local);
        }

        Entry Build(Entry entry) {
            MotionSettings settings = entry.Clip.EffectiveSettings;
            settings.delay = 0f;
            MotionPreset preset = entry.Clip.EffectivePreset;
            // Built from the designed pose, so clips don't depend on the order they are reached.
            _rest.Apply(_target, _used);
            entry.Motion = MotionBuilder.Build(_target, preset, settings, _rest, out entry.Channels);
            if (!entry.Motion.IsAlive) { return entry; }
            entry.Motion.Pause();
            _used |= entry.Channels;
            entry.Cycle = Mathf.Max(entry.Motion.Duration, 0.0001f);
            MotionCategory category = MotionLibrary.Category(preset);
            entry.Repeats = category == MotionCategory.Loop || category == MotionCategory.Attention;
            return entry;
        }
    }
}
