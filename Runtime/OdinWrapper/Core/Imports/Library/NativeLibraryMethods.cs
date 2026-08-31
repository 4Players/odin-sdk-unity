using OdinNative.Core.Handles;
using System;
using System.Runtime.InteropServices;
using System.Text;
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
            handle.GetLibraryMethod("odin_decoder_create", out _OdinDecoderCreate);
            handle.GetLibraryMethod("odin_decoder_free", out _OdinDecoderFree);
            handle.GetLibraryMethod("odin_decoder_get_pipeline", out _OdinDecoderGetPipeline);
            handle.GetLibraryMethod("odin_decoder_get_positions", out _OdinDecoderGetPositions);
            handle.GetLibraryMethod("odin_decoder_get_active_channels", out _OdinDecoderGetActiveChannels);
            handle.GetLibraryMethod("odin_decoder_is_silent", out _OdinDecoderIsSilent);
            handle.GetLibraryMethod("odin_decoder_set_event_callback", out _OdinDecoderSetEventCallback);
            handle.GetLibraryMethod("odin_decoder_pop", out _OdinDecoderPop);
            handle.GetLibraryMethod("odin_decoder_push", out _OdinDecoderPush);
            handle.GetLibraryMethod("odin_encoder_create", out _OdinEncoderCreate);
            handle.GetLibraryMethod("odin_encoder_create_ex", out _OdinEncoderCreateEx);
            handle.GetLibraryMethod("odin_encoder_free", out _OdinEncoderFree);
            handle.GetLibraryMethod("odin_encoder_get_pipeline", out _OdinEncoderGetPipeline);
            handle.GetLibraryMethod("odin_encoder_set_position", out _OdinEncoderSetPosition);
            handle.GetLibraryMethod("odin_encoder_clear_position", out _OdinEncoderClearPosition);
            handle.GetLibraryMethod("odin_encoder_is_silent", out _OdinEncoderIsSilent);
            handle.GetLibraryMethod("odin_encoder_set_event_callback", out _OdinEncoderSetEventCallback);
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
            handle.GetLibraryMethod("odin_pipeline_insert_vi_effect", out _OdinPipelineInsertViEffect);
            handle.GetLibraryMethod("odin_pipeline_get_vi_config", out _OdinPipelineGetViConfig);
            handle.GetLibraryMethod("odin_pipeline_set_vi_config", out _OdinPipelineSetViConfig);
            handle.GetLibraryMethod("odin_pipeline_move_effect", out _OdinPipelineMoveEffect);
            handle.GetLibraryMethod("odin_pipeline_remove_effect", out _OdinPipelineRemoveEffect);
            handle.GetLibraryMethod("odin_pipeline_get_apm_config", out _OdinPipelineGetApmConfig);
            handle.GetLibraryMethod("odin_pipeline_set_apm_config", out _OdinPipelineSetApmConfig);
            handle.GetLibraryMethod("odin_pipeline_set_vad_config", out _OdinPipelineSetVadConfig);
            handle.GetLibraryMethod("odin_pipeline_update_apm_playback", out _OdinPipelineUpdateApmPlayback);
            handle.GetLibraryMethod("odin_room_close", out _OdinRoomClose);
            handle.GetLibraryMethod("odin_room_create", out _OdinRoomCreate);
            handle.GetLibraryMethod("odin_room_free", out _OdinRoomFree);
            handle.GetLibraryMethod("odin_room_get_connection_id", out _OdinRoomGetConnectionId);
            handle.GetLibraryMethod("odin_room_get_connection_stats", out _OdinRoomGetConnectionStats);
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
            handle.GetLibraryMethod("odin_token_generator_get_public_key", out _OdinTokenGeneratorGetPublicKey);
            handle.GetLibraryMethod("odin_token_generator_sign", out _OdinTokenGeneratorSign);
            handle.GetLibraryMethod("odin_socket_create", out _OdinSocketCreate);
            handle.GetLibraryMethod("odin_socket_info", out _OdinSocketInfo);
            handle.GetLibraryMethod("odin_socket_send", out _OdinSocketSend);
            handle.GetLibraryMethod("odin_socket_reset", out _OdinSocketReset);
        }

        //OdinError odin_debug_dump_state(char *out_msg, uint32_t *out_msg_length);
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError odin_debug_dump_state([Out] StringBuilder value, ref uint msg_length);
        //void (*callback)(const char *msg)
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void logging_callback([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);
        //OdinError odin_debug_set_logging_hook(uint32_t verbosity, void (*callback)(const char *msg));
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError odin_debug_set_logging_hook([In] uint verbosity, [In] logging_callback callback);

        /// <summary>
        /// Room-level callbacks for incoming data and events.
        /// </summary>
        /// <remarks>
        /// void (*on_rpc)(struct OdinRoom *room, const char *json, void *user_data);
        /// </remarks>
        /// <param name="json">char[]</param>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinOnRPCDelegate(IntPtr room, [MarshalAs(UnmanagedType.LPUTF8Str)] string json, IntPtr user_data);
        /// <summary>
        /// Room-level callbacks for incoming data and events.
        /// </summary>
        /// <remarks>
        /// void (*on_datagram)(struct OdinRoom *room, const struct OdinDatagramProperties *properties, const uint8_t *bytes, uint32_t bytes_length, void *user_data);
        /// </remarks>
        /// <param name="bytes">byte[]</param>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinOnDatagramDelegate(IntPtr room, ref OdinDatagramProperties properties, IntPtr bytes, uint bytes_length, IntPtr user_data);
        /// <summary>
        /// Room-level callbacks for incoming socket data and events.
        /// </summary>
        /// <remarks>
        /// void (*on_socket)(struct OdinSocket *socket, const uint8_t* message, uint32_t message_length, void* user_data);
        /// </remarks>
        /// <param name="bytes">byte[]</param>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinOnSocketDelegate(IntPtr socket, IntPtr bytes, uint bytes_length, IntPtr user_data);
        /// <summary>
        /// Defines the signature for custom effect callbacks in an ODIN audio pipeline. The callback
        /// receives a pointer to a buffer of audio samples, the number of samples in the buffer, a
        /// pointer to a flag indicating whether the audio is silent. This allows for custom, in-place
        /// processing of an audio stream.
        /// </summary>
        /// <remarks>
        /// void (*OdinCustomEffectCallback)(float *samples, uint32_t samples_count, bool* is_silent, const void* user_data);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinCustomEffectCallbackDelegate(IntPtr samples, uint samples_count, [In, Out][MarshalAs(UnmanagedType.I1)] ref bool is_silent, IntPtr user_data);

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinDecoderEventCallbackDelegate"/>
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinEncoderEventCallbackDelegate"/>
        /// </summary>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinAudioEventCallbackDelegate(IntPtr codec, OdinAudioEvents events, IntPtr user_data);
        /// <summary>
        /// A callback invoked when the decoder reports one or more audio events.
        /// </summary>
        /// <remarks>
        /// void (* OdinDecoderEventCallback) (struct OdinDecoder *decoder, enum OdinAudioEvents events, void* user_data);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinDecoderEventCallbackDelegate(IntPtr decoder, OdinAudioEvents events, IntPtr user_data);
        /// <summary>
        /// A callback invoked when the encoder reports one or more audio events.
        /// </summary>
        /// <remarks>
        /// void (* OdinEncoderEventCallback) (struct OdinEncoder *encoder, enum OdinAudioEvents events, void* user_data);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        public delegate void OdinEncoderEventCallbackDelegate(IntPtr encoder, OdinAudioEvents events, IntPtr user_data);

        /// <remarks>
        /// OdinError odin_decoder_create(uint32_t sample_rate, bool stereo, struct OdinDecoder **out_decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderCreateDelegate(uint sample_rate, [MarshalAs(UnmanagedType.I1)] bool stereo, out IntPtr out_decoder);
        /// <summary>
        /// Creates a new ODIN decoder with default settings for processing a remote media stream. All
        /// decoded audio will be provided at the specified sample rate and channel count.
        /// </summary>
        readonly OdinDecoderCreateDelegate _OdinDecoderCreate;

        /// <remarks>
        /// void odin_decoder_free(OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinDecoderFreeDelegate(IntPtr decoder);
        /// <summary>
        /// Frees the resources associated with the specified decoder.
        /// </summary>
        readonly OdinDecoderFreeDelegate _OdinDecoderFree;

        /// <remarks>
        /// const OdinPipeline *odin_decoder_get_pipeline(OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinDecoderGetPipelineDelegate(IntPtr decoder);
        /// <summary>
        /// Returns a pointer to the internal ODIN audio pipeline instance used by the given decoder.
        /// </summary>
        readonly OdinDecoderGetPipelineDelegate _OdinDecoderGetPipeline;

        /// <remarks>
        /// OdinError odin_decoder_get_positions(struct OdinDecoder *decoder, uint64_t channel_mask, struct OdinPosition *out_positions, uint32_t* out_positions_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderGetPositionsDelegate(IntPtr decoder, ulong channel_mask, [In, Out] IntPtr out_positions, [In, Out] ref uint out_positions_length);
        /// <summary>
        /// Retrieves the list of 3D positions associated with the specified channel mask from the most
        /// recently pushed voice packet.
        /// </summary>
        readonly OdinDecoderGetPositionsDelegate _OdinDecoderGetPositions;

        /// <remarks>
        /// UInt64 odin_decoder_get_active_channels(struct OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate UInt64 OdinDecoderGetActiveChannelsDelegate(IntPtr decoder);
        /// <summary>
        /// Returns a bitmask indicating the channels on which the most recently pushed voice packet
        /// was transmitted. If no packet has been processed yet, the value is `0`.
        /// </summary>
        readonly OdinDecoderGetActiveChannelsDelegate _OdinDecoderGetActiveChannels;

        /// <remarks>
        /// bool odin_decoder_is_silent(struct OdinDecoder *decoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal delegate bool OdinDecoderIsSilentDelegate(IntPtr decoder);
        /// <summary>
        /// Returns whether the decoder is currently processing silence. This reflects the internal
        /// silence detection state of the decoder, which updates as audio is processed. If the
        /// provided handle is invalid, the function returns `true` for safety.
        /// </summary>
        readonly OdinDecoderIsSilentDelegate _OdinDecoderIsSilent;

        /// <remarks>
        ///  OdinError odin_decoder_set_event_callback(struct OdinDecoder *decoder, enum OdinAudioEvents filter, OdinDecoderEventCallback callback, void* user_data);
        /// <code>typedef void (* OdinDecoderEventCallback) (struct OdinDecoder *decoder, enum OdinAudioEvents events, void* user_data);</code>
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderSetEventCallbackDelegate(IntPtr decoder, OdinAudioEvents filter, OdinDecoderEventCallbackDelegate callback, IntPtr user_data);
        /// <summary>
        /// Registers a callback to receive decoder audio events. The `filter` determines which event
        /// types will trigger the callback, allowing selective handling. Any previously registered
        /// callback is replaced. The callback is invoked with the decoder handle, the combined event
        /// bitmask and the supplied `user_data`.
        /// </summary>
        readonly OdinDecoderSetEventCallbackDelegate _OdinDecoderSetEventCallback;

        /// <remarks>
        /// OdinError odin_decoder_pop(struct OdinDecoder *decoder, float* out_samples, uint32_t out_samples_count, bool* out_is_silent);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderPopDelegate(IntPtr decoder, [In, Out] IntPtr samples, [In, Out] uint samples_count, [MarshalAs(UnmanagedType.I1)] out bool is_silent);
        /// <summary>
        /// Retrieves a block of processed audio samples from the decoder's buffer. The samples are
        /// interleaved floating-point values in the range [-1, 1] and are written into the provided
        /// output buffer. A flag is also set to indicate if the output is silent.
        /// </summary>
        readonly OdinDecoderPopDelegate _OdinDecoderPop;

        /// <remarks>
        /// OdinError odin_decoder_push(struct OdinDecoder *decoder, const uint8_t* datagram, uint32_t datagram_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinDecoderPushDelegate(IntPtr decoder, [In] IntPtr samples, [In] uint samples_count);
        /// <summary>
        /// Pushes an incoming datagram to the specified decoder for processing.
        /// </summary>
        readonly OdinDecoderPushDelegate _OdinDecoderPush;

        /// <remarks>
        /// OdinError odin_encoder_create(uint32_t peer_id, uint32_t sample_rate, bool stereo, OdinEncoder **out_encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderCreateDelegate(uint peer_id, uint sample_rate, [MarshalAs(UnmanagedType.I1)] bool stereo, out IntPtr out_encoder);
        /// <summary>
        /// Creates a new ODIN encoder instance with default settings used to encode audio captured from
        /// local sources, such as a microphone. The encoder encapsulates an ingress resampler using the
        /// given sample rate and channel layout.
        /// </summary>
        readonly OdinEncoderCreateDelegate _OdinEncoderCreate;

        /// <remarks>
        /// OdinError odin_encoder_create_ex(uint32_t peer_id, uint32_t sample_rate, bool stereo, bool application_voip, uint32_t bitrate_kbps, uint32_t packet_loss_perc, uint64_t update_position_interval_ms, struct OdinEncoder **out_encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderCreateExDelegate(uint peer_id, uint sample_rate, [MarshalAs(UnmanagedType.I1)] bool stereo, [MarshalAs(UnmanagedType.I1)] bool application_voip, uint bitrate_kbps, uint packet_loss_perc, ulong update_position_interval_ms, out IntPtr out_encoder);
        /// <summary>
        /// Creates a new ODIN encoder instance for local media streams with extended codec configuration
        /// parameters. In addition to the sample rate and stereo configuration, it allows specification
        /// of whether the application is intended for VoIP, a target bitrate and the encoder's expected
        /// packet loss percentage.
        /// </summary>
        readonly OdinEncoderCreateExDelegate _OdinEncoderCreateEx;

        /// <remarks>
        /// void odin_encoder_free(OdinEncoder *encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinEncoderFreeDelegate(IntPtr encoder);
        /// <summary>
        /// Frees the resources associated with the specified encoder.
        /// </summary>
        readonly OdinEncoderFreeDelegate _OdinEncoderFree;

        /// <remarks>
        /// const OdinPipeline *odin_encoder_get_pipeline(OdinEncoder *encoder);
        /// </remarks> 
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinEncoderGetPipelineDelegate(IntPtr encoder);
        /// <summary>
        /// Returns a pointer to the internal ODIN audio pipeline instance used by the given encoder.
        /// </summary>
        readonly OdinEncoderGetPipelineDelegate _OdinEncoderGetPipeline;

        /// <remarks>
        /// OdinError odin_encoder_set_position(struct OdinEncoder *encoder, uint64_t channel_mask, const struct OdinPosition *position);
        /// </remarks> 
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderSetPositionDelegate(IntPtr encoder, ulong channel_mask, IntPtr position); // [In] weirdly does not generate a const pointer implicitly
        /// <summary>
        /// Updates the 3D position for the specified channel mask. To assign different positions to
        /// multiple masks, call this function once per mask.
        /// </summary>
        readonly OdinEncoderSetPositionDelegate _OdinEncoderSetPosition;

        /// <remarks>
        /// OdinError odin_encoder_clear_position(struct OdinEncoder *encoder, uint64_t channel_mask);
        /// </remarks> 
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderClearPositionDelegate(IntPtr encoder, ulong channel_mask);
        /// <summary>
        /// Clears the 3D position associated with the specified channel mask.
        /// </summary>
        readonly OdinEncoderClearPositionDelegate _OdinEncoderClearPosition;

        /// <remarks>
        /// bool odin_encoder_is_silent(struct OdinEncoder *encoder);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal delegate bool OdinEncoderIsSilentDelegate(IntPtr encoder);
        /// <summary>
        /// Returns whether the encoder is currently processing silence. This reflects the internal
        /// silence detection state of the encoder, which updates as audio is processed. If the
        /// provided handle is invalid, the function returns `true` for safety.
        /// </summary>
        readonly OdinEncoderIsSilentDelegate _OdinEncoderIsSilent;

        /// <remarks>
        ///  OdinError odin_encoder_set_event_callback(struct OdinEncoder *encoder, enum OdinAudioEvents filter, OdinEncoderEventCallback callback, void* user_data);
        /// <code>typedef void (* OdinEncoderEventCallback) (struct OdinEncoder *encoder, enum OdinAudioEvents events, void* user_data);</code>
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderSetEventCallbackDelegate(IntPtr encoder, OdinAudioEvents filter, OdinEncoderEventCallbackDelegate callback, IntPtr user_data);
        /// <summary>
        /// Registers a callback to receive encoder audio events. The `filter` determines which event
        /// types will trigger the callback, allowing selective handling. Any previously registered
        /// callback is replaced. The callback is invoked with the encoder handle, the combined event
        /// bitmask and the supplied `user_data`.
        /// </summary>
        readonly OdinEncoderSetEventCallbackDelegate _OdinEncoderSetEventCallback;

        /// <remarks>
        /// OdinError odin_encoder_pop(struct OdinEncoder *encoder, uint8_t* out_datagram, uint32_t *out_datagram_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderPopDelegate(IntPtr encoder, [In, Out] IntPtr datagram, [In, Out] ref uint out_datagram_length);
        /// <summary>
        /// Retrieves an encoded datagram from the encoder's buffer.
        /// </summary>
        readonly OdinEncoderPopDelegate _OdinEncoderPop;

        /// <remarks>
        /// OdinError odin_encoder_push(struct OdinEncoder *encoder, const float* samples, uint32_t samples_count);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinEncoderPushDelegate(IntPtr encoder, [In] IntPtr samples, uint samples_count);
        /// <summary>
        /// Pushes raw audio samples to the encoder for processing. The provided audio samples, which
        /// must be interleaved floating-point values in the range [-1, 1], are processed through the
        /// encoder's pipeline, allowing any configured effects to be applied prior to encoding.
        /// </summary>
        readonly OdinEncoderPushDelegate _OdinEncoderPush;

        /// <remarks>
        /// const char *odin_error_get_last_error(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinErrorGetLastErrorDelegate();
        /// <summary>
        /// Returns the error message from the last occurred error, if available. If no error is present,
        /// an empty string is returned.
        /// </summary>
        readonly OdinErrorGetLastErrorDelegate _OdinErrorGetLastError;

        /// <remarks>
        /// void odin_error_reset_last_error(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinErrorResetLastErrorDelegate();
        /// <summary>
        /// Resets the last error message by clearing the error buffer.
        /// </summary>
        readonly OdinErrorResetLastErrorDelegate _OdinErrorResetLastError;

        /// <remarks>
        /// OdinError odin_initialize(const char *version);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinInitializeDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string version);
        /// <summary>
        /// Initializes the internal ODIN client runtime with a specified version number, ensuring the correct
        /// header file is employed. The majority of the API functions hinge on an active ODIN runtime.
        /// <remarks>Use `ODIN_VERSION` to supply the `version` argument.</remarks>
        /// </summary>
        readonly OdinInitializeDelegate _OdinInitialize;

        /// <remarks>
        /// uint32_t odin_pipeline_get_effect_count(const OdinPipeline *pipeline);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinPipelineGetEffectCountDelegate(IntPtr pipeline);
        /// <summary>
        /// Retrieves the total number of effects currently in the audio pipeline.
        /// </summary>
        readonly OdinPipelineGetEffectCountDelegate _OdinPipelineGetEffectCount;

        /// <remarks>
        /// OdinError odin_pipeline_get_effect_id(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectIdDelegate(IntPtr pipeline, uint index, out uint out_effect_id);
        /// <summary>
        /// Returns the unique effect identifier from an audio pipeline corresponding to the effect located
        /// at the specified index.
        /// </summary>
        readonly OdinPipelineGetEffectIdDelegate _OdinPipelineGetEffectId;

        /// <remarks>
        /// OdinError odin_pipeline_get_effect_index(const OdinPipeline *pipeline, uint32_t effect_id, uint32_t *out_index);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectIndexDelegate(IntPtr pipeline, uint effect_id, out uint out_index);
        /// <summary>
        /// Searches the specified audio pipeline for the effect with the specified `effect_id` and returns
        /// its current index.
        /// </summary>
        readonly OdinPipelineGetEffectIndexDelegate _OdinPipelineGetEffectIndex;

        /// <remarks>
        /// OdinError odin_pipeline_get_effect_type(const OdinPipeline *pipeline, uint32_t effect_id, OdinEffectType *out_effect_type);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetEffectTypeDelegate(IntPtr pipeline, uint effect_id, out OdinEffectType out_effect_type);
        /// <summary>
        /// Obtains the effect type (VAD, APM, or Custom) for the effect identified by `effect_id`.
        /// </summary>
        readonly OdinPipelineGetEffectTypeDelegate _OdinPipelineGetEffectType;

        /// <remarks>
        /// OdinError odin_pipeline_get_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinVadConfig *out_config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetVadConfigDelegate(IntPtr pipeline, uint effect_id, out OdinVadConfig out_config);
        /// <summary>
        /// Retrieves the configuration for a VAD effect identified by `effect_id` from the specified
        /// audio pipeline.
        /// </summary>
        readonly OdinPipelineGetVadConfigDelegate _OdinPipelineGetVadConfig;

        /// <remarks>
        /// OdinError odin_pipeline_insert_apm_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t playback_sample_rate, bool playback_stereo, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineInsertApmEffectDelegate(IntPtr pipeline, uint index, uint playback_sample_rate, [MarshalAs(UnmanagedType.I1)] bool playback_stereo, out uint out_effect_id);
        /// <summary>
        /// Inserts an Audio Processing Module (APM) effect into the audio pipeline at the specified
        /// index, shifting subsequent effects. An index of `0` inserts at the beginning of the pipeline.
        /// On success, a unique effect identifier is returned.
        /// </summary>
        readonly OdinPipelineInsertApmEffectDelegate _OdinPipelineInsertApmEffect;

        /// <remarks>
        /// OdinError odin_pipeline_insert_custom_effect(const struct OdinPipeline *pipeline, uint32_t index, OdinCustomEffectCallback callback, const void* user_data, uint32_t *out_effect_id);
        /// <code>typedef void (*OdinCustomEffectCallback)(float *samples, uint32_t samples_count, bool* is_silent, const void* user_data);</code>
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError PipelineInsertCustomEffectDelegate(IntPtr pipeline, uint index, OdinCustomEffectCallbackDelegate callback, IntPtr user_data, out uint out_effect_id);
        /// <summary>
        /// Inserts a user-defined custom effect at the specified index in the audio pipeline. The effect
        /// is implemented via a callback function and associated user data. A unique effect identifier
        /// is returned.
        /// </summary>
        readonly PipelineInsertCustomEffectDelegate _OdinPipelineInsertCustomEffect;

        /// <remarks>
        /// OdinError odin_pipeline_insert_vad_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineInsertVadEffectDelegate(IntPtr pipeline, uint index, out uint out_effect_id);
        /// <summary>
        /// Inserts a Voice Activity Detection (VAD) effect into the audio pipeline at the specified
        /// index, shifting subsequent effects. An index of `0` inserts at the beginning of the pipeline.
        /// On success, a unique effect identifier is returned.
        /// </summary>
        readonly OdinPipelineInsertVadEffectDelegate _OdinPipelineInsertVadEffect;

        /// <remarks>
        /// OdinError odin_pipeline_insert_vi_effect(const OdinPipeline *pipeline, uint32_t index, uint32_t *out_effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineInsertViEffectDelegate(IntPtr pipeline, uint index, out uint out_effect_id);
        /// <summary>
        /// Inserts a Voice Isolation (VI) effect into the audio pipeline at the specified index, shifting
        /// subsequent effects. An index of `0` inserts at the beginning of the pipeline. On success, a
        /// unique effect identifier is returned. The effect starts out disabled and needs an explicit
        /// configuration to start filtering.
        /// </summary>
        readonly OdinPipelineInsertViEffectDelegate _OdinPipelineInsertViEffect;

        /// <remarks>
        /// OdinError odin_pipeline_get_vi_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinViConfig *out_config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetViConfigDelegate(IntPtr pipeline, uint effect_id, out OdinViConfig out_config);
        /// <summary>
        /// Retrieves the configuration for a VI effect identified by `effect_id` from the specified
        /// audio pipeline.
        /// </summary>
        readonly OdinPipelineGetViConfigDelegate _OdinPipelineGetViConfig;

        /// <remarks>
        /// OdinError odin_pipeline_set_vi_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinViConfig* config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetViConfigDelegate(IntPtr pipeline, uint effect_id, ref OdinViConfig config);
        /// <summary>
        /// Updates the configuration settings of the VI effect identified by `effect_id` in the
        /// specified audio pipeline. Re-enabling a disabled effect also resets its internal state.
        /// </summary>
        readonly OdinPipelineSetViConfigDelegate _OdinPipelineSetViConfig;

        /// <remarks>
        /// OdinError odin_pipeline_move_effect(const OdinPipeline *pipeline, uint32_t effect_id, size_t new_index);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineMoveEffectDelegate(IntPtr pipeline, uint effect_id, UIntPtr /*size_t*/ new_index);
        /// <summary>
        /// Reorders the audio pipeline by moving the effect with the specified `effect_id` to a new index.
        /// </summary>
        readonly OdinPipelineMoveEffectDelegate _OdinPipelineMoveEffect;

        /// <remarks>
        /// OdinError odin_pipeline_remove_effect(const OdinPipeline *pipeline, uint32_t effect_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineRemoveEffectDelegate(IntPtr pipeline, uint effect_id);
        /// <summary>
        /// Deletes the effect identified by `effect_id` from the specified audio pipeline.
        /// </summary>
        readonly OdinPipelineRemoveEffectDelegate _OdinPipelineRemoveEffect;

        /// <remarks>
        /// OdinError odin_pipeline_get_apm_config(const OdinPipeline *pipeline, uint32_t effect_id, OdinApmConfig *out_config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineGetApmConfigDelegate(IntPtr pipeline, uint effect_id, out OdinApmConfig out_config);
        /// <summary>
        /// Retrieves the configuration for an APM effect identified by `effect_id` from the specified
        /// audio pipeline.
        /// </summary>
        readonly OdinPipelineGetApmConfigDelegate _OdinPipelineGetApmConfig;

        /// <remarks>
        /// OdinError odin_pipeline_set_apm_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinApmConfig* config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetApmConfigDelegate(IntPtr pipeline, uint effect_id, ref OdinApmConfig config);
        /// <summary>
        /// Updates the configuration settings of the APM effect identified by `effect_id` in the specified
        /// audio pipeline.
        /// </summary>
        readonly OdinPipelineSetApmConfigDelegate _OdinPipelineSetApmConfig;

        /// <remarks>
        /// OdinError odin_pipeline_set_vad_config(const OdinPipeline *pipeline, uint32_t effect_id, const OdinVadConfig* config);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineSetVadConfigDelegate(IntPtr pipeline, uint effect_id, ref OdinVadConfig config);
        /// <summary>
        /// Updates the configuration settings of the VAD effect identified by `effect_id` in the specified
        /// audio pipeline.
        /// </summary>
        readonly OdinPipelineSetVadConfigDelegate _OdinPipelineSetVadConfig;

        /// <remarks>
        /// OdinError odin_pipeline_update_apm_playback(const struct OdinPipeline *pipeline, uint32_t effect_id, const float* samples, uint32_t samples_count, uint64_t delay_ms);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinPipelineUpdateApmPlaybackDelegate(IntPtr pipeline, uint effect_id, [In] IntPtr samples, uint samples_count, UInt64 delay_ms);
        /// <summary>
        /// Updates the specified APM effect's sample buffer for processing the reverse (playback) audio
        /// stream. The provided samples must be interleaved float values in the range [-1, 1]. The delay
        /// parameter is used to align the reverse stream processing with the forward (capture) stream.
        /// <para>
        /// The delay can be expressed as:
        /// </para>
        ///
        ///  <code>`delay = (t_render - t_analyze) + (t_process - t_capture)`</code>
        ///  
        /// where:
        /// <list type="bullet">
        /// <item>
        /// <term><c>t_render</c></term>
        /// <description>is the time the first sample of the same frame is rendered by the audio hardware.</description>
        /// </item>
        /// <item>
        /// <term><c>t_analyze</c></term>
        /// <description>is the time the frame is processed in the reverse stream.</description>
        /// </item>
        /// <item>
        /// <term><c>t_capture</c></term>
        /// <description>is the time the first sample of a frame is captured by the audio hardware.</description>
        /// </item>
        /// <item>
        /// <term><c>t_process</c></term>
        /// <description>is the time the frame is processed in the forward stream.</description>
        /// </item>
        /// </list>
        /// </summary>
        readonly OdinPipelineUpdateApmPlaybackDelegate _OdinPipelineUpdateApmPlayback;

        /// <remarks>
        /// void odin_room_close(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomCloseDelegate(IntPtr room);
        /// <summary>
        /// Closes the specified ODIN room handle, thus making our own peer leave the room on the server
        /// and closing the connection if needed. To release resources, call `odin_room_free` afterwards.
        /// </summary>
        readonly OdinRoomCloseDelegate _OdinRoomClose;

        /// <remarks>
        /// OdinError odin_room_create(const char *gateway, const char* authentication, struct OdinRoomEvents *events, struct OdinCipher *cipher, struct OdinRoom **out_room)
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomCreateDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string gateway, [MarshalAs(UnmanagedType.LPUTF8Str)] string authentication, [In] ref OdinRoomEvents events, [In] IntPtr cipher, out IntPtr out_room);
        /// <summary>
        /// Creates a new ODIN room handle and starts the asynchronous connection process. This requires
        /// the address of an ODIN gateway and an authentication string, which must either be a valid and
        /// signed JSON Web Token (JWT) or a serialized JSON object containing the token, initial peer user
        /// data, channel masks and other optional parameters. On success, a new room handle is stored in
        /// `out_room`. If provided, the room events struct enables delivery of room-specific callbacks
        /// to notify your application about incoming datagrams or RPCs. Additionally, an optional ODIN
        /// cipher plugin can be provided to enable end-to-end encryption of room communications.
        /// </summary>
        readonly OdinRoomCreateDelegate _OdinRoomCreate;

        /// <remarks>
        /// void odin_room_free(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomFreeDelegate(IntPtr room);
        /// <summary>
        /// Destroys the specified ODIN room handle and releases all underlying resources, even if the
        /// room is still connecting.
        /// </summary>
        readonly OdinRoomFreeDelegate _OdinRoomFree;

        /// <remarks>
        /// uint32_t odin_room_get_connection_id(OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomGetConnectionIdDelegate(IntPtr room);
        /// <summary>
        /// Retrieves the underlying connection identifier associated with the room, or `0` if no valid
        /// connection exists. The identifier is a 32-bit value.
        /// </summary>
        readonly OdinRoomGetConnectionIdDelegate _OdinRoomGetConnectionId;

        /// <remarks>
        /// OdinError odin_room_get_connection_stats(struct OdinRoom *room, struct OdinConnectionStats *out_stats);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomGetConnectionStatsDelegate(IntPtr room, out OdinConnectionStats out_stats);
        /// <summary>
        /// Retrieves detailed connection statistics for the specified room, filling the provided structure
        /// with data such as the number of transmitted/received datagrams, bytes and round-trip time.
        /// </summary>
        readonly OdinRoomGetConnectionStatsDelegate _OdinRoomGetConnectionStats;

        /// <remarks>
        /// OdinError odin_room_get_name(struct OdinRoom *room, char *out_value, uint32_t *out_value_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomGetNameDelegate(IntPtr room, [In, Out] IntPtr value, ref uint value_length);
        /// <summary>
        /// Retrieves the name from the specified room.
        /// </summary>
        readonly OdinRoomGetNameDelegate _OdinRoomGetName;

        /// <remarks>
        /// OdinError odin_room_resend_user_data(struct OdinRoom *room);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomResendUserDataDelegate(IntPtr room);
        /// <summary>
        /// Flushes the local peer's user data by re-sending it to the server, ensuring that the latest
        /// data is synchronized across all connected peers. This function does NOT need to be invoked
        /// manually. It is typically used internally by an ODIN cipher after encryption key rotations
        /// to update and maintain data consistency.
        /// </summary>
        readonly OdinRoomResendUserDataDelegate _OdinRoomResendUserData;

        /// <remarks>
        /// OdinError odin_room_send_datagram(struct OdinRoom *room, const uint8_t* bytes, uint32_t bytes_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendDatagramDelegate(IntPtr room, [In] IntPtr bytes, uint bytes_length);
        /// <summary>
        /// Sends an encoded voice packet to the server for the specified room.
        /// </summary>
        readonly OdinRoomSendDatagramDelegate _OdinRoomSendDatagram;

        /// <remarks>
        /// OdinError odin_room_send_loopback_rpc(struct OdinRoom *room, const char *json);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendLoopbackRpcDelegate(IntPtr room, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        /// <summary>
        /// Sends a JSON-encoded RPC message using a local loopback mechanism. It bypasses network
        /// transmission by directly invoking the room’s RPC callback from `OdinRoomEvents`. This is
        /// useful for emitting synthetic events for testing and internal processing without involving
        /// the network layer.
        /// </summary>
        readonly OdinRoomSendLoopbackRpcDelegate _OdinRoomSendLoopbackRpc;

        /// <remarks>
        /// OdinError odin_room_send_rpc(struct OdinRoom *room, const char *json);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinRoomSendRpcDelegate(IntPtr room, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        /// <summary>
        /// Sends a JSON-encoded RPC message to the server for the specified room.
        /// </summary>
        readonly OdinRoomSendRpcDelegate _OdinRoomSendRpc;

        /// <remarks>
        /// void odin_shutdown(void);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinShutdownDelegate();
        /// <summary>
        /// Shuts down the internal ODIN runtime including all active connection pools. It is advisable to
        /// invoke this function prior to terminating your application.
        /// </summary>
        readonly OdinShutdownDelegate _OdinShutdown;

        /// <remarks>
        /// OdinError odin_token_generator_create(const char *access_key, OdinTokenGenerator** out_token_generator);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorCreateDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string access_key, out IntPtr out_token_generator);
        /// <summary>
        /// Creates a new token generator using the specified ODIN access key. If no access key is provided,
        /// a new one will be generated.
        /// </summary>
        readonly OdinTokenGeneratorCreateDelegate _OdinTokenGeneratorCreate;

        /// <remarks>
        /// void odin_token_generator_free(OdinTokenGenerator *token_generator);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinTokenGeneratorFreeDelegate(IntPtr token_generator);
        /// <summary>
        /// Frees the specified token generator and releases the resources associated with it.
        /// </summary>
        readonly OdinTokenGeneratorFreeDelegate _OdinTokenGeneratorFree;

        /// <remarks>
        /// OdinError odin_token_generator_get_access_key(struct OdinTokenGenerator *token_generator, char* out_access_key, uint32_t *out_access_key_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorGetAccessKeyDelegate(IntPtr token_generator, [In, Out] IntPtr access_key, ref uint access_key_length);
        /// <summary>
        /// Retrieves the ODIN access key used by the specified token generator. An ODIN access key is a 44
        /// character long Base64 string that consists of an internal version number, a set of random bytes
        /// and a checksum.
        /// </summary>
        readonly OdinTokenGeneratorGetAccessKeyDelegate _OdinTokenGeneratorGetAccessKey;

        /// <remarks>
        /// OdinError odin_token_generator_get_key_id(struct OdinTokenGenerator *token_generator, char* out_key_id, uint32_t *out_key_id_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorGetKeyIdDelegate(IntPtr token_generator, [In, Out] IntPtr key_id, ref uint key_id_length);
        /// <summary>
        /// Extracts the key ID from the access key used by the specified token generator. The key ID is
        /// embedded in room tokens, enabling the identification of the corresponding public key required
        /// for verification.
        /// </summary>
        readonly OdinTokenGeneratorGetKeyIdDelegate _OdinTokenGeneratorGetKeyId;

        /// <remarks>
        /// OdinError odin_token_generator_get_public_key(struct OdinTokenGenerator *token_generator, char* out_public_key, uint32_t *out_public_key_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorGetPublicKeyDelegate(IntPtr token_generator, [In, Out] IntPtr public_key, ref uint public_key_length);
        /// <summary>
        /// Extracts the public key from the access key used by the specified token generator. The public
        /// key, derived from the Ed25519 curve, must be shared with _4Players_ to enable verification of
        /// a generated room token.
        /// </summary>
        readonly OdinTokenGeneratorGetPublicKeyDelegate _OdinTokenGeneratorGetPublicKey;

        /// <remarks>
        /// OdinError odin_token_generator_sign(struct OdinTokenGenerator *token_generator, const char* body, char* out_token, uint32_t *out_token_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinTokenGeneratorSignDelegate(IntPtr token_generator, [In, MarshalAs(UnmanagedType.LPUTF8Str)] string body, [In, Out] IntPtr token, ref uint token_length);
        /// <summary>
        /// Signs the provided body using the key ID and access key stored in the token generator, producing
        /// a JSON Web Token (JWT). The EdDSA (Ed25519) algorithm is used for the digital signature.
        /// </summary>
        readonly OdinTokenGeneratorSignDelegate _OdinTokenGeneratorSign;

        /// <remarks>
        /// OdinError odin_socket_create(struct OdinRoom *room, enum OdinSocketKind kind, uint32_t remote_peer_id, int32_t label, int32_t priority, struct OdinSocket **out_socket);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinSocketCreateDelegate(IntPtr room, [In] OdinSocketKind kind, uint remotePeerId, int label, int priority, out IntPtr out_socket);
        /// <summary>
        /// Create socket
        /// </summary>
        readonly OdinSocketCreateDelegate _OdinSocketCreate;

        /// <remarks>
        /// OdinError odin_socket_info(struct OdinSocket *socket, struct OdinSocketInfo *out_socket_info);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinSocketInfoDelegate(IntPtr socket, out RawOdinSocketInfo out_socket_info);
        /// <summary>
        /// Get socket info
        /// </summary>
        readonly OdinSocketInfoDelegate _OdinSocketInfo;

        /// <remarks>
        /// OdinError odin_socket_send(struct OdinSocket *socket, const uint8_t* message, uint32_t message_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinSocketSendDelegate(IntPtr socket, [In] IntPtr message, uint message_length);
        /// <summary>
        /// Socket send
        /// </summary>
        readonly OdinSocketSendDelegate _OdinSocketSend;

        /// <remarks>
        /// OdinError odin_socket_reset(struct OdinSocket *socket);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinError OdinSocketResetDelegate(IntPtr socket);
        /// <summary>
        /// Socket reset
        /// </summary>
        readonly OdinSocketResetDelegate _OdinSocketReset;
    }
}
