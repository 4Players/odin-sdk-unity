using UnityEngine;

namespace OdinNative.Unity.Audio
{
    public class PlaybackStats : MonoBehaviour
    {
        public bool Log = false;
        public bool PauseAnimationCurve = true;

        private PlaybackComponent currentPlayback;
        private Core.Imports.NativeBindings.OdinAudioStreamStats currentStats;

        public uint PacketsTotal;
        public uint PacketsProcessed;
        public uint PacketsDroppedEarly;
        public uint PacketsDroppedLate;
        public uint PacketsDropped;
        public uint PacketsInvalid;
        public uint PacketsRepeated;
        public uint PacketsLost;
        public AnimationCurve PacketsAvailableDifference;

        void OnEnable()
        {
            currentStats = new Core.Imports.NativeBindings.OdinAudioStreamStats();
            currentPlayback = GetComponent<Audio.PlaybackComponent>();
        }

        private void Update()
        {
            if (Application.isEditor == false || currentPlayback == null) return;

            currentStats = currentPlayback.GetOdinAudioStreamStats();
            PacketsTotal = currentStats.packets_total;
            PacketsProcessed = currentStats.packets_processed;
            PacketsDroppedEarly = currentStats.packets_arrived_too_early;
            PacketsDroppedLate = currentStats.packets_arrived_too_late;
            PacketsDropped = currentStats.packets_dropped;
            PacketsInvalid = currentStats.packets_invalid;
            PacketsRepeated = currentStats.packets_repeated;
            PacketsLost = currentStats.packets_lost;
            var failed = PacketsDroppedEarly + PacketsDroppedLate + PacketsLost;
            if (failed > 0 && !PauseAnimationCurve)
            {
                PacketsAvailableDifference.AddKey(Time.time, (PacketsProcessed - failed) / ((PacketsProcessed + failed) / 2f) - 1f);
                PacketsAvailableDifference.Evaluate(Time.time);
            }

            if (Log) Debug.Log($"{Time.time} {nameof(Audio.PlaybackComponent)} {currentPlayback.gameObject.name} (id {currentPlayback.MediaStreamId}):" +
                $"{nameof(PacketsProcessed)} {PacketsProcessed}," +
                $"{nameof(PacketsDroppedEarly)} {PacketsDroppedEarly}," +
                $"{nameof(PacketsDroppedLate)} {PacketsDroppedLate}," +
                $"{nameof(PacketsDropped)} {PacketsDropped}," +
                $"{nameof(PacketsInvalid)} {PacketsInvalid}," +
                $"{nameof(PacketsRepeated)} {PacketsRepeated}," +
                $"{nameof(PacketsLost)} {PacketsLost}");
        }
    }
}
