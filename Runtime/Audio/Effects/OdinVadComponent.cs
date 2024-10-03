using OdinNative.Core;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using UnityEngine;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// VoiceActivity component for <see cref="OdinNative.Wrapper.Media.VadEffect"/>
    /// <para>
    /// This class provides configuration for the native implemented voice detection. Supports speech recognition and/or a volume threshold.
    /// </para>
    /// </summary>
    /// <remarks>Autoset <see cref="OdinNative.Core.Imports.NativeBindings.OdinVadConfig"/> for build-in VAD</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinvadcomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Voice activity detection")]
    public class OdinVadComponent : MonoBehaviour, IOdinEffect
    {
        public IMedia Media { get; set; }

        public VadEffect Effect { get; private set; }
        public bool IsCreated { get; private set; } = false;
        private bool _warn = true;

        [Header("Odin Voice Activity Detection")]
        /// <summary>
        /// Idicates whether the vad setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        public bool VoiceActivityEnabled = OdinDefaults.VoiceActivityDetection;
        /// <summary>
        /// Idicates the vad attack probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityAttackThreshold = OdinDefaults.VoiceActivityDetectionAttackProbability;
        /// <summary>
        /// Idicates the vad release probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityReleaseThreshold = OdinDefaults.VoiceActivityDetectionReleaseProbability;

        [Header("Odin Volume Gate")]
        /// <summary>
        /// Idicates whether the gate setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        public bool VolumeGateEnabled = OdinDefaults.VolumeGate;
        /// <summary>
        /// Idicates the gate attack loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateAttackThreshold = OdinDefaults.VolumeGateAttackLoudness;
        /// <summary>
        /// Idicates the gate release loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateReleaseThreshold = OdinDefaults.VolumeGateReleaseLoudness;

        void Reset()
        {
            IsCreated = false;

            VoiceActivityEnabled = OdinDefaults.VoiceActivityDetection;
            VoiceActivityAttackThreshold = OdinDefaults.VoiceActivityDetectionAttackProbability;
            VoiceActivityReleaseThreshold = OdinDefaults.VoiceActivityDetectionReleaseProbability;
            VolumeGateEnabled = OdinDefaults.VolumeGate;
            VolumeGateAttackThreshold = OdinDefaults.VolumeGateAttackLoudness;
            VolumeGateReleaseThreshold = OdinDefaults.VolumeGateReleaseLoudness;
        }

        void Start()
        {
            if(Media == null)
                Media = this.gameObject.GetComponent<OdinMedia>();

            if (Media == null && _warn)
            {
                Debug.Log($"{gameObject.name} does not have a {nameof(OdinMedia)} to add {nameof(VadEffect)} for this {nameof(OdinVadComponent)}");
                return;
            }
        }

        void Update()
        {
            if (IsCreated == false)
            {
                MediaPipeline pipeline = Media?.GetPipeline();
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        Debug.LogWarning($"{gameObject.name} {nameof(OdinVadComponent)} can not create/add {nameof(VadEffect)} without a pipline");
                        _warn = false;
                    }
                    return;
                }

                if (pipeline.AddVadEffect(out VadEffect vad))
                {
                    Effect = vad;
                    UpdateConfig(pipeline);
                    IsCreated = true;
                    if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.Log($"{gameObject.name} {nameof(OdinVadComponent)} added {nameof(VadEffect)} (id {Effect.Id})");
                }
                else if(_warn)
                {
                    Debug.LogError($"{gameObject.name} {nameof(OdinVadComponent)} error in {nameof(MediaPipeline.AddVadEffect)}");
                }
                _warn = true;
            }
        }

        public void UpdateConfig() => UpdateConfig(Media?.GetPipeline());
        public void UpdateConfig(MediaPipeline pipeline)
        {
            Utility.Assert(pipeline != null, $"{nameof(OdinVadComponent)} {nameof(UpdateConfig)} {nameof(MediaPipeline)} is null");
            Utility.Assert(Effect != null, $"{nameof(OdinVadComponent)} {nameof(UpdateConfig)} {nameof(VadEffect)} is null");

            if (Effect == null) return;

            Effect.VoiceActivityEnabled = VoiceActivityEnabled;
            Effect.VoiceActivityAttackThreshold = VoiceActivityAttackThreshold;
            Effect.VoiceActivityReleaseThreshold = VoiceActivityReleaseThreshold;
            Effect.VolumeGateEnabled = VolumeGateEnabled;
            Effect.VolumeGateAttackThreshold = VolumeGateAttackThreshold;
            Effect.VolumeGateReleaseThreshold = VolumeGateReleaseThreshold;
            Effect.UpdateEffectConfig();
        }

        public void ResetEffect() => ResetEffect(Media?.GetPipeline());
        public void ResetEffect(MediaPipeline pipeline)
        {
            Utility.Assert(pipeline != null, $"{nameof(OdinVadComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");

            if (Effect != null)
                pipeline.RemoveEffect(Effect.Id);

            if (pipeline.AddVadEffect(out VadEffect effect))
            {
                Effect = effect;
                UpdateConfig(pipeline);
            }
        }

        void OnDestroy()
        {
            if (Media != null && Effect != null)
                Media.GetPipeline()?.RemoveEffect(Effect.Id);
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PiplineEffect GetEffect() => Effect;
    }
}