using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// General native cypto cipher
    /// </summary>
    public class Crypto : IDisposable
    {
        /// <summary>
        /// Cipher handle
        /// </summary>
        public OdinCipherHandle Handle { get; private set; }

        /// <summary>
        /// Create Crypto with OdinCipherHandle from Odin.Crypto.Methods
        /// </summary>
        /// <param name="handle">native OdinCipherHandle</param>
        /// <returns>Crypto wrapper</returns>
        public static Crypto Create(OdinCipherHandle handle) => new Crypto(handle);
        private Crypto(OdinCipherHandle handle)
        {
            this.Handle = handle;
        }

        /// <summary>
        /// Create Crypto with a new OdinCipherHandle
        /// </summary>
        /// <returns>new Crypto wrapper</returns>
        public static Crypto Create() => new Crypto(CryptoCreate());
        /// <summary>
        /// Create Crypto with a new OdinCipherHandle and set password
        /// </summary>
        /// <param name="password">cipher password</param>
        /// <returns>new Crypto wrapper</returns>
        public static Crypto Create(string password)
        {
            Crypto crypto = Crypto.Create();
            crypto.CryptoSetPassword(password);
            return crypto;
        }

        protected static OdinCipherHandle CryptoCreate()
        {
            OdinCipherHandle handle = Odin.Crypto.Methods.CryptoCreate();
            if (handle.IsAlive == false)
                Utility.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoCreate)} in {nameof(CryptoCreate)} failed (handle {handle.IsAlive}): {(IntPtr)handle}").ToString());
            return handle;
        }

        /// <summary>
        /// Get peer status of unencrypted/encrypted
        /// </summary>
        /// <param name="peerId">peer to check</param>
        /// <returns>encryption status of peer</returns>
        public virtual OdinCryptoPeerStatus CryptoGetPeerStatus(int peerId)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Crypto.Methods.CryptoGetPeerStatus)} {nameof(OdinCipherHandle)} is released");

            OdinCryptoPeerStatus cryptoStatus = Odin.Crypto.Methods.CryptoGetPeerStatus(Handle, (ulong)peerId);
            if (Utility.IsOk(cryptoStatus) == false)
                Utility.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoGetPeerStatus)} in {nameof(CryptoGetPeerStatus)} failed (handle {Handle.IsAlive}): {(IntPtr)Handle} (code {cryptoStatus})").ToString());
            return cryptoStatus;
        }

        /// <summary>
        /// Set Cipher password for native crypto
        /// </summary>
        /// <param name="password">cipher password</param>
        /// <returns>true on 0 or false</returns>
        public virtual bool CryptoSetPassword(string password)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Crypto.Methods.CryptoSetPassword)} {nameof(OdinCipherHandle)} is released");

            int ret = Odin.Crypto.Methods.CryptoSetPassword(Handle, password);
            if (ret < 0)
                Utility.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoSetPassword)} in {nameof(CryptoSetPassword)} failed (handle {Handle.IsAlive}): {(IntPtr)Handle} (code {ret})").ToString());
            return ret == 0;
        }

        /// <summary>
        /// Cleanup OdinCipherHandle
        /// </summary>
        /// <remarks>this only sets the handle as invalid</remarks>
        public void Dispose()
        {
            Handle?.Dispose();
            Handle = null;
        }
    }
}
