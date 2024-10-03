using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// VoiceActivity effect for <see cref="MediaPipeline"/>
    /// </summary>
    public class ApmEffect : PiplineEffect
    {
        /// <summary>
        /// Audio processing configuration
        /// </summary>
        public OdinApmConfig Config = new OdinApmConfig();

        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled
        /// </summary>
        public bool EchoCanceller { get => Config.echo_canceller; set => Config.echo_canceller = value; }
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled
        /// </summary>
        public bool HighPassFilter { get => Config.high_pass_filter; set => Config.high_pass_filter = value; }
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled
        /// </summary>
        public bool PreAmplifier { get => Config.pre_amplifier; set => Config.pre_amplifier = value; }
        /// <summary>
        /// Idicates the level of noise suppression ApmConfig setting by default
        /// </summary>
        public OdinNoiseSuppression NoiseSuppressionLevel { get => Config.noise_suppression; set => Config.noise_suppression = value; }
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled
        /// </summary>
        public bool TransientSuppressor { get => Config.transient_suppressor; set => Config.transient_suppressor = value; }
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled
        /// </summary>
        public bool GainController { get => Config.gain_controller; set => Config.gain_controller = value; }

        /// <summary>
        /// Output decoder samplerate
        /// </summary>
        public uint Samplerate { get; internal set; }
        /// <summary>
        /// Output decoder channel flag
        /// </summary>
        public bool IsStereo { get; internal set; }

        /// <summary>
        /// Internal effect constructor, use <see cref="ApmEffect.Create(MediaPipeline, uint, bool, OdinApmConfig)"/>
        /// </summary>
        /// <param name="parentHandle">pipeline handle</param>
        /// <param name="effectId">effect id</param>
        protected internal ApmEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId)
        {
        }

        /// <summary>
        /// Set audio processing configuration
        /// </summary>
        /// <returns>true on success or false</returns>
        public bool UpdateEffectConfig(MediaPipeline pipeline) => Core.Utility.IsOk(this.SetApmConfig(Config));
        /// <summary>
        /// Set managed audio processing configuration
        /// </summary>
        /// <param name="config">new config</param>
        /// <returns>updated config</returns>
        public override OdinError SetApmConfig(OdinApmConfig config)
        {
            Config = config;
            return base.SetApmConfig(config);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError SetVadConfig(OdinVadConfig config)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new OdinWrapperException($"must be of type {typeof(ApmEffect)}", new NotSupportedException($"{typeof(VadEffect)} call of type {typeof(ApmEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError GetVadConfig(out OdinVadConfig config)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new OdinWrapperException($"must be of type {typeof(ApmEffect)}", new NotSupportedException($"{typeof(VadEffect)} call of type {typeof(ApmEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            config = default;
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }

        /// <summary>
        /// Insert a apm effect in the specified pipline and sets the apm config
        /// </summary>
        /// <param name="pipeline">where to create a apm effect</param>
        /// <param name="samplerate">samplerate of playback</param>
        /// <param name="stereo">stereo of playback</param>
        /// <param name="config">configuration settings for apm</param>
        /// <returns>Instance of <see cref="ApmEffect"/></returns>
        public static ApmEffect Create(MediaPipeline pipeline, uint samplerate, bool stereo, OdinApmConfig config)
        {
            if (pipeline == null) return null;

            uint newIndex = pipeline.GetNextIndex();
            ApmEffect effect = pipeline.InsertApmEffect(newIndex, samplerate, stereo);
            if (effect != null)
                effect.SetApmConfig(config);

            return effect;
        }
    }
}