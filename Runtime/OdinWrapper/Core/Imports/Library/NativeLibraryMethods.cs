using OdinNative.Core.Handles;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// Import odin function signatures to wrapper delegates
    /// </summary>
    public partial class NativeLibraryMethods : NativeMethods<OdinLibraryHandle>
    {
        public NativeLibraryMethods(OdinLibraryHandle handle) : base(handle)
        {
            handle.GetLibraryMethod("odin_connection_pool_create", out _OdinConnectionPoolCreate);
            handle.GetLibraryMethod("odin_connection_pool_free", out _OdinConnectionPoolFree);
            handle.GetLibraryMethod("odin_decoder_create", out _OdinDecoderCreate);
            handle.GetLibraryMethod("odin_decoder_free", out _OdinDecoderFree);
            handle.GetLibraryMethod("odin_decoder_get_pipeline", out _OdinDecoderGetPipeline);
            handle.GetLibraryMethod("odin_decoder_pop", out _OdinDecoderPop);
            handle.GetLibraryMethod("odin_decoder_push", out _OdinDecoderPush);
            handle.GetLibraryMethod("odin_encoder_create", out _OdinEncoderCreate);
            handle.GetLibraryMethod("odin_encoder_create_ex", out _OdinEncoderCreateEx);
            handle.GetLibraryMethod("odin_encoder_free", out _OdinEncoderFree);
            handle.GetLibraryMethod("odin_encoder_get_pipeline", out _OdinEncoderGetPipeline);
            handle.GetLibraryMethod("odin_encoder_pop", out _OdinEncoderPop);
            handle.GetLibraryMethod("odin_encoder_push", out _OdinEncoderPush);
            handle.GetLibraryMethod("odin_error_get_last_error", out _OdinErrorGetLastError);
            handle.GetLibraryMethod("odin_error_reset_last_error", out _OdinErrorResetLastError);
            handle.GetLibraryMethod("odin_initialize", out _OdinInitialize);
            handle.GetLibraryMethod("odin_pipeline_get_effect_count", out _OdinPipelineGetEffectCount);
            handle.GetLibraryMethod("odin_pipeline_get_effect_id", out _OdinPipelineGetEffectId);
            handle.GetLibraryMethod("odin_pipeline_get_effect_index", out _OdinPipelineGetEffectIndex);
            handle.GetLibraryMethod("odin_pipeline_get_effect_type", out _OdinPipelineGetEffectType);
            handle.GetLibraryMethod("odin_pipeline_get_vad_config", out _OdinPipelineGetVadConfig);
            handle.GetLibraryMethod("odin_pipeline_insert_apm_effect", out _OdinPipelineInsertApmEffect);
            handle.GetLibraryMethod("odin_pipeline_insert_custom_effect", out _OdinPipelineInsertCustomEffect);
            handle.GetLibraryMethod("odin_pipeline_insert_vad_effect", out _OdinPipelineInsertVadEffect);
            handle.GetLibraryMethod("odin_pipeline_move_effect", out _OdinPipelineMoveEffect);
            handle.GetLibraryMethod("odin_pipeline_remove_effect", out _OdinPipelineRemoveEffect);
            handle.GetLibraryMethod("odin_pipeline_set_apm_config", out _OdinPipelineSetApmConfig);
            handle.GetLibraryMethod("odin_pipeline_set_apm_stream_delay", out _OdinPipelineSetApmStreamDelay);
            handle.GetLibraryMethod("odin_pipeline_set_vad_config", out _OdinPipelineSetVadConfig);
            handle.GetLibraryMethod("odin_pipeline_update_apm_playback", out _OdinPipelineUpdateApmPlayback);
            handle.GetLibraryMethod("odin_room_close", out _OdinRoomClose);
            handle.GetLibraryMethod("odin_room_create", out _OdinRoomCreate);
            handle.GetLibraryMethod("odin_room_create_ex", out _OdinRoomCreateEx);
            handle.GetLibraryMethod("odin_room_free", out _OdinRoomFree);
            handle.GetLibraryMethod("odin_room_get_connection_id", out _OdinRoomGetConnectionId);
            handle.GetLibraryMethod("odin_room_get_id", out _OdinRoomGetId);
            handle.GetLibraryMethod("odin_room_send_datagram", out _OdinRoomSendDatagram);
            handle.GetLibraryMethod("odin_room_get_name", out _OdinRoomGetName);
            handle.GetLibraryMethod("odin_room_resend_user_data", out _OdinRoomResendUserData);
            handle.GetLibraryMethod("odin_room_send_rpc", out _OdinRoomSendRpc);
            handle.GetLibraryMethod("odin_room_send_loopback_rpc", out _OdinRoomSendLoopbackRpc);
            handle.GetLibraryMethod("odin_shutdown", out _OdinShutdown);
            handle.GetLibraryMethod("odin_token_generator_create", out _OdinTokenGeneratorCreate);
            handle.GetLibraryMethod("odin_token_generator_free", out _OdinTokenGeneratorFree);
            handle.GetLibraryMethod("odin_token_generator_get_access_key", out _OdinTokenGeneratorGetAccessKey);
            handle.GetLibraryMethod("odin_token_generator_get_key_id", out _OdinTokenGeneratorGetKeyId);
            handle.GetLibraryMethod("odin_token_generator_sign", out _OdinTokenGeneratorSign);
        }

        /// <remarks>
        /// void (*on_rpc)(uint64_t room_id, const uint8_t *bytes, uint32_t bytes_length, void *user_data);
        /// </remarks>
        /// <param name="bytes">byte[]</param>
        public delegate void OdinConnectionPoolOnRPCDelegate(UInt64 room_id, IntPtr bytes, uint bytes_length, MarshalByRefObject user_data);
        /// <remarks>
        /// (*on_datagram)(uint64_t room_id, uint16_t media_id, const uint8_t* bytes, uint32_t bytes_length, void* user_data);
        /// </remarks>
        /// <param name="bytes">byte[]</param>
        public delegate void OdinConnectionPoolOnDatagramDelegate(UInt64 room_id, ushort media_id, IntPtr bytes, uint bytes_length, MarshalByRefObject user_data);
        /// <remarks>
        /// void (*OdinCustomEffectCallback)(float *samples, uint32_t samples_count, bool* is_silent, const void* user_data);
        /// </remarks>
        /// <param name="samples">float[]</param>
        public delegate void OdinCustomEffectCallbackDelegate(IntPtr samples, uint samples_count, [In, Out][MarshalAs(UnmanagedType.I1)] ref bool is_silent, IntPtr user_data);

        /// <remarks>
        /// OdinError odin_connection_pool_create(OdinConnectionPoolSettings settings, OdinConnectionPool** out_connection_pool);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinConnectionPoolCreateDelegate(OdinConnectionPoolSettings settings, out IntPtr out_connection_pool);
        readonly OdinConnectionPoolCreateDelegate _OdinConnectionPoolCreate;
        /// <remarks>
        /// void odin_connection_pool_free(OdinConnectionPool *connection_pool);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinConnectionPoolFreeDelegate(IntPtr connection_pool);
        readonly OdinConnectionPoolFreeDelegate _OdinConnectionPoolFree;
        /// <remarks>
        /// OdinError odin_decoder_create(uint16_t media_id, uint32_t sample_rate, bool stereo, OdinDecoder **out_decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderCreateDelegate(UInt16 media_id, UInt32 sample_rate, bool stereo, out IntPtr out_decoder);
        readonly OdinDecoderCreateDelegate _OdinDecoderCreate;
        /// <remarks>
        /// void odin_decoder_free(OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinDecoderFreeDelegate(IntPtr decoder);
        readonly OdinDecoderFreeDelegate _OdinDecoderFree;
        /// <remarks>
        /// const OdinPipeline *odin_decoder_get_pipeline(OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinDecoderGetPipelineDelegate(IntPtr decoder);
        readonly OdinDecoderGetPipelineDelegate _OdinDecoderGetPipeline;
        /// <remarks>
        /// OdinError odin_decoder_pop(struct OdinDecoder *decoder, float* out_samples, uint32_t out_samples_count, bool* out_is_silent);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderPopDelegate(IntPtr decoder, [In, Out] IntPtr samples, [In, Out] uint samples_count, [MarshalAs(UnmanagedType.I1)] out bool is_silent);
        readonly OdinDecoderPopDelegate _OdinDecoderPop;
        /// <remarks>
        /// OdinError odin_decoder_push(struct OdinDecoder *decoder, const uint8_t* datagram, uint32_t datagram_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderPushDelegate(IntPtr decoder, [In] IntPtr samples, [In] uint samples_count);
        readonly OdinDecoderPushDelegate _OdinDecoderPush;
        /// <remarks>
        /// OdinError odin_encoder_create(uint32_t sample_rate, bool stereo, OdinEncoder **out_encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderCreateDelegate(UInt32 sample_rate, bool stereo, out IntPtr out_encoder);
        readonly OdinEncoderCreateDelegate _OdinEncoderCreate;
        /// <remarks>
        /// OdinError odin_encoder_create_ex(uint32_t sample_rate, bool stereo, bool application_voip, uint32_t bitrate_kbps, OdinEncoder** out_encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderCreateExDelegate(UInt32 sample_rate, bool stereo, bool application_voip, UInt32 bitrate_kbps, out IntPtr out_encoder);
        readonly OdinEncoderCreateExDelegate _OdinEncoderCreateEx;
        /// <remarks>
        /// void odin_encoder_free(OdinEncoder *encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinEncoderFreeDelegate(IntPtr encoder);
        readonly OdinEncoderFreeDelegate _OdinEncoderFree;
        /// <remarks>
        /// const OdinPipeline *odin_encoder_get_pipeline(OdinEncoder *encoder);
        /// </remarks> 
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinEncoderGetPipelineDelegate(IntPtr decoder);
        readonly OdinEncoderGetPipelineDelegate _OdinEncoderGetPipeline;
        /// <remarks>
        /// OdinError odin_encoder_pop(struct OdinEncoder *encoder, const uint16_t* media_ids, uint32_t media_ids_length, uint8_t* out_datagram, uint32_t *out_datagram_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderPopDelegate(IntPtr encoder, [In] IntPtr media_ids, uint media_ids_length, [In, Out] IntPtr datagram, [In, Out] ref uint out_datagram_length);
        readonly OdinEncoderPopDelegate _OdinEncoderPop;
        /// <remarks>
        /// OdinError odin_encoder_push(struct OdinEncoder *encoder, const float* samples, uint32_t samples_count);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderPushDelegate(IntPtr encoder, [In] IntPtr samples, uint samples_count);
        readonly OdinEncoderPushDelegate _OdinEncoderPush;
        /// <remarks>
        /// const char *odin_error_get_last_error(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinErrorGetLastErrorDelegate();
        readonly OdinErrorGetLastErrorDelegate _OdinErrorGetLastError;
        /// <remarks>
        /// void odin_error_reset_last_error(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinErrorResetLastErrorDelegate();
        readonly OdinErrorResetLastErrorDelegate _OdinErrorResetLastError;
        /// <remarks>
        /// OdinError odin_initialize(const char *version);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinInitializeDelegate(string version);
        readonly OdinInitializeDelegate _OdinInitialize;
        /// <remarks>
        /// uint32_t odin_pipeline_get_effect_count(const OdinPipeline *pipeline);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinPipelineGetEffectCountDelegate(IntPtr pipeline);
        readonly OdinPipelineGetEffectCountDelegate _OdinPipelineGetEffectCount;
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_id(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectIdDelegate(IntPtr pipeline, uint index, out uint out_effect_id);
        readonly OdinPipelineGetEffectIdDelegate _OdinPipelineGetEffectId;
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_index(const OdinPipeline *pipeline, uint32_t effect_id, uint32_t *out_index);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectIndexDelegate(IntPtr pipeline, uint effect_id, out uint out_index);
        readonly OdinPipelineGetEffectIndexDelegate _OdinPipelineGetEffectIndex;
        /// <remarks>
        /// OdinError odin_pipeline_get_effect_type(const OdinPipeline *pipeline, uint32_t effect_id, OdinEffectType *out_effect_type);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectTypeDelegate(IntPtr pipeline, uint effect_id, out OdinEffectType out_effect_type);
        readonly OdinPipelineGetEffectTypeDelegate _OdinPipelineGetEffectType;
        /// <remarks>
        /// OdinError odin_pipeline_get_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinVadConfig *out_config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetVadConfigDelegate(IntPtr pipeline, uint effect_id, out OdinVadConfig out_config);
        readonly OdinPipelineGetVadConfigDelegate _OdinPipelineGetVadConfig;
        /// <remarks>
        /// OdinError odin_pipeline_insert_apm_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t playback_sample_rate, bool playback_stereo, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineInsertApmEffectDelegate(IntPtr pipeline, uint index, uint playback_sample_rate, bool playback_stereo, out uint out_effect_id);
        readonly OdinPipelineInsertApmEffectDelegate _OdinPipelineInsertApmEffect;
        /// <remarks>
        /// OdinError odin_pipeline_insert_custom_effect(const struct OdinPipeline *pipeline, uint32_t index, OdinCustomEffectCallback callback, const void* user_data, uint32_t *out_effect_id);
        /// <code>typedef void (*OdinCustomEffectCallback)(float *samples, uint32_t samples_count, bool* is_silent, const void* user_data);</code>
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError PipelineInsertCustomEffectDelegate(IntPtr pipeline, uint index, OdinCustomEffectCallbackDelegate callback, IntPtr user_data, out uint out_effect_id);
        readonly PipelineInsertCustomEffectDelegate _OdinPipelineInsertCustomEffect;
        /// <remarks>
        /// OdinError odin_pipeline_insert_vad_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineInsertVadEffectDelegate(IntPtr pipeline, uint index, out uint out_effect_id);
        readonly OdinPipelineInsertVadEffectDelegate _OdinPipelineInsertVadEffect;
        /// <remarks>
        /// OdinError odin_pipeline_move_effect(const OdinPipeline *pipeline, uint32_t effect_id, size_t new_index);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineMoveEffectDelegate(IntPtr pipeline, uint effect_id, UInt64 /*size_t*/ new_index);
        readonly OdinPipelineMoveEffectDelegate _OdinPipelineMoveEffect;
        /// <remarks>
        /// OdinError odin_pipeline_remove_effect(const OdinPipeline *pipeline, uint32_t effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineRemoveEffectDelegate(IntPtr pipeline, uint effect_id);
        readonly OdinPipelineRemoveEffectDelegate _OdinPipelineRemoveEffect;
        /// <remarks>
        /// OdinError odin_pipeline_set_apm_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinApmConfig* config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetApmConfigDelegate(IntPtr pipeline, uint effect_id, OdinApmConfig config);
        readonly OdinPipelineSetApmConfigDelegate _OdinPipelineSetApmConfig;
        /// <remarks>
        /// OdinError odin_pipeline_set_apm_stream_delay(const OdinPipeline *pipeline, uint32_t effect_id, uint64_t ms);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetApmStreamDelayDelegate(IntPtr pipeline, uint effect_id, UInt64 ms);
        readonly OdinPipelineSetApmStreamDelayDelegate _OdinPipelineSetApmStreamDelay;
        /// <remarks>
        /// OdinError odin_pipeline_set_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinVadConfig* config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetVadConfigDelegate(IntPtr pipeline, uint effect_id, OdinVadConfig config);
        readonly OdinPipelineSetVadConfigDelegate _OdinPipelineSetVadConfig;
        /// <remarks>
        /// OdinError odin_pipeline_update_apm_playback(const struct OdinPipeline *pipeline, uint32_t effect_id, const float* samples, uint32_t samples_count);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineUpdateApmPlaybackDelegate(IntPtr pipeline, uint effect_id, [In] IntPtr samples, uint samples_count);
        readonly OdinPipelineUpdateApmPlaybackDelegate _OdinPipelineUpdateApmPlayback;
        /// <remarks>
        /// void odin_room_close(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomCloseDelegate(IntPtr room);
        readonly OdinRoomCloseDelegate _OdinRoomClose;
        /// <remarks>
        /// OdinError odin_room_create(OdinConnectionPool *connection_pool, const char* gateway, const char* token, OdinRoom **out_room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomCreateDelegate(IntPtr connection_pool, string gateway, string token, out IntPtr out_room);
        readonly OdinRoomCreateDelegate _OdinRoomCreate;
        /// <remarks>
        /// OdinError odin_room_create_ex(struct OdinConnectionPool *connection_pool, const char* gateway, const char* token, const char* room_name, const unsigned char* user_data, uint32_t user_data_length, const float position[3], struct OdinCipher *cipher, struct OdinRoom **out_room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomCreateExDelegate(IntPtr connection_pool, string gateway, string token, string room_name, byte[] user_data, UInt32 user_data_length, float[] position, [In] /* OdinCipher */ IntPtr cipher, out IntPtr out_room);
        readonly OdinRoomCreateExDelegate _OdinRoomCreateEx;
        /// <remarks>
        /// void odin_room_free(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomFreeDelegate(IntPtr room);
        readonly OdinRoomFreeDelegate _OdinRoomFree;
        /// <remarks>
        /// uint64_t odin_room_get_connection_id(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate UInt64 OdinRoomGetConnectionIdDelegate(IntPtr room);
        readonly OdinRoomGetConnectionIdDelegate _OdinRoomGetConnectionId;
        /// <remarks>
        /// uint64_t odin_room_get_id(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate UInt64 OdinRoomGetIdDelegate(IntPtr room);
        readonly OdinRoomGetIdDelegate _OdinRoomGetId;
        /// <remarks>
        /// OdinError odin_room_get_name(struct OdinRoom *room, char *out_value, uint32_t *out_value_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomGetNameDelegate(IntPtr room, [In, Out] IntPtr value, out uint value_length);
        readonly OdinRoomGetNameDelegate _OdinRoomGetName;
        /// <remarks>
        /// OdinError odin_room_resend_user_data(struct OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomResendUserDataDelegate(IntPtr room);
        readonly OdinRoomResendUserDataDelegate _OdinRoomResendUserData;
        /// <remarks>
        /// OdinError odin_room_send_datagram(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendDatagramDelegate(IntPtr room, [In] IntPtr bytes, uint bytes_length);
        readonly OdinRoomSendDatagramDelegate _OdinRoomSendDatagram;
        /// <remarks>
        /// OdinError odin_room_send_loopback_rpc(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendLoopbackRpcDelegate(IntPtr room, [In] IntPtr bytes, uint bytes_length);
        readonly OdinRoomSendLoopbackRpcDelegate _OdinRoomSendLoopbackRpc;
        /// <remarks>
        /// OdinError odin_room_send_rpc(struct OdinRoom *room, const uint8_t *bytes, uint32_t bytes_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendRpcDelegate(IntPtr room, [In] IntPtr bytes, uint bytes_length);
        readonly OdinRoomSendRpcDelegate _OdinRoomSendRpc;
        /// <remarks>
        /// void odin_shutdown(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinShutdownDelegate();
        readonly OdinShutdownDelegate _OdinShutdown;
        /// <remarks>
        /// OdinError odin_token_generator_create(const char *access_key, OdinTokenGenerator** out_token_generator);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorCreateDelegate(string access_key, out IntPtr out_token_generator);
        readonly OdinTokenGeneratorCreateDelegate _OdinTokenGeneratorCreate;
        /// <remarks>
        /// void odin_token_generator_free(OdinTokenGenerator *token_generator);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinTokenGeneratorFreeDelegate(IntPtr token_generator);
        readonly OdinTokenGeneratorFreeDelegate _OdinTokenGeneratorFree;
        /// <remarks>
        /// OdinError odin_token_generator_get_access_key(struct OdinTokenGenerator *token_generator, char* out_access_key, uint32_t *out_access_key_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorGetAccessKeyDelegate(IntPtr token_generator, [In, Out] IntPtr access_key, out uint access_key_length);
        readonly OdinTokenGeneratorGetAccessKeyDelegate _OdinTokenGeneratorGetAccessKey;
        /// <remarks>
        /// OdinError odin_token_generator_get_key_id(struct OdinTokenGenerator *token_generator, char* out_key_id, uint32_t *out_key_id_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorGetKeyIdDelegate(IntPtr token_generator, [In, Out] IntPtr key_id, out uint key_id_length);
        readonly OdinTokenGeneratorGetKeyIdDelegate _OdinTokenGeneratorGetKeyId;
        /// <remarks>
        /// OdinError odin_token_generator_sign(struct OdinTokenGenerator *token_generator, const char* body, char* out_token, uint32_t *out_token_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorSignDelegate(IntPtr token_generator, [In] string body, [In, Out] IntPtr token, out uint token_length);
        readonly OdinTokenGeneratorSignDelegate _OdinTokenGeneratorSign;
    }
}
