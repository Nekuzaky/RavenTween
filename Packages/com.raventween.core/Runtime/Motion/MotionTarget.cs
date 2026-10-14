using System;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>Which parts of an object a motion animates.</summary>
    [Flags]
    public enum MotionChannels : byte {
        None = 0,
        Position = 1,
        Rotation = 2,
        Scale = 4,
        Alpha = 8,
        Color = 16,
        All = Position | Rotation | Scale | Alpha | Color
    }

    /// <summary>
    /// What a motion animates on one object: its transform (anchored position for UI), the first
    /// thing with an alpha it finds (a CanvasGroup, then a UI Graphic, then a SpriteRenderer) and
    /// the first thing with a color (a Graphic, a SpriteRenderer, or a renderer's material color).
    /// </summary>
    public struct MotionTarget {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock _block;

        public Transform Transform;
        public RectTransform Rect;
        public CanvasGroup Group;
        public Graphic Graphic;
        public SpriteRenderer Sprite;
        /// <summary>A mesh (or other) renderer whose material color can be tinted; null otherwise.</summary>
        public Renderer Renderer;
        /// <summary>The material color property the renderer tints: _BaseColor (URP / HDRP) or _Color.</summary>
        public int ColorId;

        public bool IsValid { get { return Transform != null; } }

        /// <summary>True when fades have something to fade.</summary>
        public bool HasAlpha { get { return Group != null || Graphic != null || Sprite != null; } }

        /// <summary>UI and sprites turn around Z (in the screen plane); 3D objects spin around Y.</summary>
        public bool IsFlat { get { return Rect != null || Sprite != null; } }

        /// <summary>True when the Tint motion has something to color.</summary>
        public bool HasColor { get { return Graphic != null || Sprite != null || Renderer != null; } }

        public static MotionTarget Resolve(Transform transform) {
            var target = new MotionTarget();
            if (transform == null) { return target; }
            target.Transform = transform;
            target.Rect = transform as RectTransform;
            target.Group = transform.GetComponent<CanvasGroup>();
            target.Graphic = transform.GetComponent<Graphic>();
            target.Sprite = transform.GetComponent<SpriteRenderer>();
            if (target.Graphic == null && target.Sprite == null) { ResolveRenderer(ref target); }
            return target;
        }

        static void ResolveRenderer(ref MotionTarget target) {
            Renderer renderer = target.Transform.GetComponent<Renderer>();
            if (renderer == null) { return; }
            Material material = renderer.sharedMaterial;
            if (material == null) { return; }
            if (material.HasProperty(BaseColorId)) { target.ColorId = BaseColorId; }
            else if (material.HasProperty(ColorPropertyId)) { target.ColorId = ColorPropertyId; }
            else { return; }
            target.Renderer = renderer;
        }

        /// <summary>The object's size, used for automatic slide distances: pixels for UI, units otherwise.</summary>
        public float AutoDistance(in MotionRest rest) {
            if (Rect != null) {
                Rect r = Rect.rect;
                return Mathf.Max(Mathf.Max(r.width, r.height) * 0.5f, 40f);
            }
            if (Sprite != null && Sprite.sprite != null) {
                Vector3 size = Sprite.sprite.bounds.size;
                return Mathf.Max(Mathf.Max(size.x * Mathf.Abs(rest.Scale.x), size.y * Mathf.Abs(rest.Scale.y)), 0.1f);
            }
            float largest = Mathf.Max(Mathf.Abs(rest.Scale.x), Mathf.Max(Mathf.Abs(rest.Scale.y), Mathf.Abs(rest.Scale.z)));
            return Mathf.Max(largest, 0.1f);
        }

        public float ReadAlpha() {
            if (Group != null) { return Group.alpha; }
            if (Graphic != null) { return Graphic.color.a; }
            if (Sprite != null) { return Sprite.color.a; }
            return 1f;
        }

        public Color ReadColor() {
            if (Graphic != null) { return Graphic.color; }
            if (Sprite != null) { return Sprite.color; }
            if (Renderer != null) { return ReadRendererColor(); }
            return Color.white;
        }

        // The block's color when the renderer carries one (a previous tint), else the material's.
        Color ReadRendererColor() {
            if (Renderer.HasPropertyBlock()) {
                if (_block == null) { _block = new MaterialPropertyBlock(); }
                Renderer.GetPropertyBlock(_block);
                if (_block.HasColor(ColorId)) { return _block.GetColor(ColorId); }
            }
            return Renderer.sharedMaterial.GetColor(ColorId);
        }

        public void WriteColor(Color color) {
            if (Graphic != null) { Graphic.color = color; return; }
            if (Sprite != null) { Sprite.color = color; return; }
            if (Renderer == null) { return; }
            if (_block == null) { _block = new MaterialPropertyBlock(); }
            Renderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            Renderer.SetPropertyBlock(_block);
        }

        public void WriteAlpha(float alpha) {
            if (Group != null) { Group.alpha = alpha; return; }
            if (Graphic != null) {
                Color c = Graphic.color;
                c.a = alpha;
                Graphic.color = c;
                return;
            }
            if (Sprite != null) {
                Color c = Sprite.color;
                c.a = alpha;
                Sprite.color = c;
            }
        }

        public Vector3 ReadPosition() { return Rect != null ? (Vector3)Rect.anchoredPosition : Transform.localPosition; }

        public void WritePosition(Vector3 position) {
            if (Rect != null) { Rect.anchoredPosition = position; }
            else { Transform.localPosition = position; }
        }
    }

    /// <summary>
    /// The pose an object returns to: entrances end on it, exits start from it, attention and
    /// loop motions move around it. Positions are anchored positions for UI, local otherwise.
    /// </summary>
    public struct MotionRest {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public float Alpha;
        public Color Color;

        public static MotionRest Capture(in MotionTarget target) {
            Debug.Assert(target.IsValid, "Capturing a rest pose needs a target.");
            return new MotionRest {
                Position = target.ReadPosition(),
                Rotation = target.Transform.localRotation,
                Scale = target.Transform.localScale,
                Alpha = target.ReadAlpha(),
                Color = target.ReadColor()
            };
        }

        /// <summary>Puts the chosen channels of <paramref name="target"/> back on this pose.</summary>
        public void Apply(in MotionTarget target, MotionChannels channels) {
            if (!target.IsValid) { return; }
            if ((channels & MotionChannels.Position) != 0) { target.WritePosition(Position); }
            if ((channels & MotionChannels.Rotation) != 0) { target.Transform.localRotation = Rotation; }
            if ((channels & MotionChannels.Scale) != 0) { target.Transform.localScale = Scale; }
            if ((channels & MotionChannels.Color) != 0 && target.HasColor) { target.WriteColor(Color); }
            if ((channels & MotionChannels.Alpha) != 0 && target.HasAlpha) { target.WriteAlpha(Alpha); }
        }
    }
}
