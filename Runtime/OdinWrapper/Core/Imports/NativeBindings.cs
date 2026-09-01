using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// C# bindings for the native ODIN runtime
    /// </summary>
    public static class NativeBindings
    {
        /// <summary>
        /// ODIN_VERSION
        /// </summary>
        public const string OdinLibraryVersion = "2.2.4";
        /// <summary>
        /// ODIN_CRYPTO_VERSION
        /// </summary>
        public const string OdinCryptoVersion = "2.1.0";

        #region NativeLibrary

        /// <summary>
        /// Defines standard error codes returned by ODIN functions. Non-negative values indicate success
        /// or non-error states while negative values indicate errors.
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_NO_DATA"/> </term>
        ///         <description>Successful status code equivalent with "end of data"</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> </term>
        ///         <description>Successful status code generic OK</description>
        ///     </item>
        /// </list>
        /// </summary>
        public enum OdinError
        {
            /// <summary>
            /// <c>Successful</c> status code OK
            /// </summary>
            ODIN_ERROR_SUCCESS = 0,
            /// <summary>
            /// <c>Successful</c> status code equivalent with "end of data"
            /// </summary>
            ODIN_ERROR_NO_DATA = 1,
            /**
             * The runtime initialization failed.
             */
            ODIN_ERROR_INITIALIZATION_FAILED = -1,
            /**
             * The specified API version is not supported.
             */
            ODIN_ERROR_UNSUPPORTED_VERSION = -2,
            /**
             * The object is in an unexpected state.
             */
            ODIN_ERROR_UNEXPECTED_STATE = -3,
            /**
             * The object is closed.
             */
            ODIN_ERROR_CLOSED = -4,
            /**
             * A mandatory argument is null.
             */
            ODIN_ERROR_ARGUMENT_NULL = -11,
            /**
             * A provided argument is too small.
             */
            ODIN_ERROR_ARGUMENT_TOO_SMALL = -12,
            /**
             * A provided argument is out of the expected bounds.
             */
            ODIN_ERROR_ARGUMENT_OUT_OF_BOUNDS = -13,
            /**
             * A provided string argument is not valid UTF-8.
             */
            ODIN_ERROR_ARGUMENT_INVALID_STRING = -14,
            /**
             * A provided handle argument is invalid.
             */
            ODIN_ERROR_ARGUMENT_INVALID_HANDLE = -15,
            /**
             * A provided identifier argument is invalid.
             */
            ODIN_ERROR_ARGUMENT_INVALID_ID = -16,
            /**
             * A provided JSON argument is invalid.
             */
            ODIN_ERROR_ARGUMENT_INVALID_JSON = -17,
            /*
             * A provided OdinCipher argument is invalid.
             */
            ODIN_ERROR_ARGUMENT_INVALID_CIPHER = -18,
            /*
             * A provided argument is too large.
             */
            ODIN_ERROR_ARGUMENT_TOO_LARGE = -19,
            /**
             * The provided version is invalid.
             */
            ODIN_ERROR_INVALID_VERSION = -21,
            /**
             * The provided access key is invalid.
             */
            ODIN_ERROR_INVALID_ACCESS_KEY = -22,
            /**
             * The provided gateway/server address is invalid.
             */
            ODIN_ERROR_INVALID_URI = -23,
            /**
             * The provided token is invalid.
             */
            ODIN_ERROR_INVALID_TOKEN = -24,
            /**
             * The provided effect is not compatible with the expected effect type.
             */
            ODIN_ERROR_INVALID_EFFECT = -25,
            /**
             * The provided MessagePack encoded bytes are invalid.
             */
            ODIN_ERROR_INVALID_MSG_PACK = -26,
            /**
             * The provided JSON string is invalid.
             */
            ODIN_ERROR_INVALID_JSON = -27,
            /**
             * The provided token does not grant access to the requested room.
             */
            ODIN_ERROR_TOKEN_ROOM_REJECTED = -31,
            /**
             * The token is missing a customer identifier.
             */
            ODIN_ERROR_TOKEN_MISSING_CUSTOMER = -32,
            /**
             * The audio processing module reported an error.
             */
            ODIN_ERROR_AUDIO_PROCESSING_FAILED = -41,
            /**
             * The setup process of the Opus audio codec reported an error.
             */
            ODIN_ERROR_AUDIO_CODEC_CREATION_FAILED = -42,
            /**
             * Encoding of an audio packet failed.
             */
            ODIN_ERROR_AUDIO_ENCODING_FAILED = -43,
            /**
             * Decoding of an audio packet failed.
             */
            ODIN_ERROR_AUDIO_DECODING_FAILED = -44,
            /**
             * Position limit reached.
             */
            ODIN_ERROR_AUDIO_POSITION_LIMIT_REACHED = -45,
            /**
             * The voice isolation effect reported an error.
             */
            ODIN_ERROR_AUDIO_VOICE_ISOLATION_FAILED = -46,
        }
        /**
         * Valid levels for aggressiveness of the noise suppression. A higher level will reduce the noise
         * level at the expense of a higher speech distortion.
         */
        public enum OdinNoiseSuppressionLevel {
            /**
             * Disable noise suppression.
             */
            ODIN_NOISE_SUPPRESSION_LEVEL_NONE = 0,
            /**
             * Use low suppression (6 dB).
             */
            ODIN_NOISE_SUPPRESSION_LEVEL_LOW = 1,
            /**
             * Use moderate suppression (12 dB).
             */
            ODIN_NOISE_SUPPRESSION_LEVEL_MODERATE = 2,
            /**
             * Use high suppression (18 dB).
             */
            ODIN_NOISE_SUPPRESSION_LEVEL_HIGH = 3,
            /**
             * Use very high suppression (21 dB).
             */
            ODIN_NOISE_SUPPRESSION_LEVEL_VERY_HIGH = 4,
        }
        /**
         * Audio-related events emitted by encoders and decoders. These values act as bitflags and
         * can be combined to filter or report multiple events.
         */
        public enum OdinAudioEvents
        {
            ///
            /// <summary>
            /// Triggered when the silence state changes.
            /// </summary>
            ODIN_AUDIO_EVENTS_IS_SILENT_CHANGED = 1,
            /// <summary>
            /// Triggered when 3D channel positions are added, updated or removed
            /// </summary>
            ODIN_AUDIO_EVENTS_POSITIONS_CHANGED = 2,
            /// <summary>
            /// Enables all available audio events.
            /// </summary>
            ODIN_AUDIO_EVENTS_ALL = -1,
        }
        /**
         * Available versions of the automatic gain controller (AGC) to be used. This adjusts the audio
         * signal's amplitude to reach a target level, helping to maintain a consistent output.
         */
        public enum OdinGainControllerVersion {
            /// <summary>
            /// AGC is disabled. The signal is untouched.
            /// </summary>
            ODIN_GAIN_CONTROLLER_VERSION_DISABLED = 0,
            /// <summary>
            /// Legacy AGC with adaptive digital gain and limiter.
            /// </summary>
            ODIN_GAIN_CONTROLLER_VERSION_V1 = 1,
            /// <summary>
            /// Enhanced AGC with digital processing and input volume control.
            /// </summary>
            ODIN_GAIN_CONTROLLER_VERSION_V2 = 2,
        }
        
        /**
         * Defines the types of audio pipeline effects available.
         */
        public enum OdinEffectType {
            /**
             * Voice Activity Detection (VAD) for detecting speech segments in an audio stream.
             */
            ODIN_EFFECT_TYPE_VAD = 0,
            /**
             * Audio Processing Module (APM) for enhancements such as noise suppression or echo cancellation.
             */
            ODIN_EFFECT_TYPE_APM = 1,
            /**
             * Voice Isolation (VI) for separating speech from background sound.
             */
            ODIN_EFFECT_TYPE_VI = 2,
            /**
             * Custom user-defined audio processing effect that can be injected into the audio pipeline.
             */
            ODIN_EFFECT_TYPE_CUSTOM = 3,
        }

        /**
        * Defines the types of socket handling.
        */
        public enum OdinSocketKind
        {
            ODIN_SOCKET_TYPE_RELIABLE,
            ODIN_SOCKET_TYPE_UNRELIABLE,
        }

        /// <summary>
        /// Pipeline configuration of the ODIN audio processing module which provides a variety of smart
        /// enhancement algorithms.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinApmConfig
        {
            /// <summary>
            /// When enabled the echo canceller will try to subtract echoes, reverberation and unwanted
            /// added sounds from the audio input signal. Note that you need to process the reverse audio
            /// stream, also known as the loopback data to be used in the ODIN echo canceller.
            /// </summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool echo_canceller;
            /// <summary>
            /// When enabled, the high-pass filter will remove low-frequency content from the input audio
            /// signal, thus making it sound cleaner and more focused.
            /// </summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool high_pass_filter;
            /// <summary>
            /// When enabled, the transient suppressor will try to detect and attenuate keyboard clicks.
            /// </summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool transient_suppressor;
            /// <summary>
            /// When enabled, the noise suppressor will remove distracting background noise from the input
            /// audio signal. You can control the aggressiveness of the suppression. Increasing the level
            /// will reduce the noise level at the expense of a higher speech distortion.
            /// </summary>
            public OdinNoiseSuppressionLevel noise_suppression;
            /// <summary>
            /// When enabled, the gain controller will bring the input audio signal to an appropriate range
            /// when it's either too loud or too quiet.
            /// </summary>
            public OdinGainControllerVersion gain_controller_version;
        };

        /**
         * Information attached to an incoming voice datagram. Provides information about the source
         * peer, the logical channel(s) it was transmitted on and internal transport identifiers. This
         * allows applications to distinguish streams, handle per-channel positioning, or perform custom
         * routing and filtering.
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinDatagramProperties {
            public uint peer_id;
            public ulong channel_mask;
            public uint ssrc_id;
            // RESERVED
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] reserved;
        }
        
        /// <summary>
        /// Defines a wrapper for custom effect callbacks. The callback receives a buffer of audio samples,
        /// a flag indicating whether the audio is silent. This allows for custom, in-place
        /// processing of an audio stream with serialization/deserialization of unmanaged userdata.
        /// For faster native use see:
        /// <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinCustomEffectCallbackDelegate"/>
        /// </summary>
        /// <remarks>
        /// Which defines the signature for custom effect callbacks in an ODIN audio pipeline. The callback
        /// receives a pointer to a buffer of audio samples, the number of samples in the buffer, a
        /// pointer to a flag indicating whether the audio is silent.
        /// </remarks>
        /// <typeparam name="T">custom user data (no GC by CLR)</typeparam>
        public delegate void PipelineCallback<T>(OdinTArray<float> array, ref bool isSilent, T userData);
        /// <summary>
        /// Pinned Buffer (for an unmanaged-compatible type T)
        /// </summary>
        /// <remarks>
        /// Uses non-generic Marshal. To avoid <c>FatalExecutionEngineError</c> on some platforms.
        /// </remarks>
        public class OdinTArray<T> : IDisposable where T : struct
        {
            internal IntPtr bufferPtr;
            internal T[] buffer;
            private GCHandle bufferHandle;

            internal OdinTArray() : this(0u) { }
            internal OdinTArray(uint size) : this(new T[size]) { }

            internal OdinTArray(IntPtr bufferPtr, int index = 0, int length = 0)
            {
                this.bufferPtr = bufferPtr;
                GetPtrBuffer(index, length);
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }

            public OdinTArray(T[] buffer) { SetBuffer(buffer); }

            public void SetBuffer(T[] buffer)
            {
                FreeHandle();
                this.buffer = buffer;
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }

            public void SetBuffer(IntPtr bufferPtr, int length)
            {
                FreeHandle();
                this.bufferPtr = bufferPtr;
                GetPtrBuffer(0, length);
            }

            public Span<T> GetBuffer() => new Span<T>(this.buffer);
            public Span<T> GetBuffer(int index) => GetBuffer().Slice(index);
            public Span<T> GetBuffer(int index, int length) => GetBuffer().Slice(index, length);

            /// <summary>
            /// Copies the managed buffer back to the native pointer.
            /// </summary>
            public void FlushBuffer() => SetPtrBuffer(0, buffer.Length);

            private void GetPtrBuffer(int index, int count)
            {
                int size = index + count;
                if (buffer == null || size > buffer.Length)
                    buffer = new T[size];

                //non-generic to prevent "FatalExecutionEngineError" (sourcePointer, destArray, index, length)
                switch (buffer)
                {
                    case byte[] b: Marshal.Copy(bufferPtr, b, index, count); break;
                    case float[] f: Marshal.Copy(bufferPtr, f, index, count); break;
                    case int[] i: Marshal.Copy(bufferPtr, i, index, count); break;
                    case long[] l: Marshal.Copy(bufferPtr, l, index, count); break;
                    case short[] s: Marshal.Copy(bufferPtr, s, index, count); break;
                    case double[] d: Marshal.Copy(bufferPtr, d, index, count); break;
                    default:
                        // Fallback for other blittable structs (safe as long as T is truly blittable).
                        int typeSize = Marshal.SizeOf<T>();
                        int byteCount = count * typeSize;
                        byte[] tmp = new byte[byteCount];
                        Marshal.Copy(bufferPtr + index * typeSize, tmp, 0, byteCount);
                        Buffer.BlockCopy(tmp, 0, buffer, index * typeSize, byteCount);
                        break;
                }
            }

            private void SetPtrBuffer(int index, int length)
            {
                //non-generic to prevent "FatalExecutionEngineError" (sourcePointer, destArray, index, length)
                switch (buffer)
                {
                    case byte[] b: Marshal.Copy(b, index, bufferPtr, length); break;
                    case float[] f: Marshal.Copy(f, index, bufferPtr, length); break;
                    case int[] i: Marshal.Copy(i, index, bufferPtr, length); break;
                    case long[] l: Marshal.Copy(l, index, bufferPtr, length); break;
                    case short[] s: Marshal.Copy(s, index, bufferPtr, length); break;
                    case double[] d: Marshal.Copy(d, index, bufferPtr, length); break;
                    default:
                        int typeSize = Marshal.SizeOf<T>();
                        int byteCount = length * typeSize;
                        byte[] tmp = new byte[byteCount];
                        Buffer.BlockCopy(buffer, index * typeSize, tmp, 0, byteCount);
                        Marshal.Copy(tmp, 0, bufferPtr + index * typeSize, byteCount);
                        break;
                }
            }

            private void FreeHandle()
            {
                if (bufferHandle.IsAllocated)
                    bufferHandle.Free();
            }

            public void Dispose()
            {
                if (bufferHandle.IsAllocated)
                {
                    bufferHandle.Free();
                    buffer = null;
                    bufferPtr = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Sensitivity parameters for ODIN voice activity detection module configuration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinSensitivityConfig
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool enabled;
            /// <summary>
            /// The threshold at which the trigger should engage.
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float attack_threshold;
            /// <summary>
            /// The threshold at which the trigger should disengage.
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float release_threshold;
        };

        /// <summary>
        /// Pipeline configuration of the ODIN voice activity detection module, which offers advanced
        /// algorithms to accurately determine when to start or stop transmitting audio data.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinVadConfig
        {
            /// <summary>
            /// When enabled, ODIN will analyze the audio input signal using smart voice detection algorithms
            /// to determine the presence of speech. You can define both the probability required to start
            /// and stop transmitting.
            /// </summary>
            public OdinSensitivityConfig voice_activity;
            /// <summary>
            /// When enabled, ODIN will measure the volume of the input audio signal, deciding when a user
            /// speaks loudly enough to transmit voice data. You can define both the root-mean-square
            /// power (dBFS) for when the gate should engage and disengage.
            /// </summary>
            public OdinSensitivityConfig volume_gate;
        };

        /// <summary>
        /// Pipeline configuration of the Voice Isolation (VI) effect, which uses a deep-learning noise
        /// suppression model to separate speech from background sound in the capture signal.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinViConfig
        {
            /// <summary>
            /// When enabled, the effect will attenuate non-speech portions of the input audio signal.
            /// Re-enabling a disabled effect also resets its internal state.
            /// </summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool enabled;
            /// <summary>
            /// Maximum attenuation applied to non-speech in dB; values of `100` and above remove
            /// background sound entirely, while values close to `0` disable noise reduction.
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float attenuation_limit_db;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinConnectionStats
        {
            /**
             * The amount of outgoing UDP datagrams observed
             */
            public ulong UdpTxDatagrams;
            /**
             * The total amount of bytes which have been transferred inside outgoing UDP datagrams
             */
            public ulong UdpTxBytes;
            /**
             * The amount of incoming UDP datagrams observed
             */
            public ulong UdpRxDatagrams;
            /**
             * The total amount of bytes which have been transferred inside incoming UDP datagrams
             */
            public ulong UdpRxBytes;
            /**
             * Current best estimate of the connection latency (round-trip-time) in milliseconds
             */
            public float Rtt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawOdinSocketInfo
        {
            public IntPtr RawRoomHandle;
            public OdinSocketKind Kind;
            [MarshalAs(UnmanagedType.I1)]
            public bool IsInbound;
            public uint RemotePeerId;
            public int Label;
            public int Priority;
            public uint UnsentBytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinSocketInfo
        {
            public OdinRoomHandle RoomHandle;
            public OdinSocketKind Kind;
            [MarshalAs(UnmanagedType.I1)]
            public bool IsInbound;
            public uint RemotePeerId;
            public int Label;
            public int Priority;
            public uint UnsentBytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinPosition
        {
            public float X;
            public float Y;
            public float Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinRoomEvents
        {
            /// <summary>
            /// void (*on_datagram)(struct OdinRoom *room, const struct OdinDatagramProperties *properties, const uint8_t* bytes, uint32_t bytes_length, void* user_data);
            /// </summary>
            public IntPtr OnDatagram;
            /// <summary>
            /// void (*on_rpc)(struct OdinRoom *room, const char *json, void *user_data);
            /// </summary>
            public IntPtr OnRpc;
            /// <summary>
            /// void (*on_socket)(struct OdinSocket *socket, const uint8_t* message, uint32_t message_length, void* user_data);
            /// </summary>
            public IntPtr OnSocket;
            /// <summary>
            /// void* user_data;
            /// </summary>
            public IntPtr UserDataPtr;

            public OdinRoomEvents(OdinOnDatagramDelegate onDatagram, OdinOnRPCDelegate onRpc, OdinOnSocketDelegate onSocket, IntPtr userData = default)
            {
                OnDatagram = onDatagram != null ? Marshal.GetFunctionPointerForDelegate(onDatagram) : IntPtr.Zero;
                OnRpc = onRpc != null ? Marshal.GetFunctionPointerForDelegate(onRpc) : IntPtr.Zero;
                OnSocket = onSocket != null ? Marshal.GetFunctionPointerForDelegate(onSocket) : IntPtr.Zero;
                UserDataPtr = userData;
            }
        }
        #endregion NativeLibrary

        #region NativeCrypto
        public interface IOdinCipher
        {
            // buffer parameters are raw native pointers valid only for the duration of the
            // call; implementations read/write them via Marshal.Copy up to the given
            // length/capacity (byte[] would need LPArray size attributes the reverse
            // p/invoke marshaller cannot infer here)
            /// <summary>
            /// int32_t(* init) (struct OdinCipher *cipher, struct OdinRoom *room);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int Init(IntPtr cipher, IntPtr room);
            /// <summary>
            /// void (* free) (struct OdinCipher *cipher);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate void Free(IntPtr cipher);
            /// <summary>
            /// void (* on_event) (struct OdinCipher *cipher, const unsigned char* bytes, uint32_t length);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate void OnEvent(IntPtr cipher, IntPtr bytes, uint length);
            /// <summary>
            /// int32_t(*encrypt_datagram)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int EncryptDatagram(IntPtr cipher, IntPtr plaintext, uint plaintext_length, IntPtr ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_datagram)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int DecryptDatagram(IntPtr cipher, uint peer_id, IntPtr ciphertext, uint ciphertext_length, IntPtr plaintext, uint plaintext_capacity);
            /// <summary>
            /// int32_t(*encrypt_message)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int EncryptMessage(IntPtr cipher, IntPtr plaintext, uint plaintext_length, IntPtr ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_message)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int DecryptMessage(IntPtr cipher, uint peer_id, IntPtr ciphertext, uint ciphertext_length, IntPtr plaintext, uint plaintext_capacity);
            /// <summary>
            /// int32_t(*encrypt_user_data)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int EncryptUserData(IntPtr cipher, IntPtr plaintext, uint plaintext_length, IntPtr ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_user_data)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
            public delegate int DecryptUserData(IntPtr cipher, uint peer_id, IntPtr ciphertext, uint ciphertext_length, IntPtr plaintext, uint plaintext_capacity);
        };
        public class OdinCipher : IOdinCipher, IDisposable
        {
            private bool disposedValue;
            private IntPtr _nativePtr;

            /// <summary>
            /// Marshaled counterpart of the native struct OdinCipher; kept separate from the
            /// wrapper class so its management fields never end up in the native layout
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct NativePayload
            {
                public IOdinCipher.Init Init;
                public IOdinCipher.Free Free;
                public IOdinCipher.OnEvent OnEvent;
                public IOdinCipher.EncryptDatagram EncryptDatagram;
                public IOdinCipher.DecryptDatagram DecryptDatagram;
                public IOdinCipher.EncryptMessage EncryptMessage;
                public IOdinCipher.DecryptMessage DecryptMessage;
                public IOdinCipher.EncryptUserData EncryptUserData;
                public IOdinCipher.DecryptUserData DecryptUserData;
                public uint AdditionalCapacityDatagram;
                public uint AdditionalCapacityMessage;
                public uint AdditionalCapacityUserData;
            }

            protected internal IntPtr GetPtr()
            {
                // native keeps the pointer for the lifetime of the cipher, so the
                // unmanaged copy must stay allocated until Dispose
                if (_nativePtr == IntPtr.Zero)
                {
                    NativePayload payload = new NativePayload
                    {
                        Init = Init,
                        Free = Free,
                        OnEvent = OnEvent,
                        EncryptDatagram = EncryptDatagram,
                        DecryptDatagram = DecryptDatagram,
                        EncryptMessage = EncryptMessage,
                        DecryptMessage = DecryptMessage,
                        EncryptUserData = EncryptUserData,
                        DecryptUserData = DecryptUserData,
                        AdditionalCapacityDatagram = AdditionalCapacityDatagram,
                        AdditionalCapacityMessage = AdditionalCapacityMessage,
                        AdditionalCapacityUserData = AdditionalCapacityUserData,
                    };
                    _nativePtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativePayload>());
                    Marshal.StructureToPtr(payload, _nativePtr, false);
                }
                return _nativePtr;
            }

            /// <summary>
            /// int32_t(* init) (struct OdinCipher *cipher, struct OdinRoom *room);
            /// </summary>
            public IOdinCipher.Init Init;
            /// <summary>
            /// void (* free) (struct OdinCipher *cipher);
            /// </summary>
            public IOdinCipher.Free Free;
            /// <summary>
            /// void (* on_event) (struct OdinCipher *cipher, const unsigned char* bytes, uint32_t length);
            /// </summary>
            public IOdinCipher.OnEvent OnEvent;
            /// <summary>
            /// int32_t(*encrypt_datagram)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public IOdinCipher.EncryptDatagram EncryptDatagram;
            /// <summary>
            /// int32_t(*decrypt_datagram)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public IOdinCipher.DecryptDatagram DecryptDatagram;
            /// <summary>
            /// int32_t(*encrypt_message)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public IOdinCipher.EncryptMessage EncryptMessage;
            /// <summary>
            /// int32_t(*decrypt_message)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public IOdinCipher.DecryptMessage DecryptMessage;
            /// <summary>
            /// int32_t(*encrypt_user_data)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public IOdinCipher.EncryptUserData EncryptUserData;
            /// <summary>
            /// int32_t(*decrypt_user_data)(struct OdinCipher *cipher, uint32_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public IOdinCipher.DecryptUserData DecryptUserData;

            /// <summary>
            /// uint32_t additional_capacity_datagram;
            /// </summary>
            public uint AdditionalCapacityDatagram;
            /// <summary>
            /// uint32_t additional_capacity_message;
            /// </summary>
            public uint AdditionalCapacityMessage;
            /// <summary>
            /// uint32_t additional_capacity_user_data;
            /// </summary>
            public uint AdditionalCapacityUserData;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing && _nativePtr != IntPtr.Zero)
                    {
                        Free?.Invoke(_nativePtr);
                    }
                    if (_nativePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_nativePtr);
                        _nativePtr = IntPtr.Zero;
                    }

                    Init = null;
                    Free = null;
                    OnEvent = null;
                    EncryptDatagram = null;
                    DecryptDatagram = null;
                    EncryptMessage = null;
                    DecryptMessage = null;
                    EncryptUserData = null;
                    DecryptUserData = null;

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        };

        /**
         * Represents the encryption status of a remote peer in an ODIN room.
         */
        public enum OdinCryptoPeerStatus
        {
            /**
             * Password does not match the local password, preventing decryption of their data.
             */
            ODIN_CRYPTO_PEER_STATUS_PASSWORD_MISSMATCH = -1,
            /**
             * Encryption status has not yet been determined.
             */
            ODIN_CRYPTO_PEER_STATUS_UNKNOWN = 0,
            /**
             * Remote peer is not using encryption; their data is transmitted in plaintext.
             */
            ODIN_CRYPTO_PEER_STATUS_UNENCRYPTED = 1,
            /**
             * The peer is using encryption and their data can be successfully decrypted.
             */
            ODIN_CRYPTO_PEER_STATUS_ENCRYPTED = 2,
        };

        #endregion NativeCrypto
    }
}
