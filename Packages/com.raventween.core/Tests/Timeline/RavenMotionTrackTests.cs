using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RavenTween.Tests {
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
        public void ScrubbingBeforeTheClip_ShowsTheDesignedPose() {
            PlayableDirector director = Director(MotionPreset.PopIn, 1.0, 0.4);
            At(director, 1.2);
            Assert.That(_go.transform.localScale.x, Is.Not.EqualTo(2f).Within(1e-3f), "Mid Pop In.");
            At(director, 0.5);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(2f).Within(1e-4f), "Before the clip: as designed, not the last pose shown.");
        }

        [Test]
        public void ExitThenEntrance_OnOneTrack_EndsAtTheDesignedPose() {
            var track = _timeline.CreateTrack<RavenMotionTrack>(null, "Motion");
            TimelineClip exit = track.CreateClip<RavenMotionClip>();
            ((RavenMotionClip)exit.asset).preset = MotionPreset.SlideOutLeft;
            exit.start = 0.0;
            exit.duration = 0.5;
            TimelineClip entrance = track.CreateClip<RavenMotionClip>();
            ((RavenMotionClip)entrance.asset).preset = MotionPreset.SlideInLeft;
            entrance.start = 1.0;
            entrance.duration = 0.5;
            _go.transform.localPosition = new Vector3(3f, 1f, 0f);
            var director = _go.AddComponent<PlayableDirector>();
            director.playableAsset = _timeline;
            director.SetGenericBinding(track, _go.transform);
            director.RebuildGraph();
            for (double t = 0.0; t <= 1.6; t += 0.05) { At(director, t); }
            Assert.That(_go.transform.localPosition, Is.EqualTo(new Vector3(3f, 1f, 0f)), "The entrance comes back to where the object was designed, not to where the exit left it.");
        }

        [Test]
        public void StopAll_DuringACutscene_DoesNotShiftTheRestPose() {
            PlayableDirector director = Director(MotionPreset.PopIn, 0.0, 0.4);
            At(director, 0.1);
            Raven.StopAll();
            At(director, 0.4);
            Assert.That(_go.transform.localScale.x, Is.EqualTo(2f).Within(1e-3f));
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
