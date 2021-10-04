using OdinNative.Core;
using OdinNative.Odin;
using UnityEngine;

namespace OdinNative.Unity
{
    /// <summary>
    /// UnityEditor UI component for instance config of <see cref="OdinDefaults"/>
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class OdinEditorConfig : MonoBehaviour
    {
        /// <summary>
        /// Enable additional Logs
        /// </summary>
        public bool Verbose = OdinDefaults.Verbose;

        [Header("Client")]
        public string ApiKey = OdinDefaults.ApiKey;
        public string Server = OdinDefaults.Server;
        public string UserDataText = OdinDefaults.UserDataText;

        [Header("Audio")]
        /// <summary>
        /// Microphone Sample-Rate
        /// </summary>
        public MediaSampleRate DeviceSampleRate = OdinDefaults.DeviceSampleRate;
        /// <summary>
        /// Microphone Channels
        /// </summary>
        public MediaChannels DeviceChannels = OdinDefaults.DeviceChannels;

        /// <summary>
        /// Playback Sample-Rate
        /// </summary>
        public MediaSampleRate RemoteSampleRate = OdinDefaults.RemoteSampleRate;
        /// <summary>
        /// Playback Channels
        /// </summary>
        public MediaChannels RemoteChannels = OdinDefaults.RemoteChannels;

        [Header("Events")]
        #region Events
        public bool PeerJoinedEvent = OdinDefaults.PeerJoinedEvent;
        public bool PeerLeftEvent = OdinDefaults.PeerLeftEvent;
        public bool PeerUpdatedEvent = OdinDefaults.PeerUpdatedEvent;
        public bool MediaAddedEvent = OdinDefaults.MediaAddedEvent;
        public bool MediaRemovedEvent = OdinDefaults.MediaRemovedEvent;
        #endregion Events

        [Header("Room")]
        public ulong TokenLifetime = OdinDefaults.TokenLifetime;
        #region Apm
        public bool VadEnable = OdinDefaults.VadEnable;
        public bool EchoCanceller = OdinDefaults.EchoCanceller;
        public bool HighPassFilter = OdinDefaults.HighPassFilter;
        public bool PreAmplifier = OdinDefaults.PreAmplifier;
        public Core.Imports.NativeBindings.OdinNoiseSuppsressionLevel NoiseSuppressionLevel = OdinDefaults.NoiseSuppressionLevel;
        public bool TransientSuppressor = OdinDefaults.TransientSuppressor;
        #endregion Apm

        internal string Identifier => SystemInfo.deviceUniqueIdentifier;
    }
}
