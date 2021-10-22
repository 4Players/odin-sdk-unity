using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Odin.Room
{
    //Room
    public class RoomJoinEventArgs : EventArgs
    {
        public Room Room;
    }

    public class RoomJoinedEventArgs : EventArgs
    {
        public Room Room;
    }

    public class RoomLeaveEventArgs : EventArgs
    {
        public Room Room;
    }

    public class RoomLeftEventArgs : EventArgs
    {
        public string RoomName;
    }

    //SubRoom
    public class PeerJoinedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
    }

    public class PeerLeftEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
    }

    public class PeerUpdatedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public byte[] UserData;
    }

    public class MediaAddedEventArgs : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
        public PlaybackStream Media;
    }

    public class MediaRemovedEventArgs : EventArgs
    {
        public ushort MediaId { get; internal set; }
        public Peer.Peer Peer;
        public MediaStream Media;
    }
}
