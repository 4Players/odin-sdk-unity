using OdinNative.Core;
using OdinNative.Core.Imports;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper.Media
{
    public abstract class PiplineEffect : IPiplineEffect
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

        public PiplineEffect(OdinPipelineHandle parentHandle, uint effectId)
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
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.GetEffectIndex)} parent is released");

            var result = Odin.Library.Methods.PipelineGetEffectIndex(Parent, Id, out indexId);

            if (result == OdinError.ODIN_ERROR_ARGUMENT_INVALID_ID) return result;
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectIndex)} in {nameof(PiplineEffect.GetEffectIndex)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");

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
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.GetEffectType)} parent is released");

            var result = Odin.Library.Methods.PipelineGetEffectType(Parent, Id, out effectType);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectType)} in {nameof(PiplineEffect.GetEffectType)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
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
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.GetVadConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineGetVadConfig(Parent, Id, out config);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetVadConfig)} in {nameof(PiplineEffect.GetVadConfig)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}");
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
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.SetApmConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineSetApmConfig(Parent, Id, config);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetApmConfig)} in {nameof(PiplineEffect.SetApmConfig)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, config {config}");
            return result;
        }
        /// <summary>
        /// Set native audio processing delay
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="ms">delay</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError SetApmStreamDelay(ulong ms)
        {
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.SetApmStreamDelay)} parent is released");

            var result = Odin.Library.Methods.PipelineSetApmStreamDelay(Parent, Id, ms);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetApmStreamDelay)} in {nameof(PiplineEffect.SetApmStreamDelay)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, ms {ms}");
            return result;
        }
        /// <summary>
        /// Set native voice activity config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VAD"/> </remarks>
        /// <param name="config">new config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError SetVadConfig(NativeBindings.OdinVadConfig config)
        {
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.SetVadConfig)} parent is released");

            var result = Odin.Library.Methods.PipelineSetVadConfig(Parent, Id, config);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineSetVadConfig)} in {nameof(PiplineEffect.SetVadConfig)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, config {config}");
            return result;
        }

        /// <summary>
        /// Send samples for native audio processing
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="audio">samples</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public virtual OdinError UpdateApmPlayback(float[] audio)
        {
            Utility.Assert(Parent.IsAlive, $"{nameof(PiplineEffect.UpdateApmPlayback)} parent is released");

            var result = Odin.Library.Methods.PipelineUpdateApmPlayback(Parent, Id, audio);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineUpdateApmPlayback)} in {nameof(PiplineEffect.UpdateApmPlayback)} failed: {Utility.OdinLastErrorString()} (code {result}) params: effect_id {Id}, audio {audio}");
            return result;
        }
    }
}