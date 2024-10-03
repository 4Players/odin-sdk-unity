using System;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Exception type for C# Wrapper
    /// </summary>
    class OdinWrapperException : Exception
    {
        public OdinWrapperException(string message) 
            : base(message)
        { }

#pragma warning disable CS8632 //UNITY The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. 
        public OdinWrapperException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Get the last native error message
        /// </summary>
        /// <remarks>null if not initialized or empty string if no errors occurred</remarks>
        /// <returns>error message or empty</returns>
        public static string GetLastError() => Odin.Library.IsInitialized ? Odin.Library.Methods.ErrorGetLastError() : null;
#pragma warning restore CS8632 //UNITY The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}
