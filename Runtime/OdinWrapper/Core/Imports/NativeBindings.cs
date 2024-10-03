using System;
using System.Runtime.InteropServices;

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
        public const string OdinLibraryVersion = "2.0.0-beta-crypto2";
        /// <summary>
        /// ODIN_CRYPTO_VERSION
        /// </summary>
        public const string OdinCryptoVersion = "1.0.0";

        #region NativeLibrary

        /// <summary>
        /// Odin error codes where negative values are errors and positive values status codes
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_NO_DATA"/></term>
        ///         <description>Successful status code equivalent with "end of data"</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/></term>
        ///         <description>Successful status code generic OK</description>
        ///     </item>
        /// </list>
        /// </summary>
        public enum OdinError
        {
            /// <summary>
            /// Successful status code equivalent with "end of data"
            /// </summary>
            ODIN_ERROR_NO_DATA = 1,
            /// <summary>
            /// Successful status code OK
            /// </summary>
            ODIN_ERROR_SUCCESS = 0,
            ODIN_ERROR_APM_ERROR = -1,
            ODIN_ERROR_ARGUMENT_INVALID_STRING = -2,
            ODIN_ERROR_ARGUMENT_INVALID_HANDLE = -3,
            ODIN_ERROR_ARGUMENT_NULL = -4,
            ODIN_ERROR_ARGUMENT_INVALID_ID = -5,
            ODIN_ERROR_ARGUMENT_OUT_OF_BOUNDS = -6,
            ODIN_ERROR_ARGUMENT_TOO_SMALL = -7,
            ODIN_ERROR_END_ARGUMENT_ERRORS = -8,
            ODIN_ERROR_INITIALIZATION_FAILED = -101,
            ODIN_ERROR_INVALID_ACCESS_KEY = -102,
            ODIN_ERROR_INVALID_GATEWAY_URI = -103,
            ODIN_ERROR_INVALID_STATE = -104,
            ODIN_ERROR_INVALID_TOKEN = -105,
            ODIN_ERROR_INVALID_VERSION = -106,
            ODIN_ERROR_OPUS_ERROR = -107,
            ODIN_ERROR_DECODER_ERROR = -108,
            ODIN_ERROR_TOKEN_ROOM_REJECTED = -109,
            ODIN_ERROR_UNSUPPORTED_VERSION = -110,
            ODIN_ERROR_UNSUPPORTED_EFFECT = -111,
            ODIN_ERROR_CLOSED = -112,
            ODIN_ERROR_ENCODER_ERROR = -113,
        }

        public enum OdinEffectType
        {
            ODIN_EFFECT_TYPE_VAD,
            ODIN_EFFECT_TYPE_APM,
            ODIN_EFFECT_TYPE_CUSTOM,
        }

        /// <summary>
        /// Valid levels for aggressiveness of the noise suppression. A higher level will reduce the noise
        /// level at the expense of a higher speech distortion.
        /// </summary>
        public enum OdinNoiseSuppression
        {
            /// <summary>
            /// Noise suppression is disabled
            /// </summary>
            ODIN_NOISE_SUPPRESSION_NONE,
            /// <summary>
            /// Use low suppression (6 dB)
            /// </summary>
            ODIN_NOISE_SUPPRESSION_LOW,
            /// <summary>
            /// Use moderate suppression (12 dB)
            /// </summary>
            ODIN_NOISE_SUPPRESSION_MODERATE,
            /// <summary>
            /// Use high suppression (18 dB)
            /// </summary>
            ODIN_NOISE_SUPPRESSION_HIGH,
            /// <summary>
            /// Use very high suppression (21 dB)
            /// </summary>
            ODIN_NOISE_SUPPRESSION_VERY_HIGH,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinApmConfig
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool echo_canceller;
            [MarshalAs(UnmanagedType.I1)]
            public bool high_pass_filter;
            [MarshalAs(UnmanagedType.I1)]
            public bool pre_amplifier;
            public OdinNoiseSuppression noise_suppression;
            [MarshalAs(UnmanagedType.I1)]
            public bool transient_suppressor;
            [MarshalAs(UnmanagedType.I1)]
            public bool gain_controller;
        };

        public struct OdinConnectionPoolSettings
        {
            internal NativeLibraryMethods.OdinConnectionPoolOnDatagramDelegate OnDatagram;
            internal NativeLibraryMethods.OdinConnectionPoolOnRPCDelegate OnRPC;
            /// <summary>
            /// <c>IntPtr</c>
            /// </summary>
            public /* void* */ MarshalByRefObject user_data;
        };


        public delegate void PipelineCallback<T>(OdinArrayf array, ref bool isSilent, T userData);
        /// <summary>
        /// Pinned byte Buffer
        /// </summary>
        public class OdinArray : IDisposable
        {
            internal IntPtr bufferPtr;
            internal byte[] buffer;
            private GCHandle bufferHandle;

            internal OdinArray() : this(0) { }
            internal OdinArray(uint size) : this(new byte[size]) { }
            internal OdinArray(IntPtr bufferPtr, int index = 0, int length = 0)
            {
                this.bufferPtr = bufferPtr;
                GetPtrBuffer(index, length);
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }
            public OdinArray(byte[] buffer) { SetBuffer(buffer); }

            public void SetBuffer(byte[] buffer)
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                    bufferHandle.Free();

                this.buffer = buffer;
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }

            public void SetBuffer(IntPtr bufferPtr, int length)
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                    bufferHandle.Free();

                this.bufferPtr = bufferPtr;
                GetPtrBuffer(0, length);
            }

            private void GetPtrBuffer(int index, int count)
            {
                int size = index + count;
                if (buffer == null || size > buffer.Length)
                    buffer = new byte[size];

                //non generic to prevent "FatalExecutionEngineError" (sourcePointer, destArray, index, length)
                Marshal.Copy(bufferPtr, buffer, index, count);
            }

            private void SetPtrBuffer(int index, int length)
            {
                //non generic to prevent "FatalExecutionEngineError" (sourceArray, index, destPointer, length)
                Marshal.Copy(buffer, index, bufferPtr, length);
            }

            public Span<byte> GetBuffer() => new Span<byte>(this.buffer);
            public Span<byte> GetBuffer(int index) => GetBuffer().Slice(index);
            public Span<byte> GetBuffer(int index, int length) => GetBuffer().Slice(index, length);

            public void FlushBuffer()
            {
                SetPtrBuffer(0, buffer.Length);
            }

            public void Dispose()
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                {
                    bufferHandle.Free();
                    buffer = null;
                    bufferPtr = IntPtr.Zero;
                }
            }
        }
        /// <summary>
        /// Pinned float Buffer
        /// </summary>
        public class OdinArrayf : IDisposable
        {
            internal IntPtr bufferPtr;
            private float[] buffer;
            private GCHandle bufferHandle;

            internal OdinArrayf() : this(0) { }
            internal OdinArrayf(uint size) : this(new float[size]) { }
            internal OdinArrayf(IntPtr bufferPtr, int index = 0, int length = 0)
            {
                this.bufferPtr = bufferPtr;
                GetPtrBuffer(index, length);
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }
            public OdinArrayf(float[] buffer) { SetBuffer(buffer); }

            public void SetBuffer(float[] buffer)
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                    bufferHandle.Free();

                this.buffer = buffer;
                bufferHandle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            }

            public void SetBuffer(IntPtr bufferPtr, int length)
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                    bufferHandle.Free();

                this.bufferPtr = bufferPtr;
                GetPtrBuffer(0, length);
            }

            private void GetPtrBuffer(int index, int count)
            {
                int size = index + count;
                if (buffer == null || size > buffer.Length)
                    buffer = new float[size];

                //non generic to prevent "FatalExecutionEngineError" (sourcePointer, destArray, index, length)
                Marshal.Copy(bufferPtr, buffer, index, count);
            }

            private void SetPtrBuffer(int index, int length)
            {
                //non generic to prevent "FatalExecutionEngineError" (sourceArray, index, destPointer, length)
                Marshal.Copy(buffer, index, bufferPtr, length);
            }

            public Span<float> GetBuffer() => new Span<float>(this.buffer);
            public Span<float> GetBuffer(int index) => GetBuffer().Slice(index);
            public Span<float> GetBuffer(int index, int length) => GetBuffer().Slice(index, length);

            public void FlushBuffer()
            {
                SetPtrBuffer(0, buffer.Length);
            }

            public void Dispose()
            {
                if (bufferHandle != null && bufferHandle.IsAllocated)
                {
                    bufferHandle.Free();
                    buffer = null;
                    bufferPtr = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OdinSensitivityConfig
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool enabled;
            /// <summary>
            /// when the trigger should engage
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float attack_threshold;
            /// <summary>
            /// when the trigger should disengage
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float release_threshold;
        };

#pragma warning disable CS0618 // Type or member is obsolete
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinVadConfig
        {
            [MarshalAs(UnmanagedType.Struct)]
            public OdinSensitivityConfig voice_activity;
            [MarshalAs(UnmanagedType.Struct)]
            public OdinSensitivityConfig volume_gate;
        };
#pragma warning restore CS0618 // Type or member is obsolete

        #endregion NativeLibrary

        #region NativeCrypto
        public interface IOdinCipher
        {
            /// <summary>
            /// void (* init) (struct OdinCipher *cipher, struct OdinRoom *room);
            /// </summary>
            public delegate void Init(IntPtr cipher, IntPtr room);
            /// <summary>
            /// void (* free) (struct OdinCipher *cipher);
            /// </summary>
            public delegate void Free(IntPtr cipher);
            /// <summary>
            /// void (* on_event) (struct OdinCipher *cipher, const unsigned char* bytes, uint32_t length);
            /// </summary>
            public delegate void OnEvent(IntPtr cipher, byte[] bytes, uint length);
            /// <summary>
            /// int32_t(*encrypt_datagram)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public delegate int EncryptDatagram(IntPtr cipher, byte[] plaintext, uint plaintext_length, byte[] ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_datagram)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public delegate int DecryptDatagram(IntPtr cipher, UInt64 peer_id, byte[] ciphertext, uint ciphertext_length, byte[] plaintext, uint plaintext_capacity);
            /// <summary>
            /// int32_t(*encrypt_message)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public delegate int EncryptMessage(IntPtr cipher, byte[] plaintext, uint plaintext_length, byte[] ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_message)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public delegate int DecryptMessage(IntPtr cipher, UInt64 peer_id, byte[] ciphertext, uint ciphertext_length, byte[] plaintext, uint plaintext_capacity);
            /// <summary>
            /// int32_t(*encrypt_user_data)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public delegate int EncryptUserData(IntPtr cipher, byte[] plaintext, uint plaintext_length, byte[] ciphertext, uint ciphertext_capacity);
            /// <summary>
            /// int32_t(*decrypt_user_data)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public delegate int DecryptUserData(IntPtr cipher, UInt64 peer_id, byte[] ciphertext, uint ciphertext_length, byte[] plaintext, uint plaintext_capacity);
        };
        [StructLayout(LayoutKind.Sequential)]
        public class OdinCipher : IOdinCipher, IDisposable
        {
            private bool disposedValue;

            protected internal IntPtr GetPtr()
            {
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(this));
                try
                {
                    Marshal.StructureToPtr(this, ptr, false);
                    return ptr;
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            /// <summary>
            /// void (* init) (struct OdinCipher *cipher, struct OdinRoom *room);
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
            /// int32_t(*decrypt_datagram)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public IOdinCipher.DecryptDatagram DecryptDatagram;
            /// <summary>
            /// int32_t(*encrypt_message)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public IOdinCipher.EncryptMessage EncryptMessage;
            /// <summary>
            /// int32_t(*decrypt_message)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
            /// </summary>
            public IOdinCipher.DecryptMessage DecryptMessage;
            /// <summary>
            /// int32_t(*encrypt_user_data)(struct OdinCipher *cipher, const unsigned char* plaintext, uint32_t plaintext_length, unsigned char* ciphertext, uint32_t ciphertext_capacity);
            /// </summary>
            public IOdinCipher.EncryptUserData EncryptUserData;
            /// <summary>
            /// int32_t(*decrypt_user_data)(struct OdinCipher *cipher, uint64_t peer_id, const unsigned char* ciphertext, uint32_t ciphertext_length, unsigned char* plaintext, uint32_t plaintext_capacity);
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
                    if (disposing)
                    {
                        Free?.Invoke(GetPtr());
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

        public enum OdinCryptoPeerStatus
        {
            ODIN_CRYPTO_PEER_STATUS_INVALID_PASSWORD = -1,
            ODIN_CRYPTO_PEER_STATUS_UNKNOWN = 0,
            ODIN_CRYPTO_PEER_STATUS_UNENCRYPTED = 1,
            ODIN_CRYPTO_PEER_STATUS_ENCRYPTED = 2,
        };

        #endregion NativeCrypto
    }
}
