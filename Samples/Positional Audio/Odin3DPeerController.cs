using OdinNative.Odin;
using OdinNative.Odin.Media;
using OdinNative.Odin.Peer;
using OdinNative.Odin.Room;
using OdinNative.Unity;
using OdinNative.Unity.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
 
    /// <summary>
    /// This script manages the creation, updating, and destruction of 3D game objects representing remote peers in an Odin room.
    /// It listens for events related to peer media creation, deletion, activity state changes, and room exit.
    /// When a new peer joins, it instantiates a prefab to represent the peer in the scene and adds spatial audio components for realistic sound.
    /// The script also updates visual feedback (like color changes) based on peer activity and handles the cleanup of peer objects when they leave the room.
    /// </summary>
    public class Odin3DPeerController : MonoBehaviour
    {
        /// <summary>
        /// Prefab instantiated for each peer. 
        /// </summary>
        [Tooltip("The prefab used to represent each peer.")]
        public GameObject prefab;
        
        /// <summary>
        /// List to store instantiated peer objects.
        /// </summary>
        private List<GameObject> PeersObjects = new List<GameObject>();
        private Color LastCubeColor;
       
        /// <summary>
        ///  Sets up event listeners and initializes peer objects.
        /// </summary>
        void Start()
        {
            // Add event listeners for Odin events.
            OdinHandler.Instance.OnCreatedMediaObject.AddListener(Instance_OnCreatedMediaObject);
            OdinHandler.Instance.OnDeleteMediaObject.AddListener(Instance_OnDeleteMediaObject);
            OdinHandler.Instance.OnMediaActiveStateChanged.AddListener(Instance_OnMediaActiveStateChanged);
            OdinHandler.Instance.OnRoomLeft.AddListener(Instance_OnRoomLeft);

            // Retrieve and set the user's data.
            var SelfData = OdinHandler.Instance.GetUserData();
            if (SelfData.IsEmpty())
            {
                // Set default user data if not present.
                SelfData = new CustomUserDataJsonFormat().ToUserData();
                OdinHandler.Instance.UpdateUserData(SelfData);
            }

            // Find and set up the player object in the scene.
            GameObject player = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
            if (player != null)
            {
                TextMesh label = player.GetComponentInChildren<TextMesh>();
                label.text = CustomUserDataJsonFormat.FromUserData(SelfData)?.name ?? player.name;
            }
        }

        /// <summary>
        /// Instantiates a new game object based on prefab.
        /// </summary>
        /// <returns>The instantiated GameObject.</returns>
        GameObject CreateObject()
        {
            return Instantiate(prefab, new Vector3(0, 0.5f, 6), Quaternion.identity);
        }

        /// <summary>
        /// Handles the event when a new media object is created.
        /// </summary>
        /// <param name="roomName">The name of the room.</param>
        /// <param name="peerId">The ID of the peer.</param>
        /// <param name="mediaStreamId">The ID of the media stream.</param>
        private void Instance_OnCreatedMediaObject(string roomName, ulong peerId, long mediaStreamId)
        {
            Room room = OdinHandler.Instance.Rooms[roomName];
            if (room == null || room.Self == null || room.Self.Id == peerId) return;

            // Create a new peer object using the prefab.
            var peerContainer = CreateObject();

            // Add a PlaybackComponent to the new peer object.
            PlaybackComponent playback = OdinHandler.Instance.AddPlaybackComponent(peerContainer, room.Config.Name, peerId, mediaStreamId);

            // Configure spatial audio settings for the playback component.
            playback.PlaybackSource.spatialBlend = 1.0f;
            playback.PlaybackSource.rolloffMode = AudioRolloffMode.Linear;
            playback.PlaybackSource.minDistance = 1;
            playback.PlaybackSource.maxDistance = 10;

            // Set the peer's label based on their user data.
            var data = CustomUserDataJsonFormat.FromUserData(room.RemotePeers[peerId]?.UserData);
            playback.gameObject.GetComponentInChildren<TextMesh>().text = data == null ?
                $"Peer {peerId} (Media {mediaStreamId})" :
                $"{data.name} (Peer {peerId} Media {mediaStreamId})";

            // Add the peer object to the list.
            PeersObjects.Add(playback.gameObject);
        }

        /// <summary>
        /// Handles changes in media active state for peers. Changes the peer object's color based on voice
        /// activity.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">Arguments containing media stream information.</param>
        private void Instance_OnMediaActiveStateChanged(object sender, MediaActiveStateChangedEventArgs args)
        {
            PlaybackComponent playback = PeersObjects
                .Select(obj => obj.GetComponent<PlaybackComponent>())
                .FirstOrDefault(p => p.MediaStreamId == args.MediaStreamId);
            if(playback == null) return;

            // Change the object's color based on media activity.
            Material cubeMaterial = playback.GetComponentInParent<Renderer>().material;
            if (playback.HasActivity)
            {
                LastCubeColor = cubeMaterial.color;
                cubeMaterial.color = Color.green;
            }
            else
                cubeMaterial.color = LastCubeColor;
        }

        /// <summary>
        /// Handles the event when a media object is deleted. Will find and destroy the gameobject, that was playing
        /// back the media stream's output.
        /// </summary>
        /// <param name="mediaStreamId">The ID of the media stream to delete.</param>
        private void Instance_OnDeleteMediaObject(long mediaStreamId)
        {
            GameObject obj = PeersObjects.FirstOrDefault(o => o.GetComponent<PlaybackComponent>()?.MediaStreamId == mediaStreamId);
            if (obj == null) return;

            // Remove the peer object from the list and destroy it.
            PeersObjects.Remove(obj);
            Destroy(obj);
        }

        /// <summary>
        /// Handles the event when a room is left.
        /// </summary>
        /// <param name="args">Arguments containing room information.</param>
        private void Instance_OnRoomLeft(RoomLeftEventArgs args)
        {
            GameObject[] objs = PeersObjects
                .Where(o => o.GetComponent<PlaybackComponent>()?.RoomName == args.RoomName)
                .ToArray();

            if (objs.Length <= 0) return;

            // Remove and destroy all objects related to the left room.
            foreach (var obj in objs)
            {
                PeersObjects.Remove(obj);
                Destroy(obj);
            }
        }

        /// <summary>
        /// Called when the script is destroyed.
        /// Cleans up all instantiated peer objects.
        /// </summary>
        private void OnDestroy()
        {
            foreach (var obj in PeersObjects)
            {
                if(obj)
                    Destroy(obj);
            }
        }
    }
}