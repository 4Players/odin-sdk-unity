using OdinNative.Core.Imports;
using OdinNative.Wrapper;
using UnityEngine;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.Crypto"/> for Unity.
    /// <para>
    /// This convenient class provides predefined encryption helper functions for Unity.
    /// If Peers do not have the same cipher they can not communicate with each other besides some events.
    /// </para>
    /// </summary>
    /// <remarks>It is important for multiple encrypted rooms to provide each OdinRoom its own OdinCrypto instance!</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odincrypto/")]
    [AddComponentMenu("Odin/Instance/OdinCrypto")]
    public class OdinCrypto : MonoBehaviour
    {
        /// <summary>
        /// Initial cipher value
        /// </summary>
        [Tooltip("Shared password for peers so they can send messages, hear and speak to each other. This should be (re)set by clients.")]
        public string InitialPassword;
        internal Crypto Crypto;


        void Awake()
        {
            Crypto = Crypto.Create(InitialPassword);
        }

        void Reset()
        {
            InitialPassword = string.Empty;
        }

        /// <summary>
        /// Get handle to cipher
        /// </summary>
        /// <remarks>null for unencrypted</remarks>
        /// <returns>Handle or null</returns>
        public OdinCipherHandle GetCryptoCipher() => Crypto?.Handle ?? null;
        /// <summary>
        /// Set a password for encryption. Peers with the same password can hear each other.
        /// null == unencrypted
        /// </summary>
        /// <remarks>There can exist multiple groups with different encryption password in one room.</remarks>
        /// <param name="password">chipher password</param>
        /// <returns>false on failure</returns>
        public bool ChangePassword(string password) => Crypto?.CryptoSetPassword(password) ?? false;
        /// <summary>
        /// Get the encryption status of peer
        /// </summary>
        /// <param name="peerId">peer id to check</param>
        /// <returns>true if encrypted or false</returns>
        public bool GetPeerStatus(int peerId) => Crypto?.CryptoGetPeerStatus(peerId) == NativeBindings.OdinCryptoPeerStatus.ODIN_CRYPTO_PEER_STATUS_ENCRYPTED;
        /// <summary>
        /// Get the encryption status of peer
        /// </summary>
        /// <param name="peer">peer to check</param>
        /// <returns>true if encrypted or false</returns>
        public bool GetPeerStatus(OdinPeer peer) => GetPeerStatus(((int)peer.Id));

        void OnDestroy()
        {
            Crypto?.Dispose();
            Crypto = null;
        }
    }
}
