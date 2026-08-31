using OdinNative.Wrapper.Peer.Rpc;
using OdinNative.Wrapper.Room.Rpc;
using System;
using static OdinNative.Core.Utility;

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
    public class RoomJoinedEventArgs : JoinedObjectContainer
    {
        /// <summary>
        /// room object
        /// </summary>
        public IRoom Room;
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
    }

    /// <summary>
    /// Arguments for PeerJoined events in the current room
    /// </summary>
    public class PeerJoinedEventArgs : PeerJoinedObjectContainer
    {
        /// <summary>
        /// room object
        /// </summary>
        public IRoom Room;

        public new UserData user_data;
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
        public uint PeerId { get; internal set; }
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
        public uint PeerId { get; internal set; }
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
    /// Arguments for MessageReceived events in the current room
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {

        /// <summary>
        /// peer id
        /// </summary>
        public uint PeerId { get; internal set; }
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
        /// room handle id
        /// </summary>
        public ulong RoomId { get; internal set; }
        /// <summary>
        /// rpc data
        /// </summary>
        public string Rpc { get; internal set; }
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
    public delegate void RpcEventHandler(object sender, RpcEventArgs e);

    /// <summary>
    /// Arguments for datagram events
    /// </summary>
    public class DatagramEventArgs : EventArgs
    {
        /// <summary>
        /// room handle id
        /// </summary>
        public ulong RoomId { get; internal set; }
        /// <summary>
        /// peer id
        /// </summary>
        public uint PeerId { get; internal set; }
        /// <summary>
        /// uint64 channel mask
        /// </summary>
        public ChannelMask ChannelMask { get; internal set; }
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
        public IntPtr Userdata { get; internal set; }
    }
    /// <summary>
    /// EventHandler in the current room
    /// </summary>
    /// <param name="sender">sender of type <see cref="Room"/></param>
    /// <param name="e">Arguments events</param>
    public delegate void DatagramEventHandler(object sender, DatagramEventArgs e);

    /// <summary>
    /// Arguments for socket events
    /// </summary>
    public class SocketEventArgs : EventArgs
    {
        /// <summary>
        /// socket
        /// </summary>
        public ulong SocketId { get; internal set; }
        /// <summary>
        /// socket data
        /// </summary>
        public IntPtr Data { get; internal set; }
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
    public delegate void SocketEventHandler(object sender, SocketEventArgs e);
}
