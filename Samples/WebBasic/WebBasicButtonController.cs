using System;
using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using UnityEngine;
using UnityEngine.UI;

namespace io.fourplayers.odin.Samples.WebBasic
{
    public class WebBasicButtonController : MonoBehaviour
    {
        [Tooltip("The buttons that should be only interactable, when an Odin Room was joined.")]
        [SerializeField] private Button[] availableOnlyWhenJoined;
        [Tooltip("The buttons that should be only interactable, when currently not connected to Odin.")]
        [SerializeField] private Button[] availableOnlyWhenNotJoined;
        [Tooltip("The button that activates the microphone.")]
        [SerializeField] private Button activateMicButton;
        [Tooltip("The button that deactivates the microphone.")]
        [SerializeField] private Button deactivateMicButton;
        
        private OdinRoom _odinRoom;
        
        /// <summary>
        /// Will initialize the buttons to only show those available when not already joined.
        /// </summary>
        private void Start()
        {
            HandleJoining(false);
            HandleMicActive(false);
        }

        private void OnDestroy()
        {
            DeregisterCallbacks();
        }
        
        /// <summary>
        /// Adds listeners to relevant Odin Room Event Callbacks. Based on this relevant buttons will be made interactable
        /// or not-interactable
        /// </summary>
        private void RegisterCallbacks()
        {
            if(_odinRoom)
            {
                _odinRoom.OnRoomJoined.AddListener(OnRoomJoinedCallback);
                activateMicButton.onClick.AddListener(OnActivateClicked);
                deactivateMicButton.onClick.AddListener(OnDeactivateClicked);

                
                IRoom baseRoom = _odinRoom.GetBaseRoom<IRoom>();
                baseRoom.OnRoomLeft += OnRoomLeft;
            }
            else
            {
                Debug.LogError("Unity WebGL Sample: Tried registered callbacks, but Odin Room Component is invalid");
            }
        }

        /// <summary>
        /// Removes Listeners from Odin Event Callbacks. Do this when leaving a room.
        /// </summary>
        private void DeregisterCallbacks()
        {
            if(_odinRoom)
            {
                _odinRoom.OnRoomJoined.RemoveListener(OnRoomJoinedCallback);

                activateMicButton.onClick.RemoveListener(OnActivateClicked);
                deactivateMicButton.onClick.RemoveListener(OnDeactivateClicked);

                IRoom baseRoom = _odinRoom.GetBaseRoom<IRoom>();
                baseRoom.OnRoomLeft -= OnRoomLeft;
            }
        }

        /// <summary>
        /// Called when a new OdinRoom component was created and joined. Will register listeners to the odin room events
        /// </summary>
        /// <param name="room">The OdinRoom that we'll connect to.</param>
        public void OnRoomWasInstantiated(OdinRoom room)
        {
            _odinRoom = room;
            RegisterCallbacks();
        }

        /// <summary>
        /// Called when an Odin Room was joined. Will update the UI accordingly
        /// </summary>
        /// <param name="sender">The <see cref="Room"/> object from which the event was sent.</param>
        /// <param name="args">The event arguments</param>
        private void OnRoomJoinedCallback(object sender, RoomJoinedEventArgs args)
        {
            HandleJoining(true);
            HandleMicActive(false);
        }

        /// <summary>
        /// Called when the current Odin Room was left. Will update the UI accordingly
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="reason">The reason for leaving the odin room.</param>
        private void OnRoomLeft(object sender, string reason)
        {
            DeregisterCallbacks();
            HandleJoining(false);
            activateMicButton.interactable = false;
            deactivateMicButton.interactable = false;
        }

        /// <summary>
        /// Called when Microphone activation was requested. Will disable "Activate Microphone" and enable "Deactivate Microphone".
        /// </summary>
        private void OnActivateClicked()
        {
            HandleMicActive(true);
        }
        
        /// <summary>
        /// Called when Microphone deactivation was requested. Will enable "Activate Microphone" and disable "Deactivate Microphone".
        /// </summary>
        private void OnDeactivateClicked()
        {
            HandleMicActive(false);
        }

       

        /// <summary>
        /// Handles button interactable settings, if the room joined state is changed.
        /// </summary>
        /// <param name="newIsJoinedState">Whether the room was joined or not</param>
        private void HandleJoining(bool newIsJoinedState)
        {
            foreach (Button onlyWhenJoined in availableOnlyWhenJoined)
            {
                if (onlyWhenJoined != null) onlyWhenJoined.interactable = newIsJoinedState;
            }
            foreach (Button onlyWhenNotJoined in availableOnlyWhenNotJoined)
            {
                if (onlyWhenNotJoined != null) onlyWhenNotJoined.interactable = !newIsJoinedState;
            }
        }

        /// <summary>
        /// Handles the microphone interaction settings based on whether the microphone is on or off
        /// </summary>
        /// <param name="microphoneIsOn"></param>
        private void HandleMicActive(bool microphoneIsOn)
        {
            if (activateMicButton != null) activateMicButton.interactable = !microphoneIsOn && _odinRoom.IsJoined;
            if (deactivateMicButton != null) deactivateMicButton.interactable = microphoneIsOn && _odinRoom.IsJoined;
        }
    }
}