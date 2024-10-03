using OdinNative.Wrapper.Media;
using System;

namespace OdinNative.Wrapper.Room
{
    /// <summary>
    /// Arguments for RoomJoin events right before the room is joined
    /// </summary>
    public class RoomJoinEventArgs : EventArgs
    {
        /// <summary>
        /// room object
        /// </summary>
        public IRoom Room;
    }

    /// <summary>
    /// Arguments for RoomJoined events when the room was joined successfully
    /// </summary>
    public class RoomJoinedEventArgs : EventArgs
    {
        /// <summary>
        /// room object
        /// </summary>
        public IRoom Room;
        /// <summary>
        /// customer id
        /// </summary>
        public string Customer;
    }

    /// <summary>
    /// Arguments for RoomLeave events right before the room handle is destroyed
    /// </summary>
    public class RoomLeaveEventArgs : EventArgs
    {
        /// <summary>
        /// room object
        /// </summary>
        public IRoom Room;
    }

    /// <summary>
    /// Arguments for RoomLeft events when the room handle was destroyed
    /// </summary>
    public class RoomLeftEventArgs : EventArgs
    {
        /// <summary>
        /// room id
        /// </summary>
        public ulong RoomId;

        /// <summary>
        /// Reason for the Room Left event.
        /// </summary>
        public string Reason;
    }

    /// <summary>
    /// Arguments for PeerJoined events in the current room
    /// </summary>
    public class PeerJoinedEventArgs : EventArgs
    {
        public ulong PeerId;
        public string UserId;
        public UserData UserData;
        public MediaRpc[] Medias;
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomPeerJoinedEventHandler(object sender, PeerJoinedEventArgs e);

    /// <summary>
    /// Arguments for PeerLeft events in the current room
    /// </summary>
    public class PeerLeftEventArgs : EventArgs
    {
        /// <summary>
        /// peer id
        /// </summary>
        public ulong PeerId { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomPeerLeftEventHandler(object sender, PeerLeftEventArgs e);

    /// <summary>
    /// Arguments for PeerUserDataChanged events in the current room
    /// </summary>
    public class PeerUserDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// peer id
        /// </summary>
        public ulong PeerId { get; internal set; }
        /// <summary>
        /// peer object
        /// </summary>
        public PeerEntity Peer;
        /// <summary>
        /// peer userdata
        /// </summary>
        public UserData UserData;
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomPeerUserDataChangedEventHandler(object sender, PeerUserDataChangedEventArgs e);

    /// <summary>
    /// Arguments for MediaAdded events in the current room
    /// </summary>
    public class MediaAddedEventArgs : EventArgs
    {
        /// <summary>
        /// room id
        /// </summary>
        public ulong RoomId { get; internal set; }
        /// <summary>
        /// peer id
        /// </summary>
        public ulong PeerId { get; internal set; }
        /// <summary>
        /// media id
        /// </summary>
        public ushort MediaId { get; internal set; }
        /// <summary>
        /// media uid
        /// </summary>
        public string MediaUId { get; internal set; }

    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomMediaAddedEventHandler(object sender, MediaAddedEventArgs e);

    /// <summary>
    /// Arguments for MediaRemoved events in the current room
    /// </summary>
    public class MediaRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// stream handle id
        /// </summary>
        public ushort MediaId { get; internal set; }
        /// <summary>
        /// peer object
        /// </summary>
        public ulong PeerId { get; internal set; }
        /// <summary>
        /// Media uid
        /// </summary>
        public string MediaUID { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomMediaRemovedEventHandler(object sender, MediaRemovedEventArgs e);

    /// <summary>
    /// Arguments for MediaActiveStateChanged events in the current room
    /// </summary>
    public class MediaActiveStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// media id
        /// </summary>
        public ushort MediaId { get; internal set; }
        /// <summary>
        /// peer id
        /// </summary>
        public ulong PeerId { get; internal set; }
        /// <summary>
        /// state of the media
        /// </summary>
        public bool Active { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room. Will work for both local and remote peers.
    /// </summary>
    /// <remarks>
    /// Will currently only work if either Voice Activity Detection or Volume Gate is active.
    /// </remarks>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void MediaActiveStateChangedEventHandler(object sender, MediaActiveStateChangedEventArgs e);

    /// <summary>
    /// Arguments for RoomUserDataChanged events in the current room
    /// </summary>
    public class RoomUserDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// room name
        /// </summary>
        public string RoomName { get; internal set; }
        /// <summary>
        /// room userdata
        /// </summary>
        public UserData Data;
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomUserDataChangedEventHandler(object sender, RoomUserDataChangedEventArgs e);

    /// <summary>
    /// Arguments for MessageReceived events in the current room
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {

        /// <summary>
        /// peer id
        /// </summary>
        public ulong PeerId { get; internal set; }
        /// <summary>
        /// arbitrary data
        /// </summary>
        public byte[] Data;
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomMessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

    /// <summary>
    /// Arguments for ConnectionStateChanged events in the current room
    /// </summary>
    public class RoomStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Room state of the ODIN client
        /// </summary>
        public string RoomState { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events in the current room</param>
    public delegate void RoomConnectionStateChangedEventHandler(object sender, RoomStateChangedEventArgs e);

    /// <summary>
    /// Arguments for rpc events
    /// </summary>
    public class RpcEventArgs : EventArgs
    {
        /// <summary>
        /// room id
        /// </summary>
        public ulong RoomId { get; internal set; }
        /// <summary>
        /// raw Msgpack rpc data
        /// </summary>
        public byte[] Rpc { get; internal set; } = Array.Empty<byte>();
        /// <summary>
        /// unused
        /// </summary>
        public MarshalByRefObject Userdata { get; internal set; }
    }
    /// <summary>
    /// Result of send rpc responses 
    /// <code>msgid, (method, params, error, result)</code>
    /// </summary>
    public class RpcResult : EventArgs
    {
        /// <summary>
        /// msgid
        /// </summary>
        public uint Id { get; internal set; }
        /// <summary>
        /// method
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// error
        /// </summary>
        public string Error { get; internal set; }
        /// <summary>
        /// result
        /// </summary>
        public object Value { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events</param>
    public delegate void RpcEventHandler(object sender, RpcEventArgs e);

    /// <summary>
    /// Arguments for datagram events
    /// </summary>
    public class DatagramEventArgs : EventArgs
    {
        /// <summary>
        /// room id
        /// </summary>
        public ulong RoomId { get; internal set; }
        /// <summary>
        /// decoder datagram
        /// </summary>
        public IntPtr Datagram { get; internal set; }
        /// <summary>
        /// decoder datagram payload
        /// </summary>
        public byte[] Payload { get; internal set; } = Array.Empty<byte>();
        /// <summary>
        /// unused
        /// </summary>
        public MarshalByRefObject Userdata { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events</param>
    public delegate void DatagramEventHandler(object sender, DatagramEventArgs e);
}
