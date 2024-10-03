using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core
{
    /// <summary>
    /// Exception type for the native ODIN runtime
    /// </summary>
    public class OdinException : Exception
    {
        /// <summary>
        /// OdinErrorCode
        /// </summary>
        public OdinError ErrorCode;
        /// <summary>
        /// OdinErrorCode container
        /// </summary>
        /// <param name="error">OdinError enum</param>
        /// <param name="message">odin error message</param>
        public OdinException(OdinError error, string message)
            : base(message)
        {
            ErrorCode = error;
        }

        /// <summary>
        /// OdinErrorCode container
        /// </summary>
        /// <param name="error">OdinError enum</param>
        /// <param name="message">odin error message</param>
        /// <param name="innerException">wrapper inner</param>
        public OdinException(OdinError error, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = error;
        }
    }
}
