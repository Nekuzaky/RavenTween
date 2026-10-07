using NUnit.Framework;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>Seek drives editor scrubbing: any time, any order, same pose as real playback.</summary>
    public sealed class SeekTests {
        GameObject _go;

        [SetUp]
        public void SetUp() {
            TweenEngine.KillAll(false);
            _go = new GameObject("seek-target");
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
        }

        Sequence TwoMovesOnSameProperty() {
            return Raven.Sequence()
                .Chain(Raven.LocalPosition(_go.transform, new Vector3(1f, 0f, 0f), 1f))
                .Chain(Raven.LocalPosition(_go.transform, new Vector3(1f, 1f, 0f), 1f));
        }

        // The editor restores recorded values before every seek; tests do the same.
        void SeekFromRest(Sequence s, float time) {
            _go.transform.localPosition = Vector3.zero;
            TweenEngine.Seek(s.Index, s.Version, time);
        }

        static void AssertNear(Vector3 actual, Vector3 expected) {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-4f), "Expected " + expected + ", got " + actual);
        }

        [Test]
        public void Seek_Forward_ChainsOnSameProperty() {
            Sequence s = TwoMovesOnSameProperty();
            SeekFromRest(s, 0.5f);
            AssertNear(_go.transform.localPosition, new Vector3(0.5f, 0f, 0f));
            SeekFromRest(s, 1.5f);
            AssertNear(_go.transform.localPosition, new Vector3(1f, 0.5f, 0f));
        }

        [Test]
        public void Seek_Backward_RecapturesCorrectly() {
            Sequence s = TwoMovesOnSameProperty();
            SeekFromRest(s, 2f);
            AssertNear(_go.transform.localPosition, new Vector3(1f, 1f, 0f));
            SeekFromRest(s, 0.25f);
            AssertNear(_go.transform.localPosition, new Vector3(0.25f, 0f, 0f));
            SeekFromRest(s, 1.75f);
            AssertNear(_go.transform.localPosition, new Vector3(1f, 0.75f, 0f));
        }

        [Test]
        public void Seek_MatchesRealPlayback() {
            Sequence s = TwoMovesOnSameProperty();
            SeekFromRest(s, 1.3f);
            Vector3 scrubbed = _go.transform.localPosition;
            s.Stop();
            _go.transform.localPosition = Vector3.zero;
            TwoMovesOnSameProperty();
            for (int i = 0; i < 13; i++) { TweenEngine.Process(0.1f, 0.1f); }
            AssertNear(_go.transform.localPosition, scrubbed);
        }

        [Test]
        public void Seek_Tween_HonorsYoyoCycles() {
            float value = -1f;
            Tween t = Raven.Value(0f, 10f, 1f).Cycles(2, CycleMode.Yoyo).OnUpdate(v => value = v);
            TweenEngine.Seek(t.Index, t.Version, 0.5f);
            Assert.That(value, Is.EqualTo(5f).Within(1e-3f));
            TweenEngine.Seek(t.Index, t.Version, 1.75f);
            Assert.That(value, Is.EqualTo(2.5f).Within(1e-3f), "Second yoyo cycle plays backwards.");
        }

        [Test]
        public void Seek_BeforeDelay_LeavesTargetAlone() {
            Sequence s = Raven.Sequence().Chain(Raven.LocalPosition(_go.transform, Vector3.one, 1f)).Delay(0.5f);
            SeekFromRest(s, 0.25f);
            AssertNear(_go.transform.localPosition, Vector3.zero);
        }
    }
}
