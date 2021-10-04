using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Odin.Room
{
    public class PeerJoinedEvent : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
    }

    public class PeerLeftEvent : EventArgs
    {
        public ulong PeerId { get; internal set; }
    }

    public class PeerUpdatedEvent : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public byte[] UserData;
    }

    public class MediaAddedEvent : EventArgs
    {
        public ulong PeerId { get; internal set; }
        public Peer.Peer Peer;
        public PlaybackStream Media;
    }

    public class MediaRemovedEvent : EventArgs
    {
        public ushort MediaId { get; internal set; }
        public Peer.Peer Peer;
        public MediaStream Media;
    }
}
