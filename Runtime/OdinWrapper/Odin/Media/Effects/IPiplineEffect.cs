using OdinNative.Core.Imports;

namespace OdinNative.Wrapper.Media
{
    public interface IPiplineEffect
    {
        /// <summary>
        /// Effect id
        /// </summary>
        uint Id { get; }
        /// <summary>
        /// Effect index
        /// </summary>
        uint Index { get; set; }
        /// <summary>
        /// Pipeline handle
        /// </summary>
        OdinPipelineHandle Parent { get; }
        /// <summary>
        /// Get native effect index
        /// </summary>
        /// <param name="indexId">native index</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError GetEffectIndex(out uint indexId);
        /// <summary>
        /// Get native effect <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType"/>
        /// </summary>
        /// <param name="effectType">native type</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError GetEffectType(out NativeBindings.OdinEffectType effectType);
        /// <summary>
        /// Get native voice activity config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VAD"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError GetVadConfig(out NativeBindings.OdinVadConfig config);
        /// <summary>
        /// Set native audio processing config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="config">native config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError SetApmConfig(NativeBindings.OdinApmConfig config);
        /// <summary>
        /// Set native audio processing delay
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="ms">delay</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError SetApmStreamDelay(ulong ms);
        /// <summary>
        /// Set native voice activity config
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_VAD"/> </remarks>
        /// <param name="config">new config</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError SetVadConfig(NativeBindings.OdinVadConfig config);
        /// <summary>
        /// Send samples for native audio processing
        /// </summary>
        /// <remarks>Only successful on <see cref="OdinNative.Core.Imports.NativeBindings.OdinEffectType.ODIN_EFFECT_TYPE_APM"/> </remarks>
        /// <param name="audio">samples</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        NativeBindings.OdinError UpdateApmPlayback(float[] audio);
    }
}