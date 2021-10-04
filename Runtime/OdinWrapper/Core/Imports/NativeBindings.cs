using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Imports
{
    public static class NativeBindings
    {
        public enum OdinNoiseSuppsressionLevel
        {
            None,
            Low,
            Moderate,
            High,
            VeryHigh,
        }

        public struct OdinApmConfig
        {
            public bool vad_enable;
            public bool echo_canceller;
            public bool high_pass_filter;
            public bool pre_amplifier;
            public OdinNoiseSuppsressionLevel noise_suppression_level;
            public bool transient_suppressor;
        }

        public enum OdinTokenAudience
        {
            None,
            Gateway,
            Sfu
        }

        public struct OdinTokenOptions
        {
            public string customer;
            public OdinTokenAudience audience;
            public ulong lifetime;
        }   

        #region Events
        #region EventStructs
        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.I4)]
            public OdinEventTag tag;

            #region OdinEvent union
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_PeerJoinedData peer_joined;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_PeerLeftData peer_left;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_PeerUpdatedData peer_updated;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_MediaAddedData media_added;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_MediaRemovedData media_removed;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_ConnectionStateChanged StateChanged;
            #endregion OdinEvent union
        };

        public enum OdinEventTag
        {
            OdinEvent_PeerJoined,
            OdinEvent_PeerLeft,
            OdinEvent_PeerUpdated,
            OdinEvent_MediaAdded,
            OdinEvent_MediaRemoved,
            OdinEvent_ConnectionStateChanged,
            OdinEvent_None,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_PeerJoinedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong id;
            [FieldOffset(8)]
            public IntPtr user_data;
            [FieldOffset(16)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong user_data_len;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_PeerLeftData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong id;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_PeerUpdatedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong id;
            [FieldOffset(8)]
            public IntPtr user_data;
            [FieldOffset(16)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong user_data_len;
        }

        /// <summary>
        /// Provides access to output media stream
        /// (stream is read only! "OdinAudioReadData only")
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_MediaAddedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong peer_id;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.U2)]
            public ushort media_id;
            [FieldOffset(16)]
            public IntPtr stream;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_MediaRemovedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U2)]
            public ushort media_id;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OdinEvent_ConnectionStateChanged
        {
            [FieldOffset(0)]
            public OdinConnectionState state;
        }
        #endregion EventStructs

        #region EventMap
        public enum OdinConnectionState
        {
            Connecting,
            Connected,
            Disconnected,
        }

        [Obsolete]
        public interface IPeerJoinedEvent
        {
            ulong PeerId { get; set; } // id
            byte[] UserData { get; set; }
        }

        [Obsolete]
        public interface IPeerLeftEvent
        {
            ulong PeerId { get; set; } // id
        }

        [Obsolete]
        public interface IPeerUpdatedEvent
        {
            ulong PeerId { get; set; } // id
            byte[] UserData { get; set; }
        }

        [Obsolete]
        public interface IMediaAddedEvent
        {
            ulong PeerId { get; set; } // peer_id
            ushort MediaId { get; set; }
            PlaybackStream Stream { get; set; }
        }

        [Obsolete]
        public interface IMediaRemovedEvent
        {
            ushort MediaId { get; set; }
        }

        [Obsolete]
        public class RoomEventArgs : EventArgs, IPeerJoinedEvent, IPeerLeftEvent, IPeerUpdatedEvent, IMediaAddedEvent, IMediaRemovedEvent
        {
            public ulong PeerId { get; set; } // peer_id == id
            public byte[] UserData { get; set; }
            public ushort MediaId { get; set; }
            public PlaybackStream Stream { get; set; }
        }
        #endregion EventMap
        #endregion Events

        #region Odin
        [StructLayout(LayoutKind.Sequential)]
        public struct OdinAudioStreamConfig
        {
            public uint sample_rate;
            public byte channel_count;
        }
        #endregion Odin
    }
}
