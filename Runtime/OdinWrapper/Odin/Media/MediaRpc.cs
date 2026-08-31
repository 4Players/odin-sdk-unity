using System.Collections.Generic;

namespace OdinNative.Wrapper.Media.Rpc
{
    public class AudioParametersObject
    {
        public Dictionary<uint, ulong> audio_map;
    }

    public class VideoParametersObject
    {
        public Dictionary<uint, uint> video_map;
    }
}
