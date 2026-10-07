using System.Collections.Generic;
using NUnit.Framework;
using RavenTween.Editor;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>The timeline must place steps exactly where the runtime sequence plays them.</summary>
    public sealed class SequenceLayoutTests {
        readonly List<Object> _cleanup = new List<Object>();
        readonly List<SequenceLayout.Block> _blocks = new List<SequenceLayout.Block>();
        GameObject _go;
        RavenSequencePlayer _player;

        [SetUp]
        public void SetUp() {
            _go = new GameObject("layout-host");
            _player = _go.AddComponent<RavenSequencePlayer>();
        }

        [TearDown]
        public void TearDown() {
            TweenEngine.KillAll(false);
            Object.DestroyImmediate(_go);
            foreach (Object o in _cleanup) { Object.DestroyImmediate(o); }
            _cleanup.Clear();
        }

        TweenTemplate Template(float duration, float delay = 0f, int cycles = 1, CycleMode mode = CycleMode.Restart) {
            var t = ScriptableObject.CreateInstance<TweenTemplate>();
            t.property = PropertyKind.LocalPosition;
            t.settings = TweenParams.Default;
            t.settings.duration = duration;
            t.settings.startDelay = delay;
            t.settings.cycles = cycles;
            t.settings.cycleMode = mode;
            _cleanup.Add(t);
            return t;
        }

        void Add(RavenSequencePlayer.StepMode mode, TweenTemplate template, float insertTime = 0f) {
            _player.Steps.Add(new RavenSequencePlayer.Step { mode = mode, template = template, target = _go.transform, insertTime = insertTime });
        }

        float RuntimeDuration() {
            Sequence sequence = _player.BuildSequence();
            float duration = sequence.Duration;
            sequence.Stop();
            return duration;
        }

        [Test]
        public void ChainGroupInsert_MatchRuntime() {
            Add(RavenSequencePlayer.StepMode.Chain, Template(0.5f));
            Add(RavenSequencePlayer.StepMode.Group, Template(0.3f, 0.1f));
            Add(RavenSequencePlayer.StepMode.Chain, Template(0.4f, 0f, 2, CycleMode.Yoyo));
            Add(RavenSequencePlayer.StepMode.Insert, Template(0.2f), 2.5f);
            float total = SequenceLayout.Compute(_player.Steps, _blocks);
            Assert.That(_blocks[0].Start, Is.EqualTo(0f));
            Assert.That(_blocks[1].Start, Is.EqualTo(0f), "Group starts with the previous item.");
            Assert.That(_blocks[2].Start, Is.EqualTo(0.5f).Within(1e-5f), "Chain starts after everything so far.");
            Assert.That(_blocks[2].Length, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(_blocks[3].Start, Is.EqualTo(2.5f));
            Assert.That(total, Is.EqualTo(RuntimeDuration()).Within(1e-5f));
        }

        [Test]
        public void LoopingTemplate_IsClampedLikeRuntime() {
            Add(RavenSequencePlayer.StepMode.Chain, Template(0.25f, 0f, -1, CycleMode.Yoyo));
            Add(RavenSequencePlayer.StepMode.Chain, Template(0.3f, 0f, -1, CycleMode.Restart));
            float total = SequenceLayout.Compute(_player.Steps, _blocks);
            Assert.That(_blocks[0].LoopClamped, Is.True);
            Assert.That(_blocks[0].Length, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(_blocks[1].Length, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(total, Is.EqualTo(RuntimeDuration()).Within(1e-5f));
        }

        [Test]
        public void IncompleteSteps_AreSkippedLikeRuntime() {
            Add(RavenSequencePlayer.StepMode.Chain, Template(0.5f));
            _player.Steps.Add(new RavenSequencePlayer.Step { mode = RavenSequencePlayer.StepMode.Chain });
            Add(RavenSequencePlayer.StepMode.Group, Template(0.2f));
            float total = SequenceLayout.Compute(_player.Steps, _blocks);
            Assert.That(_blocks[1].Valid, Is.False);
            Assert.That(_blocks[2].Start, Is.EqualTo(0f), "Group pairs with the last valid step.");
            Assert.That(total, Is.EqualTo(RuntimeDuration()).Within(1e-5f));
        }
    }
}
