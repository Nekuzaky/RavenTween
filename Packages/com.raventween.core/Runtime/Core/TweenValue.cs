using UnityEngine;

namespace RavenTween {
    /// <summary>Kind of value carried by a <see cref="TweenValue"/>.</summary>
    public enum ValueKind : byte {
        Float = 0,
        Vector2 = 1,
        Vector3 = 2,
        Vector4 = 3,
        Quaternion = 4,
        Color = 5
    }

    /// <summary>
    /// Compact tagged union for every interpolated type. Stored as a Vector4 so tween
    /// slots stay fixed-size and the engine never boxes values.
    /// </summary>
    public readonly struct TweenValue {
        public readonly Vector4 Raw;
        public readonly ValueKind Kind;

        public TweenValue(float value) { Raw = new Vector4(value, 0f, 0f, 0f); Kind = ValueKind.Float; }
        public TweenValue(Vector2 value) { Raw = new Vector4(value.x, value.y, 0f, 0f); Kind = ValueKind.Vector2; }
        public TweenValue(Vector3 value) { Raw = new Vector4(value.x, value.y, value.z, 0f); Kind = ValueKind.Vector3; }
        public TweenValue(Vector4 value) { Raw = value; Kind = ValueKind.Vector4; }
        public TweenValue(Quaternion value) { Raw = new Vector4(value.x, value.y, value.z, value.w); Kind = ValueKind.Quaternion; }
        public TweenValue(Color value) { Raw = new Vector4(value.r, value.g, value.b, value.a); Kind = ValueKind.Color; }

        public float Float { get { return Raw.x; } }
        public Vector2 Vector2 { get { return new Vector2(Raw.x, Raw.y); } }
        public Vector3 Vector3 { get { return new Vector3(Raw.x, Raw.y, Raw.z); } }
        public Vector4 Vector4 { get { return Raw; } }
        public Quaternion Quaternion { get { return new Quaternion(Raw.x, Raw.y, Raw.z, Raw.w); } }
        public Color Color { get { return new Color(Raw.x, Raw.y, Raw.z, Raw.w); } }

        /// <summary>Interpolates between two values of the same kind. Quaternions use slerp.</summary>
        public static TweenValue Lerp(in TweenValue from, in TweenValue to, float t) {
            Debug.Assert(from.Kind == to.Kind, "Cannot interpolate between different value kinds.");
            Debug.Assert(!float.IsNaN(t), "Interpolation factor must be a number.");
            if (from.Kind == ValueKind.Quaternion) {
                return new TweenValue(Quaternion.SlerpUnclamped(from.Quaternion, to.Quaternion, t));
            }
            Vector4 raw = Vector4.LerpUnclamped(from.Raw, to.Raw, t);
            return new TweenValue(raw, from.Kind);
        }

        TweenValue(Vector4 raw, ValueKind kind) { Raw = raw; Kind = kind; }
    }
}
