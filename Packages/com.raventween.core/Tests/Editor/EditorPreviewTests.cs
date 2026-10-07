using NUnit.Framework;
using RavenTween.Editor;
using UnityEngine;

namespace RavenTween.Tests {
    /// <summary>Edit Mode previews restore everything they touched, whatever the property.</summary>
    public sealed class EditorPreviewTests {
        GameObject _go;
        Material _material;
        readonly object _owner = new object();

        [SetUp]
        public void SetUp() {
            EditorTweenPreview.End();
            _go = new GameObject("preview-target");
        }

        [TearDown]
        public void TearDown() {
            EditorTweenPreview.End();
            Object.DestroyImmediate(_go);
            if (_material != null) { Object.DestroyImmediate(_material); }
        }

        [Test]
        public void Preview_RestoresAxesAndPropertyBlocks() {
            Shader shader = Shader.Find("Unlit/Color");
            Assume.That(shader != null, "Unlit/Color shader is not available.");
            _material = new Material(shader) { color = Color.white };
            var renderer = _go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            int id = Shader.PropertyToID("_Color");
            _go.transform.position = new Vector3(1f, 2f, 3f);

            EditorTweenPreview.Record(renderer, PropertyKind.PropertyBlockColor, id);
            EditorTweenPreview.Record(_go.transform, PropertyKind.PositionX, 0);
            Sequence preview = Raven.Sequence()
                .Group(Raven.PropertyBlockColor(renderer, id, Color.red, 1f))
                .Group(Raven.PositionX(_go.transform, 9f, 1f));
            EditorTweenPreview.Begin(_owner, preview, 1f, false);
            EditorTweenPreview.Seek(0.5f);
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            Assert.That(_go.transform.position.x, Is.EqualTo(5f).Within(1e-4f));

            EditorTweenPreview.End();
            Assert.That(renderer.HasPropertyBlock(), Is.False, "A renderer without a block before the preview has none after.");
            Assert.That(Vector3.Distance(_go.transform.position, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-5f));
            Assert.That(_material.color, Is.EqualTo(Color.white));
        }
    }
}
