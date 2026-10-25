using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RavenTween {
    [Serializable]
    public sealed class FeedbackAction {
        public enum Kind : byte {
            Motion = 0,
            CameraShake = 1,
            Punch = 2,
            ColorFlash = 3,
            Sound = 4,
            HitStop = 5,
            Particles = 6,
            SetActive = 7,
            Event = 8,
            LightFlash = 9,
            FieldOfView = 10,
            Nudge = 11,
            Twist = 12,
            SlowMotion = 13,
            AnimatorTrigger = 14,
            Spawn = 15,
            Rumble = 16,
            VolumeFlash = 17,
            CameraImpulse = 18
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
        [Tooltip("Random pitch between these two values, so repeated sounds don't sound identical. Changes the Audio Source's pitch: use a source dedicated to this sound.")]
        [Range(0.5f, 2f)] public float pitchMin = 1f;
        [Range(0.5f, 2f)] public float pitchMax = 1f;
        [Tooltip("Time.timeScale during the hit stop (0.05 = almost frozen).")]
        [Range(0f, 1f)] public float timeScale = 0.05f;
        public Component particles;
        [Tooltip("Turn the object on (ticked) or off.")]
        public bool active = true;
        public UnityEvent onPlay = new UnityEvent();
        public Light light;
        [Tooltip("Direction of a nudge (local units) or axis of a twist (degrees per axis).")]
        public Vector3 direction = new Vector3(0f, 0.2f, 0f);
        [Tooltip("Animator trigger parameter to set.")]
        public string parameter = "";
        public GameObject prefab;
        [Tooltip("Seconds before the spawned object is destroyed (0 = keep it).")]
        [Min(0f)] public float lifetime = 2f;
        [Tooltip("Chance that this action plays each time (1 = always, 0.3 = three times in ten).")]
        [Range(0f, 1f)] public float chance = 1f;
        [Tooltip("Random change of the strength each time (0 = always the same, 0.3 = up to 30% more or less).")]
        [Range(0f, 1f)] public float variation;
        [Tooltip("Gamepad low-frequency motor (heavy rumble), 0 to 1.")]
        [Range(0f, 1f)] public float lowMotor = 0.6f;
        [Tooltip("Gamepad high-frequency motor (buzz), 0 to 1.")]
        [Range(0f, 1f)] public float highMotor = 0.3f;
        [Tooltip("A post-processing Volume (URP or HDRP) for Volume Flash, or a Cinemachine Impulse Source for Camera Impulse.")]
        public Component effect;

        [NonSerialized] internal RavenFeedback Owner;
        [NonSerialized] MotionTarget _resolved;
        [NonSerialized] MotionRest _rest;
        [NonSerialized] bool _hasRest;
        [NonSerialized] Sequence _running;
        [NonSerialized] MotionChannels _channels;
        [NonSerialized] float _restIntensity;
        [NonSerialized] float _restLens;
        [NonSerialized] UnityEngine.Object _lensOwner;
        [NonSerialized] UnityEngine.Object _lightOwner;
        [NonSerialized] Tween _lightFlash;
        [NonSerialized] Tween _lensKick;
        [NonSerialized] bool _warnedMissing;

        internal void RunIfOwnerActive() {
            if (Owner != null && Owner.isActiveAndEnabled) { Run(); }
        }

        internal void Run() {
            if (chance < 1f && UnityEngine.Random.value > chance) { return; }
            Transform on = target != null ? target : (Owner != null ? Owner.transform : null);
            float power = strength * (1f + UnityEngine.Random.Range(-variation, variation));
            switch (kind) {
                case Kind.Motion: PlayMotion(on, motion, settings); break;
                case Kind.CameraShake: ShakeCamera(power); break;
                case Kind.Punch: if (on != null) { Raven.PunchScale(on, Vector3.one * (0.25f * power), duration); } break;
                case Kind.ColorFlash: FlashColor(on, power); break;
                case Kind.Sound: PlaySound(on); break;
                case Kind.HitStop: Raven.HitStop(timeScale, duration); break;
                case Kind.Particles: PlayParticles(); break;
                case Kind.SetActive: if (on != null) { on.gameObject.SetActive(active); } break;
                case Kind.LightFlash: FlashLight(power); break;
                case Kind.FieldOfView: KickFieldOfView(power); break;
                case Kind.Nudge: if (on != null) { Raven.PunchPosition(on, direction * power, duration); } break;
                case Kind.Twist: if (on != null) { Raven.PunchRotation(on, direction * power, duration); } break;
                case Kind.SlowMotion: SlowDown(); break;
                case Kind.AnimatorTrigger: SetAnimatorTrigger(on); break;
                case Kind.Spawn: Spawn(on); break;
                case Kind.Rumble: RunHook(FeedbackHooks.Rumble, power, "the Input System package"); break;
                case Kind.VolumeFlash: RunHook(FeedbackHooks.VolumeFlash, power, "URP or HDRP"); break;
                case Kind.CameraImpulse: RunHook(FeedbackHooks.CameraImpulse, power, "the Cinemachine package"); break;
                default: onPlay.Invoke(); break;
            }
        }

        void RunHook(Action<FeedbackAction, float> hook, float power, string needs) {
            if (hook != null) { hook(this, power); return; }
            if (_warnedMissing) { return; }
            _warnedMissing = true;
            Debug.LogWarning("RavenTween: the " + kind + " feedback needs " + needs + " in the project.", Owner);
        }

        void FlashLight(float power) {
            if (light == null) { return; }
            if (!_lightFlash.IsAlive || _lightOwner != light) {
                _restIntensity = light.intensity;
                _lightOwner = light;
            }
            if (_lightFlash.IsAlive) { _lightFlash.Stop(); }
            float peak = _restIntensity + Mathf.Max(_restIntensity, 1f) * power;
            _lightFlash = Raven.Intensity(light, peak, duration * 0.5f).From(_restIntensity).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo);
        }

        void KickFieldOfView(float power) {
            Camera kicked = camera != null ? camera : Camera.main;
            if (kicked == null) { return; }
            bool ortho = kicked.orthographic;
            if (!_lensKick.IsAlive || _lensOwner != kicked) {
                _restLens = ortho ? kicked.orthographicSize : kicked.fieldOfView;
                _lensOwner = kicked;
            }
            if (_lensKick.IsAlive) { _lensKick.Stop(); }
            float peak = ortho ? _restLens * (1f + 0.15f * power) : _restLens + 10f * power;
            Tween kick = ortho ? Raven.OrthographicSize(kicked, peak, duration * 0.5f) : Raven.FieldOfView(kicked, peak, duration * 0.5f);
            _lensKick = kick.From(_restLens).Ease(Ease.OutQuad).Cycles(2, CycleMode.Yoyo);
        }

        void SlowDown() {
            Raven.SlowMotion(timeScale, duration);
        }

        void SetAnimatorTrigger(Transform on) {
#if RAVEN_MODULE_ANIMATION
            if (on == null || string.IsNullOrEmpty(parameter)) { return; }
            var animator = on.GetComponent<Animator>();
            if (animator != null) { animator.SetTrigger(parameter); }
#endif
        }

        void Spawn(Transform on) {
            if (prefab == null) { return; }
            Vector3 position = on != null ? on.position : Vector3.zero;
            Quaternion rotation = on != null ? on.rotation : Quaternion.identity;
            GameObject spawned = UnityEngine.Object.Instantiate(prefab, position, rotation);
            if (lifetime > 0f) { UnityEngine.Object.Destroy(spawned, lifetime); }
        }

        internal void Stop() {
            if (!_running.IsAlive) { return; }
            _running.Stop();
            if (_hasRest && _resolved.IsValid) { _rest.Apply(_resolved, _channels); }
        }

        void PlayMotion(Transform on, MotionPreset preset, MotionSettings motionSettings) {
            if (on == null || preset == MotionPreset.None) { return; }
            if (_resolved.Transform != on) {
                _resolved = MotionTarget.Resolve(on);
                _hasRest = false;
            }
            bool idle = !_running.IsAlive;
            if (!idle) { _running.Stop(); }
            if (!_hasRest) { _rest = MotionRest.Capture(_resolved); _hasRest = true; }
            else if (idle) {
                _rest.Position = _resolved.ReadPosition();
                _rest.Rotation = _resolved.Transform.localRotation;
            }
            _running = MotionBuilder.Build(_resolved, preset, motionSettings, _rest, out _channels);
        }

        void FlashColor(Transform on, float power) {
            MotionSettings flash = MotionSettings.Default;
            flash.color = color;
            flash.duration = duration;
            flash.intensity = Mathf.Clamp01(power);
            PlayMotion(on, MotionPreset.Tint, flash);
        }

        void ShakeCamera(float power) {
            Camera shaken = camera != null ? camera : Camera.main;
            if (shaken != null) { Raven.ShakeCamera(shaken, power, duration); }
        }

        void PlaySound(Transform on) {
            if (clip == null) { return; }
            if (source != null) {
                bool pitched = !Mathf.Approximately(pitchMin, 1f) || !Mathf.Approximately(pitchMax, 1f);
                if (pitched) { source.pitch = UnityEngine.Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax)); }
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

    public static class FeedbackHooks {
        public static Action<FeedbackAction, float> Rumble;
        public static Action<FeedbackAction, float> VolumeFlash;
        public static Action<FeedbackAction, float> CameraImpulse;
    }

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
        UnityEngine.UI.Button _button;
        float _lastPlay = float.NegativeInfinity;

        public List<FeedbackAction> Actions { get { return actions; } }
        public MotionTrigger Trigger { get { return trigger; } set { trigger = value; EnsureTriggerHooks(); } }
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
                if (action.delay > 0f) {
                    _pending.Add(Raven.Delay(action.delay).UnscaledTime().OnComplete(action, a => a.RunIfOwnerActive()));
                    continue;
                }
                action.Run();
                if (!isActiveAndEnabled) { break; }
            }
        }

        public void Stop() {
            for (int i = 0; i < _pending.Count; i++) { _pending[i].Stop(); }
            _pending.Clear();
            for (int i = 0; i < actions.Count; i++) {
                if (actions[i] != null) { actions[i].Stop(); }
            }
        }

        public void HandleTrigger(MotionTrigger started) {
            if (started != trigger) { return; }
            if (started == MotionTrigger.OnClick && _button != null) { return; }
            Play();
        }

        void PrunePending() {
            for (int i = _pending.Count - 1; i >= 0; i--) {
                if (!_pending[i].IsAlive) { _pending.RemoveAt(i); }
            }
        }

        void EnsureTriggerHooks() {
            _button = MotionTriggers.HookTrigger(this, trigger, _button, Play);
        }

        void OnDestroy() {
            if (_button != null) { _button.onClick.RemoveListener(Play); }
        }

        void OnValidate() {
            if (Application.isPlaying && isActiveAndEnabled) { EnsureTriggerHooks(); }
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
