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
        
        public string AccessKey;
        public string Server = OdinDefaults.Server;
        public string UserDataText = OdinDefaults.UserDataText;
        
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
        
        #region Events
        public bool PeerJoinedEvent = OdinDefaults.PeerJoinedEvent;
        public bool PeerLeftEvent = OdinDefaults.PeerLeftEvent;
        public bool PeerUpdatedEvent = OdinDefaults.PeerUpdatedEvent;
        public bool MediaAddedEvent = OdinDefaults.MediaAddedEvent;
        public bool MediaRemovedEvent = OdinDefaults.MediaRemovedEvent;
        #endregion Events
        
        /// <summary>
        /// Time untill the token expires
        /// </summary>
        public ulong TokenLifetime = OdinDefaults.TokenLifetime;
        #region Apm
        /// <summary>
        /// Turns VAC on and off
        /// </summary>
        public bool VadEnable = OdinDefaults.VadEnable;
        /// <summary>
        /// Turns Echo cancellation on and off
        /// </summary>
        public bool EchoCanceller = OdinDefaults.EchoCanceller;
        /// <summary>
        /// Reduces lower frequencies of the input (Automatic game control)
        /// </summary>
        public bool HighPassFilter = OdinDefaults.HighPassFilter;
        /// <summary>
        /// Amplifies the audio input
        /// </summary>
        public bool PreAmplifier = OdinDefaults.PreAmplifier;
        /// <summary>
        /// Turns noise suppression on and off
        /// </summary>
        public Core.Imports.NativeBindings.OdinNoiseSuppsressionLevel NoiseSuppressionLevel = OdinDefaults.NoiseSuppressionLevel;
        /// <summary>
        /// Filters high amplitude noices
        /// </summary>
        public bool TransientSuppressor = OdinDefaults.TransientSuppressor;
        #endregion Apm

        internal string Identifier => SystemInfo.deviceUniqueIdentifier;
    }
}
