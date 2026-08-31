using System.Collections.Generic;

namespace OdinNative.Wrapper
{
    public interface IPeer
    {
        /// <summary>
        /// peer id
        /// </summary>
        uint Id { get; }
        /// <summary>
        /// token user id
        /// </summary>
        string UserId { get; }
        /// <summary>
        /// user tags
        /// </summary>
        List<string> Tags { get; }
        /// <summary>
        /// peer userdata
        /// </summary>
        IUserData UserData { get; }
        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        IRoom Parent { get; }

        /// <summary>
        /// Get the underlying <see cref="PeerEntity"/>
        /// </summary>
        /// <returns>native wrapper peer</returns>
        PeerEntity GetBasePeer();

        /// <summary>
        /// Get the underlying <see cref="Room.Room"/>
        /// </summary>
        /// <returns>native wrapper room</returns>
        OdinNative.Wrapper.Room.Room GetRoomApi();

        /// <summary>
        /// Get encoder
        /// </summary>
        /// <returns>input media encoder of the peer or null</returns>
        MediaEncoder GetEncoder();
        /// <summary>
        /// Get decoder
        /// </summary>
        /// <returns>output media decoder of the peer or null</returns>
        MediaDecoder GetDecoder();
    }
}