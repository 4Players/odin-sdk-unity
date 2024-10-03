#if UNITY_WEBGL

using AOT;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using OdinNative.Unity;
using OdinNative.Utils.MessagePack;
using OdinNative.Core.Imports;
using OdinNative.Core;
using OdinNative.Wrapper.Room;
using static OdinNative.Core.Imports.NativeBindings;

namespace UnityEngine
{
    /// <summary>
    /// Main Room for Javascript bridge
    /// </summary>
    public class OdinWebRoom : MonoBehaviour, IRoom
    {
        internal static Dictionary<string, TaskCompletionSource<string>> _Sources { get; private set; } = new Dictionary<string, TaskCompletionSource<string>>();
        /// <summary>
        /// Room server gateway endpoint
        /// </summary>
        public string EndPoint { get; set; }
        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// RoomStatus
        /// </summary>
        public string RoomStatus { get; private set; }
        /// <summary>
        /// IsJoined state
        /// </summary>
        public bool IsJoined => RoomStatus?.Equals("Joined") ?? false;
        private bool IsInit = false;
        public ulong Id => 0;
        /// <summary>
        /// Room joining token
        /// </summary>
        public string Token { get; private set; }
        /// <summary>
        /// Odin connection status
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="roomStatus">string status</param>
        public event OnRoomStatusChangedDelegate OnRoomStatusChanged;
        /// <summary>
        /// Odin room joined bookkeeping
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="ownPeerId">Self id</param>
        /// <param name="name">Room name</param>
        /// <param name="roomUserData">arbitrary datas</param>
        /// <param name="mediaIds">raw media ids in remote room</param>
        /// <param name="peers">Parsed peer rpc data</param>
        public event OnRoomJoinedDelegate OnRoomJoined;
        /// <summary>
        /// Odin room left
        /// </summary>
        /// <remarks>The Left event is usually only received by server side force</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="reason">indicate leaving reason</param>
        public event OnRoomLeftDelegate OnRoomLeft;
        /// <summary>
        /// Odin peer joined wrapped data
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        /// <param name="userId"></param>
        /// <param name="userData"></param>
        /// <param name="medias"></param>
        public event OnPeerJoinedDelegate OnPeerJoined;
        /// <summary>
        /// Odin peer left
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        public event OnPeerLeftDelegate OnPeerLeft;
        /// <summary>
        /// Odin media started
        /// </summary>
        /// <remarks>encoder/decoder</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        /// <param name="media"></param>
        public event OnMediaStartedDelegate OnMediaStarted;
        /// <summary>
        /// Odin media stopped
        /// </summary>
        /// <remarks>encoder/decoder</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        /// <param name="mediaId"></param>
        public event OnMediaStoppedDelegate OnMediaStopped;
        /// <summary>
        /// Odin peer changed userdata
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        /// <param name="userData"></param>
        public event OnUserDataChangedDelegate OnUserDataChanged;
        /// <summary>
        /// Odin room received message
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId"></param>
        /// <param name="message"></param>
        public event OnMessageReceivedDelegate OnMessageReceived;

        /// <summary>
        /// IRoom parent for Unity gameobjects
        /// </summary>
        public object Parent { get; set; }

        /// <summary>
        /// Peer map
        /// </summary>
        public Dictionary<int, IUserData> Peers { get; private set; } = new Dictionary<int, IUserData>();

        public Crypto CryptoCipher => throw new NotImplementedException();

        [DllImport("__Internal")]
        public static extern void JsLibLoadWebPlugin();
        /// <summary>
        /// Initialize the js bridge with OdinPlugin
        /// </summary>
        public void InitJSBridgePlugin()
        {
            OdinWebRoom.JsLibLoadWebPlugin();
            IsInit = true;
        }

        [Serializable]
        private struct JoinParamsData
        {
            public string token;
            public string gateway;
        }
        [DllImport("__Internal")]
        private static extern void JsLibJoinRoomPlugin(string args);
        /// <summary>
        /// Join a room based on token info with custom gateway
        /// </summary>
        public static void JoinRoom(string token, string gateway)
        {
            _Sources.Clear();
            string args = JsonUtility.ToJson(new JoinParamsData { token = token, gateway = gateway });
            OdinWebRoom.JsLibJoinRoomPlugin(args);
        }

        [DllImport("__Internal")]
        private static extern void JsLibDisconnectOdin();
        public static void ClientDisconnect()
        {
            _Sources.Clear();
            OdinWebRoom.JsLibDisconnectOdin();
        }

        /// <summary>
        /// Join a room based on token info
        /// </summary>
        /// <param name="token">JWT</param>
        public bool Join(string token)
        {
            OdinWebRoom.JoinRoom(token, (Parent as OdinRoom)?.Gateway ?? OdinDefaults.GATEWAY);
            return IsInit;
        }

        [DllImport("__Internal")]
        private static extern void JsLibCloseRoom();
        public void Leave()
        {
            _Sources.Clear();
            OdinWebRoom.JsLibCloseRoom();
            Peers.Clear();
            OnRoomLeft?.Invoke(this, "Left");
        }

        /// <summary>
        /// Register a callback function.
        /// </summary>
        /// <param name="eventType">The event type to listen to</param>
        /// <param name="callbackObjectName">The name of the game object on which the callback function will be invoked</param>
        /// <param name="callbackFunctionName">The name of the invoked callback funciton</param>
        [DllImport("__Internal")]
        private static extern void JsLibAddCallback(string eventType, string callbackObjectName, string callbackFunctionName);
        /// <summary>
        /// Register a custom callback function.
        /// </summary>
        public void AddCallback(CallbackEvent odinEvent, GameObject uobject, string functionName) => AddCallback(odinEvent, uobject.name, functionName);
        /// <summary>
        /// Register a custom callback function.
        /// </summary>
        public static void AddCallback(CallbackEvent odinEvent, string objectName, string functionName)
        {
            if (OdinDefaults.Verbose) Debug.Log($"#Odin-CS AddCallback: \"{odinEvent}\" => {objectName}.{functionName}");

            OdinWebRoom.JsLibAddCallback(odinEvent.ToString(), objectName, functionName);
        }

        /// <summary>
        /// Enum representation of Event-String
        /// </summary>
        public enum CallbackEvent
        {
            RoomStatusChanged,
            MessageReceived,
            Joined,
            PeerJoined,
            PeerLeft,
            UserDataChanged,
            MediaStarted,
            MediaStopped,
        }

        protected readonly Dictionary<CallbackEvent, string> _EventMap = new()
        {
            { CallbackEvent.RoomStatusChanged,  nameof(WebRoom_OnRoomStatusChanged) },
            { CallbackEvent.Joined,             nameof(WebRoom_OnRoomJoined) },
            { CallbackEvent.MessageReceived,    nameof(WebRoom_OnMessageReceived) },
            { CallbackEvent.PeerJoined,         nameof(WebRoom_OnPeerJoined) },
            { CallbackEvent.PeerLeft,           nameof(WebRoom_OnPeerLeft) },
            { CallbackEvent.UserDataChanged,    nameof(WebRoom_OnUserDataChanged) },
            { CallbackEvent.MediaStarted,       nameof(WebRoom_OnMediaStarted) },
            { CallbackEvent.MediaStopped,       nameof(WebRoom_OnMediaStopped) },
        };

        [Serializable]
        private struct RoomStatusChangedData
        {
            public string status;
            public string message;
        }
        public void WebRoom_OnRoomStatusChanged(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnRoomStatusChanged: {data}");

            RoomStatusChangedData rpcdata = JsonUtility.FromJson<RoomStatusChangedData>(data);
            RoomStatus = rpcdata.status;
            if (OdinDefaults.Verbose)
                Debug.Log($"#Odin-JS OnRoomStatusChanged: {rpcdata.message}");

            OnRoomStatusChanged?.Invoke(this, rpcdata.status);
        }

        [Serializable]
        private struct MessageReceivedData
        {
            public int peer_id;
            public byte[] message;
        }
        public void WebRoom_OnMessageReceived(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnMessageReceived: {data}");

            MessageReceivedData rpcdata = JsonUtility.FromJson<MessageReceivedData>(data);
            OnMessageReceived?.Invoke(this, (ulong)rpcdata.peer_id, rpcdata.message);
        }

        [Serializable]
        private struct RoomJoinedData
        {
            public string room_id;
            public int own_peer_id;
            public string customer;
            public byte[] room_user_data;
            public PeerJoinedData[] peers;
        }
        public void WebRoom_OnRoomJoined(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnRoomJoined: {data}");

            RoomJoinedData rpcdata = JsonUtility.FromJson<RoomJoinedData>(data);
            var prpc = new ReadOnlyCollection<PeerRpc>(rpcdata.peers.Select((peer) => new PeerRpc()
            {
                Id = (ulong)peer.peer_id,
                UserId = peer.user_id,
                UserData = new UserData(peer.user_data),
                Medias = peer.medias.Select((media) => new MediaRpc()
                {
                    Id = (ushort)media.media_id,
                    Paused = media.paused,
                    Properties = new MediaRpcProperties()
                    {
                        uId = media.media_uid
                    }
                }).ToList()
            }).ToList());

            OnRoomJoined?.Invoke(this,
                (ulong)rpcdata.own_peer_id,
                rpcdata.room_id,
                rpcdata.customer,
                rpcdata.room_user_data,
                new ushort[0], // unknown/handled on js side
                prpc);
        }

        [Serializable]
        private struct PeerJoinedData
        {
            public int peer_id;
            public string user_id;
            public byte[] user_data;
            public PeerJoinedMediasData[] medias;
        }
        [Serializable]
        private struct PeerJoinedMediasData
        {
            public int media_id;
            public string media_uid;
            public bool paused;
        }
        public void WebRoom_OnPeerJoined(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnPeerJoined: {data}");

            PeerJoinedData rpcdata = JsonUtility.FromJson<PeerJoinedData>(data);
            Peers.Add(rpcdata.peer_id, new UserData(rpcdata.user_data));
            var mrpc = rpcdata.medias.Select((m) => new MediaRpc() { Id = (ushort)m.media_id, Properties = new MediaRpcProperties() { Kind = "audio", uId = m.media_uid }, Paused = m.paused });
            OnPeerJoined?.Invoke(this, (ulong)rpcdata.peer_id, rpcdata.user_id, rpcdata.user_data, mrpc.ToArray());
        }

        [Serializable]
        private struct PeerLeftData
        {
            public int peer_id;
        }
        public void WebRoom_OnPeerLeft(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnPeerLeft: {data}");

            PeerLeftData rpcdata = JsonUtility.FromJson<PeerLeftData>(data);
            try { Peers.Remove(rpcdata.peer_id); } catch { /* NOP */ }
            OnPeerLeft?.Invoke(this, (ulong)rpcdata.peer_id);
        }

        [Serializable]
        private struct UserDataChangedData
        {
            public int peer_id;
            public byte[] user_data;
        }
        public void WebRoom_OnUserDataChanged(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnUserDataChanged: {data}");

            UserDataChangedData rpcdata = JsonUtility.FromJson<UserDataChangedData>(data);
            if (Peers.TryGetValue(rpcdata.peer_id, out var userdata))
                Peers[rpcdata.peer_id] = new UserData(rpcdata.user_data);

            OnUserDataChanged?.Invoke(this, (ulong)rpcdata.peer_id, rpcdata.user_data);
        }

        [Serializable]
        private struct MediaStartedEventData
        {
            public int peer_id;
            public int media_id;
            public string media_uid;
            public bool paused;
        }
        public void WebRoom_OnMediaStarted(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnMediaStarted: {data}");

            MediaStartedEventData rpcdata = JsonUtility.FromJson<MediaStartedEventData>(data);
            OnMediaStarted?.Invoke(this, (ulong)rpcdata.peer_id, new MediaRpc() 
            { 
                Id = (ushort)rpcdata.media_id,
                Properties = new MediaRpcProperties() 
                { 
                    Kind = "audio",
                    uId = rpcdata.media_uid
                }, 
                Paused = rpcdata.paused 
            });
        }
        [Serializable]
        private struct MediaStoppedEventData
        {
            public int peer_id;
            public int media_id;
        }
        public void WebRoom_OnMediaStopped(string data)
        {
            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS OnMediaStopped: {data}");

            MediaStoppedEventData rpcdata = JsonUtility.FromJson<MediaStoppedEventData>(data);
            OnMediaStopped?.Invoke(this, (ulong)rpcdata.peer_id, (ushort)rpcdata.media_id);
        }

        /// <summary>
        /// Register event callbacks for Odin events
        /// </summary>
        /// <param name="objectName">gameObject name where to find the callback functions</param>
        public void SubscribeEvents(string objectName)
        {
            foreach (var (eventName, funcName) in _EventMap)
                AddCallback(eventName, objectName, funcName);
        }

        [DllImport("__Internal")]
        private static extern void JsLibRequestToken(string roomName, string userId, string customerId, string tokenRequestUrl, Action<string> callback);

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void ReceiveToken(string token)
        {
            if (_Sources.Remove("GetToken", out TaskCompletionSource<string> response))
                response.TrySetResult(token);

            if (OdinDefaults.Debug)
                Debug.Log($"#Odin-JS ReceiveToken: {token}");
        }

        /// <summary>
        /// Call the JS bridge to request a token from the 4players token-server
        /// </summary>
        /// <param name="roomName">room name to join</param>
        /// <param name="userId">joining username</param>
        /// <param name="customerId">customer id</param>
        /// <param name="tokenRequestUrl">Api endpoint fromn which the odin room token should be requested</param>
        /// <returns>token</returns>
        public static Task<string> GetToken(string roomName, string userId, string customerId, string tokenRequestUrl)
        {
            var result = new TaskCompletionSource<string>();
            if (_Sources.TryAdd("GetToken", result))
                OdinWebRoom.JsLibRequestToken(roomName, userId, customerId, tokenRequestUrl, ReceiveToken);

            return result.Task;
        }

        /// <summary>
        /// Uses UnityWebRequest with POST data as json to get a response from a token-server
        /// </summary>
        /// <remarks>Default callback will set <see cref="Token"/> with the response plain text</remarks>
        /// <param name="url">Token-Server endpoint</param>
        /// <param name="jsonPayload">Request data</param>
        /// <returns>IEnumerator for Coroutine</returns>
        public IEnumerator WebRequestToken(string url, string jsonPayload) => WebRequestToken(url, jsonPayload, (response) => this.Token = response.text);
        /// <summary>
        /// Uses UnityWebRequest with POST data as json to get a response from a token-server
        /// </summary>
        /// <param name="url">Token-Server endpoint</param>
        /// <param name="jsonPayload">Request data</param>
        /// <param name="response">Response callback</param>
        /// <returns>IEnumerator for Coroutine</returns>
        public IEnumerator WebRequestToken(string url, string jsonPayload, UnityAction<DownloadHandler> response)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();


#if UNITY_2020_3_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
                    Debug.LogError(www.error);
                else
                    response?.Invoke(www.downloadHandler);
#else
                if (string.IsNullOrEmpty(www.error))
                    response?.Invoke(www.downloadHandler);
                else
                    Debug.LogError(www.error);
#endif
            }
        }

        [Serializable]
        public struct CaptureParamsData
        {
            public RtcParamsData rtc;
            public ApmParamsData apm;

            public static CaptureParamsData Default()
            {
               return new CaptureParamsData()
               {
                   rtc = new RtcParamsData()
                   {
                       vad_attackThreshold = 0.9f,
                       vad_releaseThreshold = 0.8f,
                       vg_attackThreshold = -30.0f,
                       vg_releaseThreshold = -40.0f,
                   },
                   apm = new ApmParamsData()
                   {
                       echoCanceller = true,
                       highPassFilter = false,
                       preAmplifier = false,
                       captureLevelAdjustment = false,
                       noiseSuppression = "Moderate",
                       transientSuppressor = false,
                       gainController = true,
                   },
               };
            }
        }
        [Serializable]
        public struct RtcParamsData
        {
            public float vad_attackThreshold;
            public float vad_releaseThreshold;
            public float vg_attackThreshold;
            public float vg_releaseThreshold;
        }
        [Serializable]
        public struct ApmParamsData
        {
            public bool echoCanceller;
            public bool highPassFilter;
            public bool preAmplifier;
            public bool captureLevelAdjustment;
            public string noiseSuppression;
            public bool transientSuppressor;
            public bool gainController;
        }

        [DllImport("__Internal")]
        private static extern void JsLibCreateCapture(string args);
        /// <summary>
        /// Create and link an input media with vad/apm settings to the room
        /// </summary>
        /// <remarks>Currently only default device</remarks>
        /// <param name="settings">vad/apm settings</param>
        public void CreateCapture(CaptureParamsData settings)
        {
            string args = JsonUtility.ToJson(settings);
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS CreateCapture: {args}");
            OdinWebRoom.JsLibCreateCapture(args);
        }

        [DllImport("__Internal")]
        private static extern void JsLibLinkCapture();
        /// <summary>
        /// Link an input media from the room
        /// </summary>
        public void LinkCaptureMedia()
        {
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS LinkCapture");
            OdinWebRoom.JsLibLinkCapture();
        }

        [DllImport("__Internal")]
        private static extern void JsLibUnlinkCapture();
        /// <summary>
        /// Unlink an input media from the room
        /// </summary>
        public void UnlinkCaptureMedia()
        {
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS UnlinkCapture");
            OdinWebRoom.JsLibUnlinkCapture();
        }

        [DllImport("__Internal")]
        private static extern void JsLibStartPlayback(string media_uid);
        /// <summary>
        /// Starts an output media
        /// </summary>
        /// <param name="media_uid">unique id (default guid)</param>
        public void StartPlaybackMedia(string media_uid)
        {
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS StartPlayback: {media_uid}");
            OdinWebRoom.JsLibStartPlayback(media_uid);
        }

        [DllImport("__Internal")]
        private static extern void JsLibStopPlayback(string media_uid);
        /// <summary>
        /// Stops an output media
        /// </summary>
        /// <param name="media_uid">unique id (default guid)</param>
        public void StopPlaybackMedia(string media_uid)
        {
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS StopPlayback: {media_uid}");
            OdinWebRoom.JsLibStopPlayback(media_uid);
        }

        [Serializable]
        private struct SendMessageData
        {
            public byte[] data;
            public ulong[] peerIds;
        }

        [DllImport("__Internal")]
        private static extern void JsLibSendMessage(string args);

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="peerIds">optional target peers array</param>
        public void SendMessage(string message, ulong[] peerIds = null) => SendMessage(string.IsNullOrEmpty(message) ? new byte[0] : Encoding.UTF8.GetBytes(message), peerIds);
        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="peerIds">optional target peers array</param>
        public void SendMessage(byte[] message, ulong[] peerIds = null)
        {
            if (message == null) message = new byte[0];
            if (peerIds == null) peerIds = new ulong[0];

            string args = JsonUtility.ToJson(new SendMessageData { data = message, peerIds = peerIds });
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS SendOdinMessage: {args}");
            OdinWebRoom.JsLibSendMessage(args);
        }

        [Serializable]
        private struct UserdataParamsData
        {
            public byte[] data;
        }
        [DllImport("__Internal")]
        private static extern void JsLibUpdateUserData(string args);

        /// <summary>
        /// Set UserData
        /// </summary>
        /// <param name="userdata">arbitrary string</param>
        public void UpdateUserData(string userdata) => UpdateUserData(Encoding.UTF8.GetBytes(userdata));
        /// <summary>
        /// Set UserData
        /// </summary>
        /// <param name="userdata">arbitrary container</param>
        public void UpdateUserData(UserData userdata) => UpdateUserData(userdata.ToBytes());
        /// <summary>
        /// Set UserData
        /// </summary>
        /// <param name="data">arbitrary bytes</param>
        public void UpdateUserData(byte[] data)
        {
            string args = JsonUtility.ToJson(new UserdataParamsData { data = data });
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS UpdateOdinUserData: {args}");
            OdinWebRoom.JsLibUpdateUserData(args);
        }

        /// <summary>
        /// Redirects audio buffer to javascript.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="isSilent"></param>
        public virtual void ProxyAudio(float[] buffer, int position, bool isSilent = false)
        {
            if (OdinDefaults.Debug) Debug.Log($"#Odin-CS ProxyAudio: ");
        }

        /// <summary>
        /// This will always return itself
        /// </summary>
        public T GetBaseRoom<T>() where T : IRoom
        {
            return (T)GetBaseRoom();
        }
        private IRoom GetBaseRoom()
        {
            return this;
        }

        /// <summary>
        /// Join a room based on token info
        /// </summary>
        /// <remarks>Cipher is not supported for WebGL. Redirect to <see cref="Join(string)"/></remarks>
        /// <param name="token">JWT</param>
        /// <param name="cipher">NotSupported</param>
        public bool Join(string token, OdinCipherHandle cipher) => this.Join(token);
    }
}

#endif
