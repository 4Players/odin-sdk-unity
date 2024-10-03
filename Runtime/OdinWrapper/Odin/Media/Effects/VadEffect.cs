using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// VoiceActivity effect for <see cref="MediaPipeline"/>
    /// </summary>
    public class VadEffect : PiplineEffect
    {
        /// <summary>
        /// Intern voice activity configuration
        /// </summary>
        public OdinVadConfig Config = new OdinVadConfig()
        {
            voice_activity = new OdinSensitivityConfig(),
            volume_gate = new OdinSensitivityConfig()
        };

        /// <summary>
        /// Idicates whether the vad setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        public bool VoiceActivityEnabled { get => Config.voice_activity.enabled; set => Config.voice_activity.enabled = value; }
        /// <summary>
        /// Idicates the vad attack probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityAttackThreshold { get => Config.voice_activity.attack_threshold; set => Config.voice_activity.attack_threshold = value; }
        /// <summary>
        /// Idicates the vad release probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityReleaseThreshold { get => Config.voice_activity.release_threshold; set => Config.voice_activity.release_threshold = value; }

        /// <summary>
        /// Idicates whether the gate setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        public bool VolumeGateEnabled { get => Config.volume_gate.enabled; set => Config.volume_gate.enabled = value; }
        /// <summary>
        /// Idicates the gate attack loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateAttackThreshold { get => Config.volume_gate.attack_threshold; set => Config.volume_gate.attack_threshold = value; }
        /// <summary>
        /// Idicates the gate release loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateReleaseThreshold { get => Config.volume_gate.release_threshold; set => Config.volume_gate.release_threshold = value; }

        /// <summary>
        /// Internal effect constructor, use <see cref="VadEffect.Create(MediaPipeline, OdinVadConfig)"/>
        /// </summary>
        /// <param name="parentHandle">pipeline handle</param>
        /// <param name="effectId">effect id</param>
        protected internal VadEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId)
        {
        }

        /// <summary>
        /// Set voice activity configuration
        /// </summary>
        /// <returns>true on success or false</returns>
        public bool UpdateEffectConfig() => Core.Utility.IsOk(this.SetVadConfig(Config));
        /// <summary>
        /// Set managed voice activity configuration
        /// </summary>
        /// <param name="config">new config</param>
        /// <returns>updated config</returns>
        public override OdinError SetVadConfig(OdinVadConfig config)
        {
            Config = config;
            return base.SetVadConfig(config);
        }

        /// <summary>
        /// Get native voice activity configuration
        /// </summary>
        /// <returns>updated config</returns>
        public OdinVadConfig GetVadConfig()
        {
            if (Utility.IsOk(base.GetVadConfig(out OdinVadConfig config)))
                Config = config;

            return Config;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError SetApmConfig(OdinApmConfig config)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new OdinWrapperException($"must be of type {typeof(VadEffect)}", new NotSupportedException($"{typeof(ApmEffect)} call of type {typeof(VadEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError SetApmStreamDelay(ulong ms)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new OdinWrapperException($"must be of type {typeof(VadEffect)}", new NotSupportedException($"{typeof(ApmEffect)} call of type {typeof(VadEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError UpdateApmPlayback(float[] audio)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new OdinWrapperException($"must be of type {typeof(VadEffect)}", new NotSupportedException($"{typeof(ApmEffect)} call of type {typeof(VadEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }

        /// <summary>
        /// Insert a vad effect in the specified pipline
        /// </summary>
        /// <param name="pipeline">where to create a vad effect</param>
        /// <returns>Instance of <see cref="VadEffect"/></returns>
        public static bool Create(MediaPipeline pipeline, out VadEffect vad)
        {
            vad = null;
            if (pipeline == null) return false;

            uint newIndex = pipeline.GetNextIndex();
            vad = pipeline.InsertVadEffect(newIndex);
            return vad != null;
        }
        /// <summary>
        /// Insert a vad effect in the specified pipline and sets the vad config
        /// </summary>
        /// <param name="pipeline">where to create a vad effect</param>
        /// <param name="config">configuration settings for vad</param>
        /// <returns>Instance of <see cref="VadEffect"/></returns>
        public static VadEffect Create(MediaPipeline pipeline, OdinVadConfig config)
        {
            if (pipeline == null) return null;

            uint newIndex = pipeline.GetNextIndex();
            VadEffect effect = pipeline.InsertVadEffect(newIndex);
            if (effect != null)
                effect.SetVadConfig(config);

            return effect;
        }
    }
}