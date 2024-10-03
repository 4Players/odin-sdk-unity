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
            OdinNative.Core.Utility.Throw(new OdinWrapperException(message));
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

#if UNITY_STANDALONE || UNITY_EDITOR || ENABLE_IL2CPP || ENABLE_MONO
        [Obsolete("Future versions of Unity are expected to always throw exceptions and not have Assertions.Assert._raiseExceptions https://docs.unity3d.com/ScriptReference/Assertions.Assert.html")]
#endif
        internal static void Throw(Exception e, bool assertion = false)
        {
#if !UNITY_STANDALONE && !UNITY_EDITOR && !ENABLE_IL2CPP && !ENABLE_MONO
            if (assertion)
                Debug.WriteLine(e.ToString());
            else
                throw e;
#else
            if (assertion)
                UnityEngine.Debug.LogAssertion(e);
            else
                UnityEngine.Debug.LogException(e);
#endif
        }

        [Conditional("DEBUG"), Conditional("UNITY_ASSERTIONS"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal static void Assert(bool condition = false) => Assert(condition, Wrapper.OdinWrapperException.GetLastError() ?? "");
        [Conditional("DEBUG"), Conditional("UNITY_ASSERTIONS"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal static void Assert(bool condition = false, string message = "")
        {
            if (OdinDefaults.Debug) Debug.WriteLine($"Assert: {condition} \"{message}\" {(condition ? "OK" : "\n"+Environment.StackTrace)}");
            if (condition) return;
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new Wrapper.OdinWrapperException(message), true);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
