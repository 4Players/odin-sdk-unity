using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// C# bindings for ODIN library
    /// </summary>
    public static class NativeBindings
    {
        /// <summary>
        /// Noise suppression level for Room apm
        /// </summary>
        public enum OdinNoiseSuppressionLevel
        {
            None,
            Low,
            Moderate,
            High,
            VeryHigh,
        }

        internal struct OdinApmConfig
        {
            public bool vad_enable;
            public bool echo_canceller;
            public bool high_pass_filter;
            public bool pre_amplifier;
            public OdinNoiseSuppressionLevel noise_suppression_level;
            public bool transient_suppressor;
        }

        internal enum OdinTokenAudience
        {
            None,
            Gateway,
            Sfu
        }

        internal struct OdinTokenOptions
        {
            public string customer;
            public OdinTokenAudience audience;
            public ulong lifetime;
        }   

        #region EventStructs
        [StructLayout(LayoutKind.Explicit)]
        internal struct OdinEvent
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
            public OdinEvent_MessageReceivedData message_received;
            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.Struct)]
            public OdinEvent_ConnectionStateChanged StateChanged;
            #endregion OdinEvent union
        };

        internal enum OdinEventTag
        {
            OdinEvent_PeerJoined,
            OdinEvent_PeerLeft,
            OdinEvent_PeerUpdated,
            OdinEvent_MediaAdded,
            OdinEvent_MediaRemoved,
            OdinEvent_MessageReceived,
            OdinEvent_ConnectionStateChanged,
            OdinEvent_None,
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct OdinEvent_PeerJoinedData
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
        internal struct OdinEvent_PeerLeftData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong id;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct OdinEvent_PeerUpdatedData
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
        internal struct OdinEvent_MediaAddedData
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
        internal struct OdinEvent_MediaRemovedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U2)]
            public ushort media_id;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct OdinEvent_MessageReceivedData
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong peer_id;
            [FieldOffset(8)]
            public IntPtr data;
            [FieldOffset(16)]
            [MarshalAs(UnmanagedType.U8)]
            public ulong data_len;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct OdinEvent_ConnectionStateChanged
        {
            [FieldOffset(0)]
            public OdinConnectionState state;
        }

        /// <summary>
        /// Odin library connection state
        /// </summary>
        public enum OdinConnectionState
        {
            Connecting,
            Connected,
            Disconnected,
        }
        #endregion EventStructs

        [StructLayout(LayoutKind.Sequential)]
        internal struct OdinAudioStreamConfig
        {
            public uint sample_rate;
            public byte channel_count;
        }

        internal enum OdinChannelLayout
        {
            OdinChannelLayout_Mono,
            OdinChannelLayout_Stereo
        }
    }
}
