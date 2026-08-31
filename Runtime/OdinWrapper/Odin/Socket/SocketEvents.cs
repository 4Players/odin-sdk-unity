using System;

namespace OdinNative.Wrapper.Socket
{
    /// <summary>
    /// Arguments for socket events
    /// </summary>
    public class SocketMessageEventArgs : EventArgs
    {
        /// <summary>
        /// socket
        /// </summary>
        public ISocket Socket { get; internal set; }
        /// <summary>
        /// room the socket belongs to
        /// </summary>
        public IRoom Room { get; internal set; }
        /// <summary>
        /// socket payload
        /// </summary>
        public byte[] Payload { get; internal set; } = Array.Empty<byte>();
        /// <summary>
        /// unused
        /// </summary>
        public IntPtr Userdata { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events</param>
    public delegate void SocketMessageEventHandler(object sender, SocketMessageEventArgs e);
}
