using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RavenTween.Tests {
    /// <summary>Raven Motion Track: clips sampled exactly at the playhead.</summary>
    public sealed class RavenMotionTrackTests {
        GameObject _go;
        TimelineAsset _timeline;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            _go = new GameObject("timeline-target");
            _go.transform.localScale = new Vector3(2f, 2f, 2f);
            _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_timeline);
        }

        PlayableDirector Director(MotionPreset preset, double start, double length) {
            var track = _timeline.CreateTrack<RavenMotionTrack>(null, "Motion");
            TimelineClip clip = track.CreateClip<RavenMotionClip>();
            ((RavenMotionClip)clip.asset).preset = preset;
            clip.start = start;
            clip.duration = length;
            var director = _go.AddComponent<PlayableDirector>();
            director.playableAsset = _timeline;
            director.SetGenericBinding(track, _go.transform);
            director.RebuildGraph();
            return director;
        }

        static void At(PlayableDirector director, double time) {
            director.time = time;
            director.Evaluate();
        }

        [Test]
        public void Entrance_IsSampledExactlyAtThePlayhead() {
            PlayableDirector director = Director(MotionPreset.PopIn, 0.0, 0.4);
            At(director, 0.0);
            Assert.That(_go.transform.localScale.x, Is.LessThan(0.01f), "Start of Pop In: nothing.");
            At(director, 0.2);
            float middle = _go.transform.localScale.x;
            Assert.That(middle, Is.GreaterThan(0.5f));
            At(director, 0.05);
            Assert.That(_go.transform.localScale.x, Is.LessThan(middle), "Scrubbing backwards goes back.");
            At(director, 0.4);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(2f).Within(1e-3f), "End: the designed scale.");
        }

        [Test]
        public void Loop_RepeatsOverALongerClip() {
            PlayableDirector director = Director(MotionPreset.Breathe, 0.0, 9.0);
            float cycle = MotionLibrary.DefaultDuration(MotionPreset.Breathe);
            At(director, cycle * 0.5f);
            float swell = _go.transform.localScale.x;
            At(director, cycle * 2.5f);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(swell).Within(1e-3f), "Same point of a later cycle, same pose.");
            Assert.That(swell, Is.GreaterThan(2f));
        }

        [Test]
        public void Exit_HoldsItsEndAfterTheClip() {
            PlayableDirector director = Director(MotionPreset.PopOut, 0.0, 0.4);
            At(director, 0.2);
            At(director, 0.4);
            Assert.That(_go.transform.localScale.x, Is.LessThan(0.01f), "Hidden at the end of Pop Out.");
        }
    }
}
