using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// VoiceIsolation effect for <see cref="MediaPipeline"/>
    /// </summary>
    public class ViEffect : PipelineEffect
    {
        /// <summary>
        /// Intern voice isolation configuration
        /// </summary>
        public OdinViConfig Config = new OdinViConfig();

        /// <summary>
        /// Indicates whether the effect will attenuate non-speech portions of the input audio signal
        /// </summary>
        public bool Enabled { get => Config.enabled; set => Config.enabled = value; }
        /// <summary>
        /// Maximum attenuation applied to non-speech in dB; values of `100` and above remove
        /// background sound entirely, while values close to `0` disable noise reduction.
        /// </summary>
        public float AttenuationLimitDb { get => Config.attenuation_limit_db; set => Config.attenuation_limit_db = value; }

        /// <summary>
        /// Internal effect constructor, use <see cref="ViEffect.Create(MediaPipeline, OdinViConfig)"/>
        /// </summary>
        /// <param name="parentHandle">pipeline handle</param>
        /// <param name="effectId">effect id</param>
        protected internal ViEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId)
        {
        }

        /// <summary>
        /// Set voice isolation configuration
        /// </summary>
        /// <returns>true on success or false</returns>
        public bool UpdateEffectConfig() => Core.Utility.IsOk(this.SetViConfig(Config));
        /// <summary>
        /// Set managed voice isolation configuration
        /// </summary>
        /// <param name="config">new config</param>
        /// <returns>updated config</returns>
        public override OdinError SetViConfig(OdinViConfig config)
        {
            Config = config;
            return base.SetViConfig(config);
        }

        /// <summary>
        /// Get native voice isolation configuration
        /// </summary>
        /// <returns>updated config</returns>
        public OdinViConfig GetViConfig()
        {
            if (Utility.IsOk(base.GetViConfig(out OdinViConfig config)))
                Config = config;

            return Config;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError SetVadConfig(OdinVadConfig config)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            OdinLog.Throw(new OdinWrapperException($"must be of type {typeof(ViEffect)}", new NotSupportedException($"{typeof(VadEffect)} call of type {typeof(ViEffect)}")));
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
            OdinLog.Throw(new OdinWrapperException($"must be of type {typeof(ViEffect)}", new NotSupportedException($"{typeof(VadEffect)} call of type {typeof(ViEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            config = default;
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError SetApmConfig(OdinApmConfig config)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            OdinLog.Throw(new OdinWrapperException($"must be of type {typeof(ViEffect)}", new NotSupportedException($"{typeof(ApmEffect)} call of type {typeof(ViEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="OdinWrapperException"></exception>
        public override OdinError UpdateApmPlayback(float[] audio, ulong msDelay)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            OdinLog.Throw(new OdinWrapperException($"must be of type {typeof(ApmEffect)}", new NotSupportedException($"{typeof(ViEffect)} call of type {typeof(ApmEffect)}")));
#pragma warning restore CS0618 // Type or member is obsolete
            return OdinError.ODIN_ERROR_UNSUPPORTED_VERSION; // obsolete
        }

        /// <summary>
        /// Insert a vi effect in the specified pipeline
        /// </summary>
        /// <param name="pipeline">where to create a vi effect</param>
        /// <param name="vi">created <see cref="ViEffect"/> instance or null</param>
        /// <returns>true on success or false</returns>
        public static bool Create(MediaPipeline pipeline, out ViEffect vi)
        {
            vi = null;
            if (pipeline == null) return false;

            uint newIndex = pipeline.GetNextIndex();
            vi = pipeline.InsertViEffect(newIndex);
            return vi != null;
        }
        /// <summary>
        /// Insert a vi effect in the specified pipeline and sets the vi config
        /// </summary>
        /// <param name="pipeline">where to create a vi effect</param>
        /// <param name="config">configuration settings for vi</param>
        /// <returns>Instance of <see cref="ViEffect"/></returns>
        public static ViEffect Create(MediaPipeline pipeline, OdinViConfig config)
        {
            if (pipeline == null) return null;

            uint newIndex = pipeline.GetNextIndex();
            ViEffect effect = pipeline.InsertViEffect(newIndex);
            if (effect != null)
                effect.SetViConfig(config);

            return effect;
        }
    }
}
