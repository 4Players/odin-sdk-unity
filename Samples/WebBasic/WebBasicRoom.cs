using System;
using System.Text;
using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace io.fourplayers.odin.Samples.WebBasic
{
    /// <summary>
    /// Unity Example to show WebGL Odin js-bridge with the optional use of OdinRoom.
    /// </summary>
    /// <remarks>The javascript side can be controlled with OdinWebRoom only but more setup is needed</remarks>
    public class WebBasicRoom : MonoBehaviour
    {
        /// <summary>
        /// The prefab used to instantiate a new Odin Room component
        /// </summary>
        [SerializeField] private OdinRoom roomPrefab;
        /// <summary>
        /// The Odin room name to connect to
        /// </summary>
        [SerializeField] private string roomName = "Test";
        /// <summary>
        /// The user id with which the current client should connect
        /// </summary>
        [SerializeField] private string userId = "TestUser";
        /// <summary>
        /// Customer id. 
        /// </summary>
        [SerializeField] private string customerId = "default";
        /// <summary>
        /// The endpoint at which we'll request the Odin Token from. 
        /// </summary>
        [SerializeField] private string tokenRequestUrl = "https://app-server.odin.4players.io/v1/token";

        /// <summary>
        /// The input field for sending messages. Will use the text input there to send messages to other peers in the room.
        /// </summary>
        [SerializeField] private InputField exampleMessageInput;
        /// <summary>
        /// Displays all messages received from the room. Will additionally show Status Logs from Odin.
        /// </summary>
        [SerializeField] private Text messageDisplay;
        /// <summary>
        /// Used to ensure, that the Scroll view will always stick to the buttom, when a new message or Log is displayed on
        /// the message display
        /// </summary>
        [SerializeField] private Scrollbar messageDisplayScrollbar;

        /// <summary>
        /// Event that will be invoked, after the web basic room instantiated a new Odin Room and will 
        /// </summary>
        [SerializeField] private UnityEvent<OdinRoom> onRequestedJoin;
        

        /// <summary>
        /// Counter that goes up each time we update the user data. Used to show, that user data is changing between updates
        /// </summary>
        private int _userDataUpdateCount = 0;
        /// <summary>
        /// The current Odin Room component used as connection
        /// </summary>
        private OdinRoom _roomComponent;
        /// <summary>
        /// Used to store and build up the messages shown on screen
        /// </summary>
        private readonly StringBuilder _messageStringBuilder = new();


#if UNITY_EDITOR
        private UnityEditor.BuildTarget Target;
#endif
        private void Awake()
        {
#if UNITY_EDITOR
            Target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
#endif
            Assert.IsNotNull(messageDisplay, "MessageDisplay != null");
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Target != UnityEditor.BuildTarget.WebGL)
            {
                Debug.LogError($"Sample {nameof(WebBasicRoom)} is only for WebGL!");
                enabled = false;
                return;
            }
#endif
        }
        
        /// <summary>
        /// Will listen to relevant Odin events in the current room component
        /// </summary>
        private void RegisterCallbacks()
        {
            if (_roomComponent)
            {
                _roomComponent.OnRoomJoined.AddListener(Example_OnRoomJoined);
                _roomComponent.OnPeerJoined.AddListener(Example_OnPeerJoined);
                _roomComponent.OnPeerLeft.AddListener(Example_OnPeerLeft);
                _roomComponent.OnUserDataChanged.AddListener(Example_OnUserDataChanged);
                _roomComponent.OnMediaAdded.AddListener(Example_OnMediaAdded);
                _roomComponent.OnMediaRemoved.AddListener(Example_OnMediaRemoved);
                _roomComponent.OnMessageReceived.AddListener(Example_OnMessageLog);
                _roomComponent.OnRoomStateChanged.AddListener(Example_OnRoomStateChanged);

                _roomComponent.GetBaseRoom<IRoom>().OnRoomLeft += Example_OnRoomLeft;
            }
        }
        
        /// <summary>
        /// Will remove callbacks from room component
        /// </summary>
        private void DeregisterCallbacks()
        {
            if (_roomComponent)
            {
                _roomComponent.OnRoomJoined.RemoveListener(Example_OnRoomJoined);
                _roomComponent.OnPeerJoined.RemoveListener(Example_OnPeerJoined);
                _roomComponent.OnPeerLeft.RemoveListener(Example_OnPeerLeft);
                _roomComponent.OnUserDataChanged.RemoveListener(Example_OnUserDataChanged);
                _roomComponent.OnMediaAdded.RemoveListener(Example_OnMediaAdded);
                _roomComponent.OnMediaRemoved.RemoveListener(Example_OnMediaRemoved);
                _roomComponent.OnMessageReceived.RemoveListener(Example_OnMessageLog);
                _roomComponent.OnRoomStateChanged.RemoveListener(Example_OnRoomStateChanged);
                _roomComponent.GetBaseRoom<IRoom>().OnRoomLeft -= Example_OnRoomLeft;

            }
        }

        /// <summary>
        /// Called when the Odin Room was joined. 
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The Event arguments.</param>
        private void Example_OnRoomJoined(object sender, RoomJoinedEventArgs args)
        {
            Log($"Room \"{args.Room.Id}\" joined");
        }

        /// <summary>
        /// Called when the Odin Room was left.
        /// </summary>
        /// <param name="arg">The event arguments.</param>
        private void Example_OnRoomLeft(object sender, string reason)
        {
            Log($"Room left, reason: {reason}");
        }

        /// <summary>
        /// Called when the Odin room state changed, e.g. "Joining", "Joined", "Closed"
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments.</param>
        private void Example_OnRoomStateChanged(object sender, RoomStateChangedEventArgs args)
        {
            Log($"Room state changed, new room state: {args.RoomState}");
        }

        /// <summary>
        /// Called when a remote peer joined the room.
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments.</param>
        private void Example_OnPeerJoined(object sender, PeerJoinedEventArgs args)
        {
            Log($"Peer {args.PeerId} joined ");
        }

        /// <summary>
        /// Called when a remote peer left the current room.
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments.</param>
        private void Example_OnPeerLeft(object sender, PeerLeftEventArgs args)
        {
            Log($"Peer {args.PeerId} left {args.PeerId}");
        }
        
        /// <summary>
        /// Called when the user data of a remote peer changed. Please take a look at https://www.4players.io/odin/guides/unity/user-data/
        /// for more information on how to use user data for your application's custom needs. 
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments.</param>
        private void Example_OnUserDataChanged(object sender, PeerUserDataChangedEventArgs args)
        {
            string userDataAsString = args.UserData.ToString();
            Log($"User Data Update of Peer {args.PeerId}: {userDataAsString}");
        }

        /// <summary>
        /// Called when a new media stream was added to the room. A media stream plays back the voice input from a
        /// remote peer.
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments</param>
        private void Example_OnMediaAdded(object sender, MediaAddedEventArgs args)
        {
            Log($"Media {args.MediaId} added, MediaUId: {args.MediaUId} ");
#if UNITY_WEBGL
            // Will start up the microphone input in the browser and start sending microphone data to Odin
            _roomComponent.ResumeOutputMedia(args.MediaUId);
#endif
        }

        /// <summary>
        /// Called when a media stream was removed from the room. Usually means a remote peer has deactivated their
        /// microphone or the peer left the room.
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event argumetns</param>
        private void Example_OnMediaRemoved(object sender, MediaRemovedEventArgs args)
        {
            Log($"Media {args.MediaId} removed");
        }

        /// <summary>
        /// Called when a message was sent to this peer by another peer in the room.
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">Event arguments</param>
        private void Example_OnMessageLog(object sender, MessageReceivedEventArgs args)
        {
            var textMessage = args.Data.Length > 0 ? Encoding.UTF8.GetString(args.Data) : string.Empty;
            Log(
                $"[Peer {args.PeerId}] (Msg): \"{textMessage}\"");
        }


#if UNITY_WEBGL
        /// <summary>
        /// Join an Odin room with the name <see cref="roomName"/>.The function will create a new OdinRoom, request
        /// a token from the api at <see cref="tokenRequestUrl"/> and then start the room join process.
        /// </summary>
        public async void JoinRoom()
        {
            if (null == _roomComponent)
            {
                _roomComponent = Instantiate(roomPrefab.gameObject, transform).GetComponent<OdinRoom>();
                Debug.Log("Called on instantiated room");
            }
            onRequestedJoin?.Invoke(_roomComponent);
            RegisterCallbacks();
            Log("Joining room ...");
            var token = await OdinWebRoom.GetToken(roomName, userId, customerId, tokenRequestUrl);
            var bJoinedSuccessfully = _roomComponent.Join(token);
            if (!bJoinedSuccessfully) Log("Failed joining room.");
        }

        /// <summary>
        /// Leaves the current Odin room.
        /// </summary>
        public void LeaveRoom()
        {
            if (_roomComponent)
            {
                Log("Leaving room ...");
                _roomComponent.GetBaseRoom<OdinWebRoom>().Leave();
                DeregisterCallbacks();
            }
            else
            {
                Log("Leaving room failed, Room Component is invalid.");
            }
            
        }

        /// <summary>
        /// Create an input capture media and link it to the room. This will start the microphone in the browser, create
        /// a new media stream and add it to the currently joined Odin room. Will be handled by the Odin javascript library.
        /// </summary>
        public void LinkCaptureMedia()
        {
            Log("Starting Microphone Capturing...");
            _roomComponent?.LinkInputMedia(OdinWebRoom.CaptureParamsData.Default());
        }

        /// <summary>
        /// Remove the current capture media stream from the room and close the browser capture stream.
        /// This will make the browser audio capture icon disappear.
        /// </summary>
        public void UnlinkCaptureMedia()
        {
            Log("Stopping Capture Stream...");
            _roomComponent?.UnlinkInputMedia();
        }

        /// <summary>
        /// Send a example string message in web-client compatible json format to all peers in the room.
        /// </summary>
        /// <remarks>Usually used to send a byte[] message of arbitrary data</remarks>
        public void SendMessageAll()
        {
            var message = "ExampleText";
            if (exampleMessageInput != null)
                message = exampleMessageInput.text;

            Log($"Sending message: {message}");
            _roomComponent?.GetBaseRoom<OdinWebRoom>()
                .SendMessage($"{{\"kind\": \"message\", \"payload\": \"{message}\"}}");
            exampleMessageInput.text = "";
        }

        /// <summary>
        /// User data can be used to share arbitrary data about the current peer with other peers in the room.
        /// </summary>
        /// <remarks>Please take a look at https://www.4players.io/odin/guides/unity/user-data/ for more information on
        /// using custom user data </remarks>
        public void UpdateUserdata()
        {
            _userDataUpdateCount++;
            var exampleUserdata =
                new UserData(
                    $"{{\"userId\":\"{userId}\",\"roomId\":\"{roomName}\",\"updateCount\":\"{_userDataUpdateCount}\"}}");
            Log($"Updating own peer userdata with example data: {exampleUserdata.ToString()}");

            _roomComponent?.GetBaseRoom<OdinWebRoom>().UpdateUserData(exampleUserdata);
        }
#endif

        /// <summary>
        /// Helper function for both logging a message in the console and showing it in the on-screen chat
        /// </summary>
        /// <param name="message"></param>
        private void Log(string message)
        {
            Debug.Log($"Unity WebGL Sample: {message}");
            _messageStringBuilder.AppendLine(message);
            if (messageDisplay)
            {
                messageDisplay.text = _messageStringBuilder.ToString();
                if (messageDisplayScrollbar) messageDisplayScrollbar.value = 0;
            }
        }
        
        private void OnDestroy()
        {
            Log("WebBasicRoom is being destroyed");
            DeregisterCallbacks();
        }
    }
}