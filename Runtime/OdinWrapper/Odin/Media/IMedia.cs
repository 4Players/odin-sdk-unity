namespace OdinNative.Wrapper.Media
{
    public interface IMedia
    {
        /// <summary>
        /// Odin media id
        /// </summary>
        ulong Id { get; }
        /// <summary>
        /// Peer of media
        /// </summary>
        IPeer Parent { get; }

        /// <summary>
        /// Get native effect pipeline
        /// </summary>
        /// <returns>managed effect pipeline</returns>
        MediaPipeline GetPipeline();

        public Core.Utility.ChannelMask ChannelMask { get; }
        public Core.Imports.NativeBindings.OdinPosition Position { get; }
    }
}