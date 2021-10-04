using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core
{
    /// <summary>
    /// Exception type for ODIN ffi libary
    /// </summary>
    public class OdinException : Exception
    {
        public uint ErrorCode;

        public OdinException(uint error, string message)
            : base(message)
        {
            ErrorCode = error;
        }

        public OdinException(uint error, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = error;
        }
    }
}
