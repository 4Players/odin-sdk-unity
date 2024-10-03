using System;
using System.Runtime.InteropServices;
using System.Text;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    public partial class NativeCryptoMethods
    {
        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeCryptoMethods.OdinCryptoCreateDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinCipher *odin_crypto_create(const char *version);
        /// </remarks>
        public OdinCipherHandle CryptoCreate(string version = OdinNative.Core.Imports.NativeBindings.OdinCryptoVersion)
        {
            _DbgTrace();
            using (Lock)
                return new OdinCipherHandle(_OdinCryptoCreate(version));
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeCryptoMethods.OdinCryptoGetPeerStatusDelegate"/>
        /// </summary>
        /// <remarks>
        /// OdinCryptoPeerStatus odin_crypto_get_peer_status(OdinCipher *cipher, uint64_t peer_id);
        /// </remarks>
        public OdinCryptoPeerStatus CryptoGetPeerStatus(OdinCipherHandle cipher, ulong peer_id)
        {
            _DbgTrace();
            using (Lock)
                return _OdinCryptoGetPeerStatus(cipher, peer_id);
        }

        /// <summary>
        /// <see cref="OdinNative.Core.Imports.NativeCryptoMethods.OdinCryptoSetPasswordDelegate"/>
        /// </summary>
        /// <remarks>
        /// int32_t odin_crypto_set_password(OdinCipher *cipher, const uint8_t* password, uint32_t password_length);
        /// </remarks>
        public int CryptoSetPassword(OdinCipherHandle cipher, string password)
        {
            _DbgTrace();
            using (Lock)
            {
                if (string.IsNullOrEmpty(password)) 
                    return _OdinCryptoSetPassword(cipher, IntPtr.Zero, 0);

                byte[] bytes = Encoding.UTF8.GetBytes(password);
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return _OdinCryptoSetPassword(cipher, handle.AddrOfPinnedObject(), (uint)bytes.Length);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }
    }
}
