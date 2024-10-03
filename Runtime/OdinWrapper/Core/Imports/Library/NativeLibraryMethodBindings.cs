using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    public partial class NativeLibraryMethods
    {
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinConnectionPoolCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_connection_pool_create(OdinConnectionPoolSettings settings, OdinConnectionPool** out_connection_pool);
        /// </remarks>
        public OdinError ConnectionPoolCreate(OdinConnectionPoolSettings settings, out OdinConnectionPoolHandle connection_pool)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinConnectionPoolCreate(settings, out IntPtr out_connection_pool);
                connection_pool = new OdinConnectionPoolHandle(out_connection_pool);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinConnectionPoolFreeDelegate"/>
        /// </summary>
        /// <remarks>
        /// void odin_connection_pool_free(OdinConnectionPool *connection_pool);
        /// </remarks>
        public void ConnectionPoolFree(OdinConnectionPoolHandle connection_pool)
        {
            _DbgTrace();
            using (Lock)
                _OdinConnectionPoolFree(connection_pool);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_decoder_create(uint16_t media_id, uint32_t sample_rate, bool stereo, OdinDecoder **out_decoder);
        /// </remarks>
        public OdinError DecoderCreate(UInt16 media_id, UInt32 sample_rate, bool stereo, out OdinDecoderHandle decoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinDecoderCreate(media_id, sample_rate, stereo, out IntPtr out_decoder);
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
        /// OdinError odin_encoder_create(uint32_t sample_rate, bool stereo, OdinEncoder **out_encoder);
        /// </remarks>
        public OdinError EncoderCreate(UInt32 sample_rate, bool stereo, out OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinEncoderCreate(sample_rate, stereo, out IntPtr out_encoder);
                encoder = new OdinEncoderHandle(out_encoder);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderCreateExDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_create_ex(uint32_t sample_rate, bool stereo, bool application_voip, uint32_t bitrate_kbps, OdinEncoder** out_encoder);
        /// </remarks>
        public OdinError EncoderCreateEx(UInt32 sample_rate, bool stereo, bool application_voip, UInt32 bitrate_kbps, out OdinEncoderHandle encoder)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinEncoderCreateEx(sample_rate, stereo, application_voip, bitrate_kbps, out IntPtr out_encoder);
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
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderPopDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_encoder_pop(struct OdinEncoder *encoder, const uint16_t* media_ids, uint32_t media_ids_length, uint8_t* out_datagram, uint32_t *out_datagram_length);
        /// </remarks>
        public OdinError EncoderPop(OdinEncoderHandle encoder, ushort[] mediaIds, ref byte[] datagram)
        {
            _DbgTrace();
            using (Lock)
            {
                OdinError error;
                GCHandle datagramhandle = GCHandle.Alloc(datagram, GCHandleType.Pinned);
                GCHandle mediaIdshandle = GCHandle.Alloc(mediaIds, GCHandleType.Pinned);
                try
                {
                    IntPtr datagramPtr = datagramhandle.AddrOfPinnedObject();
                    IntPtr mediaIdsPtr = mediaIdshandle.AddrOfPinnedObject();
                    uint count = (uint)datagram.Length;
                    error = _OdinEncoderPop(encoder, mediaIdsPtr, (uint)mediaIds.Length, datagramPtr, ref count);

                    datagram = new byte[count];
                    if (datagramPtr != IntPtr.Zero)
                        Marshal.Copy(datagramPtr, datagram, 0, (int)count);
                }
                finally
                {
                    if (mediaIdshandle.IsAllocated)
                        mediaIdshandle.Free();
                    if (datagramhandle.IsAllocated)
                        datagramhandle.Free();

                }
                return error;
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
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineMoveEffectDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_move_effect(const OdinPipeline *pipeline, uint32_t effect_id, size_t new_index);
        /// </remarks>
        public OdinError PipelineMoveEffect(OdinPipelineHandle pipeline, uint effect_id, UInt64 new_index)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineMoveEffect(pipeline, effect_id, new_index);
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
                return _OdinPipelineSetApmConfig(pipeline, effect_id, config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineSetApmStreamDelayDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_set_apm_stream_delay(const OdinPipeline *pipeline, uint32_t effect_id, uint64_t ms);
        /// </remarks>
        public OdinError PipelineSetApmStreamDelay(OdinPipelineHandle pipeline, uint effect_id, UInt64 ms)
        {
            _DbgTrace();
            using (Lock)
                return _OdinPipelineSetApmStreamDelay(pipeline, effect_id, ms);
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
                return _OdinPipelineSetVadConfig(pipeline, effect_id, config);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinPipelineUpdateApmPlaybackDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_pipeline_update_apm_playback(const struct OdinPipeline *pipeline, uint32_t effect_id, const float* samples, uint32_t samples_count);
        /// </remarks>
        public OdinError PipelineUpdateApmPlayback(OdinPipelineHandle pipeline, uint effect_id, float[] audio)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(audio, GCHandleType.Pinned);
                try
                {
                    return _OdinPipelineUpdateApmPlayback(pipeline, effect_id, handle.AddrOfPinnedObject(), (uint)audio.Length);
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
        /// OdinError odin_room_create(OdinConnectionPool *connection_pool, const char* gateway, const char* token, OdinRoom **out_room);
        /// </remarks>
        public OdinError RoomCreate(OdinConnectionPoolHandle connection_pool, string gateway, string token, out OdinRoomHandle roomHandle)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinRoomCreate(connection_pool, gateway, token, out IntPtr out_room);
                roomHandle = new OdinRoomHandle(out_room);
                return error;
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomCreateExDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_create_ex(struct OdinConnectionPool *connection_pool, const char* gateway, const char* token, const char* room_name, const unsigned char* user_data, uint32_t user_data_length, const float position[3], struct OdinCipher *cipher, struct OdinRoom **out_room);
        /// </remarks>
        public OdinError RoomCreateEx(OdinConnectionPoolHandle connection_pool, string gateway, string token, out OdinRoomHandle roomHandle, string room_name = null, byte[] user_data = null, float positionX = 0, float positionY = 0, float positionZ = 0, OdinCipherHandle cipher = null)
        {
            _DbgTrace();
            using (Lock)
            {
                var error = _OdinRoomCreateEx(connection_pool,
                    gateway,
                    token,
                    room_name,
                    user_data,
                    (uint)(user_data?.Length ?? 0),
                    new float[] { positionX, positionY, positionZ },
                    cipher ?? IntPtr.Zero,
                    out IntPtr out_room);
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
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomGetIdDelegate"/>
        /// </summary>
        /// <remarks>
        /// uint64_t odin_room_get_id(OdinRoom *room);
        /// </remarks>
        public UInt64 RoomGetId(OdinRoomHandle room)
        {
            _DbgTrace();
            using (Lock)
                return _OdinRoomGetId(room);
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
                    OdinError result = _OdinRoomGetName(room, ptr, out value_length);
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
                    if (handle != null && handle.IsAllocated)
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
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(datagram, GCHandleType.Pinned);
                try
                {
                    return _OdinRoomSendDatagram(room, handle.AddrOfPinnedObject(), (uint)datagram.Length);
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
        /// OdinError odin_room_send_rpc(struct OdinRoom *room, const uint8_t *bytes, uint32_t bytes_length);
        /// </remarks>
        public OdinError RoomSendRpc(OdinRoomHandle room, byte[] bytes)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return _OdinRoomSendRpc(room, handle.AddrOfPinnedObject(), (uint)bytes.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinRoomSendLoopbackRpcDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinError odin_room_send_loopback_rpc(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        public OdinError RoomSendLoopbackRpc(OdinRoomHandle room, byte[] bytes)
        {
            _DbgTrace();
            using (Lock)
            {
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return _OdinRoomSendLoopbackRpc(room, handle.AddrOfPinnedObject(), (uint)bytes.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
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
                    OdinError result = _OdinTokenGeneratorGetAccessKey(token_generator, ptr, out access_key_length);
                    Utility.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorGetAccessKey)} failed: {Utility.OdinLastErrorString()} (code {result}) params: buffer {buffer.Length} ptr {ptr} length {access_key_length}");
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
                    if (handle != null && handle.IsAllocated)
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
                    OdinError result = _OdinTokenGeneratorGetKeyId(token_generator, ptr, out key_id_length);
                    Utility.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorGetKeyId)} failed: {Utility.OdinLastErrorString()} (code {result}) params: buffer {buffer.Length} ptr {ptr} length {key_id_length}");
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
                    if (handle != null && handle.IsAllocated)
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
                byte[] buffer = new byte[2048];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint token_length = (uint)buffer.Length;
                    OdinError result = _OdinTokenGeneratorSign(token_generator, body, ptr, out token_length);
                    Utility.Assert(result != OdinError.ODIN_ERROR_ARGUMENT_TOO_SMALL, $"{nameof(NativeLibraryMethods)} in {nameof(TokenGeneratorSign)} failed: {Utility.OdinLastErrorString()} (code {result}) params: body {body}, buffer {buffer.Length} ptr {ptr} length {token_length}");
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
                    if (handle != null && handle.IsAllocated)
                        handle.Free();
                }
            }
        }
    }
}
