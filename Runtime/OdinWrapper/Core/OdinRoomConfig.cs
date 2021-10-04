using System.Collections;
using System.Collections.Generic;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core
{
    /// <summary>
    /// Odin Room Apm configuration
    /// </summary>
    public class OdinRoomConfig
    {
        public bool VadEnable
        {
            get { return ApmConfig.vad_enable; }
            set { ApmConfig.vad_enable = value; }
        }

        public bool EchoCanceller
        {
            get { return ApmConfig.echo_canceller; }
            set { ApmConfig.echo_canceller = value; }
        }
        public bool HighPassFilter
        {
            get { return ApmConfig.high_pass_filter; }
            set { ApmConfig.high_pass_filter = value; }
        }
        public bool PreAmplifier
        {
            get { return ApmConfig.pre_amplifier; }
            set { ApmConfig.pre_amplifier = value; }
        }
        public OdinNoiseSuppsressionLevel NoiseSuppsressionLevel
        {
            get { return ApmConfig.noise_suppression_level; }
            set { ApmConfig.noise_suppression_level = value; }
        }
        public bool TransientSuppressor
        {
            get { return ApmConfig.transient_suppressor; }
            set { ApmConfig.transient_suppressor = value; }
        }

        internal bool RemoteConfig { get; private set; }

        public static explicit operator OdinRoomConfig(OdinApmConfig config) => new OdinRoomConfig(config);
        public static implicit operator OdinApmConfig(OdinRoomConfig config) => config.ApmConfig;

        private OdinApmConfig ApmConfig = new OdinApmConfig();

        private OdinRoomConfig(OdinApmConfig config) : this(config.vad_enable, config.echo_canceller, config.high_pass_filter, config.pre_amplifier, config.noise_suppression_level, config.transient_suppressor, true) { }
        public OdinRoomConfig(bool vadEnable = false, bool echoCanceller = false, bool highPassFilter = false, bool preAmplifier = false, OdinNoiseSuppsressionLevel noiseSuppsressionLevel = OdinNoiseSuppsressionLevel.None, bool transientSuppressor = false)
            : this(vadEnable, echoCanceller, highPassFilter, preAmplifier, noiseSuppsressionLevel, transientSuppressor, false) { }
        internal OdinRoomConfig(bool vadEnable, bool echoCanceller, bool highPassFilter, bool preAmplifier, OdinNoiseSuppsressionLevel noiseSuppsressionLevel, bool transientSuppressor, bool remote = false)
        {
            VadEnable = vadEnable;
            EchoCanceller = echoCanceller;
            HighPassFilter = highPassFilter;
            PreAmplifier = preAmplifier;
            NoiseSuppsressionLevel = noiseSuppsressionLevel;
            TransientSuppressor = transientSuppressor;
            RemoteConfig = remote;
        }
    }
}