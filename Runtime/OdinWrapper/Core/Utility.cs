using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core
{
    public static class Utility
    {
        /// <summary>
        /// Representative ErrorCode for Ok.
        /// </summary>
        public const uint OK = 0;

        public static int RateToSamples(MediaSampleRate sampleRate = MediaSampleRate.Hz48000, int milliseconds = 20)
        {
            return ((int)sampleRate / 1000) * milliseconds;
        }

        /// <summary>
        /// Local check if the error code is in range of errors.
        /// </summary>
        /// <param name="error">error code</param>
        /// <returns>true if error</returns>
        public static bool IsError(int error)
        {
            return (error >> 29) > 0;
        }
    }
}
