namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// RPC data map
    /// </summary>
    public struct MediaRpc
    {
        /// <summary>
        /// media id
        /// </summary>
        public ushort Id;
        /// <summary>
        /// arbitrary media data
        /// </summary>
        public MediaRpcProperties Properties;
        /// <summary>
        /// media state
        /// </summary>
        public bool Paused;
    }

    /// <summary>
    /// arbitrary media data
    /// </summary>
    public class MediaRpcProperties
    {
        /// <summary>
        /// preset field <c>"Audio"</c>
        /// </summary>
        public string Kind;
        /// <summary>
        /// preset field of an unique id
        /// </summary>
        public string uId;
    }
}
