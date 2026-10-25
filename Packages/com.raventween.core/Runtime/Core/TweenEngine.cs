using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RavenTween {
    public enum UpdatePhase : byte {
        Update = 0,
        LateUpdate = 1,
        FixedUpdate = 2,
        Manual = 3
    }

    static class TweenEngine {
        const int InitialCapacity = 64;
        const int MaxSequenceWrapsPerStep = 16;

        static TweenSlot[] _slots = new TweenSlot[InitialCapacity];
        static int _slotCount;
        static readonly Stack<int> FreeIndices = new Stack<int>(InitialCapacity);
        static readonly Dictionary<EffectKey, int> Effects = new Dictionary<EffectKey, int>(InitialCapacity);
        static bool _processing;
        static long _pass;

        public static float TimeScale = 1f;

        public static UpdatePhase Phase = UpdatePhase.Update;

        public static bool Diagnostics;

        public static int AliveCount { get { return _aliveCount; } }

        static int _aliveCount;

        public static int PoolSize { get { return _slotCount; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void DomainReset() {
            ReleaseEverySlot();
            Application.quitting -= ReleaseEverySlot;
            Application.quitting += ReleaseEverySlot;
            TimeScale = 1f;
            Phase = UpdatePhase.Update;
            Diagnostics = false;
            _processing = false;
            _sequenceDepth = 0;
            _pass = 0;
            _lastUpdateFrame = -1;
            _lastLateUpdateFrame = -1;
            _lastLoopCheckFrame = -1;
            _evaluatedRoot = null;
            _evaluatedRootRevision = 0;
            _hitStopRemaining = 0f;
            _hitStopRestore = 1f;
            Install();
        }

        static void ReleaseEverySlot() {
            _aliveCount = 0;
            FreeIndices.Clear();
            Effects.Clear();
            for (int i = _slotCount - 1; i >= 0; i--) {
                TweenSlot slot = _slots[i];
                if (slot.State != SlotState.Free) {
                    slot.ClearReferences();
                    if (slot.Items != null) { slot.Items.Clear(); }
                    slot.State = SlotState.Free;
                    slot.Version++;
                }
                FreeIndices.Push(i);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void VerifyInstalled() {
            if (ContainsEngine(PlayerLoop.GetCurrentPlayerLoop())) { return; }
            Install();
            Debug.LogWarning("RavenTween: the player loop was replaced without RavenTween at startup. RavenTween installed itself again.");
        }

        static float _hitStopRemaining;
        static float _hitStopRestore = 1f;
        static float _hitStopScale;

        public static bool HitStopActive { get { return _hitStopRemaining > 0f; } }

        public static float TimeScaleBeforeHitStop { get { return _hitStopRestore; } }

        public static void BeginHitStop(float timeScale, float duration) {
            Debug.Assert(timeScale >= 0f && duration >= 0f, "A hit stop needs a non-negative time scale and duration.");
            if (duration <= 0f) { return; }
            bool active = _hitStopRemaining > 0f;
            if (active && !Mathf.Approximately(UnityEngine.Time.timeScale, _hitStopScale)) { return; }
            if (!active && UnityEngine.Time.timeScale < Mathf.Max(timeScale, 0.0001f)) { return; }
            if (!active) { _hitStopRestore = UnityEngine.Time.timeScale; }
            _hitStopRemaining = Mathf.Max(_hitStopRemaining, duration);
            _hitStopScale = Mathf.Max(timeScale, 0.0001f);
            UnityEngine.Time.timeScale = _hitStopScale;
        }

        public static void EndHitStop() {
            if (_hitStopRemaining <= 0f) { return; }
            _hitStopRemaining = 0f;
            if (Mathf.Approximately(UnityEngine.Time.timeScale, _hitStopScale)) { UnityEngine.Time.timeScale = _hitStopRestore; }
        }

        static void TickHitStop(float unscaledDelta) {
            if (_hitStopRemaining <= 0f) { return; }
            _hitStopRemaining -= unscaledDelta;
            if (_hitStopRemaining <= 0f) {
                _hitStopRemaining = 1f;
                EndHitStop();
            }
        }

        static void Install() {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            if (ContainsEngine(root)) { return; }
            InjectSystem(ref root, typeof(FixedUpdate), OnFixedUpdate, typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate));
            InjectSystem(ref root, typeof(Update), OnUpdate, null);
            InjectSystem(ref root, typeof(PreLateUpdate), OnLateUpdate, null);
            PlayerLoop.SetPlayerLoop(root);
        }

        static bool ContainsEngine(PlayerLoopSystem root) {
            if (root.subSystemList == null) { return false; }
            for (int i = 0; i < root.subSystemList.Length; i++) {
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList;
                if (children == null) { continue; }
                for (int j = 0; j < children.Length; j++) {
                    if (children[j].type == typeof(TweenEngine)) { return true; }
                }
            }
            return false;
        }

        static void InjectSystem(ref PlayerLoopSystem root, Type phaseType, PlayerLoopSystem.UpdateFunction callback, Type afterType) {
            Debug.Assert(phaseType != null, "Player loop phase type is required.");
            Debug.Assert(callback != null, "Player loop callback is required.");
            for (int i = 0; i < root.subSystemList.Length; i++) {
                if (root.subSystemList[i].type != phaseType) { continue; }
                PlayerLoopSystem[] children = root.subSystemList[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                int at = InsertionIndex(children, afterType);
                var extended = new PlayerLoopSystem[children.Length + 1];
                Array.Copy(children, 0, extended, 0, at);
                extended[at] = new PlayerLoopSystem { type = typeof(TweenEngine), updateDelegate = callback };
                Array.Copy(children, at, extended, at + 1, children.Length - at);
                root.subSystemList[i].subSystemList = extended;
                return;
            }
        }

        static int InsertionIndex(PlayerLoopSystem[] children, Type afterType) {
            if (afterType == null) { return children.Length; }
            for (int i = 0; i < children.Length; i++) {
                if (children[i].type == afterType) { return i + 1; }
            }
            return children.Length;
        }

        static int _lastUpdateFrame = -1;
        static int _lastLateUpdateFrame = -1;
        static int _lastLoopCheckFrame = -1;

        static void EnsurePlayerLoop() {
            if (_lastUpdateFrame < 0) { return; }
            int frame = Time.frameCount;
            if (frame - _lastUpdateFrame <= 2 || frame == _lastLoopCheckFrame) { return; }
            _lastLoopCheckFrame = frame;
            if (!Application.isPlaying || ContainsEngine(PlayerLoop.GetCurrentPlayerLoop())) { return; }
            Install();
            Debug.LogWarning("RavenTween: the player loop was replaced without RavenTween, so tweens had stopped advancing. RavenTween installed itself again.");
        }

        static void OnFixedUpdate() {
            if (!Application.isPlaying) { return; }
            ProcessPhase(UpdatePhase.FixedUpdate, Sanitize(Time.fixedDeltaTime), Sanitize(Time.fixedUnscaledDeltaTime));
        }

        static void OnUpdate() {
            if (!Application.isPlaying || Time.frameCount == _lastUpdateFrame) { return; }
            _lastUpdateFrame = Time.frameCount;
            ProcessPhase(UpdatePhase.Update, Sanitize(Time.deltaTime), Sanitize(Time.unscaledDeltaTime));
        }

        static void OnLateUpdate() {
            if (!Application.isPlaying || Time.frameCount == _lastLateUpdateFrame) { return; }
            _lastLateUpdateFrame = Time.frameCount;
            ProcessPhase(UpdatePhase.LateUpdate, Sanitize(Time.deltaTime), Sanitize(Time.unscaledDeltaTime));
        }

        static float Sanitize(float delta) {
            return float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f ? 0f : delta;
        }

        static UpdatePhase EffectivePhase(TweenSlot slot) {
            return slot.HasPhase ? slot.Phase : Phase;
        }

        public static void Process(float scaledDelta, float unscaledDelta) {
            ProcessFiltered(-1, scaledDelta, unscaledDelta);
        }

        public static void ProcessPhase(UpdatePhase phase, float scaledDelta, float unscaledDelta) {
            ProcessFiltered((int)phase, scaledDelta, unscaledDelta);
        }

        static void ProcessFiltered(int phase, float scaledDelta, float unscaledDelta) {
            Debug.Assert(scaledDelta >= 0f, "Delta time cannot be negative.");
            Debug.Assert(unscaledDelta >= 0f, "Unscaled delta time cannot be negative.");
            if (_processing) { return; }
            scaledDelta = Sanitize(scaledDelta);
            unscaledDelta = Sanitize(unscaledDelta);
            _processing = true;
            if (phase < 0 || phase == (int)UpdatePhase.Update) { TickHitStop(unscaledDelta); }
            long pass = ++_pass;
            int count = _slotCount;
            try {
                int next = 0;
                while (next < count) {
                    try {
                        next = StepRange(next, count, phase, pass, scaledDelta, unscaledDelta);
                    } catch (Exception exception) {
                        StopFailedTween(exception);
                        next = _stepping + 1;
                    }
                }
            } finally {
                _processing = false;
                _sequenceDepth = 0;
            }
        }

        static int _stepping;
        static uint _steppingVersion;

        static int StepRange(int first, int count, int phase, long pass, float scaledDelta, float unscaledDelta) {
            TweenSlot[] slots = _slots;
            float scaled = Sanitize(scaledDelta * TimeScale);
            float unscaled = Sanitize(unscaledDelta * TimeScale);
            for (int i = first; i < count; i++) {
                TweenSlot slot = slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                if (phase >= 0 && (int)(slot.HasPhase ? slot.Phase : Phase) != phase) { continue; }
                if (slot.HasCancellation && slot.Cancellation.IsCancellationRequested) {
                    KillInternal(slot, i, true);
                    continue;
                }
                if (slot.State != SlotState.Running) { continue; }
                float delta = slot.UseUnscaledTime ? unscaled : scaled;
                if (slot.TimeScale != 1f) { delta = Sanitize(delta * slot.TimeScale); }
                _stepping = i;
                _steppingVersion = slot.Version;
                StepRoot(slot, i, delta);
            }
            return count;
        }

        static void StopFailedTween(Exception exception) {
            _sequenceDepth = 0;
            StopAfterException(_slots[_stepping], _stepping, _steppingVersion, exception);
        }

        static void LogTargetDestroyed(TweenSlot slot) {
            string what = slot.IsSequence ? "A sequence" : slot.Property != PropertyKind.None ? "A " + slot.Property + " tween" : "A tween";
            string why = slot.LinkIndex >= 0 ? "the tween it controls ended" : "its target was destroyed";
            Debug.Log("RavenTween: " + what + " stopped at " + slot.Elapsed.ToString("0.00") + " s because " + why + ".");
        }

        static bool IsLive(TweenSlot slot, uint version) {
            return slot.State != SlotState.Free && slot.Version == version;
        }

        static void StepRoot(TweenSlot slot, int index, float delta) {
            uint version = slot.Version;
            if ((slot.RequiresTarget || slot.LinkIndex >= 0) && TargetLost(slot)) {
                HandleTargetDestroyed(slot, index);
                return;
            }
            slot.Elapsed += delta;
            float local = slot.Elapsed - slot.StartDelay;
            if (local < 0f) { return; }
            if (!slot.StartFired && !Start(slot, Quiet.None)) { return; }
            if (slot.IsSequence) { StepSequence(slot, index, local, version); }
            else { StepTween(slot, index, local, version); }
        }

        static bool TargetLost(TweenSlot slot) {
            if (slot.RequiresTarget && slot.UnityTarget == null) { return true; }
            return slot.LinkIndex >= 0 && !TryGetSlot(slot.LinkIndex, slot.LinkVersion, out _);
        }

        enum Quiet : byte {
            None = 0,
            Callbacks = 1,
            Rewind = 2,
            Jump = 3
        }

        static bool Start(TweenSlot slot, Quiet quiet) {
            uint version = slot.Version;
            EnsureStarted(slot, quiet);
            if (!IsLive(slot, version)) { return false; }
            if (!slot.RequiresTarget || slot.UnityTarget != null) { return true; }
            HandleTargetDestroyed(slot, slot.Index);
            return false;
        }

        static void EnsureStarted(TweenSlot slot, Quiet quiet) {
            if (slot.StartFired) { return; }
            slot.StartFired = true;
            if (!slot.IsSequence) { CaptureFromIfNeeded(slot); }
            if (quiet < Quiet.Rewind) { InvokeSafe(slot.OnStart); }
        }

        static void CaptureFromIfNeeded(TweenSlot slot) {
            if (slot.FromCaptured) { return; }
            slot.FromCaptured = true;
            if (slot.HasExplicitFrom) { return; }
            if (slot.Property != PropertyKind.None && slot.UnityTarget != null) {
                slot.StartValue = PropertyAccessor.Read(slot.Property, slot.UnityTarget, slot.PropertyId);
            } else if (slot.CustomGetter != null) {
                slot.StartValue = slot.CustomGetter(slot.CustomTarget, slot.CustomGetterDelegate);
            } else if (slot.LinkIndex >= 0 && TryGetSlot(slot.LinkIndex, slot.LinkVersion, out TweenSlot linked)) {
                slot.StartValue = new TweenValue(linked.TimeScale);
            }
        }

        static void StepTween(TweenSlot slot, int index, float local, uint version) {
            float duration = slot.Duration;
            if (duration <= 0f) {
                FinishZeroLength(slot, index, version);
                return;
            }
            if (local < duration) {
                ApplyProgress(slot, local / duration, slot.CyclesDone);
                return;
            }
            double whole = Math.Floor(local / duration);
            int remaining = slot.Cycles < 0 ? int.MaxValue : slot.Cycles - slot.CyclesDone;
            if (whole >= remaining) {
                CompleteAfterFinalValue(slot, index, version, slot.Cycles - 1);
                return;
            }
            int n = (int)whole;
            AddCycles(slot, n);
            float consumed = n * duration;
            local -= consumed;
            slot.Elapsed -= consumed;
            ApplyProgress(slot, Mathf.Clamp01(local / duration), slot.CyclesDone);
        }

        static void AddCycles(TweenSlot slot, int n) {
            Debug.Assert(n >= 0, "Cycle increments are never negative.");
            long next = (long)slot.CyclesDone + n;
            slot.CyclesDone = next > 1000000000L ? (int)(next & 1L) : (int)next;
        }

        static void FinishZeroLength(TweenSlot slot, int index, uint version) {
            if (slot.Cycles < 0) {
                ApplyProgress(slot, 1f, 0);
                return;
            }
            CompleteAfterFinalValue(slot, index, version, Mathf.Max(slot.Cycles, 1) - 1);
        }

        static void CompleteAfterFinalValue(TweenSlot slot, int index, uint version, int lastCycle) {
            slot.CyclesDone = lastCycle + 1;
            slot.Elapsed = slot.StartDelay + Mathf.Max(slot.Duration, 0f);
            slot.Completing = true;
            ApplyProgress(slot, 1f, lastCycle);
            if (!IsLive(slot, version)) { return; }
            slot.Completing = false;
            CompleteAndRelease(slot, index);
        }

        static void ApplyProgress(TweenSlot slot, float cycleProgress, int cycleIndex) {
            if (TryWriteTransformVector(slot, cycleProgress, cycleIndex)) { return; }
            WriteValue(slot, ValueAt(slot, cycleProgress, cycleIndex));
        }

        static bool TryWriteTransformVector(TweenSlot slot, float cycleProgress, int cycleIndex) {
            PropertyKind property = slot.Property;
            if (property != PropertyKind.Position && property != PropertyKind.LocalPosition && property != PropertyKind.LocalScale) { return false; }
            if (slot.Effect != EffectKind.None || slot.Mode == CycleMode.Incremental || slot.LinkIndex >= 0) { return false; }
            if (slot.CustomInvoker != null || slot.OnUpdateFloat != null || slot.OnUpdateValue != null || slot.UpdateInvoker != null || slot.OnUpdate != null) { return false; }
            Transform transform = slot.UnityTarget as Transform;
            if (transform == null) { return false; }
            bool odd = (cycleIndex & 1) == 1;
            float eased = EvaluateEase(slot, slot.Mode == CycleMode.Yoyo && odd ? 1f - cycleProgress : cycleProgress);
            bool reverse = slot.Mode == CycleMode.PingPong && odd;
            Vector4 from = reverse ? slot.EndValue.Raw : slot.StartValue.Raw;
            Vector4 to = reverse ? slot.StartValue.Raw : slot.EndValue.Raw;
            var value = new Vector3(from.x + (to.x - from.x) * eased, from.y + (to.y - from.y) * eased, from.z + (to.z - from.z) * eased);
            if (property == PropertyKind.Position) { transform.position = value; }
            else if (property == PropertyKind.LocalPosition) { transform.localPosition = value; }
            else { transform.localScale = value; }
            return true;
        }

        static TweenValue ValueAt(TweenSlot slot, float cycleProgress, int cycleIndex) {
            bool odd = (cycleIndex & 1) == 1;
            float t = slot.Mode == CycleMode.Yoyo && odd ? 1f - cycleProgress : cycleProgress;
            float eased = EvaluateEase(slot, t);
            if (slot.Effect != EffectKind.None) { return EvaluateEffect(slot, eased); }
            if (slot.Mode == CycleMode.PingPong && odd) { return TweenValue.Lerp(slot.EndValue, slot.StartValue, eased); }
            TweenValue value = TweenValue.Lerp(slot.StartValue, slot.EndValue, eased);
            if (slot.Mode != CycleMode.Incremental) { return value; }
            return TweenValue.AddCycles(value, slot.StartValue, slot.EndValue, cycleIndex);
        }

        public static float InterpolationFactor(int index, uint version) {
            if (!TryGetSlot(index, version, out TweenSlot slot) || slot.IsSequence) { return 0f; }
            float duration = slot.Duration;
            float local = Mathf.Max(slot.Elapsed - slot.StartDelay, 0f);
            bool finished = slot.Cycles > 0 && slot.CyclesDone >= slot.Cycles;
            float progress = finished || duration <= 0f ? 1f : Mathf.Clamp01(local / duration);
            bool odd = ((finished ? slot.Cycles - 1 : slot.CyclesDone) & 1) == 1;
            if (slot.Mode == CycleMode.Yoyo && odd) { return EvaluateEase(slot, 1f - progress); }
            float eased = EvaluateEase(slot, progress);
            return slot.Mode == CycleMode.PingPong && odd ? 1f - eased : eased;
        }

        static TweenValue EvaluateEffect(TweenSlot slot, float t) {
            Debug.Assert(slot.Effect != EffectKind.None, "EvaluateEffect requires an effect slot.");
            Debug.Assert(slot.StartValue.Kind == ValueKind.Vector3, "Effects operate on Vector3 properties.");
            float clamped = Mathf.Clamp01(t);
            float phase = clamped * slot.EffectFrequency;
            Vector3 offset;
            float envelope;
            if (slot.Effect == EffectKind.Shake) {
                float seed = slot.EffectSeed;
                offset = new Vector3(
                    Mathf.PerlinNoise(seed, phase) - Mathf.PerlinNoise(seed, 0f),
                    Mathf.PerlinNoise(seed + 31.7f, phase) - Mathf.PerlinNoise(seed + 31.7f, 0f),
                    Mathf.PerlinNoise(seed + 63.1f, phase) - Mathf.PerlinNoise(seed + 63.1f, 0f)) * 2f;
                envelope = (1f - clamped) * Mathf.Min(1f, clamped * 20f);
            } else {
                float wave = Mathf.Sin(phase * 2f * Mathf.PI);
                offset = new Vector3(wave, wave, wave);
                envelope = 1f - clamped;
            }
            offset.Scale(slot.EffectStrength * envelope);
            return new TweenValue(slot.StartValue.Vector3 + offset);
        }

        static float EvaluateEase(TweenSlot slot, float t) {
            if (slot.Ease != Ease.Custom) { return EaseUtility.Evaluate(slot.Ease, t); }
            if (slot.Parametric > EasingKind.Curve) {
                float distance = slot.Parametric == EasingKind.BounceExact ? TweenValue.Distance(slot.StartValue, slot.EndValue) : 1f;
                return EaseUtility.EvaluateParametric(slot.Parametric, t, slot.EaseA, slot.EaseB, distance);
            }
            if (slot.CustomEase != null) { return slot.CustomEase(t); }
            if (slot.CustomCurve != null) { return slot.CustomCurve.Evaluate(t); }
            return t;
        }

        static void WriteValue(TweenSlot slot, in TweenValue value) {
            uint version = slot.Version;
            if (slot.Property != PropertyKind.None) {
                PropertyAccessor.Write(slot.Property, slot.UnityTarget, slot.PropertyId, value);
            }
            if (slot.LinkIndex >= 0) { WriteLinkedTimeScale(slot, value.Float); }
            if (slot.CustomInvoker != null) {
                slot.CustomInvoker(slot.CustomTarget, slot.CustomSetter, value);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.OnUpdateFloat != null) {
                InvokeSafe(slot.OnUpdateFloat, value.Float);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.OnUpdateValue != null) {
                InvokeSafe(slot.OnUpdateValue, value);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.UpdateInvoker != null) {
                InvokeTargetUpdate(slot);
                if (!IsLive(slot, version)) { return; }
            }
            if (slot.OnUpdate != null) { InvokeSafe(slot.OnUpdate); }
        }

        static void WriteLinkedTimeScale(TweenSlot slot, float scale) {
            if (TryGetSlot(slot.LinkIndex, slot.LinkVersion, out TweenSlot linked)) {
                linked.TimeScale = Mathf.Max(scale, 0f);
            }
        }

        static void InvokeTargetUpdate(TweenSlot slot) {
            if (IsDestroyedUnityObject(slot.UpdateTarget)) { return; }
            try { slot.UpdateInvoker(slot.UpdateTarget, slot.UpdateDelegate, new Tween(slot.Index, slot.Version)); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static bool IsDestroyedUnityObject(object target) {
            return target is UnityEngine.Object unityObject && unityObject == null;
        }

        static void StepSequence(TweenSlot slot, int index, float local, uint version) {
            float duration = Mathf.Max(slot.SequenceDuration, 0f);
            if (duration <= 0f) {
                FinishEmptySequence(slot, index, version);
                return;
            }
            local = SkipExcessCycles(slot, local, duration);
            for (int wraps = 0; wraps <= MaxSequenceWrapsPerStep + 1; wraps++) {
                if (local < duration) {
                    ApplySequenceTime(slot, local, slot.CyclesDone, Quiet.None);
                    return;
                }
                int closing = slot.CyclesDone;
                int revision = slot.Revision;
                ApplySequenceTime(slot, duration, closing, Quiet.None);
                if (!IsLive(slot, version) || slot.Revision != revision) { return; }
                AddCycles(slot, 1);
                if (slot.Cycles >= 0 && slot.CyclesDone >= slot.Cycles) {
                    CompleteAndRelease(slot, index);
                    return;
                }
                local -= duration;
                slot.Elapsed -= duration;
                ResetSequenceChildren(slot);
            }
        }

        static float SkipExcessCycles(TweenSlot slot, float local, float duration) {
            double whole = Math.Floor(local / duration);
            if (whole <= MaxSequenceWrapsPerStep) { return local; }
            double skip = whole - MaxSequenceWrapsPerStep;
            if (slot.Cycles >= 0) { skip = Math.Min(skip, Math.Max(slot.Cycles - slot.CyclesDone - 1, 0)); }
            int n = (int)Math.Min(skip, 100000000d);
            AddCycles(slot, n);
            float consumed = n * duration;
            slot.Elapsed -= consumed;
            return local - consumed;
        }

        static void FinishEmptySequence(TweenSlot slot, int index, uint version) {
            int revision = slot.Revision;
            ApplySequenceTime(slot, 0f, 0, Quiet.None);
            if (slot.Cycles < 0 || !IsLive(slot, version) || slot.Revision != revision) { return; }
            slot.CyclesDone = Mathf.Max(slot.Cycles, 1);
            CompleteAndRelease(slot, index);
        }

        static TweenSlot[] _resetWork = new TweenSlot[32];
        static TweenSlot[] _walkWork = new TweenSlot[32];

        static void ResetSequenceChildren(TweenSlot slot) {
            Debug.Assert(slot.IsSequence, "Only sequences carry child items.");
            ResetForReplay(slot, false);
        }

        static void ResetForReplay(TweenSlot root, bool recapture) {
            Debug.Assert(root != null, "Reset needs a slot.");
            Debug.Assert(_resetWork.Length > 0, "Reset work stack must be allocated.");
            if (recapture) { ClearRunState(root, true); }
            int top = 0;
            _resetWork[top++] = root;
            while (top > 0) {
                TweenSlot current = _resetWork[--top];
                _resetWork[top] = null;
                if (current.Items == null) { continue; }
                for (int i = 0; i < current.Items.Count; i++) {
                    SequenceItem item = current.Items[i];
                    if (!TryGetSlot(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                    ClearRunState(child, recapture);
                    if (!child.IsSequence) { continue; }
                    if (top == _resetWork.Length) { Array.Resize(ref _resetWork, _resetWork.Length * 2); }
                    _resetWork[top++] = child;
                }
            }
        }

        static void ClearRunState(TweenSlot slot, bool recapture) {
            slot.StartFired = false;
            slot.CompleteNotified = false;
            slot.Rewound = false;
            slot.CyclesDone = 0;
            slot.CallbackCycle = -1;
            slot.CallbackCursor = -1f;
            if (recapture && !slot.HasExplicitFrom) { slot.FromCaptured = false; }
        }

        public static bool IsInSubtree(TweenSlot root, TweenSlot target) {
            Debug.Assert(root != null && target != null, "Subtree check needs two slots.");
            int top = 0;
            _walkWork[top++] = root;
            bool found = false;
            while (top > 0) {
                TweenSlot current = _walkWork[--top];
                _walkWork[top] = null;
                if (current == target) { found = true; continue; }
                if (current.Items == null || found) { continue; }
                for (int i = 0; i < current.Items.Count; i++) {
                    SequenceItem item = current.Items[i];
                    if (!TryGetSlot(item.ChildIndex, item.ChildVersion, out TweenSlot child)) { continue; }
                    if (top == _walkWork.Length) { Array.Resize(ref _walkWork, _walkWork.Length * 2); }
                    _walkWork[top++] = child;
                }
            }
            return found;
        }

        static void ApplySequenceTime(TweenSlot slot, float time, int cycleIndex, Quiet quiet) {
            Debug.Assert(slot.IsSequence, "ApplySequenceTime requires a sequence slot.");
            Debug.Assert(slot.Items != null, "Sequence slot is missing its item list.");
            bool isRoot = !slot.OwnedBySequence;
            TweenSlot outerRoot = _evaluatedRoot;
            int outerRevision = _evaluatedRootRevision;
            if (isRoot) {
                _evaluatedRoot = slot;
                _evaluatedRootRevision = slot.Revision;
            }
            try {
                EvaluateTimeline(slot, time, cycleIndex, quiet);
            } finally {
                if (isRoot) {
                    _evaluatedRoot = outerRoot;
                    _evaluatedRootRevision = outerRevision;
                }
            }
        }

        static TweenSlot _evaluatedRoot;
        static int _evaluatedRootRevision;

        static void EvaluateTimeline(TweenSlot slot, float time, int cycleIndex, Quiet quiet) {
            uint version = slot.Version;
            int revision = slot.Revision;
            bool backwards = slot.Mode == CycleMode.Yoyo && (cycleIndex & 1) == 1;
            float t = backwards ? slot.SequenceDuration - time : time;
            Quiet childQuiet = quiet == Quiet.None && backwards ? Quiet.Callbacks : quiet;
            if (!RewindPass(slot, version, revision, t)) { return; }
            if (!ActivePass(slot, version, revision, t, childQuiet)) { return; }
            if (slot.HasCallbacks) { FireCallbacks(slot, version, revision, time, cycleIndex, backwards, quiet); }
        }

        static bool StillCurrent(TweenSlot slot, uint version, int revision) {
            return IsLive(slot, version) && slot.Revision == revision && RootUnchanged();
        }

        static bool RewindPass(TweenSlot slot, uint version, int revision, float t) {
            List<SequenceItem> items = slot.Items;
            for (int i = items.Count - 1; i >= 0; i--) {
                SequenceItem item = items[i];
                if (t >= item.ActiveStart) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child) && !child.IsCallback) { RewindOnce(child); }
                if (!StillCurrent(slot, version, revision)) { return false; }
            }
            return true;
        }

        static bool ActivePass(TweenSlot slot, uint version, int revision, float t, Quiet quiet) {
            List<SequenceItem> items = slot.Items;
            for (int i = 0; i < items.Count; i++) {
                SequenceItem item = items[i];
                if (t < item.ActiveStart) { continue; }
                if (TryGetLiveChild(item, out TweenSlot child) && !child.IsCallback) {
                    child.Rewound = false;
                    EvaluateChildAtTime(child, Mathf.Min(t - item.StartTime, item.Duration), quiet);
                }
                if (!StillCurrent(slot, version, revision)) { return false; }
            }
            return true;
        }

        static void FireCallbacks(TweenSlot slot, uint version, int revision, float time, int cycleIndex, bool backwards, Quiet quiet) {
            if (cycleIndex != slot.CallbackCycle) {
                slot.CallbackCycle = cycleIndex;
                slot.CallbackCursor = -1f;
            }
            float from = slot.CallbackCursor;
            bool landing = quiet == Quiet.Jump && !slot.OwnedBySequence;
            slot.CallbackCursor = Mathf.Max(landing ? time - JumpMargin : time, from);
            if (quiet != Quiet.None || time <= from) { return; }
            bool skipTurningPoint = slot.Mode == CycleMode.Yoyo && cycleIndex > 0;
            List<SequenceItem> items = slot.Items;
            for (int i = 0; i < items.Count; i++) {
                int at = backwards ? items.Count - 1 - i : i;
                SequenceItem item = items[at];
                float spot = backwards ? slot.SequenceDuration - item.StartTime : item.StartTime;
                if (spot <= from || spot > time || (skipTurningPoint && spot <= 0f)) { continue; }
                if (TryGetSlot(item.ChildIndex, item.ChildVersion, out TweenSlot child) && child.IsCallback) {
                    InvokeCompleteCallbacks(child.OnComplete, child.CompleteTarget, child.CompleteDelegate, child.CompleteInvoker);
                }
                if (!StillCurrent(slot, version, revision)) { return; }
            }
        }

        static bool TryGetLiveChild(SequenceItem item, out TweenSlot child) {
            if (!TryGetSlot(item.ChildIndex, item.ChildVersion, out child)) { return false; }
            if (!child.RequiresTarget || child.UnityTarget != null) { return true; }
            HandleTargetDestroyed(child, item.ChildIndex);
            child = null;
            return false;
        }

        static void RewindOnce(TweenSlot child) {
            if (!child.StartFired || child.Rewound) { return; }
            child.Rewound = true;
            child.CompleteNotified = false;
            EvaluateChildAtTime(child, child.StartDelay, Quiet.Rewind);
            if (child.IsSequence) { child.CallbackCycle = -1; }
        }

        const int MaxSequenceDepth = 8;
        const float JumpMargin = 1e-5f;
        static int _sequenceDepth;

        static void EvaluateChildAtTime(TweenSlot child, float childLocal, Quiet quiet) {
            float local = childLocal - child.StartDelay;
            if (local < 0f) { return; }
            uint version = child.Version;
            if (quiet == Quiet.Jump && local <= 0f && !child.StartFired) {
                ShowUnstarted(child, quiet);
                return;
            }
            if (!child.StartFired && !Start(child, quiet)) { return; }
            if (child.IsSequence) { EvaluateSequenceChild(child, local, quiet); return; }
            float cycleLength = Mathf.Max(child.Duration, 0f);
            int totalCycles = Mathf.Max(child.Cycles, 1);
            bool atEnd = cycleLength <= 0f || local >= cycleLength * totalCycles;
            int cycleIndex = atEnd ? totalCycles - 1 : Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            float inCycle = atEnd ? cycleLength : local - cycleIndex * cycleLength;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            child.Elapsed = child.StartDelay + inCycle;
            ApplyProgress(child, atEnd ? 1f : Mathf.Clamp01(inCycle / cycleLength), cycleIndex);
            if (atEnd && IsLive(child, version) && RootUnchanged()) { NotifyChildComplete(child, quiet); }
        }

        static void ShowUnstarted(TweenSlot child, Quiet quiet) {
            if (child.IsSequence) {
                ShowUnstartedSequence(child, quiet);
                return;
            }
            CaptureFromIfNeeded(child);
            if (child.Duration > 0f) { ApplyProgress(child, 0f, 0); }
        }

        static void ShowUnstartedSequence(TweenSlot child, Quiet quiet) {
            if (_sequenceDepth >= MaxSequenceDepth) { return; }
            _sequenceDepth++;
            try {
                ApplySequenceTime(child, 0f, 0, quiet);
            } finally {
                _sequenceDepth--;
            }
            child.CallbackCycle = -1;
            child.CallbackCursor = -1f;
        }

        static bool RootUnchanged() {
            return _evaluatedRoot == null || _evaluatedRoot.Revision == _evaluatedRootRevision;
        }

        static void EvaluateSequenceChild(TweenSlot child, float local, Quiet quiet) {
            Debug.Assert(child.IsSequence, "EvaluateSequenceChild requires a sequence slot.");
            if (_sequenceDepth >= MaxSequenceDepth) {
                Debug.LogError("RavenTween: sequence nesting exceeds " + MaxSequenceDepth + " levels; deeper levels are skipped.");
                return;
            }
            uint version = child.Version;
            bool atEnd;
            _sequenceDepth++;
            try {
                atEnd = EvaluateNestedTimeline(child, local, quiet);
            } finally {
                _sequenceDepth--;
            }
            if (atEnd && IsLive(child, version) && RootUnchanged()) { NotifyChildComplete(child, quiet); }
        }

        static bool EvaluateNestedTimeline(TweenSlot child, float local, Quiet quiet) {
            float cycleLength = Mathf.Max(child.SequenceDuration, 0f);
            int totalCycles = Mathf.Max(child.Cycles, 1);
            if (cycleLength <= 0f) {
                child.CyclesDone = totalCycles;
                child.Elapsed = child.StartDelay;
                ApplySequenceTime(child, 0f, 0, quiet);
                return true;
            }
            bool atEnd = local >= cycleLength * totalCycles;
            int cycleIndex = atEnd ? totalCycles - 1 : Mathf.Min((int)(local / cycleLength), totalCycles - 1);
            if (!CloseEarlierCycle(child, cycleIndex, cycleLength, quiet)) { return false; }
            float inCycle = atEnd ? cycleLength : local - cycleIndex * cycleLength;
            child.CyclesDone = atEnd ? totalCycles : cycleIndex;
            child.Elapsed = child.StartDelay + inCycle;
            ApplySequenceTime(child, inCycle, cycleIndex, quiet);
            return atEnd;
        }

        static bool CloseEarlierCycle(TweenSlot child, int cycleIndex, float cycleLength, Quiet quiet) {
            if (quiet == Quiet.Rewind || quiet == Quiet.Callbacks) { return true; }
            int shown = Mathf.Min(child.CyclesDone, Mathf.Max(child.Cycles, 1) - 1);
            uint version = child.Version;
            int last = Mathf.Min(cycleIndex, shown + MaxSequenceWrapsPerStep);
            for (int cycle = shown; cycle < last; cycle++) {
                ApplySequenceTime(child, cycleLength, cycle, quiet);
                if (!IsLive(child, version)) { return false; }
                ResetSequenceChildren(child);
            }
            return true;
        }

        static void NotifyChildComplete(TweenSlot child, Quiet quiet) {
            if (quiet == Quiet.Rewind || child.CompleteNotified) { return; }
            child.CompleteNotified = true;
            if (quiet == Quiet.Jump) { return; }
            InvokeCompleteCallbacks(child.OnComplete, child.CompleteTarget, child.CompleteDelegate, child.CompleteInvoker);
        }

        static void InvokeCompleteCallbacks(Action callback, object target, Delegate targetCallback, Action<object, Delegate> invoker) {
            InvokeSafe(callback);
            if (invoker == null || IsDestroyedUnityObject(target)) { return; }
            try { invoker(target, targetCallback); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void RecalculateSequenceDuration(TweenSlot slot) {
            Debug.Assert(slot.IsSequence, "Only sequences have a computed duration.");
            float max = 0f;
            for (int i = 0; i < slot.Items.Count; i++) {
                float end = slot.Items[i].StartTime + slot.Items[i].Duration;
                if (end > max) { max = end; }
            }
            slot.SequenceDuration = max;
        }

        public static int Rent(out TweenSlot slot) {
            int index;
            if (FreeIndices.Count > 0) {
                index = FreeIndices.Pop();
                slot = _slots[index];
            } else {
                slot = new TweenSlot();
                index = AddSlot(slot);
            }
            slot.Reset();
            slot.Index = index;
            slot.State = SlotState.Running;
            _aliveCount++;
            EnsurePlayerLoop();
            slot.BornPass = _pass;
            return index;
        }

        public static int RentCallback(out TweenSlot slot) {
            int index = Rent(out slot);
            slot.IsCallback = true;
            _aliveCount--;
            return index;
        }

        public static void EnsureCapacity(int capacity) {
            Debug.Assert(capacity >= 0, "Capacity cannot be negative.");
            const int Limit = 1 << 20;
            int target = Mathf.Min(capacity, Limit);
            if (_slots.Length < target) { Array.Resize(ref _slots, target); }
            while (_slotCount < target) {
                var slot = new TweenSlot();
                slot.Index = AddSlot(slot);
                FreeIndices.Push(slot.Index);
            }
        }

        static int AddSlot(TweenSlot slot) {
            if (_slotCount == _slots.Length) { Array.Resize(ref _slots, _slots.Length * 2); }
            _slots[_slotCount] = slot;
            return _slotCount++;
        }

        public static bool TryGetSlot(int index, uint version, out TweenSlot slot) {
            if ((uint)index >= (uint)_slotCount) { slot = null; return false; }
            TweenSlot candidate = _slots[index];
            bool alive = candidate.State != SlotState.Free && candidate.Version == version;
            slot = alive ? candidate : null;
            return alive;
        }

        static void HandleTargetDestroyed(TweenSlot slot, int index) {
            if (Diagnostics) { LogTargetDestroyed(slot); }
            Action callback = slot.OnTargetDestroyed;
            Action continuations = ReleaseCollect(index);
            InvokeSafe(callback);
            InvokeEach(continuations);
        }

        static void CompleteAndRelease(TweenSlot slot, int index) {
            Action callback = slot.OnComplete;
            object target = slot.CompleteTarget;
            Delegate targetCallback = slot.CompleteDelegate;
            Action<object, Delegate> invoker = slot.CompleteInvoker;
            Action continuations = ReleaseCollect(index);
            InvokeCompleteCallbacks(callback, target, targetCallback, invoker);
            InvokeEach(continuations);
        }

        static void KillInternal(TweenSlot slot, int index, bool fireOnKill) {
            Action callback = fireOnKill ? slot.OnKill : null;
            Action continuations = ReleaseCollect(index);
            InvokeSafe(callback);
            InvokeEach(continuations);
        }

        public static void Kill(int index, uint version, bool complete) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; stop or complete the sequence instead.");
                return;
            }
            if (complete) { CompleteGuarded(slot, index); }
            else { KillInternal(slot, index, true); }
        }

        static void CompleteGuarded(TweenSlot slot, int index) {
            uint version = slot.Version;
            try {
                ForceComplete(slot, index);
            } catch (Exception exception) {
                StopAfterException(slot, index, version, exception);
            }
        }

        static void ForceComplete(TweenSlot slot, int index) {
            if (slot.Completing) { return; }
            uint version = slot.Version;
            bool targetAlive = !slot.RequiresTarget || slot.UnityTarget != null;
            if (targetAlive) {
                slot.Completing = true;
                try {
                    if (!ApplyFinalState(slot, version)) { return; }
                } finally {
                    if (IsLive(slot, version)) { slot.Completing = false; }
                }
            }
            CompleteAndRelease(slot, index);
        }

        static bool ApplyFinalState(TweenSlot slot, uint version) {
            if (!slot.StartFired && !Start(slot, Quiet.None)) { return false; }
            int lastCycle = slot.Cycles > 0 ? slot.Cycles - 1 : Mathf.Max(slot.CyclesDone, 0);
            slot.CyclesDone = lastCycle + 1;
            if (slot.IsSequence) { ApplySequenceTime(slot, slot.SequenceDuration, lastCycle, Quiet.None); }
            else { ApplyProgress(slot, 1f, lastCycle); }
            return IsLive(slot, version);
        }

        static readonly Stack<int> ReleaseWork = new Stack<int>(16);

        static Action ReleaseCollect(int index) {
            Debug.Assert(_slots[index].State != SlotState.Free, "Slot is already free.");
            Debug.Assert(ReleaseWork.Count == 0, "Release work stack must start empty.");
            Action continuations = null;
            ReleaseWork.Push(index);
            while (ReleaseWork.Count > 0) {
                int current = ReleaseWork.Pop();
                TweenSlot currentSlot = _slots[current];
                if (currentSlot.State == SlotState.Free) { continue; }
                if (currentSlot.AwaitContinuations != null) { continuations += currentSlot.AwaitContinuations; }
                if (currentSlot.IsSequence && currentSlot.Items != null) {
                    for (int i = 0; i < currentSlot.Items.Count; i++) {
                        SequenceItem item = currentSlot.Items[i];
                        if (TryGetSlot(item.ChildIndex, item.ChildVersion, out _)) { ReleaseWork.Push(item.ChildIndex); }
                    }
                    currentSlot.Items.Clear();
                }
                if (currentSlot.Effect != EffectKind.None) { UnregisterEffect(currentSlot, current); }
                if (!currentSlot.IsCallback) { _aliveCount--; }
                currentSlot.ClearReferences();
                currentSlot.State = SlotState.Free;
                currentSlot.Version++;
                FreeIndices.Push(current);
            }
            return continuations;
        }

        public static void SetPaused(int index, uint version, bool paused) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; pause the sequence instead.");
                return;
            }
            slot.State = paused ? SlotState.Paused : SlotState.Running;
        }

        public static void KillAll(bool complete) {
            long pass = ++_pass;
            int count = _slotCount;
            for (int i = 0; i < count; i++) {
                TweenSlot slot = _slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                Kill(i, slot.Version, complete);
            }
        }

        public enum TargetAction : byte { Count, Stop, Complete, Pause, Resume }

        public static int ForEachOnTarget(object target, TargetAction action) {
            long pass = ++_pass;
            int count = _slotCount;
            int matched = 0;
            for (int i = 0; i < count; i++) {
                TweenSlot slot = _slots[i];
                if (slot.State == SlotState.Free || slot.OwnedBySequence || slot.BornPass >= pass) { continue; }
                if (target != null && !Targets(slot, target)) { continue; }
                matched++;
                ApplyTargetAction(slot, i, action);
            }
            return matched;
        }

        static bool Targets(TweenSlot slot, object target) {
            if (ReferenceEquals(slot.CustomTarget, target)) { return true; }
            if (slot.UnityTarget is null) { return false; }
            if (ReferenceEquals(slot.UnityTarget, target)) { return true; }
            var go = target as GameObject;
            return !ReferenceEquals(go, null) && slot.UnityTarget is Component component && component != null &&
                   ReferenceEquals(component.gameObject, go);
        }

        static void ApplyTargetAction(TweenSlot slot, int index, TargetAction action) {
            switch (action) {
                case TargetAction.Stop: KillInternal(slot, index, true); break;
                case TargetAction.Complete: CompleteGuarded(slot, index); break;
                case TargetAction.Pause: slot.State = SlotState.Paused; break;
                case TargetAction.Resume: slot.State = SlotState.Running; break;
                default: break;
            }
        }

        public static float ElapsedTotal(int index, uint version) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return 0f; }
            bool finished = slot.Cycles > 0 && slot.CyclesDone >= slot.Cycles;
            int before = finished ? slot.Cycles - 1 : slot.CyclesDone;
            return slot.Elapsed + (float)before * Mathf.Max(slot.CycleDuration, 0f);
        }

        public static void SetElapsedTotal(int index, uint version, float total) {
            SetElapsedTotal(index, version, total, false);
        }

        public static void SetElapsedTotal(int index, uint version, float total, bool endOfCycle) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogWarning("RavenTween: this tween belongs to a sequence; move the sequence's time instead.");
                return;
            }
            if (slot.Moving || slot.Completing || float.IsNaN(total) || float.IsInfinity(total)) { return; }
            if (TargetLost(slot)) {
                HandleTargetDestroyed(slot, index);
                return;
            }
            slot.Revision++;
            slot.Moving = true;
            try {
                MoveTo(slot, index, version, Mathf.Max(total, 0f), endOfCycle);
            } catch (Exception exception) {
                StopAfterException(slot, index, version, exception);
            } finally {
                if (IsLive(slot, version)) { slot.Moving = false; }
            }
        }

        static void MoveTo(TweenSlot slot, int index, uint version, float total, bool endOfCycle) {
            float cycle = Mathf.Max(slot.CycleDuration, 0f);
            double local = total - slot.StartDelay;
            if (slot.Cycles >= 0 && local >= (double)cycle * Mathf.Max(slot.Cycles, 1)) {
                slot.Moving = false;
                ForceComplete(slot, index);
                return;
            }
            if (local < 0d) { MoveIntoDelay(slot, version, total); return; }
            int cycleIndex = cycle > 0f ? (int)Math.Min(Math.Floor(local / cycle), int.MaxValue - 1) : 0;
            if (endOfCycle && cycleIndex > 0 && local - (double)cycleIndex * cycle < cycle * 1e-6) { cycleIndex--; }
            float inCycle = Mathf.Min((float)(local - (double)cycleIndex * cycle), cycle);
            if (slot.IsSequence) { MoveSequence(slot, version, cycleIndex, inCycle); }
            else { MoveTween(slot, cycleIndex, inCycle); }
        }

        static void StopAfterException(TweenSlot slot, int index, uint version, Exception exception) {
            Debug.LogException(exception);
            if (!IsLive(slot, version)) { return; }
            Debug.LogError("RavenTween: a tween threw an exception and was stopped.");
            KillInternal(slot, index, false);
        }

        static void MoveIntoDelay(TweenSlot slot, uint version, float total) {
            if (slot.StartFired) { ShowStart(slot, version); }
            if (!IsLive(slot, version)) { return; }
            slot.CyclesDone = 0;
            slot.Elapsed = total;
        }

        static void ShowStart(TweenSlot slot, uint version) {
            if (slot.IsSequence) { RewindSequence(slot, version); return; }
            ApplyProgress(slot, 0f, 0);
        }

        static void MoveTween(TweenSlot slot, int cycleIndex, float inCycle) {
            if (!slot.StartFired && !Start(slot, Quiet.None)) { return; }
            slot.CyclesDone = cycleIndex;
            slot.Elapsed = slot.StartDelay + inCycle;
            float duration = Mathf.Max(slot.Duration, 1e-6f);
            ApplyProgress(slot, Mathf.Clamp01(inCycle / duration), cycleIndex);
        }

        static void MoveSequence(TweenSlot slot, uint version, int cycleIndex, float inCycle) {
            if (!slot.StartFired && !Start(slot, Quiet.None)) { return; }
            RewindSequence(slot, version);
            if (!IsLive(slot, version)) { return; }
            slot.CyclesDone = cycleIndex;
            slot.Elapsed = slot.StartDelay + inCycle;
            ApplySequenceTime(slot, inCycle, cycleIndex, Quiet.Jump);
        }

        static void RewindSequence(TweenSlot slot, uint version) {
            ApplySequenceTime(slot, 0f, 0, Quiet.Jump);
            if (!IsLive(slot, version)) { return; }
            ResetSequenceChildren(slot);
            slot.CallbackCycle = -1;
            slot.CallbackCursor = -1f;
        }

        public static void SetRemainingCycles(int index, uint version, int remaining) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            if (slot.OwnedBySequence) {
                Debug.LogError("RavenTween: set the cycles of the sequence, not of its children; ignored.");
                return;
            }
            if (slot.Completing) { return; }
            slot.Cycles = remaining < 0 ? -1 : slot.CyclesDone + Mathf.Max(remaining, 1);
        }

        public static void SetRemainingCycles(int index, uint version, bool stopAtEndValue) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            bool backAndForth = slot.Mode == CycleMode.Yoyo || slot.Mode == CycleMode.PingPong;
            bool headingToEnd = (slot.CyclesDone & 1) == 0;
            int remaining = !backAndForth || headingToEnd == stopAtEndValue ? 1 : 2;
            SetRemainingCycles(index, version, remaining);
        }

        public static bool TryTakeOverEffect(UnityEngine.Object target, PropertyKind property, out TweenValue rest) {
            rest = default;
            var key = new EffectKey(target, property);
            if (!Effects.TryGetValue(key, out int index)) { return false; }
            Effects.Remove(key);
            TweenSlot slot = _slots[index];
            if (slot.State == SlotState.Free || slot.Effect == EffectKind.None || slot.OwnedBySequence) { return false; }
            if (!ReferenceEquals(slot.UnityTarget, target) || slot.Property != property) { return false; }
            bool found = slot.FromCaptured;
            if (found) { rest = slot.StartValue; }
            KillInternal(slot, index, true);
            return found;
        }

        public static void RegisterEffect(int index) {
            TweenSlot slot = _slots[index];
            Debug.Assert(slot.Effect != EffectKind.None, "Only effects are registered.");
            Debug.Assert(slot.UnityTarget != null, "Effects are registered with a live target.");
            var key = new EffectKey(slot.UnityTarget, slot.Property);
            if (Effects.TryGetValue(key, out int other) && other != index) {
                TweenSlot previous = _slots[other];
                bool live = previous.State != SlotState.Free && previous.Effect != EffectKind.None && !previous.OwnedBySequence
                    && ReferenceEquals(previous.UnityTarget, slot.UnityTarget) && previous.Property == slot.Property;
                if (live) { KillInternal(previous, other, false); }
            }
            Effects[key] = index;
        }

        static void UnregisterEffect(TweenSlot slot, int index) {
            if (ReferenceEquals(slot.UnityTarget, null)) { return; }
            var key = new EffectKey(slot.UnityTarget, slot.Property);
            if (Effects.TryGetValue(key, out int registered) && registered == index) { Effects.Remove(key); }
        }

        readonly struct EffectKey : IEquatable<EffectKey> {
            readonly UnityEngine.Object _target;
            readonly PropertyKind _property;

            public EffectKey(UnityEngine.Object target, PropertyKind property) {
                _target = target;
                _property = property;
            }

            public bool Equals(EffectKey other) { return ReferenceEquals(_target, other._target) && _property == other._property; }
            public override bool Equals(object obj) { return obj is EffectKey other && Equals(other); }
            public override int GetHashCode() { return (RuntimeHelpers.GetHashCode(_target) * 397) ^ (int)_property; }
        }

        internal struct DebugInfo {
            public int Index;
            public uint Version;
            public bool IsSequence;
            public bool OwnedBySequence;
            public bool Paused;
            public UnityEngine.Object Target;
            public PropertyKind Property;
            public float Elapsed;
            public float StartDelay;
            public float CycleLength;
            public int Cycles;
            public int CyclesDone;
            public CycleMode Mode;
            public float TimeScale;
            public UpdatePhase Phase;
            public bool IsCallback;
            public bool UnscaledTime;
        }

        public static void CollectDebugInfo(List<DebugInfo> buffer) {
            Debug.Assert(buffer != null, "Debug buffer cannot be null.");
            if (buffer == null) { return; }
            buffer.Clear();
            for (int i = 0; i < _slotCount; i++) {
                TweenSlot slot = _slots[i];
                if (slot.State == SlotState.Free) { continue; }
                buffer.Add(new DebugInfo {
                    Index = i,
                    Version = slot.Version,
                    IsSequence = slot.IsSequence,
                    OwnedBySequence = slot.OwnedBySequence,
                    Paused = slot.State == SlotState.Paused,
                    Target = slot.UnityTarget,
                    Property = slot.Property,
                    Elapsed = slot.Elapsed,
                    StartDelay = slot.StartDelay,
                    CycleLength = slot.CycleLength,
                    Cycles = slot.Cycles,
                    CyclesDone = slot.CyclesDone,
                    Mode = slot.Mode,
                    TimeScale = slot.TimeScale,
                    Phase = EffectivePhase(slot),
                    IsCallback = slot.IsCallback,
                    UnscaledTime = slot.UseUnscaledTime
                });
            }
        }

        internal struct DebugSequenceItem {
            public float StartTime;
            public float Duration;
            public UnityEngine.Object Target;
            public PropertyKind Property;
            public bool IsSequence;
            public bool IsCallback;
        }

        public static bool CollectSequenceItems(int index, uint version, List<DebugSequenceItem> buffer) {
            Debug.Assert(buffer != null, "Item buffer cannot be null.");
            buffer.Clear();
            if (!TryGetSlot(index, version, out TweenSlot slot) || !slot.IsSequence || slot.Items == null) { return false; }
            for (int i = 0; i < slot.Items.Count; i++) {
                SequenceItem item = slot.Items[i];
                TryGetSlot(item.ChildIndex, item.ChildVersion, out TweenSlot child);
                buffer.Add(new DebugSequenceItem {
                    StartTime = item.StartTime,
                    Duration = item.Duration,
                    Target = child != null ? child.UnityTarget : null,
                    Property = child != null ? child.Property : PropertyKind.None,
                    IsSequence = child != null && child.IsSequence,
                    IsCallback = child != null && child.IsCallback
                });
            }
            return true;
        }

        public static void Seek(int index, uint version, float time) {
            if (!TryGetSlot(index, version, out TweenSlot slot)) { return; }
            Debug.Assert(!slot.OwnedBySequence, "Seek the root, not a sequence child.");
            Debug.Assert(!float.IsNaN(time), "Seek time must be a number.");
            if (float.IsNaN(time)) { time = 0f; }
            ResetForReplay(slot, true);
            float local = time - slot.StartDelay;
            if (local < 0f || !Start(slot, Quiet.Jump)) { return; }
            if (slot.IsSequence) {
                ApplySequenceTime(slot, Mathf.Min(local, Mathf.Max(slot.SequenceDuration, 0f)), 0, Quiet.Jump);
                return;
            }
            float duration = Mathf.Max(slot.Duration, 1e-6f);
            int maxCycles = slot.Cycles < 0 ? int.MaxValue : Mathf.Max(slot.Cycles, 1);
            int cycle = (int)Math.Min(Math.Floor(local / duration), maxCycles - 1d);
            float progress = Mathf.Clamp01((local - cycle * duration) / duration);
            ApplyProgress(slot, progress, cycle);
        }

        static float _seedCursor;

        public static float NextSeed() {
            _seedCursor = (_seedCursor + 61.803398f) % 1000f;
            return _seedCursor;
        }

        static void InvokeSafe(Action callback) {
            if (callback == null) { return; }
            try { callback(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static void InvokeEach(Action callbacks) {
            if (callbacks == null) { return; }
            Delegate[] list = callbacks.GetInvocationList();
            for (int i = 0; i < list.Length; i++) {
                try { ((Action)list[i])(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        static void InvokeSafe(Action<float> callback, float value) {
            Debug.Assert(callback != null, "Callback must be checked before dispatch.");
            try { callback(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static void InvokeSafe(Action<TweenValue> callback, in TweenValue value) {
            Debug.Assert(callback != null, "Callback must be checked before dispatch.");
            try { callback(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
