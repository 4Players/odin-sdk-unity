using OdinNative.Core.Handles;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// Import odin_crypto function signatures to wrapper delegates
    /// </summary>
    public partial class NativeCryptoMethods : NativeMethods<OdinCryptoHandle>
    {
        public NativeCryptoMethods(OdinCryptoHandle handle) : base(handle)
        {
            handle.GetLibraryMethod("odin_crypto_create", out _OdinCryptoCreate);
            handle.GetLibraryMethod("odin_crypto_get_peer_status", out _OdinCryptoGetPeerStatus);
            handle.GetLibraryMethod("odin_crypto_set_password", out _OdinCryptoSetPassword);
        }

        /// <remarks>
        /// OdinCipher *odin_crypto_create(const char *version);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinCryptoCreateDelegate(string version);
        readonly OdinCryptoCreateDelegate _OdinCryptoCreate;
        /// <remarks>
        /// OdinCryptoPeerStatus odin_crypto_get_peer_status(OdinCipher *cipher, uint64_t peer_id);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate OdinCryptoPeerStatus OdinCryptoGetPeerStatusDelegate(OdinCipherHandle cipher, ulong peer_id);
        readonly OdinCryptoGetPeerStatusDelegate _OdinCryptoGetPeerStatus;
        /// <remarks>
        /// int32_t odin_crypto_set_password(OdinCipher *cipher, const uint8_t* password, uint32_t password_length);
        /// </remarks>
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinCryptoSetPasswordDelegate(IntPtr cipher, [In] IntPtr password, uint password_length);
        readonly OdinCryptoSetPasswordDelegate _OdinCryptoSetPassword;
    }
}
