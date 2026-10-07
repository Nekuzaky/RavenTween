using RavenTween;
using UnityEngine;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// A head tracks an orbiting target with RavenLookAt, an antenna swings with RavenSpringChain,
    /// and RavenTween blends both in and out by tweening their weights.
    /// </summary>
    public sealed class ProceduralDemo : MonoBehaviour {
        RavenLookAt _lookAt;
        RavenSpringChain _antenna;
        Transform _body;

        void Start() {
            SetupCamera();
            _body = BuildCharacter(out Transform head, out Transform antennaRoot);
            Transform target = BuildTarget();

            _lookAt = head.gameObject.AddComponent<RavenLookAt>();
            _lookAt.Target = target;
            _lookAt.MaxAngle = 75f;

            _antenna = antennaRoot.gameObject.AddComponent<RavenSpringChain>();
            _antenna.Gravity = new Vector3(0f, -3f, 0f);
            _antenna.Stiffness = 0.06f;
            _antenna.Damping = 0.1f;

            // The body hops, which makes the antenna swing.
            Raven.LocalPosition(_body, new Vector3(0f, 1.5f, 0f), 0.35f).Ease(Ease.OutQuad).Infinite(CycleMode.Yoyo);
            // The target orbits the character.
            Raven.LocalEulerAngles(target.parent, new Vector3(0f, 360f, 0f), 6f).Infinite(CycleMode.Restart);
            // Every few seconds the head looks away, then back: a weight tween on the look-at.
            Raven.Sequence()
                .ChainDelay(2.5f)
                .Chain(_lookAt.TweenWeight(0f, 0.6f).Ease(Ease.InOutSine))
                .ChainDelay(1.5f)
                .Chain(_lookAt.TweenWeight(1f, 0.6f).Ease(Ease.InOutSine))
                .Infinite();
        }

        static void SetupCamera() {
            Camera camera = Camera.main;
            if (camera == null) {
                camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0f, 2f, -6f);
            camera.transform.LookAt(new Vector3(0f, 1.2f, 0f));
        }

        // Bones are unscaled empty transforms; meshes hang off them as children, so rotations
        // never shear. A spring chain follows each bone's FIRST child, so bones go first.
        Transform BuildCharacter(out Transform head, out Transform antennaRoot) {
            var root = new GameObject("Character").transform;
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            body.localPosition = new Vector3(0f, 1f, 0f);
            Primitive(PrimitiveType.Capsule, body, Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), "BodyMesh");

            head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 0.95f, 0f);
            Primitive(PrimitiveType.Cube, head, Vector3.zero, new Vector3(0.6f, 0.4f, 0.5f), "HeadMesh");
            Primitive(PrimitiveType.Cube, head, new Vector3(0f, 0.05f, 0.26f), new Vector3(0.5f, 0.12f, 0.05f), "Visor");

            antennaRoot = new GameObject("Antenna").transform;
            antennaRoot.SetParent(head, false);
            antennaRoot.localPosition = new Vector3(0f, 0.2f, 0f);
            antennaRoot.SetAsFirstSibling();
            Transform parent = antennaRoot;
            for (int i = 0; i < 6; i++) {
                var segment = new GameObject("Segment" + i).transform;
                segment.SetParent(parent, false);
                segment.localPosition = new Vector3(0f, 0.15f, 0f);
                segment.SetAsFirstSibling();
                Primitive(PrimitiveType.Sphere, segment, Vector3.zero, Vector3.one * 0.07f, "Bead");
                parent = segment;
            }
            return body;
        }

        static Transform BuildTarget() {
            var pivot = new GameObject("Orbit").transform;
            pivot.position = new Vector3(0f, 2f, 0f);
            Transform target = Primitive(PrimitiveType.Sphere, pivot, new Vector3(3f, 0f, 0f), Vector3.one * 0.3f, "Target");
            return target;
        }

        static Transform Primitive(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, string name) {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            return go.transform;
        }
    }
}
