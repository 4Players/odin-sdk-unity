using OdinNative.Wrapper;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Utility;

namespace OdinNative.Core.Imports
{
    public partial class NativeLibraryMethods
    {
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_create(uint32_t sample_rate, bool stereo, struct OdinDecoder **out_decoder);
        /// </remarks>
        public OdinError DecoderCreate(uint sample_rate, bool stereo, out OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinDecoderCreate(sample_rate, stereo, out IntPtr out_decoder);
                decoder = new OdinDecoderHandle(out_decoder);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderFreeDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_decoder_free(OdinDecoder *decoder);
        /// </remarks>
        public void DecoderFree(OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
                _OdinDecoderFree(decoder);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderGetPipelineDelegate"/>
        /// </summary>
        /// <remarks>
        /// const OdinPipeline *odin_decoder_get_pipeline(OdinDecoder *decoder);
        /// </remarks>
        public OdinPipelineHandle DecoderGetPipeline(OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
            {
                IntPtr ptr = _OdinDecoderGetPipeline(decoder);
                return new OdinPipelineHandle(ptr);
            }

        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderGetPositionsDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_get_positions(struct OdinDecoder *decoder, uint64_t channel_mask, struct OdinPosition *out_positions, uint32_t* out_positions_length);
        /// </remarks>
        public OdinError DecoderGetPositions(OdinDecoderHandle decoder, ChannelMask mask, out OdinPosition[] positions)
        {
            _DbgTrace();
            using (Lock)
            {
                OdinError error;
                OdinPosition[] posBuf = new OdinPosition[64];
                uint count = (uint)posBuf.Length;
                GCHandle handle = GCHandle.Alloc(posBuf, GCHandleType.Pinned);
                try
                {
                    IntPtr posPtr = handle.AddrOfPinnedObject();
                    error = _OdinDecoderGetPositions(decoder, (ulong)mask, posPtr, ref count);
                    if (error != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        positions = null;
                        return error;
                    }
                    if (count == 0)
                    {
                        positions = new OdinPosition[0];
                        return error;
                    }

                    positions = new OdinPosition[count];
                    Array.Copy(posBuf, positions, (int)count);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }

                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderGetActiveChannelsDelegate"/>
        /// </summary>
        /// <remarks>
        /// uint64_t odin_decoder_get_active_channels(struct OdinDecoder *decoder);
        /// </remarks>
        public ChannelMask DecoderGetActiveChannels(OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
                return (ChannelMask)_OdinDecoderGetActiveChannels(decoder);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderIsSilentDelegate"/>
        /// </summary>
        /// <remarks>
        /// bool odin_decoder_is_silent(struct OdinDecoder *decoder);
        /// </remarks>
        public bool DecoderIsSilent(OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
                return _OdinDecoderIsSilent(decoder);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderSetEventCallbackDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_set_event_callback(struct OdinDecoder *decoder, enum OdinAudioEvents filter, OdinDecoderEventCallback callback, void* user_data);
        /// </remarks>
        public OdinError DecoderSetEventCallback(OdinDecoderHandle decoder, OdinAudioEvents filter, OdinDecoderEventCallbackDelegate callback, IntPtr userData)
        {
            _DbgTrace();
            using (Lock)
                return _OdinDecoderSetEventCallback(decoder, filter, callback, userData);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderPopDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_pop(struct OdinDecoder *decoder, float* out_samples, uint32_t out_samples_count, bool* out_is_silent);
        /// </remarks>
        public OdinError DecoderPop(OdinDecoderHandle decoder, ref float[] samples, out bool isSilent)
        {
            _DbgTrace();
            using (Lock)
            {
                // hold a reference so a concurrent Dispose (e.g. peer left on the rpc thread)
                // defers the native free until this call is done instead of freeing under it
                bool refAdded = false;
                try
                {
                    decoder.DangerousAddRef(ref refAdded);
                }
                catch (ObjectDisposedException)
                {
                    isSilent = true;
                    return OdinError.ODIN_ERROR_CLOSED;
                }

                OdinError error;
                GCHandle handle = GCHandle.Alloc(samples, GCHandleType.Pinned);
                try
                {
                    IntPtr samplesPtr = handle.AddrOfPinnedObject();
                    uint count = (uint)samples.Length;
                    error = _OdinDecoderPop(decoder, samplesPtr, count, out isSilent);
                    if (samplesPtr != IntPtr.Zero && isSilent == false)
                        Marshal.Copy(samplesPtr, samples, 0, (int)count);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                    if (refAdded)
                        decoder.DangerousRelease();
                }

                return error;
            }
        }
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderPushDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_push(struct OdinDecoder *decoder, const uint8_t* datagram, uint32_t datagram_length);
        /// </remarks>
        protected internal OdinError DecoderPush(IntPtr decoder, IntPtr samples, uint samplesCount)
        {
            _DbgTrace();
            using (Lock)
                return _OdinDecoderPush(decoder, samples, samplesCount);
        }
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderPushDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_push(struct OdinDecoder *decoder, const uint8_t* datagram, uint32_t datagram_length);
        /// </remarks>
        public OdinError DecoderPush(IntPtr decoder, float[] samples)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(samples, GCHandleType.Pinned);
                try
                {
                    return _OdinDecoderPush(decoder, handle.AddrOfPinnedObject(), (uint)samples.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_create(uint32_t peer_id, uint32_t sample_rate, bool stereo, OdinEncoder **out_encoder);
        /// Default bitrate 32000 (with stereo 128000), voip true (with stereo false), disabled interval, and expected packet loss of 15%
        /// </remarks>
        public OdinError EncoderCreate(UInt32 peer_id, UInt32 sample_rate, bool stereo, out OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinEncoderCreate(peer_id, sample_rate, stereo, out IntPtr out_encoder);
                encoder = new OdinEncoderHandle(out_encoder);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderCreateExDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_create_ex(uint32_t peer_id, uint32_t sample_rate, bool stereo, bool application_voip, uint32_t bitrate_kbps, uint32_t packet_loss_perc, uint64_t update_position_interval_ms, struct OdinEncoder **out_encoder);
        /// </remarks>
        public OdinError EncoderCreateEx(uint peer_id, uint sample_rate, bool stereo, bool application_voip, uint bitrate_kbps, uint packet_loss_perc, ulong update_position_interval_ms, out OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinEncoderCreateEx(peer_id, sample_rate, stereo, application_voip, bitrate_kbps, packet_loss_perc, update_position_interval_ms, out IntPtr out_encoder);
                encoder = new OdinEncoderHandle(out_encoder);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderFreeDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_encoder_free(OdinEncoder *encoder);
        /// </remarks>
        public void EncoderFree(OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
                _OdinEncoderFree(encoder);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderGetPipelineDelegate"/>
        /// </summary>
        /// <remarks>
        /// const OdinPipeline *odin_encoder_get_pipeline(OdinEncoder *encoder);
        /// </remarks>        
        public OdinPipelineHandle EncoderGetPipeline(OdinEncoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
                return new OdinPipelineHandle(_OdinEncoderGetPipeline(decoder));
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderSetPositionDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_set_position(struct OdinEncoder *encoder, uint64_t channel_mask, const struct OdinPosition *position);
        /// </remarks>
        public OdinError EncoderSetPosition(OdinEncoderHandle encoder, ChannelMask mask, OdinPosition position)
        {
            _DbgTrace();
            using (Lock)
            {
                int size = Marshal.SizeOf(typeof(OdinPosition));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(position, ptr, false);
                    return _OdinEncoderSetPosition(encoder, (ulong)mask, ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr); // blittable so no destroy
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderClearPositionDelegate"/>
        /// </summary>
        /// <remarks>
        /// enum OdinError odin_encoder_clear_position(struct OdinEncoder *encoder, uint64_t channel_mask);
        /// </remarks>
        public OdinError EncoderClearPosition(OdinEncoderHandle encoder, ChannelMask mask)
        {
            _DbgTrace();
            using (Lock)
                return _OdinEncoderClearPosition(encoder, (ulong)mask);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderIsSilentDelegate"/>
        /// </summary>
        /// <remarks>
        /// bool odin_encoder_is_silent(struct OdinEncoder *encoder);
        /// </remarks>
        public bool EncoderIsSilent(OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
                return _OdinEncoderIsSilent(encoder);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderSetEventCallbackDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_set_event_callback(struct OdinEncoder *encoder, enum OdinAudioEvents filter, OdinEncoderEventCallback callback, void* user_data);
        /// </remarks>
        public OdinError EncoderSetEventCallback(OdinEncoderHandle encoder, OdinAudioEvents filter, OdinEncoderEventCallbackDelegate callback, IntPtr userData)
        {
            _DbgTrace();
            using (Lock)
                return _OdinEncoderSetEventCallback(encoder, filter, callback, userData);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderPopDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_pop(struct OdinEncoder *encoder, uint8_t* out_datagram, uint32_t *out_datagram_length);
        /// </remarks>
        public OdinError EncoderPop(OdinEncoderHandle encoder, ref byte[] datagram)
        {
#if UNITY_2022_3_OR_NEWER
            using var profilerScope = EncoderPopMarker.Auto();
#endif
            _DbgTrace();
            using (Lock)
            {
                OdinError error;
                GCHandle datagramhandle = GCHandle.Alloc(datagram, GCHandleType.Pinned);
                try
                {
                    IntPtr datagramPtr = datagramhandle.AddrOfPinnedObject();
                    uint count = (uint)datagram.Length;
                    error = _OdinEncoderPop(encoder, datagramPtr, ref count);

                    datagram = new byte[count];
                    if (datagramPtr != IntPtr.Zero)
                        Marshal.Copy(datagramPtr, datagram, 0, (int)count);
                }
                finally
                {
                    if (datagramhandle.IsAllocated)
                        datagramhandle.Free();

                }
                return error;
            }
        }

#if UNITY_2022_3_OR_NEWER
        private static readonly global::Unity.Profiling.ProfilerMarker EncoderPushMarker = new global::Unity.Profiling.ProfilerMarker("ODIN.Encoder.Push");
        private static readonly global::Unity.Profiling.ProfilerMarker EncoderPopMarker = new global::Unity.Profiling.ProfilerMarker("ODIN.Encoder.Pop");
        private static readonly global::Unity.Profiling.ProfilerMarker SendDatagramMarker = new global::Unity.Profiling.ProfilerMarker("ODIN.Room.SendDatagram");
#endif

        internal OdinError EncoderPop(OdinEncoderHandle encoder, byte[] buffer, out uint count)
        {
#if UNITY_2022_3_OR_NEWER
            using var profilerScope = EncoderPopMarker.Auto();
#endif
            _DbgTrace();
            count = (uint)buffer.Length;
            using (Lock)
            {
                GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    var result = _OdinEncoderPop(encoder, pinned.AddrOfPinnedObject(), ref count);
                    if (result == OdinError.ODIN_ERROR_SUCCESS && count > buffer.Length)
                        return OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL;
                    return result;
                }
                finally
                {
                    pinned.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderPushDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_push(struct OdinEncoder *encoder, const float* samples, uint32_t samples_count);
        /// </remarks>
        public OdinError EncoderPush(OdinEncoderHandle encoder, float[] samples)
        {
#if UNITY_2022_3_OR_NEWER
            using var profilerScope = EncoderPushMarker.Auto();
#endif
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(samples, GCHandleType.Pinned);
                try
                {
                    return _OdinEncoderPush(encoder, handle.AddrOfPinnedObject(), (uint)samples.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinErrorGetLastErrorDelegate"/>
        /// </summary>
        /// <remarks>
        /// const char *odin_error_get_last_error(void);
        /// </remarks>
        public string ErrorGetLastError()
        {
            _DbgTrace();
            using (Lock)
            {
                IntPtr strPtr = _OdinErrorGetLastError();
                return Native.TryReadCString(strPtr);
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinErrorResetLastErrorDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_error_reset_last_error(void);
        /// </remarks>
        public void ErrorResetLastError()
        {
            _DbgTrace();
            using (Lock)
                _OdinErrorResetLastError();
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinInitializeDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_initialize(const char *version);
        /// </remarks>
        public OdinError Initialize(string version = OdinNative.Core.Imports.NativeBindings.OdinLibraryVersion)
        {
            _DbgTrace();
            using (Lock)
                return _OdinInitialize(version);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetEffectCountDelegate"/>
        /// </summary>
        /// <remarks>
        /// uint32_t odin_pipeline_get_effect_count(const OdinPipeline *pipeline);
        /// </remarks>
        public uint PipelineGetEffectCount(OdinPipelineHandle pipeline)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetEffectCount(pipeline);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetEffectIdDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_id(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        public OdinError PipelineGetEffectId(OdinPipelineHandle pipeline, uint index, out uint out_effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetEffectId(pipeline, index, out out_effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetEffectIndexDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_index(const OdinPipeline *pipeline, uint32_t effect_id, uint32_t *out_index);
        /// </remarks>
        public OdinError PipelineGetEffectIndex(OdinPipelineHandle pipeline, uint effect_id, out uint out_index)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetEffectIndex(pipeline, effect_id, out out_index);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetEffectTypeDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_type(const OdinPipeline *pipeline, uint32_t effect_id, OdinEffectType *out_effect_type);
        /// </remarks>
        public OdinError PipelineGetEffectType(OdinPipelineHandle pipeline, uint effect_id, out OdinEffectType out_effect_type)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetEffectType(pipeline, effect_id, out out_effect_type);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetVadConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinVadConfig *out_config);
        /// </remarks>
        public OdinError PipelineGetVadConfig(OdinPipelineHandle pipeline, uint effect_id, out OdinVadConfig out_config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetVadConfig(pipeline, effect_id, out out_config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineInsertApmEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_insert_apm_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t playback_sample_rate, bool playback_stereo, uint32_t *out_effect_id);
        /// </remarks>
        public OdinError PipelineInsertApmEffect(OdinPipelineHandle pipeline, uint index, uint playback_sample_rate, bool playback_stereo, out uint out_effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineInsertApmEffect(pipeline, index, playback_sample_rate, playback_stereo, out out_effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetApmConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_apm_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinApmConfig *out_config);
        /// </remarks>
        public OdinError PipelineGetApmConfig(OdinPipelineHandle pipeline, uint effect_id, out OdinApmConfig out_config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetApmConfig(pipeline, effect_id, out out_config);
        }
        
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.PipelineInsertCustomEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_insert_custom_effect(const struct OdinPipeline *pipeline, uint32_t index, OdinCustomEffectCallback callback, const void* user_data, uint32_t *out_effect_id);
        /// <code>typedef void (*OdinCustomEffectCallback)(float *samples, uint32_t samples_count, bool* is_silent, const void* user_data);</code>
        /// </remarks>
        public OdinError PipelineInsertCustomEffect(OdinPipelineHandle pipeline, uint index, OdinCustomEffectCallbackDelegate callback, IntPtr user_data, out uint out_effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineInsertCustomEffect(pipeline, index, callback, user_data, out out_effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineInsertVadEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_insert_vad_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        public OdinError PipelineInsertVadEffect(OdinPipelineHandle pipeline, uint index, out uint out_effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineInsertVadEffect(pipeline, index, out out_effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineInsertViEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_insert_vi_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        public OdinError PipelineInsertViEffect(OdinPipelineHandle pipeline, uint index, out uint out_effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineInsertViEffect(pipeline, index, out out_effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineGetViConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_get_vi_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinViConfig *out_config);
        /// </remarks>
        public OdinError PipelineGetViConfig(OdinPipelineHandle pipeline, uint effect_id, out OdinViConfig out_config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineGetViConfig(pipeline, effect_id, out out_config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineSetViConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_set_vi_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinViConfig* config);
        /// </remarks>
        public OdinError PipelineSetViConfig(OdinPipelineHandle pipeline, uint effect_id, OdinViConfig config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineSetViConfig(pipeline, effect_id, ref config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineMoveEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_move_effect(const OdinPipeline *pipeline, uint32_t effect_id, size_t new_index);
        /// </remarks>
        public OdinError PipelineMoveEffect(OdinPipelineHandle pipeline, uint effect_id, UInt64 new_index)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineMoveEffect(pipeline, effect_id, (UIntPtr)new_index);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineRemoveEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_remove_effect(const OdinPipeline *pipeline, uint32_t effect_id);
        /// </remarks>
        public OdinError PipelineRemoveEffect(OdinPipelineHandle pipeline, uint effect_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineRemoveEffect(pipeline, effect_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineSetApmConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_set_apm_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinApmConfig* config);
        /// </remarks>
        public OdinError PipelineSetApmConfig(OdinPipelineHandle pipeline, uint effect_id, OdinApmConfig config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineSetApmConfig(pipeline, effect_id, ref config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineSetVadConfigDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_set_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinVadConfig* config);
        /// </remarks>
        public OdinError PipelineSetVadConfig(OdinPipelineHandle pipeline, uint effect_id, OdinVadConfig config)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineSetVadConfig(pipeline, effect_id, ref config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineUpdateApmPlaybackDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_update_apm_playback(const struct OdinPipeline *pipeline, uint32_t effect_id, const float* samples, uint32_t samples_count, uint64_t delay_ms);
        /// </remarks>
        public OdinError PipelineUpdateApmPlayback(OdinPipelineHandle pipeline, uint effect_id, float[] audio, ulong delay)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(audio, GCHandleType.Pinned);
                try
                {
                    return _OdinPipelineUpdateApmPlayback(pipeline, effect_id, handle.AddrOfPinnedObject(), (uint)audio.Length, delay);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomCloseDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_room_close(OdinRoom *room);
        /// </remarks>
        public void RoomClose(OdinRoomHandle room)
        {
            _DbgTrace();
            using (Lock)
                _OdinRoomClose(room);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_create(const char *gateway, const char* authentication, struct OdinRoomEvents *events, struct OdinCipher *cipher, struct OdinRoom **out_room);
        /// </remarks>
        public OdinError RoomCreate(string gateway, string authentication, ref OdinRoomEvents events, OdinCipherHandle cipher, out OdinRoomHandle roomHandle)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinRoomCreate(gateway, authentication, ref events, cipher, out IntPtr out_room);
                if (error == OdinError.ODIN_ERROR_SUCCESS && out_room == IntPtr.Zero)
                    throw new OdinWrapperException($"{nameof(_OdinRoomCreate)} invalid handle \"{out_room}\"");
                roomHandle = new OdinRoomHandle(out_room);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomFreeDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_room_free(OdinRoom *room);
        /// </remarks>
        public void RoomFree(OdinRoomHandle room)
        {
            _DbgTrace();
            using (Lock)
                _OdinRoomFree(room);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomGetConnectionIdDelegate"/>
        /// </summary>
        /// <remarks>
        /// uint64_t odin_room_get_connection_id(OdinRoom *room);
        /// </remarks>
        public UInt64 RoomGetConnectionId(OdinRoomHandle room)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomGetConnectionId(room);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomGetConnectionStatsDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_get_connection_stats(struct OdinRoom *room, struct OdinConnectionStats *out_stats);
        /// </remarks>
        public OdinError RoomGetConnectionStats(OdinRoomHandle room, out OdinConnectionStats stats)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomGetConnectionStats(room, out stats);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomGetNameDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_get_name(struct OdinRoom *room, char *out_value, uint32_t *out_value_length);
        /// </remarks>
        public OdinError RoomGetName(OdinRoomHandle room, out string roomName)
        {
            _DbgTrace();
            using (Lock)
            {
                byte[] buffer = new byte[4096];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint value_length = (uint)buffer.Length;
                    OdinError result = _OdinRoomGetName(room, ptr, ref value_length);
                    if (result != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        roomName = string.Empty;
                        return result;
                    }

                    Marshal.Copy(ptr, buffer, 0, (int)value_length);
                    roomName = Native.Encoding.GetString(buffer, 0, (int)value_length);
                    return result;
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomResendUserDataDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_resend_user_data(struct OdinRoom *room);
        /// </remarks>
        public OdinError RoomResendUserData(OdinRoomHandle room)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomResendUserData(room);
        }

        /// <remarks>
        /// OdinError odin_room_send_datagram(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        protected internal OdinError RoomSendDatagram(OdinRoomHandle room, IntPtr bytes, uint bytes_length)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomSendDatagram(room, bytes, bytes_length);
        }
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomSendDatagramDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_send_datagram(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        public OdinError RoomSendDatagram(OdinRoomHandle room, byte[] datagram)
            => RoomSendDatagram(room, datagram, (uint)datagram.Length);

        internal OdinError RoomSendDatagram(OdinRoomHandle room, byte[] datagram, uint count)
        {
            if (count > datagram.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
#if UNITY_2022_3_OR_NEWER
            using var profilerScope = SendDatagramMarker.Auto();
#endif
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(datagram, GCHandleType.Pinned);
                try
                {
                    return _OdinRoomSendDatagram(room, handle.AddrOfPinnedObject(), count);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomSendRpcDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_send_rpc(struct OdinRoom *room, const char *json);
        /// </remarks>
        public OdinError RoomSendRpc(OdinRoomHandle room, string json)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomSendRpc(room, json);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomSendLoopbackRpcDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_send_loopback_rpc(struct OdinRoom *room, const char *json);
        /// </remarks>
        public OdinError RoomSendLoopbackRpc(OdinRoomHandle room, string json)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomSendLoopbackRpc(room, json);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinShutdownDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_shutdown(void);
        /// </remarks>
        public void Shutdown()
        {
            _DbgTrace();
            using (Lock)
                _OdinShutdown();
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_token_generator_create(const char *access_key, OdinTokenGenerator** out_token_generator);
        /// </remarks>
        public OdinError TokenGeneratorCreate(string access_key, out OdinTokenGeneratorHandle token_generator)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinTokenGeneratorCreate(access_key, out IntPtr out_token_generator);
                token_generator = new OdinTokenGeneratorHandle(out_token_generator);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorFreeDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_token_generator_free(OdinTokenGenerator *token_generator);
        /// </remarks>
        public void TokenGeneratorFree(OdinTokenGeneratorHandle token_generator)
        {
            _DbgTrace();
            using (Lock)
                _OdinTokenGeneratorFree(token_generator);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorGetAccessKeyDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_token_generator_get_access_key(struct OdinTokenGenerator *token_generator, char* out_access_key, uint32_t *out_access_key_length);
        /// </remarks>
        public OdinError TokenGeneratorGetAccessKey(OdinTokenGeneratorHandle token_generator, out string accessKey)
        {
            _DbgTrace();
            using (Lock)
            {
                byte[] buffer = new byte[255];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint access_key_length = (uint)buffer.Length;
                    OdinError result = _OdinTokenGeneratorGetAccessKey(token_generator, ptr, ref access_key_length);
                    OdinLog.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorGetAccessKey)}: {Utility.OdinLastErrorString()} (code {result}) params: buffer {buffer.Length} ptr {ptr} length {access_key_length}");
                    if (result != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        accessKey = string.Empty;
                        return result;
                    }

                    Marshal.Copy(ptr, buffer, 0, (int)access_key_length);
                    accessKey = Native.Encoding.GetString(buffer, 0, (int)access_key_length);
                    return result;
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorGetKeyIdDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_token_generator_get_key_id(struct OdinTokenGenerator *token_generator, char* out_key_id, uint32_t *out_key_id_length);
        /// </remarks>
        public OdinError TokenGeneratorGetKeyId(OdinTokenGeneratorHandle token_generator, out string keyId)
        {
            _DbgTrace();
            using (Lock)
            {
                byte[] buffer = new byte[255];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint key_id_length = (uint)buffer.Length;
                    OdinError result = _OdinTokenGeneratorGetKeyId(token_generator, ptr, ref key_id_length);
                    OdinLog.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorGetKeyId)}: {Utility.OdinLastErrorString()} (code {result}) params: buffer {buffer.Length} ptr {ptr} length {key_id_length}");
                    if (result != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        keyId = string.Empty;
                        return result;
                    }

                    Marshal.Copy(ptr, buffer, 0, (int)key_id_length);
                    keyId = Native.Encoding.GetString(buffer, 0, (int)key_id_length);
                    return result;
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorGetPublicKeyDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_token_generator_get_public_key(struct OdinTokenGenerator *token_generator, char* out_public_key, uint32_t *out_public_key_length);
        /// </remarks>
        public OdinError TokenGeneratorGetPublicKey(OdinTokenGeneratorHandle token_generator, out string pub)
        {
            _DbgTrace();
            using (Lock)
            {
                byte[] buffer = new byte[1024];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint key_length = (uint)buffer.Length;
                    OdinError result = _OdinTokenGeneratorGetPublicKey(token_generator, ptr, ref key_length);
                    OdinLog.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorGetPublicKey)}: {Utility.OdinLastErrorString()} (code {result}) params: buffer {buffer.Length} ptr {ptr} length {key_length}");
                    if (result != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        pub = string.Empty;
                        return result;
                    }

                    Marshal.Copy(ptr, buffer, 0, (int)key_length);
                    pub = Native.Encoding.GetString(buffer, 0, (int)key_length);
                    return result;
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinTokenGeneratorSignDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_token_generator_sign(struct OdinTokenGenerator *token_generator, const char* body, char* out_token, uint32_t *out_token_length);
        /// </remarks>
        public OdinError TokenGeneratorSign(OdinTokenGeneratorHandle token_generator, string body, out string token)
        {
            _DbgTrace();
            using (Lock)
            {
                byte[] buffer = new byte[512];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint token_length = (uint)buffer.Length;
                    OdinError result = _OdinTokenGeneratorSign(token_generator, body, ptr, ref token_length);
                    OdinLog.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorSign)}: {Utility.OdinLastErrorString()} (code {result}) params: body {body}, buffer {buffer.Length} ptr {ptr} length {token_length}");
                    if (result != OdinError.ODIN_ERROR_SUCCESS)
                    {
                        token = string.Empty;
                        return result;
                    }

                    Marshal.Copy(ptr, buffer, 0, (int)token_length);
                    token = Native.Encoding.GetString(buffer, 0, (int)token_length);
                    return result;
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinSocketCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_socket_create(struct OdinRoom *room, enum OdinSocketKind kind, uint32_t remote_peer_id, int32_t label, int32_t priority, struct OdinSocket **out_socket);
        /// </remarks>
        public OdinError SocketCreate(OdinRoomHandle room, OdinSocketKind socketKind, uint peerId, int label, int priority, out OdinSocketHandle socket)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinSocketCreate(room, socketKind, peerId, label, priority, out IntPtr out_socket);
                socket = new OdinSocketHandle(out_socket);
                return error;
            }
        }

        /// <summary>
        /// RawOdinSocketInfo
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinSocketInfoDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_socket_info(struct OdinSocket *socket, struct OdinSocketInfo *out_socket_info);
        /// </remarks>
        public OdinError SocketInfo(IntPtr socket, out RawOdinSocketInfo out_socket_info)
        {
            _DbgTrace();
            using (Lock)
                return _OdinSocketInfo(socket, out out_socket_info);
        }
        /// <summary>
        /// OdinSocketInfo
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinSocketInfoDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_socket_info(struct OdinSocket *socket, struct OdinSocketInfo *out_socket_info);
        /// </remarks>
        public OdinError SocketInfo(OdinSocketHandle socket, ref OdinSocketInfo socket_info)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinSocketInfo(socket, out RawOdinSocketInfo out_socket_info);
                if (error != OdinError.ODIN_ERROR_SUCCESS) return error;

                socket_info.RoomHandle = new OdinRoomHandle(out_socket_info.RawRoomHandle, false);
                socket_info.Kind = out_socket_info.Kind;
                socket_info.IsInbound = out_socket_info.IsInbound;
                socket_info.RemotePeerId = out_socket_info.RemotePeerId;
                socket_info.Label = out_socket_info.Label;
                socket_info.Priority = out_socket_info.Priority;
                socket_info.UnsentBytes = out_socket_info.UnsentBytes;
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinSocketSendDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_socket_send(struct OdinSocket *socket, const uint8_t* message, uint32_t message_length);
        /// </remarks>
        public OdinError SocketSend(OdinSocketHandle socket, byte[] message)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(message, GCHandleType.Pinned);
                try
                {
                    return _OdinSocketSend(socket, handle.AddrOfPinnedObject(), (uint)message.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
            
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinSocketResetDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_socket_reset(struct OdinSocket *socket);
        /// </remarks>
        public OdinError SocketReset(OdinSocketHandle socket)
        {
            _DbgTrace();
            using (Lock)
                return _OdinSocketReset(socket);
        }
    }
}
