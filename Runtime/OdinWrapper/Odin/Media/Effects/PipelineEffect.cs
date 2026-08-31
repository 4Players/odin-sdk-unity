using OdinNative.Core;
using OdinNative.Core.Imports;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper.Media
{
    public abstract class PipelineEffect : IPipelineEffect
    {
        /// <summary>
        /// Effect id
        /// </summary>
        public uint Id { get; internal set; }
        /// <summary>
        /// Effect index
        /// </summary>
        public uint Index { get; set; }
        /// <summary>
        /// Pipeline handle
        /// </summary>
        public OdinPipelineHandle Parent { get; private set; }
        internal OdinCustomEffectCallbackDelegate Callback { get; set; }

        public PipelineEffect(OdinPipelineHandle parentHandle, uint effectId)
        {
            Parent = parentHandle;
            Id = effectId;
            Index = 0;
        }
        /// <summary>
        /// Get native effect index
        /// </summary>
        /// <param name="indexId">native index</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError GetEffectIndex(out uint indexId)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.GetEffectIndex)} parent is released");

            var result = Odin.Library.Methods.PipelineGetEffectIndex(Parent, Id, out indexId);

            if (result == OdinError.ODIN_ERROR_ARGUMENT_INVALID_ID) return result;
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectIndex)} in {nameof(PipelineEffect.GetEffectIndex)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");

            this.Index = indexId;
            return result;
        }
        /// <summary>
        /// Get native effect <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType"/>
        /// </summary>
        /// <param name="effectType">native type</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError GetEffectType(out NativeBindings.OdinEffectType effectType)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.GetEffectType)} parent is released");

            var result = Odin.Library.Methods.PipelineGetEffectType(Parent, Id, out effectType);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectType)} in {nameof(PipelineEffect.GetEffectType)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
            return result;
        }
        /// <summary>
        /// Get native voice activity config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VAD"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError GetVadConfig(out NativeBindings.OdinVadConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.GetVadConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineGetVadConfig(Parent, Id, out config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetVadConfig)} in {nameof(PipelineEffect.GetVadConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
            return result;
        }
        /// <summary>
        /// Get native apm config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError GetApmConfig(out NativeBindings.OdinApmConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.GetApmConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineGetApmConfig(Parent, Id, out config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetApmConfig)} in {nameof(PipelineEffect.GetApmConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
            return result;
        }
        /// <summary>
        /// Get native voice isolation config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VI"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError GetViConfig(out NativeBindings.OdinViConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.GetViConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineGetViConfig(Parent, Id, out config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetViConfig)} in {nameof(PipelineEffect.GetViConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
            return result;
        }
        /// <summary>
        /// Updates the configuration settings of the VI effect identified by `effect_id` in the specified
        /// audio pipeline.
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VI"/> </remarks>
        /// <param name="config">new config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError SetViConfig(NativeBindings.OdinViConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.SetViConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineSetViConfig(Parent, Id, config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetViConfig)} in {nameof(PipelineEffect.SetViConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, config {config}");
            return result;
        }
        /// <summary>
        /// Set native audio processing config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError SetApmConfig(NativeBindings.OdinApmConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.SetApmConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineSetApmConfig(Parent, Id, config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetApmConfig)} in {nameof(PipelineEffect.SetApmConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, config {config}");
            return result;
        }
        /// <summary>
        /// Updates the configuration settings of the VAD effect identified by `effect_id` in the specified
        /// audio pipeline.
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VAD"/> </remarks>
        /// <param name="config">new config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError SetVadConfig(NativeBindings.OdinVadConfig config)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.SetVadConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineSetVadConfig(Parent, Id, config);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetVadConfig)} in {nameof(PipelineEffect.SetVadConfig)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, config {config}");
            return result;
        }

        /// <summary>
        /// Updates the specified APM effect's sample buffer for processing the reverse (playback) audio
        /// stream. The provided samples must be interleaved float values in the range [-1, 1]. The delay
        /// parameter is used to align the reverse stream processing with the forward (capture) stream.
        /// The delay can be expressed as:
        ///   `delay = (t_render - t_analyze) + (t_process - t_capture)`
        /// 
        ///  <list>where:
        ///  <item> `t_render` is the time the first sample of the same frame is rendered by the audio hardware.</item>
        ///  <item> `t_analyze` is the time the frame is processed in the reverse stream.</item>
        ///  <item> `t_capture` is the time the first sample of a frame is captured by the audio hardware.</item>
        ///  <item> `t_process` is the time the frame is processed in the forward stream.</item>
        /// </list>
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="audio">samples</param>
        /// <param name="msDelay">delay in ms</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError UpdateApmPlayback(float[] audio, ulong msDelay)
        {
            OdinLog.Assert(Parent.IsAlive, $"{nameof(PipelineEffect.UpdateApmPlayback)} parent is released");

            var result = Odin.Library.Methods.PipelineUpdateApmPlayback(Parent, Id, audio, msDelay);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineUpdateApmPlayback)} in {nameof(PipelineEffect.UpdateApmPlayback)}: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, audio {audio}");
            return result;
        }
    }
}