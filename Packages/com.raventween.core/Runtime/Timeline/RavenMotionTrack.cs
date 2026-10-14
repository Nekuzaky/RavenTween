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
        // Tells Timeline which properties the clips animate, so its editor preview puts them back.
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
            var bound = director.GetGenericBinding(this) as Transform;
            if (bound == null) { return; }
            GameObject go = bound.gameObject;
            if (bound is RectTransform) {
                driver.AddFromName<RectTransform>(go, "m_AnchoredPosition.x");
                driver.AddFromName<RectTransform>(go, "m_AnchoredPosition.y");
            }
            Add(driver, go, "m_LocalPosition", true);
            driver.AddFromName<Transform>(go, "m_LocalRotation.x");
            driver.AddFromName<Transform>(go, "m_LocalRotation.y");
            driver.AddFromName<Transform>(go, "m_LocalRotation.z");
            driver.AddFromName<Transform>(go, "m_LocalRotation.w");
            Add(driver, go, "m_LocalScale", true);
            if (go.GetComponent<CanvasGroup>() != null) { driver.AddFromName<CanvasGroup>(go, "m_Alpha"); }
            if (go.GetComponent<Graphic>() != null) { AddColor<Graphic>(driver, go); }
            if (go.GetComponent<SpriteRenderer>() != null) { AddColor<SpriteRenderer>(driver, go); }
            base.GatherProperties(director, driver);
        }

        static void Add(IPropertyCollector driver, GameObject go, string vector, bool xyz) {
            driver.AddFromName<Transform>(go, vector + ".x");
            driver.AddFromName<Transform>(go, vector + ".y");
            if (xyz) { driver.AddFromName<Transform>(go, vector + ".z"); }
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

        public ClipCaps clipCaps { get { return ClipCaps.None; } }

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

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            var playable = ScriptPlayable<RavenMotionBehaviour>.Create(graph);
            playable.GetBehaviour().Clip = this;
            return playable;
        }
    }

    /// <summary>Samples a motion at the playhead. The motion is an ordinary sequence, paused and sought.</summary>
    public sealed class RavenMotionBehaviour : PlayableBehaviour {
        public RavenMotionClip Clip;

        Sequence _motion;
        MotionTarget _target;
        MotionRest _rest;
        MotionChannels _channels;
        float _cycle;
        bool _repeats;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            var bound = playerData as Transform;
            if (bound == null || Clip == null) { return; }
            if (!_motion.IsAlive || _target.Transform != bound) { Build(bound); }
            if (!_motion.IsAlive) { return; }
            Sample((float)playable.GetTime());
        }

        // Leaving the clip: show its last frame, as a Timeline animation clip would.
        public override void OnBehaviourPause(Playable playable, FrameData info) {
            if (!_motion.IsAlive) { return; }
            double time = playable.GetTime();
            if (time >= playable.GetDuration() - 1e-4) { Sample((float)playable.GetDuration()); }
        }

        public override void OnPlayableDestroy(Playable playable) {
            if (_motion.IsAlive) { _motion.Stop(); }
            _motion = default;
            // Outside Play Mode nothing of a preview may stay: a mesh tint lives in a property
            // block Timeline does not track, so it is put back here.
            if (!Application.isPlaying && _target.IsValid && (_channels & MotionChannels.Color) != 0) {
                _rest.Apply(_target, MotionChannels.Color);
            }
        }

        void Build(Transform bound) {
            if (_motion.IsAlive) { _motion.Stop(); }
            _target = MotionTarget.Resolve(bound);
            _rest = MotionRest.Capture(_target);
            MotionSettings settings = Clip.EffectiveSettings;
            settings.delay = 0f;
            MotionPreset preset = Clip.EffectivePreset;
            _motion = MotionBuilder.Build(_target, preset, settings, _rest, out _channels);
            if (!_motion.IsAlive) { return; }
            // The playhead drives it: the engine must never step it.
            _motion.Pause();
            _cycle = Mathf.Max(_motion.Duration, 0.0001f);
            MotionCategory category = MotionLibrary.Category(preset);
            _repeats = category == MotionCategory.Loop || category == MotionCategory.Attention;
        }

        // Loops and attention motions repeat over a longer clip; the rest hold their end.
        void Sample(float time) {
            float local = _repeats ? Mathf.Repeat(time, _cycle) : Mathf.Min(time, _cycle);
            if (_repeats && time > 0f && local <= 0f) { local = _cycle; }
            _rest.Apply(_target, _channels);
            TweenEngine.Seek(_motion.Index, _motion.Version, local);
        }
    }
}
