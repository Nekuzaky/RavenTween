using UnityEngine;
using UnityEngine.UI;

namespace RavenTween {
    /// <summary>Built-in animated properties. Driven internally without reflection or boxing.</summary>
    public enum PropertyKind : byte {
        None = 0,
        Position,
        LocalPosition,
        Rotation,
        LocalRotation,
        EulerAngles,
        LocalEulerAngles,
        LocalScale,
        AnchoredPosition,
        SizeDelta,
        CanvasGroupAlpha,
        GraphicColor,
        GraphicAlpha,
        CameraFieldOfView,
        CameraOrthographicSize,
        CameraBackgroundColor,
        AudioVolume,
        AudioPitch,
        MaterialFloat,
        MaterialColor,
        SpriteColor,
        LightIntensity,
        LightColor,
        // Values are serialized in templates: new kinds are only ever appended.
        PositionX,
        PositionY,
        PositionZ,
        LocalPositionX,
        LocalPositionY,
        LocalPositionZ,
        LocalScaleX,
        LocalScaleY,
        LocalScaleZ,
        AnchoredPositionX,
        AnchoredPositionY,
        PropertyBlockFloat,
        PropertyBlockColor,
        SpriteAlpha
    }

    /// <summary>Reads and writes built-in properties on their targets. No reflection, no allocation.</summary>
    static class PropertyAccessor {
        /// <summary>The object type a property is read from and written to.</summary>
        public static System.Type TargetType(PropertyKind kind) {
            switch (kind) {
                case PropertyKind.AnchoredPosition:
                case PropertyKind.AnchoredPositionX:
                case PropertyKind.AnchoredPositionY:
                case PropertyKind.SizeDelta: return typeof(RectTransform);
                case PropertyKind.PropertyBlockFloat:
                case PropertyKind.PropertyBlockColor: return typeof(Renderer);
                case PropertyKind.CanvasGroupAlpha: return typeof(CanvasGroup);
                case PropertyKind.GraphicColor:
                case PropertyKind.GraphicAlpha: return typeof(Graphic);
                case PropertyKind.CameraFieldOfView:
                case PropertyKind.CameraOrthographicSize:
                case PropertyKind.CameraBackgroundColor: return typeof(Camera);
                case PropertyKind.AudioVolume:
                case PropertyKind.AudioPitch: return typeof(AudioSource);
                case PropertyKind.MaterialFloat:
                case PropertyKind.MaterialColor: return typeof(Material);
                case PropertyKind.SpriteColor:
                case PropertyKind.SpriteAlpha: return typeof(SpriteRenderer);
                case PropertyKind.LightIntensity:
                case PropertyKind.LightColor: return typeof(Light);
                default: return typeof(Transform);
            }
        }

        /// <summary>
        /// Finds the object a property needs from whatever was assigned: the object itself, or a
        /// component on the same GameObject (a renderer's material for material properties).
        /// Returns null when nothing suitable exists.
        /// </summary>
        public static Object ResolveTarget(Object assigned, PropertyKind kind, bool instanceMaterial) {
            if (assigned == null) { return null; }
            System.Type type = TargetType(kind);
            if (type.IsInstanceOfType(assigned)) { return assigned; }
            GameObject go = assigned as GameObject;
            if (go == null && assigned is Component component) { go = component.gameObject; }
            if (go == null) { return null; }
            if (type != typeof(Material)) { return go.GetComponent(type); }
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) { return null; }
            return instanceMaterial ? renderer.material : renderer.sharedMaterial;
        }

        /// <summary>Value kind produced and consumed by a given property.</summary>
        public static ValueKind KindOf(PropertyKind kind) {
            switch (kind) {
                case PropertyKind.AnchoredPosition:
                case PropertyKind.SizeDelta:
                    return ValueKind.Vector2;
                case PropertyKind.Position:
                case PropertyKind.LocalPosition:
                case PropertyKind.EulerAngles:
                case PropertyKind.LocalEulerAngles:
                case PropertyKind.LocalScale:
                    return ValueKind.Vector3;
                case PropertyKind.Rotation:
                case PropertyKind.LocalRotation:
                    return ValueKind.Quaternion;
                case PropertyKind.GraphicColor:
                case PropertyKind.CameraBackgroundColor:
                case PropertyKind.MaterialColor:
                case PropertyKind.SpriteColor:
                case PropertyKind.LightColor:
                case PropertyKind.PropertyBlockColor:
                    return ValueKind.Color;
                default:
                    return ValueKind.Float;
            }
        }

        /// <summary>Reads the current value of <paramref name="kind"/> from <paramref name="target"/>.</summary>
        public static TweenValue Read(PropertyKind kind, Object target, int propertyId) {
            Debug.Assert(kind != PropertyKind.None, "PropertyKind.None has no backing property.");
            Debug.Assert(target != null, "Cannot read a property from a destroyed target.");
            switch (kind) {
                case PropertyKind.Position: return new TweenValue(((Transform)target).position);
                case PropertyKind.LocalPosition: return new TweenValue(((Transform)target).localPosition);
                case PropertyKind.Rotation: return new TweenValue(((Transform)target).rotation);
                case PropertyKind.LocalRotation: return new TweenValue(((Transform)target).localRotation);
                case PropertyKind.EulerAngles: return new TweenValue(((Transform)target).eulerAngles);
                case PropertyKind.LocalEulerAngles: return new TweenValue(((Transform)target).localEulerAngles);
                case PropertyKind.LocalScale: return new TweenValue(((Transform)target).localScale);
                case PropertyKind.AnchoredPosition: return new TweenValue(((RectTransform)target).anchoredPosition);
                case PropertyKind.SizeDelta: return new TweenValue(((RectTransform)target).sizeDelta);
                case PropertyKind.CanvasGroupAlpha: return new TweenValue(((CanvasGroup)target).alpha);
                case PropertyKind.GraphicColor: return new TweenValue(((Graphic)target).color);
                case PropertyKind.GraphicAlpha: return new TweenValue(((Graphic)target).color.a);
                case PropertyKind.CameraFieldOfView: return new TweenValue(((Camera)target).fieldOfView);
                case PropertyKind.CameraOrthographicSize: return new TweenValue(((Camera)target).orthographicSize);
                case PropertyKind.CameraBackgroundColor: return new TweenValue(((Camera)target).backgroundColor);
                case PropertyKind.AudioVolume: return new TweenValue(((AudioSource)target).volume);
                case PropertyKind.AudioPitch: return new TweenValue(((AudioSource)target).pitch);
                case PropertyKind.MaterialFloat: return new TweenValue(((Material)target).GetFloat(propertyId));
                case PropertyKind.MaterialColor: return new TweenValue(((Material)target).GetColor(propertyId));
                case PropertyKind.SpriteColor: return new TweenValue(((SpriteRenderer)target).color);
                case PropertyKind.LightIntensity: return new TweenValue(((Light)target).intensity);
                case PropertyKind.LightColor: return new TweenValue(((Light)target).color);
                default: return ReadExtended(kind, target, propertyId);
            }
        }

        static TweenValue ReadExtended(PropertyKind kind, Object target, int propertyId) {
            switch (kind) {
                case PropertyKind.PositionX: return new TweenValue(((Transform)target).position.x);
                case PropertyKind.PositionY: return new TweenValue(((Transform)target).position.y);
                case PropertyKind.PositionZ: return new TweenValue(((Transform)target).position.z);
                case PropertyKind.LocalPositionX: return new TweenValue(((Transform)target).localPosition.x);
                case PropertyKind.LocalPositionY: return new TweenValue(((Transform)target).localPosition.y);
                case PropertyKind.LocalPositionZ: return new TweenValue(((Transform)target).localPosition.z);
                case PropertyKind.LocalScaleX: return new TweenValue(((Transform)target).localScale.x);
                case PropertyKind.LocalScaleY: return new TweenValue(((Transform)target).localScale.y);
                case PropertyKind.LocalScaleZ: return new TweenValue(((Transform)target).localScale.z);
                case PropertyKind.AnchoredPositionX: return new TweenValue(((RectTransform)target).anchoredPosition.x);
                case PropertyKind.AnchoredPositionY: return new TweenValue(((RectTransform)target).anchoredPosition.y);
                case PropertyKind.PropertyBlockFloat: return new TweenValue(PropertyBlocks.ReadFloat((Renderer)target, propertyId));
                case PropertyKind.PropertyBlockColor: return new TweenValue(PropertyBlocks.ReadColor((Renderer)target, propertyId));
                case PropertyKind.SpriteAlpha: return new TweenValue(((SpriteRenderer)target).color.a);
                default: return new TweenValue(0f);
            }
        }

        /// <summary>Writes <paramref name="value"/> to <paramref name="kind"/> on <paramref name="target"/>.</summary>
        public static void Write(PropertyKind kind, Object target, int propertyId, in TweenValue value) {
            Debug.Assert(kind != PropertyKind.None, "PropertyKind.None has no backing property.");
            Debug.Assert(target != null, "Cannot write a property to a destroyed target.");
            switch (kind) {
                case PropertyKind.Position: ((Transform)target).position = value.Vector3; break;
                case PropertyKind.LocalPosition: ((Transform)target).localPosition = value.Vector3; break;
                case PropertyKind.Rotation: ((Transform)target).rotation = value.Quaternion; break;
                case PropertyKind.LocalRotation: ((Transform)target).localRotation = value.Quaternion; break;
                case PropertyKind.EulerAngles: ((Transform)target).eulerAngles = value.Vector3; break;
                case PropertyKind.LocalEulerAngles: ((Transform)target).localEulerAngles = value.Vector3; break;
                case PropertyKind.LocalScale: ((Transform)target).localScale = value.Vector3; break;
                case PropertyKind.AnchoredPosition: ((RectTransform)target).anchoredPosition = value.Vector2; break;
                case PropertyKind.SizeDelta: ((RectTransform)target).sizeDelta = value.Vector2; break;
                case PropertyKind.CanvasGroupAlpha: ((CanvasGroup)target).alpha = value.Float; break;
                case PropertyKind.GraphicColor: ((Graphic)target).color = value.Color; break;
                case PropertyKind.GraphicAlpha: WriteGraphicAlpha((Graphic)target, value.Float); break;
                case PropertyKind.CameraFieldOfView: ((Camera)target).fieldOfView = value.Float; break;
                case PropertyKind.CameraOrthographicSize: ((Camera)target).orthographicSize = value.Float; break;
                case PropertyKind.CameraBackgroundColor: ((Camera)target).backgroundColor = value.Color; break;
                case PropertyKind.AudioVolume: ((AudioSource)target).volume = value.Float; break;
                case PropertyKind.AudioPitch: ((AudioSource)target).pitch = value.Float; break;
                case PropertyKind.MaterialFloat: ((Material)target).SetFloat(propertyId, value.Float); break;
                case PropertyKind.MaterialColor: ((Material)target).SetColor(propertyId, value.Color); break;
                case PropertyKind.SpriteColor: ((SpriteRenderer)target).color = value.Color; break;
                case PropertyKind.LightIntensity: ((Light)target).intensity = value.Float; break;
                case PropertyKind.LightColor: ((Light)target).color = value.Color; break;
                default: WriteExtended(kind, target, propertyId, value); break;
            }
        }

        static void WriteExtended(PropertyKind kind, Object target, int propertyId, in TweenValue value) {
            switch (kind) {
                case PropertyKind.PositionX:
                case PropertyKind.PositionY:
                case PropertyKind.PositionZ: WritePositionAxis((Transform)target, kind - PropertyKind.PositionX, value.Float); break;
                case PropertyKind.LocalPositionX:
                case PropertyKind.LocalPositionY:
                case PropertyKind.LocalPositionZ: WriteLocalPositionAxis((Transform)target, kind - PropertyKind.LocalPositionX, value.Float); break;
                case PropertyKind.LocalScaleX:
                case PropertyKind.LocalScaleY:
                case PropertyKind.LocalScaleZ: WriteLocalScaleAxis((Transform)target, kind - PropertyKind.LocalScaleX, value.Float); break;
                case PropertyKind.AnchoredPositionX:
                case PropertyKind.AnchoredPositionY: WriteAnchoredAxis((RectTransform)target, kind - PropertyKind.AnchoredPositionX, value.Float); break;
                case PropertyKind.PropertyBlockFloat: PropertyBlocks.WriteFloat((Renderer)target, propertyId, value.Float); break;
                case PropertyKind.PropertyBlockColor: PropertyBlocks.WriteColor((Renderer)target, propertyId, value.Color); break;
                case PropertyKind.SpriteAlpha: WriteSpriteAlpha((SpriteRenderer)target, value.Float); break;
                default: break;
            }
        }

        static void WritePositionAxis(Transform target, int axis, float value) {
            Vector3 v = target.position;
            v[axis] = value;
            target.position = v;
        }

        static void WriteLocalPositionAxis(Transform target, int axis, float value) {
            Vector3 v = target.localPosition;
            v[axis] = value;
            target.localPosition = v;
        }

        static void WriteLocalScaleAxis(Transform target, int axis, float value) {
            Vector3 v = target.localScale;
            v[axis] = value;
            target.localScale = v;
        }

        static void WriteGraphicAlpha(Graphic graphic, float alpha) {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        static void WriteSpriteAlpha(SpriteRenderer target, float alpha) {
            Color color = target.color;
            color.a = alpha;
            target.color = color;
        }

        static void WriteAnchoredAxis(RectTransform target, int axis, float value) {
            Vector2 v = target.anchoredPosition;
            v[axis] = value;
            target.anchoredPosition = v;
        }
    }

    /// <summary>
    /// Per-renderer shader values through one shared MaterialPropertyBlock: no material copy, no
    /// allocation. Values the block doesn't hold yet are read from the shared material.
    /// </summary>
    static class PropertyBlocks {
        static MaterialPropertyBlock _block;

        static MaterialPropertyBlock Block {
            get { return _block ?? (_block = new MaterialPropertyBlock()); }
        }

        public static float ReadFloat(Renderer renderer, int id) {
            renderer.GetPropertyBlock(Block);
            if (Block.HasFloat(id)) { return Block.GetFloat(id); }
            Material material = renderer.sharedMaterial;
            return material != null && material.HasProperty(id) ? material.GetFloat(id) : 0f;
        }

        public static Color ReadColor(Renderer renderer, int id) {
            renderer.GetPropertyBlock(Block);
            if (Block.HasColor(id)) { return Block.GetColor(id); }
            Material material = renderer.sharedMaterial;
            return material != null && material.HasProperty(id) ? material.GetColor(id) : Color.white;
        }

        public static void WriteFloat(Renderer renderer, int id, float value) {
            renderer.GetPropertyBlock(Block);
            Block.SetFloat(id, value);
            renderer.SetPropertyBlock(Block);
        }

        public static void WriteColor(Renderer renderer, int id, Color value) {
            renderer.GetPropertyBlock(Block);
            Block.SetColor(id, value);
            renderer.SetPropertyBlock(Block);
        }
    }
}
