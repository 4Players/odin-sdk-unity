using OdinNative.Wrapper.Media;
using System.Collections.Generic;

namespace OdinNative.Wrapper.Peer
{
    /// <summary>
    /// Client/Remote peer
    /// </summary>
    public struct PeerRpc
    {
        /// <summary>
        /// peer id
        /// </summary>
        public ulong Id;
        /// <summary>
        /// token user id
        /// </summary>
        public string UserId;
        /// <summary>
        /// peer userdata
        /// </summary>
        public byte[] UserData;
        /// <summary>
        /// peer decoders
        /// </summary>
        public List<MediaRpc> Medias;
    }
}