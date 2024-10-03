using OdinNative.Unity;
using OdinNative.Unity.Audio;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using System;
using System.Collections;
using System.Text;
using UnityEngine;

public class SampleManager : MonoBehaviour
{
    public OdinRoom _Room;
    public string AccessKey;
    public string RoomName;
    public string UserId;

    public OdinMicrophoneReader _AudioInput;

    void Awake()
    {
        if (_Room == null)
            _Room = GetComponent<OdinRoom>();

        if (_AudioInput == null)
            _AudioInput = GetComponent<OdinMicrophoneReader>();
    }

    void Reset()
    {
        if (_Room == null)
            _Room = GetComponent<OdinRoom>();
        if (_AudioInput == null)
            _AudioInput = GetComponent<OdinMicrophoneReader>();
        
        RoomName = "Test";
        UserId = "DummyUsername";

        if (_AudioInput != null)
        {
#if UNITY_EDITOR
            UnityEditor.Events.UnityEventTools.AddPersistentListener(_AudioInput.OnAudioData, _Room.ProxyAudio);
#else
            _AudioInput.OnAudioData.AddListener(_Room.ProxyAudio);
#endif
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        if (_Room == null)
        {
            Debug.LogError($"This example can only work with a room component ({nameof(OdinRoom)})");
            return;
        }

        // Showcase token format
        // Generate a test token locally. see GenerateTestToken function in OdinRoom; *for production use WebRequestToken
        DateTime utc = DateTime.UtcNow;
        _Room.Token = ExampleKey(new ExampleTokenBody()
        {
            rid = RoomName, // room id
            uid = UserId, // username
            nbf = ((DateTimeOffset)utc).ToUnixTimeSeconds(), // token issue
            exp = ((DateTimeOffset)utc.AddMinutes(5)).ToUnixTimeSeconds() // token valid timeframe
        }.ToString(), AccessKey);

        // Showcase getting a token from a endpoint
        //StartCoroutine(GetToken());

        // reset event if PersistentListener is not set by prefab spawn
        if (_AudioInput && _AudioInput.OnAudioData?.GetPersistentEventCount() <= 0 && _Room != null)
            _AudioInput.OnAudioData.AddListener(_Room.ProxyAudio);
    }

    #region ExampleToken
    [Serializable]
    class TokenRequest
    {
        public string kid;
        public string token;
    }

    /// <summary>
    /// Request token from a web endpoint see <see href="https://www.npmjs.com/package/@4players/odin-tokens"/>
    /// </summary>
    IEnumerator GetToken() => _Room.WebRequestToken(
            "https://app-server.odin.4players.io/v1/token", 
            $"{{ \"roomId\": \"{RoomName}\",\"userId\": \"{UserId}\", \"customer\": \"\" }}", 
            (handler) => _Room.Token = JsonUtility.FromJson<TokenRequest>(handler.text).token);

    [Serializable]
    class ExampleTokenBody
    {
        public string rid;
        public string uid;
        public long nbf;
        public long exp;

        public override string ToString() => JsonUtility.ToJson(this);
    }

    private string ExampleKey(string body, string accesskey = "")
    {
        if (string.IsNullOrEmpty(accesskey))
        {
            string currentKey = OdinClient.CreateAccessKey();
            Debug.LogWarning($"Generated example key: \"{currentKey}\"");
            return OdinClient.CreateToken(currentKey, body);
        }
        else
        {
            Debug.LogWarning($"Using example key: \"{accesskey}\"");
            return OdinClient.CreateToken(accesskey, body);
        }
    }
    #endregion ExampleToken

    public void MarkGameobjectFromRoom(object sender, RoomJoinedEventArgs args)
    {
        Room eventRoom = args.Room as Room;
        Debug.Log($"Rename Gameobject \"{gameObject.name}\" to \"{eventRoom.Name}\"");
        gameObject.name = eventRoom.Name;
        Debug.Log($"RoomJoined : \"{eventRoom.Name}\"");

        var mic = gameObject.GetComponent<OdinMicrophoneReader>();
        if (mic == null)
        {
            Debug.LogError($"Missing required {nameof(OdinMicrophoneReader)} component");
            return;
        }

        // Example to create and start encoder for input 
        OdinRoom room = sender as OdinRoom;
        if (room.LinkInputMedia((uint)mic.MicrophoneSamplerate, mic.MicrophoneChannels > 1, out MediaEncoder encoder))
        {
            var mute = this.gameObject.GetComponent<OdinMuteAudioComponent>();
            if (mute != null)
                mute.Media = encoder;
        }
        else
            Debug.LogError($"can not create an encoder and/or start media!");
    }

    public void LogRoomMessage(object sender, MessageReceivedEventArgs args)
    {
        Debug.Log($"Room \"{(sender as Room).Name}\" got message ({args.Data.Length} bytes) from peer {args.PeerId}: \"{Encoding.UTF8.GetString(args.Data)}\"");
    }

    public void EchoRoomMessage(object sender, MessageReceivedEventArgs args)
    {
        (sender as Room).SendMessage(args.Data);
    }

    public void LogConnectionStatus(object sender, RoomStateChangedEventArgs args)
    {
        Debug.Log($"Connection status: {args.RoomState}");
    }
}
