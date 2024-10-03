namespace OdinNative.Wrapper
{
    public interface IPeer
    {
        /// <summary>
        /// peer id
        /// </summary>
        ulong Id { get; }
        /// <summary>
        /// token user id
        /// </summary>
        string UserId { get; }
        /// <summary>
        /// peer userdata
        /// </summary>
        IUserData UserData { get; }
        /// <summary>
        /// Default value <code>null</code> indicates root or not set
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
    }
}