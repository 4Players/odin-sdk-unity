using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Odin.Room
{
    /// <summary>
    /// Room join arguments before the room is actually joined
    /// </summary>
    public class RoomJoinEventArgs : EventArgs
    {
        public Room Room;
    }

    /// <summary>
    /// Room joined arguments after a room is joined
    /// </summary>
    public class RoomJoinedEventArgs : EventArgs
    {
        public Room Room;
    }

    /// <summary>
    /// Room leave arguments before the room is destroyed
    /// </summary>
    public class RoomLeaveEventArgs : EventArgs
    {
        public Room Room;
    }

    /// <summary>
    /// Room left arguments after the room is destroyed
    /// </summary>
    public class RoomLeftEventArgs : EventArgs
    {
        public string RoomName;
    }

    /// <summary>
    /// Peer joined arguments after a peer used <see cref="OdinHandler.JoinRoom"/>
    /// </summary>
    public class PeerJoinedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
    }

    /// <summary>
    /// Peer left arguments
    /// </summary>
    public class PeerLeftEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
    }

    /// <summary>
    /// Peer updated arguments with arbitrary data
    /// </summary>
    public class PeerUpdatedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public byte[] UserData;
    }

    /// <summary>
    /// Media added arguments in the current room
    /// </summary>
    public class MediaAddedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
        public PlaybackStream Media;
    }

    /// <summary>
    /// Media removed arguments in the current room
    /// </summary>
    public class MediaRemovedEventArgs : EventArgs
    {
        public ushort MediaId { get; internal set; }
        public Peer.Peer Peer;
        public MediaStream Media;
    }

    /// <summary>
    /// Message received arguments in the current room
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public byte[] Data;
    }
}
