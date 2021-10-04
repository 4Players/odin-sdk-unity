using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Odin
{
    /// <summary>
    /// Odin UserData helper for marshal byte arrays
    /// </summary>
    public sealed class UserData
    {
        public Encoding Encoding { get; set; }
        private byte[] Buffer { get; set; }

        public static implicit operator string(UserData userdata) => userdata.ToString();
        public static implicit operator byte[](UserData userdata) => userdata.ToBytes();

        internal UserData() : this(string.Empty, Encoding.UTF8) { }
        public UserData(string text) : this(text, Encoding.UTF8) { }
        public UserData(string text, Encoding encoding) : this(encoding.GetBytes(text), encoding) { }
        public UserData(byte[] data) : this(data, null) { }
        public UserData(byte[] data, Encoding encoding)
        {
            Encoding = encoding ?? Encoding.UTF8;
            Buffer = data;
        }

        public void CopyFrom(IntPtr ptr, uint size)
        {
            Buffer = new byte[size];
            Marshal.Copy(ptr, Buffer, 0, Buffer.Length);
        }

        public bool IsEmpty()
        {
            return Buffer.Length == 0 || string.IsNullOrEmpty(this.ToString()) || this.ToString() == "{}";
        }

        public bool Contains(string value)
        {
            return this.ToString()
                .Contains(value);
        }

        public override string ToString()
        {
            return Encoding.GetString(Buffer);
        }

        /// <summary>
        /// Get Userdata
        /// </summary>
        /// <returns>Copy of byte buffer</returns>
        public byte[] ToBytes()
        {
            return Buffer.ToArray();
        }
    }
}
