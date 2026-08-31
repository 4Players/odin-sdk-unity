using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Optional, pluggable encryption module for room communications. A Crypto object can be attached to an
    /// ODIN room on creation to enable customizable, end-to-end encryption (E2EE). When enabled,
    /// it intercepts data right before transmission and immediately after reception, allowing custom
    /// processing of datagrams, messages and custom peer user data.
    /// </summary>
    public class Crypto : IDisposable
    {
        /// <summary>
        /// Cipher handle
        /// </summary>
        public OdinCipherHandle Handle { get; private set; }
        // a Crypto wrapping an externally provided handle (e.g. a cipher shared between rooms)
        // must not dispose it; only handles created by this wrapper are owned
        private readonly bool _ownsHandle;

        /// <summary>
        /// Create Crypto with OdinCipherHandle from Odin.Crypto.Methods
        /// </summary>
        /// <remarks>The caller keeps ownership of the handle; <see cref="Dispose"/> will not release it</remarks>
        /// <param name="handle">native OdinCipherHandle</param>
        /// <returns>Crypto wrapper</returns>
        public static Crypto Create(OdinCipherHandle handle) => new Crypto(handle, ownsHandle: false);
        private Crypto(OdinCipherHandle handle, bool ownsHandle)
        {
            this.Handle = handle;
            _ownsHandle = ownsHandle;
        }

        /// <summary>
        /// Create Crypto with a new OdinCipherHandle
        /// </summary>
        /// <returns>new Crypto wrapper</returns>
        public static Crypto Create() => new Crypto(CryptoCreate(), ownsHandle: true);
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
                OdinLog.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoCreate)} in {nameof(CryptoCreate)} failed (handle {handle.IsAlive}): {(IntPtr)handle}").ToString());
            return handle;
        }

        /// <summary>
        /// Get peer status of unencrypted/encrypted
        /// </summary>
        /// <param name="peerId">peer to check</param>
        /// <returns>encryption status of peer</returns>
        public virtual OdinCryptoPeerStatus CryptoGetPeerStatus(int peerId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Crypto.Methods.CryptoGetPeerStatus)} {nameof(OdinCipherHandle)} is released");

            OdinCryptoPeerStatus cryptoStatus = Odin.Crypto.Methods.CryptoGetPeerStatus(Handle, (ulong)peerId);
            if (Utility.IsOk(cryptoStatus) == false)
                OdinLog.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoGetPeerStatus)} in {nameof(CryptoGetPeerStatus)} failed (handle {Handle.IsAlive}): {(IntPtr)Handle} (code {cryptoStatus})").ToString());
            return cryptoStatus;
        }

        /// <summary>
        /// Set Cipher password for native crypto
        /// </summary>
        /// <param name="password">cipher password</param>
        /// <returns>true on success, otherwise false</returns>
        public virtual bool CryptoSetPassword(string password)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Crypto.Methods.CryptoSetPassword)} {nameof(OdinCipherHandle)} is released");

            int ret = Odin.Crypto.Methods.CryptoSetPassword(Handle, password);
            if (ret < 0)
                OdinLog.Assert(message: new OdinWrapperException($"{nameof(Odin.Crypto.Methods.CryptoSetPassword)} in {nameof(CryptoSetPassword)} failed (handle {Handle.IsAlive}): {(IntPtr)Handle} (code {ret})").ToString());
            return ret == 0;
        }

        /// <summary>
        /// Cleanup OdinCipherHandle
        /// </summary>
        /// <remarks>releases the handle only if it was created by this wrapper, an externally provided handle stays alive</remarks>
        public void Dispose()
        {
            if (_ownsHandle)
                Handle?.Dispose();
            Handle = null;
        }
    }
}
