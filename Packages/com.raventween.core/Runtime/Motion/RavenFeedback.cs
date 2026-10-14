using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    /// <summary>One step of a <see cref="RavenFeedback"/>: a shake, a flash, a sound, a hit stop…</summary>
    [Serializable]
    public sealed class FeedbackAction {
        public enum Kind : byte {
            /// <summary>Any ready-made motion on an object.</summary>
            Motion = 0,
            /// <summary>Shakes a camera.</summary>
            CameraShake = 1,
            /// <summary>A springy scale kick.</summary>
            Punch = 2,
            /// <summary>Flashes a color and fades back.</summary>
            ColorFlash = 3,
            /// <summary>Plays a sound.</summary>
            Sound = 4,
            /// <summary>Freezes the game for an instant, like a heavy hit.</summary>
            HitStop = 5,
            /// <summary>Plays a particle system.</summary>
            Particles = 6,
            /// <summary>Turns an object on or off.</summary>
            SetActive = 7,
            /// <summary>Calls anything through a UnityEvent.</summary>
            Event = 8
        }

        public Kind kind = Kind.Punch;
        [Tooltip("Seconds after the feedback starts. Counted in real time, so a hit stop does not delay it.")]
        [Min(0f)] public float delay;
        [Tooltip("Object to act on. Empty = the feedback's own object.")]
        public Transform target;
        public MotionPreset motion = MotionPreset.Pulse;
        public MotionSettings settings = MotionSettings.Default;
        [Tooltip("Camera to shake. Empty = the main camera.")]
        public Camera camera;
        [Tooltip("1 = a solid hit, 0.3 = light, 2 = huge.")]
        [Min(0f)] public float strength = 1f;
        [Tooltip("Seconds.")]
        [Min(0.01f)] public float duration = 0.3f;
        public Color color = MotionSettings.DefaultTint;
        public AudioClip clip;
        [Tooltip("Source to play through (its mixer, 3D settings…). Empty = a one-shot at the object's position.")]
        public AudioSource source;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch between these two values, so repeated sounds don't sound identical.")]
        [Range(0.5f, 2f)] public float pitchMin = 1f;
        [Range(0.5f, 2f)] public float pitchMax = 1f;
        [Tooltip("Time.timeScale during the hit stop (0.05 = almost frozen).")]
        [Range(0f, 1f)] public float timeScale = 0.05f;
        public Component particles;
        [Tooltip("Turn the object on (ticked) or off.")]
        public bool active = true;
        public UnityEvent onPlay = new UnityEvent();

        [NonSerialized] internal RavenFeedback Owner;
        [NonSerialized] MotionTarget _resolved;
        [NonSerialized] MotionRest _rest;
        [NonSerialized] bool _hasRest;
        [NonSerialized] Sequence _running;

        /// <summary>Does the action now. Each kind is a few lines on top of the engine.</summary>
        internal void Run() {
            Transform on = target != null ? target : (Owner != null ? Owner.transform : null);
            switch (kind) {
                case Kind.Motion: PlayMotion(on, motion, settings); break;
                case Kind.CameraShake: ShakeCamera(); break;
                case Kind.Punch: if (on != null) { Raven.PunchScale(on, Vector3.one * (0.25f * strength), duration); } break;
                case Kind.ColorFlash: FlashColor(on); break;
                case Kind.Sound: PlaySound(on); break;
                case Kind.HitStop: Raven.HitStop(timeScale, duration); break;
                case Kind.Particles: PlayParticles(); break;
                case Kind.SetActive: if (on != null) { on.gameObject.SetActive(active); } break;
                default: onPlay.Invoke(); break;
            }
        }

        /// <summary>Stops a motion this action started and puts its object back.</summary>
        internal void Stop() {
            if (!_running.IsAlive) { return; }
            _running.Stop();
            if (_hasRest && _resolved.IsValid) { _rest.Apply(_resolved, MotionChannels.All); }
        }

        // The rest pose is kept from the first run, so rapid repeats never drift.
        void PlayMotion(Transform on, MotionPreset preset, MotionSettings motionSettings) {
            if (on == null || preset == MotionPreset.None) { return; }
            if (_resolved.Transform != on) {
                _resolved = MotionTarget.Resolve(on);
                _hasRest = false;
            }
            if (_running.IsAlive) { _running.Stop(); }
            if (!_hasRest) { _rest = MotionRest.Capture(_resolved); _hasRest = true; }
            _running = Raven.Motion(_resolved, preset, motionSettings, _rest);
        }

        void FlashColor(Transform on) {
            MotionSettings flash = MotionSettings.Default;
            flash.color = color;
            flash.duration = duration;
            flash.intensity = Mathf.Clamp01(strength);
            PlayMotion(on, MotionPreset.Tint, flash);
        }

        void ShakeCamera() {
            Camera shaken = camera != null ? camera : Camera.main;
            if (shaken != null) { Raven.ShakeCamera(shaken, strength, duration); }
        }

        void PlaySound(Transform on) {
            if (clip == null) { return; }
            float pitch = UnityEngine.Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));
            if (source != null) {
                source.pitch = pitch;
                source.PlayOneShot(clip, volume);
                return;
            }
            Vector3 position = on != null ? on.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        void PlayParticles() {
#if RAVEN_MODULE_PARTICLES
            var system = particles as ParticleSystem;
            if (system != null) { system.Play(true); }
#endif
        }
    }

    /// <summary>
    /// "Juice" without code: a list of actions — camera shake, punch, color flash, sound, hit
    /// stop, particles, any motion — played together, each after its own delay. Start it from
    /// a click, a hover, a trigger zone, a collision, or any UnityEvent (Play).
    /// </summary>
    [AddComponentMenu("RavenTween/Raven Feedback")]
    [HelpURL("https://nekuzaky.com/docs/raventween/designer-workflow")]
    public sealed class RavenFeedback : MonoBehaviour, IMotionTriggerable {
        [SerializeField] MotionTrigger trigger = MotionTrigger.Manual;
        [Tooltip("Trigger and collision starts: only objects with this tag count. Empty = any object.")]
        [SerializeField] string filterTag = "";
        [Tooltip("Seconds before the same feedback can play again (0 = no limit). Stops a burst of hits from stacking.")]
        [SerializeField, Min(0f)] float cooldown;
        [SerializeField] List<FeedbackAction> actions = new List<FeedbackAction>();

        readonly List<Tween> _pending = new List<Tween>(8);
        float _lastPlay = float.NegativeInfinity;

        public List<FeedbackAction> Actions { get { return actions; } }
        public MotionTrigger Trigger { get { return trigger; } set { trigger = value; EnsureTriggerHooks(); } }
        /// <summary>Seconds before the feedback can play again (0 = no limit).</summary>
        public float Cooldown { get { return cooldown; } set { cooldown = Mathf.Max(value, 0f); } }

        void Awake() {
            EnsureTriggerHooks();
        }

        void OnEnable() {
            if (trigger == MotionTrigger.OnEnable) { Play(); }
        }

        void Start() {
            if (trigger == MotionTrigger.OnStart) { Play(); }
        }

        void OnDisable() {
            Stop();
        }

        /// <summary>Plays every action, each after its own delay.</summary>
        public void Play() {
            if (!isActiveAndEnabled) { return; }
            float now = Time.unscaledTime;
            if (cooldown > 0f && now - _lastPlay < cooldown) { return; }
            _lastPlay = now;
            PrunePending();
            for (int i = 0; i < actions.Count; i++) {
                FeedbackAction action = actions[i];
                if (action == null) { continue; }
                action.Owner = this;
                if (action.delay <= 0f) { action.Run(); continue; }
                _pending.Add(Raven.Delay(action.delay).UnscaledTime().OnComplete(action, a => a.Run()));
            }
        }

        /// <summary>Cancels delayed actions and puts animated objects back.</summary>
        public void Stop() {
            for (int i = 0; i < _pending.Count; i++) { _pending[i].Stop(); }
            _pending.Clear();
            for (int i = 0; i < actions.Count; i++) {
                if (actions[i] != null) { actions[i].Stop(); }
            }
        }

        public void HandleTrigger(MotionTrigger started) {
            if (started == trigger) { Play(); }
        }

        void PrunePending() {
            for (int i = _pending.Count - 1; i >= 0; i--) {
                if (!_pending[i].IsAlive) { _pending.RemoveAt(i); }
            }
        }

        void EnsureTriggerHooks() {
            if (MotionTriggers.IsPointer(trigger)) { MotionTriggers.EnsurePointerRelay(gameObject); }
        }

        void HandlePhysics(MotionTrigger started, GameObject other) {
            if (started == trigger && MotionTriggers.PassesTag(filterTag, other)) { Play(); }
        }

#if RAVEN_MODULE_PHYSICS
        void OnTriggerEnter(Collider other) { HandlePhysics(MotionTrigger.OnTriggerEnter, other.gameObject); }
        void OnTriggerExit(Collider other) { HandlePhysics(MotionTrigger.OnTriggerExit, other.gameObject); }
        void OnCollisionEnter(Collision collision) { HandlePhysics(MotionTrigger.OnCollisionEnter, collision.gameObject); }
#endif

#if RAVEN_MODULE_PHYSICS2D
        void OnTriggerEnter2D(Collider2D other) { HandlePhysics(MotionTrigger.OnTriggerEnter, other.gameObject); }
        void OnTriggerExit2D(Collider2D other) { HandlePhysics(MotionTrigger.OnTriggerExit, other.gameObject); }
        void OnCollisionEnter2D(Collision2D collision) { HandlePhysics(MotionTrigger.OnCollisionEnter, collision.gameObject); }
#endif
    }
}
