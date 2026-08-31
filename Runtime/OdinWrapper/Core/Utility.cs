using OdinNative.Wrapper;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OdinNative.Core
{
    /// <summary>
    /// 
    /// </summary>
    public static class Utility
    {
        public const float MIN_DBFS = -897.069f;

        /// <summary>
        /// Get Odin native buffer IntPtr data
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetNativeBuffer(IntPtr pointer, uint length)
        {
            /* Compatibility with Unity .NET prior to 4.5 (i.e 2.0) so we don't use Int32.MaxValue 0x7fffffff
             * MSDN: The maximum size in any single dimension is 2,147,483,591 (0x7FFFFFC7) for byte arrays 
             * and arrays of single-byte structures, and 2,146,435,071 (0X7FEFFFFF) for arrays containing other types. */
            ulong size = Math.Min(length, 0x7FFFFFC7);
            byte[] buffer = new byte[size];
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            return buffer;
        }

        public static bool Test(bool condition, string message)
        {
            if (condition) return false;
#pragma warning disable CS0618 // Type or member is obsolete
            OdinNative.OdinLog.Throw(new OdinWrapperException(message));
#pragma warning restore CS0618 // Type or member is obsolete
            return true;
        }

        /// <summary>
        /// Checks the return code for errors
        /// </summary>
        /// <param name="error">Odin error return code</param>
        /// <returns>false on error</returns>
        public static bool IsOk(Imports.NativeBindings.OdinError error)
        {
            return !(error < Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS);
        }
        /// <summary>
        /// Checks the return code for crypto status
        /// </summary>
        /// <param name="status">Odin crypto return status</param>
        /// <returns>false on error</returns>
        public static bool IsOk(Imports.NativeBindings.OdinCryptoPeerStatus status)
        {
            return !(status <= Imports.NativeBindings.OdinCryptoPeerStatus.ODIN_CRYPTO_PEER_STATUS_UNKNOWN);
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified <see cref="Imports.NativeBindings.OdinError">error</see>
        /// </summary>
        /// <param name="error">Odin error return code</param>
        /// <returns>string representation of <see cref="Imports.NativeBindings.OdinError">OdinError</see></returns>
        public static string OdinErrorToString(Imports.NativeBindings.OdinError error)
        {
            return Enum.GetName(typeof(Imports.NativeBindings.OdinError), error);
        }

        /// <summary>
        /// Retrieves last native error message
        /// </summary>
        /// <returns>error message</returns>
        public static string OdinLastErrorString()
        {
            return OdinWrapperException.GetLastError() ?? string.Empty;
        }

        /// <summary>
        /// Get sample size by samplerate and time
        /// </summary>
        /// <param name="sampleRate">samplerate in hz</param>
        /// <param name="ms">time in milliseconds</param>
        /// <returns>sample size</returns>
        public static int RateToSamples(uint sampleRate = 48000, int ms = 20)
        {
            return ((int)sampleRate / 1000) * ms;
        }

        internal static ulong ToDataId(byte[] data)
        {
            OdinLog.Assert(data != null, nameof(data));

            if (data.Length >= sizeof(ulong))
                return BitConverter.ToUInt64(data, 0);

            byte[] tmp = new byte[sizeof(ulong)];
            for (int i = 0; i < data.Length; ++i)
            {
                if (BitConverter.IsLittleEndian)
                    tmp[i] = data[i];
                else
                    tmp[tmp.Length - i - 1] = data[data.Length - i - 1];
            }
            return BitConverter.ToUInt64(tmp, 0);
        }

        [Flags]
        public enum ChannelMask : ulong
        {
            None = 0,
            Channel1 = (ulong)1 << 0,
            Channel2 = (ulong)1 << 1,
            Channel3 = (ulong)1 << 2,
            Channel4 = (ulong)1 << 3,
            Channel5 = (ulong)1 << 4,
            Channel6 = (ulong)1 << 5,
            Channel7 = (ulong)1 << 6,
            Channel8 = (ulong)1 << 7,
            Channel9 = (ulong)1 << 8,
            Channel10 = (ulong)1 << 9,
            Channel11 = (ulong)1 << 10,
            Channel12 = (ulong)1 << 11,
            Channel13 = (ulong)1 << 12,
            Channel14 = (ulong)1 << 13,
            Channel15 = (ulong)1 << 14,
            Channel16 = (ulong)1 << 15,
            Channel17 = (ulong)1 << 16,
            Channel18 = (ulong)1 << 17,
            Channel19 = (ulong)1 << 18,
            Channel20 = (ulong)1 << 19,
            Channel21 = (ulong)1 << 20,
            Channel22 = (ulong)1 << 21,
            Channel23 = (ulong)1 << 22,
            Channel24 = (ulong)1 << 23,
            Channel25 = (ulong)1 << 24,
            Channel26 = (ulong)1 << 25,
            Channel27 = (ulong)1 << 26,
            Channel28 = (ulong)1 << 27,
            Channel29 = (ulong)1 << 28,
            Channel30 = (ulong)1 << 29,
            Channel31 = (ulong)1 << 30,
            Channel32 = (ulong)1 << 31,
            Channel33 = (ulong)1 << 32,
            Channel34 = (ulong)1 << 33,
            Channel35 = (ulong)1 << 34,
            Channel36 = (ulong)1 << 35,
            Channel37 = (ulong)1 << 36,
            Channel38 = (ulong)1 << 37,
            Channel39 = (ulong)1 << 38,
            Channel40 = (ulong)1 << 39,
            Channel41 = (ulong)1 << 40,
            Channel42 = (ulong)1 << 41,
            Channel43 = (ulong)1 << 42,
            Channel44 = (ulong)1 << 43,
            Channel45 = (ulong)1 << 44,
            Channel46 = (ulong)1 << 45,
            Channel47 = (ulong)1 << 46,
            Channel48 = (ulong)1 << 47,
            Channel49 = (ulong)1 << 48,
            Channel50 = (ulong)1 << 49,
            Channel51 = (ulong)1 << 50,
            Channel52 = (ulong)1 << 51,
            Channel53 = (ulong)1 << 52,
            Channel54 = (ulong)1 << 53,
            Channel55 = (ulong)1 << 54,
            Channel56 = (ulong)1 << 55,
            Channel57 = (ulong)1 << 56,
            Channel58 = (ulong)1 << 57,
            Channel59 = (ulong)1 << 58,
            Channel60 = (ulong)1 << 59,
            Channel61 = (ulong)1 << 60,
            Channel62 = (ulong)1 << 61,
            Channel63 = (ulong)1 << 62,
            Channel64 = (ulong)1 << 63,
            All = Channel1 | Channel2 | Channel3 | Channel4 | Channel5 | Channel6 | Channel7 | Channel8 | Channel9 | Channel10 | Channel11 | Channel12 | Channel13 | Channel14 | Channel15 | Channel16 | Channel17 | Channel18 | Channel19 | Channel20 | Channel21 | Channel22 | Channel23 | Channel24 | Channel25 | Channel26 | Channel27 | Channel28 | Channel29 | Channel30 | Channel31 | Channel32 | Channel33 | Channel34 | Channel35 | Channel36 | Channel37 | Channel38 | Channel39 | Channel40 | Channel41 | Channel42 | Channel43 | Channel44 | Channel45 | Channel46 | Channel47 | Channel48 | Channel49 | Channel50 | Channel51 | Channel52 | Channel53 | Channel54 | Channel55 | Channel56 | Channel57 | Channel58 | Channel59 | Channel60 | Channel61 | Channel62 | Channel63 | Channel64
        }
    }
}
