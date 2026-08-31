using OdinNative.Utils.Json;
using OdinNative.Wrapper.Media.Rpc;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OdinNative.Wrapper.Peer.Rpc
{
    #region ChangeSelfEvent
    public class ChangeSelfObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "ChangeSelf";
        public string EventName => EVENTNAME;

        [DataMember(Name = ChangeSelfObject.EVENTNAME)]
        public ChangeSelfObjectContainer Values;
    }

    public class ChangeSelfObjectContainer : EventArgs
    {
        public string user_id;
        public string user_data;
        public List<string> tags;
        public AudioParametersObject audio_parameters;
        public VideoParametersObject video_parameters;

        public override string ToString()
        {
            return ChangeSelfObject.EVENTNAME;
        }
    }
    #endregion ChangeSelfEvent

    #region PeerJoinedEvent
    public class PeerJoinedObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "PeerJoined";
        public string EventName => EVENTNAME;

        [DataMember(Name = PeerJoinedObject.EVENTNAME)]
        public PeerJoinedObjectContainer Values;
    }

    public class PeerJoinedObjectContainer : ChangeSelfObjectContainer
    {
        public uint peer_id;

        public override string ToString()
        {
            return PeerJoinedObject.EVENTNAME;
        }
    }
    #endregion PeerJoinedEvent

    #region PeerLeftEvent
    public class PeerLeftObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "PeerLeft";
        public string EventName => EVENTNAME;

        [DataMember(Name = PeerLeftObject.EVENTNAME)]
        public PeerLeftObjectContainer Values;
    }

    public class PeerLeftObjectContainer : EventArgs
    {
        public uint peer_id;

        public override string ToString()
        {
            return PeerLeftObject.EVENTNAME;
        }
    }
    #endregion PeerLeftEvent

    #region PeerChangedEvent
    public class PeerChangedObject : IJsonRpcPayload
    {
        [IgnoreDataMember]
        internal const string EVENTNAME = "PeerChanged";
        public string EventName => EVENTNAME;

        [DataMember(Name = PeerChangedObject.EVENTNAME)]
        public PeerChangedObjectContainer Values;
    }

    public class PeerChangedObjectContainer : EventArgs
    {
        public uint peer_id;
        public string user_id;
        public string user_data;
        public List<string> tags;
        public AudioParametersObject audio_parameters;
        public VideoParametersObject video_parameters;

        public override string ToString()
        {
            return PeerChangedObject.EVENTNAME;
        }
    }
    #endregion PeerChangedEvent
}