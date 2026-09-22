using OdinNative.Utils.Json;
using OdinNative.Wrapper.Media.Rpc;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OdinNative.Wrapper.Room.Rpc
{
    #region odin_room_create
    public class RoomCreateObject
    {
        public string token;
        public string room_Id;
        public string user_data;
        public AudioParametersObject audio_parameters;
        public VideoParametersObject video_parameters;
        [IgnoreDataMember]
        public bool error;
    }
    #endregion odin_room_create

    #region RoomStatusEvent
    public class RoomStatusChangedObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "RoomStatusChanged";
        public string EventName => EVENTNAME;

        [DataMember(Name = RoomStatusChangedObject.EVENTNAME)]
        public RoomStatusChangedObjectContainer Values;
    }

    public class RoomStatusChangedObjectContainer : EventArgs
    {
        public string status;
        public string message;

        public override string ToString()
        {
            return RoomStatusChangedObject.EVENTNAME;
        }
    }
    #endregion RoomStatusEvent

    #region JoinedEvent
    public class JoinedObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "Joined";
        public string EventName => EVENTNAME;

        [DataMember(Name = JoinedObject.EVENTNAME)]
        public JoinedObjectContainer Values;
    }

    public class JoinedObjectContainer : EventArgs
    {
        public uint own_peer_id;
        [DataMember(Name = "room_id")]
        public string room_name;
        public string customer;

        public override string ToString()
        {
            return JoinedObject.EVENTNAME;
        }
    }
    #endregion JoinedEvent

    #region TokenEvent
    public class NewReconnectTokenObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "NewReconnectToken";
        public string EventName => EVENTNAME;

        [DataMember(Name = NewReconnectTokenObject.EVENTNAME)]
        public NewReconnectTokenObjectContainer Values;
    }
    public class NewReconnectTokenObjectContainer : EventArgs
    {
        public string token;

        public override string ToString()
        {
            return NewReconnectTokenObject.EVENTNAME;
        }
    }
    #endregion TokenEvent

    #region MessageReceivedEvent
    public class MessageReceivedObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "MessageReceived";
        public string EventName => EVENTNAME;

        [DataMember(Name = MessageReceivedObject.EVENTNAME)]
        public MessageReceivedObjectContainer Values;
    }

    /// <summary>
    /// Payload of a <c>MessageReceived</c> event, sent by another peer via <c>SendMessage</c>.
    /// </summary>
    public class MessageReceivedObjectContainer : EventArgs
    {
        /// <summary>
        /// Peer id of the sender
        /// </summary>
        public uint sender_peer_id;
        /// <summary>
        /// Raw payload as parsed from the event json: a <see cref="List{T}"/> of numbers for binary
        /// messages or a <see cref="Dictionary{TKey, TValue}"/> for messages that were sent as json object
        /// </summary>
        public object message;

        /// <summary>
        /// Message payload as bytes
        /// </summary>
        /// <remarks>
        /// Binary messages are returned as sent. A message that was sent as json object is
        /// re-serialized, so the bytes are its UTF8 encoded json text.
        /// </remarks>
        /// <returns>payload bytes or an empty array</returns>
        public byte[] GetPayload()
        {
            if (message is List<object> bytes)
            {
                byte[] result = new byte[bytes.Count];
                for (int i = 0; i < bytes.Count; i++)
                    result[i] = Convert.ToByte(bytes[i]);
                return result;
            }

            if (message != null)
                return System.Text.Encoding.UTF8.GetBytes(JSONWriter.ToJson(message));

            return Array.Empty<byte>();
        }

        public override string ToString()
        {
            return MessageReceivedObject.EVENTNAME;
        }
    }
    #endregion MessageReceivedEvent
}
