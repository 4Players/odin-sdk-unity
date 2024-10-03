using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OdinNative.Utils.MessagePack
{
    /// <summary>
    /// Msgpack RPC spec <see href="https://github.com/msgpack-rpc/msgpack-rpc/blob/master/spec.md"/>
    /// </summary>
    public enum MsgPackMessageType
    {
        /// <summary>
        /// Format: [type, msgid, method, params]
        /// </summary>
        Request = 0,
        /// <summary>
        /// Format: [type, msgid, error, result]
        /// </summary>
        Response,
        /// <summary>
        /// Format: [type, method, params]
        /// </summary>
        Notification
    }

    /// <summary>
    /// Msgpack RPC spec <see href="https://github.com/msgpack-rpc/msgpack-rpc/blob/master/spec.md"/>
    /// </summary>
    public static class RpcFormat
    {
        /// <summary>
        /// <c>PositiveFixInt</c> field (type)
        /// </summary>
        public const int TypeIndex = 0;

        /// <summary>
        /// Msgpack RPC spec <see href="https://github.com/msgpack-rpc/msgpack-rpc/blob/master/spec.md"/>
        /// </summary>
        public static class Request
        {
            /// <summary>
            /// <c>uint32</c> field (msgid)
            /// </summary>
            public const int MsgidIndex = 1;
            /// <summary>
            /// <c>string</c> field (method)
            /// </summary>
            public const int MethodIndex = 2;
            /// <summary>
            /// <c>binary</c> field (params)
            /// </summary>
            public const int ParamsIndex = 3;
        }

        /// <summary>
        /// Msgpack RPC spec <see href="https://github.com/msgpack-rpc/msgpack-rpc/blob/master/spec.md"/>
        /// </summary>
        public static class Response
        {
            /// <summary>
            /// <c>uint32</c> field (msgid)
            /// </summary>
            public const int MsgidIndex = 1;
            /// <summary>
            /// <c>binary|string|null</c> field (error)
            /// </summary>
            public const int ErrorIndex = 2;
            /// <summary>
            /// <c>binary|null</c> field (result)
            /// </summary>
            public const int ResultIndex = 3;
        }

        /// <summary>
        /// Msgpack RPC spec <see href="https://github.com/msgpack-rpc/msgpack-rpc/blob/master/spec.md"/>
        /// </summary>
        public static class Notification
        {
            /// <summary>
            /// <c>string</c> field (method)
            /// </summary>
            public const int MethodIndex = 1;
            /// <summary>
            /// <c>params</c> field (params)
            /// </summary>
            public const int ParamsIndex = 2;
        }
    }

    /// <summary>
    /// General Msgpack interface that will be used for sending RPC to the server.
    /// </summary>
    /// <remarks>For the default protocol see <see href="https://github.com/msgpack/msgpack/blob/master/spec.md"/></remarks>
    public interface IMsgPackWriter : IDisposable
    {
        Encoding Encoding { get; }

        void Append(byte[] bytes);
        bool GetBuffer(out ArraySegment<byte> buffer);
        void Clear();
        byte[] GetBytes();
        long GetLength();
        int Write(int value);
        int Write(uint value);
        int WriteArrayHeader(uint count);
        int WriteBinary(byte[] value);
        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteDouble(double value);
        void WriteFloat(float value);
        void WriteInt(int value);
        void WriteLong(long value);
        int WriteMapHeader(uint count);
        void WriteSByte(sbyte value);
        void WriteShort(short value);
        int WriteString(string value);
        int WriteString(string value, Encoding encoding);
        void WriteUInt(uint value);
        void WriteULong(ulong value);
        void WriteUShort(ushort value);
    }

    /// <summary>
    /// Rudimentary Msgpack implementation that will be used for sending RPC to the server.
    /// </summary>
    /// <remarks>Custom Msgpack writer should follow <see href="https://github.com/msgpack/msgpack/blob/master/spec.md"/></remarks>
    public class MsgPackWriter : IMsgPackWriter
    {
        /// <summary>
        /// Used Encoding defaults to UTF8
        /// </summary>
        public Encoding Encoding { get; private set; }
        /// <summary>
        /// Indicates the value byte order
        /// </summary>
        public bool LittleEndianness { get; protected set; }

        MemoryStream memory; // only use MemoryStream() ctor for access to underlying buffer
        BinaryWriter writer;
        private bool disposedValue;

        /// <summary>
        /// Create Writer
        /// </summary>
        public MsgPackWriter()
            : this(Native.Encoding)
        { }

        /// <summary>
        /// Create Writer
        /// </summary>
        /// <param name="encoding">4Players online endpoints use UTF8</param>
        /// <param name="keep">keep or close the binary stream</param>
        public MsgPackWriter(Encoding encoding, bool keep = true)
        {
            Encoding = encoding ?? Native.Encoding;
            memory = new MemoryStream();
            writer = new BinaryWriter(memory, Encoding, keep);
            LittleEndianness = BitConverter.IsLittleEndian;
        }

        /// <summary>
        /// Clear stream buffer
        /// </summary>
        public void Clear()
        {
            byte[] buffer = memory.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            memory.Position = 0;
            memory.SetLength(0);
        }

        /// <summary>
        /// Create writer with a prefilled stream buffer
        /// </summary>
        /// <param name="bytes">data that will be pushed into the stream</param>
        /// <returns>writer</returns>
        public static MsgPackWriter Create(byte[] bytes) => Create(bytes, Native.Encoding);
        public static MsgPackWriter Create(byte[] bytes, Encoding encoding)
        {
            MsgPackWriter writer = new MsgPackWriter(encoding);
            writer.memory.Write(bytes, 0, bytes.Length);
            return writer;
        }

        /// <summary>
        /// Set MsgPackToken based on the unsigend value size
        /// </summary>
        /// <param name="value">value to write and identify size</param>
        /// <returns>size written</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int Write(uint value)
        {
            int size = 0;
            if (value <= MsgPackToken.MaxPositiveFixInt)
            {
                writer.Write((byte)value);
                size += 1;
            }
            else if (value <= byte.MaxValue)
            {
                this.WriteByte((byte)value);
                size += 2;
            }
            else if (value <= ushort.MaxValue)
            {
                this.WriteUShort((ushort)value);
                size += 3;
            }
            else
            {
                this.WriteUInt(value);
            }

            return size;
        }

        /// <summary>
        /// Set MsgPackToken based on the sigend value size
        /// </summary>
        /// <param name="value">value to write and identify size</param>
        /// <returns>size written</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int Write(int value)
        {
            int size = 0;

            if (value >= 0)
            {
                this.Write((uint)value);
            }
            else
            {
                if (value >= MsgPackToken.MinNegativeFixInt)
                {
                    writer.Write((byte)value);
                    size += 1;
                }
                else if (value >= sbyte.MinValue)
                {
                    this.WriteSByte((sbyte)value);
                    size += 2;
                }
                else if (value >= short.MinValue)
                {
                    this.WriteShort((short)value);
                    size += 3;
                }
                else
                {
                    this.WriteInt(value);
                    size += 4;
                }
            }

            return size;
        }

        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Uint8"/>
        /// </summary>
        public void WriteByte(byte value)
        {
            writer.Write(MsgPackToken.Uint8);
            writer.Write(value);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Int8"/>
        /// </summary>
        public void WriteSByte(sbyte value)
        {
            writer.Write(MsgPackToken.Int8);
            writer.Write(value);
        }

        protected void WriteShortValue(short value, bool isLittleEndian) => WriteUShortValue((ushort)value, isLittleEndian);
        protected void WriteUShortValue(ushort value, bool isLittleEndian) => writer.Write(isLittleEndian ?
                (ushort)(((value & 0x00FFU) << 8) | ((value & 0xFF00U) >> 8)) :
                value);
        protected void WriteFloatValue(float value, bool isLittleEndian)
        {
            if (isLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                foreach (byte b in bytes.Reverse())
                    writer.Write(b);
            } else
                writer.Write(value);
        }
        protected void WriteDoubleValue(double value, bool isLittleEndian)
        {
            if (isLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                foreach (byte b in bytes.Reverse())
                    writer.Write(b);
            }
            else
                writer.Write(value);
        }
        protected void WriteIntValue(int value, bool isLittleEndian) => WriteUIntValue((uint)value, isLittleEndian);
        protected void WriteUIntValue(uint value, bool isLittleEndian) => writer.Write(isLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
        protected void WriteLongValue(long value, bool isLittleEndian) => WriteULongValue((ulong)value, isLittleEndian);
        protected void WriteULongValue(ulong value, bool isLittleEndian) => writer.Write(isLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Int16"/>
        /// </summary>
        public void WriteShort(short value)
        {
            writer.Write(MsgPackToken.Int16);
            WriteShortValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Uint16"/>
        /// </summary>
        public void WriteUShort(ushort value)
        {
            writer.Write(MsgPackToken.Uint16);
            WriteUShortValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Int32"/>
        /// </summary>
        public void WriteInt(int value)
        {
            writer.Write(MsgPackToken.Int32);
            WriteIntValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Uint32"/>
        /// </summary>
        public void WriteUInt(uint value)
        {
            writer.Write(MsgPackToken.Uint32);
            WriteUIntValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Int64"/>
        /// </summary>
        public void WriteLong(long value)
        {
            writer.Write(MsgPackToken.Int64);
            WriteLongValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Uint64"/>
        /// </summary>
        public void WriteULong(ulong value)
        {
            writer.Write(MsgPackToken.Uint64);
            WriteULongValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Float32"/>
        /// </summary>
        public void WriteFloat(float value)
        {
            writer.Write(MsgPackToken.Float32);
            WriteFloatValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write value with token <see cref="MsgPackToken.Float64"/>
        /// </summary>
        public void WriteDouble(double value)
        {
            writer.Write(MsgPackToken.Float64);
            WriteDoubleValue(value, LittleEndianness);
        }
        /// <summary>
        /// Write fixedbool of either <see cref="MsgPackToken.True"/> or <see cref="MsgPackToken.False"/> 
        /// </summary>
        public void WriteBool(bool value) => writer.Write(value ? MsgPackToken.True : MsgPackToken.False);
        /// <summary>
        /// Write string with writer encoding (default UTF8)
        /// </summary>
        /// <returns>size written</returns>
        public int WriteString(string value) => WriteString(value, Encoding);
        /// <summary>
        /// MessagePack header is big-endian, value binarywriter default little-endian
        /// </summary>
        public int WriteString(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value) || encoding == null) return 0;

            byte[] data = encoding.GetBytes(value);
            int size = data.Length;

            if (size <= MsgPackToken.MaxFixStr - MsgPackToken.MinFixStr)
            {
                writer.Write((byte)(MsgPackToken.MinFixStr | size));
                size += 1;
            }
            else if (size <= byte.MaxValue)
            {
                writer.Write(MsgPackToken.Str8);
                writer.Write((byte)size);
                size += 2;
            }
            else if (size <= ushort.MaxValue)
            {
                writer.Write(MsgPackToken.Str16);
                WriteUShortValue((ushort)size, LittleEndianness);
                size += 3;
            }
            else
            {
                writer.Write(MsgPackToken.Str32);
                WriteIntValue(size, LittleEndianness);
                size += 5;
            }
            writer.Write(data);
            return size;
        }

        /// <summary>
        /// MessagePack header is big-endian, value binarywriter default little-endian
        /// </summary>
        public int WriteBinary(byte[] value)
        {
            int size = value.Length;
            if (size <= byte.MaxValue)
            {
                writer.Write(MsgPackToken.Bin8);
                writer.Write((byte)size);
                size += 2;
            }
            else if (size <= ushort.MaxValue)
            {
                writer.Write(MsgPackToken.Bin16);
                WriteUShortValue((ushort)size, LittleEndianness);
                size += 3;
            }
            else
            {
                writer.Write(MsgPackToken.Bin32);
                WriteIntValue(size, LittleEndianness);
                size += 5;
            }
            writer.Write(value);
            return size;
        }

        /// <summary>
        /// MessagePack header is big-endian, value binarywriter default little-endian
        /// </summary>
        public int WriteArrayHeader(uint count)
        {
            if (count <= MsgPackToken.MaxFixArray - MsgPackToken.MinFixArray)
            {
                writer.Write((byte)(MsgPackToken.MinFixArray | count));
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                writer.Write(MsgPackToken.Array16);
                WriteUShortValue((ushort)count, LittleEndianness);
                return 3;
            }
            else
            {
                writer.Write(MsgPackToken.Array32);
                WriteUIntValue(count, LittleEndianness);
                return 5;
            }
        }

        /// <summary>
        /// MessagePack header is big-endian value binarywriter default little-endian
        /// </summary>
        public int WriteMapHeader(uint count)
        {
            if (count <= MsgPackToken.MaxFixMap - MsgPackToken.MinFixMap)
            {
                writer.Write((byte)(MsgPackToken.MinFixMap | count));
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                writer.Write(MsgPackToken.Map16);
                WriteUShortValue((ushort)count, LittleEndianness);
                return 3;
            }
            else
            {
                writer.Write(MsgPackToken.Map32);
                WriteUIntValue(count, LittleEndianness);
                return 5;
            }
        }

        /// <summary>
        /// Get raw bytes from the stream buffer
        /// </summary>
        /// <returns>Msgpack bytes</returns>
        public byte[] GetBytes()
        {
            return memory.ToArray();
        }

        /// <summary>
        /// Get raw bytes from the stream buffer
        /// </summary>
        /// <returns>Msgpack buffer length</returns>
        public long GetLength()
        {
            return memory.Length;
        }

        /// <summary>
        /// Get raw bytes from the underlying stream buffer
        /// </summary>
        /// <param name="buffer">underlying stream buffer</param>
        /// <returns>true on success or false</returns>
        public bool GetBuffer(out ArraySegment<byte> buffer)
        {
            return memory.TryGetBuffer(out buffer);
        }

        /// <summary>
        /// Append bytes to the stream at the currrent stream position
        /// </summary>
        /// <param name="bytes">bytes to write</param>
        public void Append(byte[] bytes)
        {
            memory.Write(bytes, (int)memory.Position, bytes.Length);
        }

        /// <summary>
        /// String representation of the stream based on encoding (default UTF8)
        /// </summary>
        /// <returns>string of stream</returns>
        public override string ToString()
        {
            return Encoding.GetString(memory.ToArray());
        }

        /// <summary>
        /// String representation of the stream in hex
        /// </summary>
        /// <returns>hex string of stream</returns>
        public string ToHex()
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte value in memory.ToArray())
                sb.AppendFormat("{0:X2} ", value);

            return sb.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                    memory.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region helper
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Uint8"/> token to bytes
        /// </summary>
        public static void WriteByte(ref List<byte> bytes, byte value)
        {
            bytes.Add(MsgPackToken.Uint8);
            bytes.Add(value);
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Int8"/> token to bytes
        /// </summary>
        public static void WriteSByte(ref List<byte> bytes, sbyte value)
        {
            bytes.Add(MsgPackToken.Int8);
            bytes.Add((byte)value);
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Int16"/> token to bytes
        /// </summary>
        public static void WriteShort(ref List<byte> bytes, short value)
        {
            bytes.Add(MsgPackToken.Int16);
            bytes.Add((byte)(value >> 0));
            bytes.Add((byte)(value >> 8));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Uint16"/> token to bytes
        /// </summary>
        public static void WriteUShort(ref List<byte> bytes, ushort value)
        {
            bytes.Add(MsgPackToken.Uint16);
            bytes.Add((byte)((0xFF00 & value) >> 8));
            bytes.Add((byte)((0x00FF & value) << 8));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Int32"/> token to bytes
        /// </summary>
        public static void WriteInt(ref List<byte> bytes, int value)
        {
            bytes.Add(MsgPackToken.Int32);
            bytes.Add((byte)(value >> 0));
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 24));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Uint32"/> token to bytes
        /// </summary>
        public static void WriteUInt(ref List<byte> bytes, uint value)
        {
            bytes.Add(MsgPackToken.Uint32);
            bytes.Add((byte)((0xFF000000 & value) >> 24));
            bytes.Add((byte)((0x00FF0000 & value) >> 8));
            bytes.Add((byte)((0x0000FF00 & value) << 8));
            bytes.Add((byte)((0x000000FF & value) << 24));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Int64"/> token to bytes
        /// </summary>
        public static void WriteLong(ref List<byte> bytes, long value)
        {
            bytes.Add(MsgPackToken.Int64);
            bytes.Add((byte)(value >> 0));
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 24));
            bytes.Add((byte)(value >> 32));
            bytes.Add((byte)(value >> 40));
            bytes.Add((byte)(value >> 48));
            bytes.Add((byte)(value >> 56));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Uint64"/> token to bytes
        /// </summary>
        public static void WriteULong(ref List<byte> bytes, ulong value)
        {
            bytes.Add(MsgPackToken.Uint64);
            bytes.Add((byte)((0xFF00000000000000 & value) >> 56));
            bytes.Add((byte)((0x00FF000000000000 & value) >> 40));
            bytes.Add((byte)((0x0000FF0000000000 & value) >> 24));
            bytes.Add((byte)((0x000000FF00000000 & value) >> 8));
            bytes.Add((byte)((0x00000000FF000000 & value) << 8));
            bytes.Add((byte)((0x0000000000FF0000 & value) << 24));
            bytes.Add((byte)((0x000000000000FF00 & value) << 40));
            bytes.Add((byte)((0x00000000000000FF & value) << 56));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Float32"/> token to bytes
        /// </summary>
        public static void WriteFloat(ref List<byte> bytes, float value)
        {
            bytes.Add(MsgPackToken.Float32);
            bytes.AddRange(BitConverter.GetBytes(value));
        }
        /// <summary>
        /// Append value with <see cref="MsgPackToken.Float64"/> token to bytes
        /// </summary>
        public static void WriteDouble(ref List<byte> bytes, double value)
        {
            bytes.Add(MsgPackToken.Float64);
            bytes.AddRange(BitConverter.GetBytes(value));
        }
        /// <summary>
        /// Append fixedbool with either <see cref="MsgPackToken.True"/> or <see cref="MsgPackToken.False"/> to bytes
        /// </summary>
        public static void WriteBool(ref List<byte> bytes, bool value) => bytes.Add(value ? MsgPackToken.True : MsgPackToken.False);
        /// <summary>
        /// Append string value with size based token to bytes
        /// </summary>
        /// <returns>size written</returns>
        public static int WriteString(ref List<byte> bytes, string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value) || encoding == null) return 0;

            byte[] data = encoding.GetBytes(value);
            int size = data.Length;

            if (size <= byte.MaxValue)
            {
                bytes.Add(MsgPackToken.Str8);
                bytes.Add((byte)size);
                size += 2;
            }
            else if (size <= ushort.MaxValue)
            {
                bytes.Add(MsgPackToken.Str16);
                bytes.Add((byte)(size >> 0));
                bytes.Add((byte)(size >> 8));
                size += 3;
            }
            else
            {
                bytes.Add(MsgPackToken.Str32);
                bytes.Add((byte)(size >> 0));
                bytes.Add((byte)(size >> 8));
                bytes.Add((byte)(size >> 16));
                bytes.Add((byte)(size >> 24));
                size += 5;
            }
            bytes.AddRange(data);
            return size;
        }
        /// <summary>
        /// Append values with size based token to bytes
        /// </summary>
        public static int WriteBinary(ref List<byte> bytes, byte[] value)
        {
            int size = value.Length;
            if (size <= 0)
            {
                bytes.Add(MsgPackToken.Bin8);
                bytes.Add(0);
                return 2;
            }

            if (size <= byte.MaxValue)
            {
                bytes.Add(MsgPackToken.Bin8);
                bytes.Add((byte)size);
                size += 2;
            }
            else if (size <= ushort.MaxValue)
            {
                bytes.Add(MsgPackToken.Bin16);
                bytes.Add((byte)(size >> 0));
                bytes.Add((byte)(size >> 8));
                size += 3;
            }
            else
            {
                bytes.Add(MsgPackToken.Bin32);
                bytes.Add((byte)(size >> 0));
                bytes.Add((byte)(size >> 8));
                bytes.Add((byte)(size >> 16));
                bytes.Add((byte)(size >> 24));
                size += 5;
            }
            bytes.AddRange(value);
            return size;
        }
        #endregion
    }

    public partial struct MsgPackReader
    {
        #region public
        public static MsgPackReader Create(byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                throw new System.NullReferenceException();
            }

            return new MsgPackReader(data);
        }

        public MsgPackReader this[byte[] key]
        {
            get
            {
                int mapValuePosition = _reader.GetMapValuePosition(key);
                return new MsgPackReader(_reader.Source, mapValuePosition);
            }
        }

        public static byte[] KeyToBytes(string key)
        {
            return Native.Encoding.GetBytes(key);
        }

        public MsgPackReader this[string key]
        {
            get
            {
                var binkey = Native.Encoding.GetBytes(key);
                return this[binkey];
            }
        }

        public MapEnumerable AsMapEnumerable()
        {
            return new MapEnumerable(ref this);
        }

        public int ArrayLength { get { return _reader.GetArrayLength(); } }

        public MsgPackReader this[int index]
        {
            get
            {
                int arrayElementPosition = _reader.GetArrayElementPosition(index);
                return new MsgPackReader(_reader.Source, arrayElementPosition);
            }
        }

        public ArrayEnumerable AsArrayEnumerable()
        {
            return new ArrayEnumerable(ref this);
        }

        public byte GetByte() { return _reader.GetByte(); }
        public sbyte GetSByte() { return _reader.GetSByte(); }
        public short GetShort() { return _reader.GetShort(); }
        public ushort GetUShort() { return _reader.GetUShort(); }
        public int GetInt() { return _reader.GetInt(); }
        public uint GetUInt() { return _reader.GetUInt(); }
        public long GetLong() { return _reader.GetLong(); }
        public ulong GetULong() { return _reader.GetULong(); }
        public float GetFloat() { return _reader.GetFloat(); }
        public double GetDouble() { return _reader.GetDouble(); }
        public bool GetBool() { return _reader.GetBool(); }
        public string GetString() { return _reader.GetString(); }
        public byte[] GetBinary() { return _reader.GetBinary(); }
        public DateTime GetTimestamp() { return _reader.GetTimestamp(); }
        public MessagePackExtension GetExtension() { return _reader.GetExtension(); }
        public byte GetFormatCode() { return _reader.Source[_reader.Position]; }
        public string GetFormatName() { return MsgPackToken.GetFormatName(GetFormatCode()); }
        #endregion 
    }

    public class MessagePackReaderException : System.Exception
    {
        public MessagePackReaderException() { }
        public MessagePackReaderException(string message) : base(message) { }
    }

    public struct MessagePackExtension
    {
        public sbyte TypeCode;
        public byte[] Data;
    }

    public partial struct MsgPackReader
    {
        readonly MessagePackProcessor _reader;

        #region constructor
        MsgPackReader(byte[] data, int position = 0)
        {
            _reader = new MessagePackProcessor(data, position);
        }
        MsgPackReader(ref MsgPackReader r)
        {
            _reader = new MessagePackProcessor(r._reader.Source, r._reader.Position);
        }
        #endregion 

        public sealed class MapEnumerable : IEnumerable<KeyValuePair<string, MsgPackReader>>
        {
            private MsgPackReader _view;
            public MapEnumerable(ref MsgPackReader r)
            {
                _view = r;
            }

            public IEnumerator<KeyValuePair<string, MsgPackReader>> GetEnumerator()
            {
                return new Enumerator(ref _view);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(ref _view);
            }

            public struct Enumerator : IEnumerator<KeyValuePair<string, MsgPackReader>>
            {
                private SequencialReader _reader;
                private KeyValuePair<string, MsgPackReader> _current;
                private readonly int _count;
                private int _index;

                internal Enumerator(ref MsgPackReader r)
                {
                    _reader = new SequencialReader(r._reader.Source, r._reader.Position);
                    _current = new KeyValuePair<string, MsgPackReader>("", new MsgPackReader());
                    _index = 0;
                    _count = _reader.ReadMapElementCount();
                }
                public bool MoveNext()
                {
                    if (_index < _count)
                    {
                        var key = _reader.ReadString();
                        _current = new KeyValuePair<string, MsgPackReader>(key, new MsgPackReader(_reader.Source, _reader.Position));
                        _reader.SkipElement();
                        _index++;
                        return true;
                    }
                    _index = _count + 1;
                    return false;
                }

                public KeyValuePair<string, MsgPackReader> Current { get { return _current; } }

                public void Dispose() { }

                object IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || _index == _count + 1)
                        {
                            throw new MessagePackReaderException(string.Format("Invalid access to array. index={0}", _index));
                        }
                        return new KeyValuePair<string, MsgPackReader>(_current.Key, _current.Value);
                    }
                }

                void IEnumerator.Reset()
                {
                    throw new MessagePackReaderException("can not reset MiniMessagePack.Reader.Enumerator");
                }
            }

        }

        public sealed class ArrayEnumerable : IEnumerable<MsgPackReader>
        {
            private MsgPackReader _view;
            public ArrayEnumerable(ref MsgPackReader r)
            {
                _view = r;
            }

            public IEnumerator<MsgPackReader> GetEnumerator()
            {
                return new Enumerator(ref _view);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(ref _view);
            }

            public struct Enumerator : IEnumerator<MsgPackReader>
            {
                private SequencialReader _reader;
                private MsgPackReader _current;
                private readonly int _count;
                private int _index;

                internal Enumerator(ref MsgPackReader r)
                {
                    _reader = new SequencialReader(r._reader.Source, r._reader.Position);
                    _current = new MsgPackReader();
                    _index = 0;
                    _count = _reader.ReadArrayElementCount();
                }
                public bool MoveNext()
                {
                    if (_index < _count)
                    {
                        _current = new MsgPackReader(_reader.Source, _reader.Position);
                        _reader.SkipElement();
                        _index++;
                        return true;
                    }
                    _index = _count + 1;
                    return false;
                }

                public MsgPackReader Current { get { return _current; } }

                public void Dispose() { }

                object IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || _index == _count + 1)
                        {
                            throw new MessagePackReaderException(string.Format("Invalid access to array. index={0}", _index));
                        }
                        return new MsgPackReader(ref _current);
                    }
                }

                void IEnumerator.Reset()
                {
                    throw new MessagePackReaderException("can not reset MiniMessagePack.Reader.Enumerator");
                }
            }
        }

        internal struct MessagePackProcessor
        {
            public MessagePackProcessor(byte[] data, int position = 0)
            {
                _source = data;
                _position = position;
            }

            public byte[] Source { get { return _source; } }

            public int Position { get { return _position; } }

            public int GetArrayLength()
            {
                var reader = new SequencialReader(_source, _position);
                return reader.ReadArrayElementCount();
            }

            public int GetArrayElementPosition(int index)
            {
                var reader = new SequencialReader(_source, _position);

                int arrayElementCount = reader.ReadArrayElementCount();

                if (index > arrayElementCount)
                {
                    throw new MessagePackReaderException(string.Format("invalid array index. index : {0}", index));
                }

                for (int i = 0; i < index; i++)
                {
                    reader.SkipElement();
                }

                return reader.Position;
            }

            public int GetMapValuePosition(byte[] key)
            {
                var reader = new SequencialReader(_source, _position);

                int mapElementCount = reader.ReadMapElementCount();
                for (int i = 0; i < mapElementCount; i++)
                {
                    if (reader.CompareAndReadString(key))
                    {
                        return reader.Position;
                    }
                    reader.SkipElement();
                }
                throw new MessagePackReaderException(string.Format("not found key {0}", key));
            }

            public byte GetByte()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MessagePackReaderException(string.Format("failed to read byte. code : {0:X}", token));
                }
            }

            public sbyte GetSByte()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case MsgPackToken.Uint8:
                        byte byteResult = reader.ReadByte();
                        return unchecked((sbyte)byteResult);
                    case MsgPackToken.Int8:
                        return reader.ReadSByte();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        else if (MsgPackToken.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MessagePackReaderException(string.Format("failed to read sbyte. code : {0:X}", token));
                }
            }

            public short GetShort()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Int8:
                        return reader.ReadSByte();
                    case MsgPackToken.Int16:
                        return reader.ReadShort();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (MsgPackToken.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MessagePackReaderException(string.Format("failed to read short. code : {0:X}", token));
                }
            }

            public ushort GetUShort()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Uint16:
                        return reader.ReadUShort();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MessagePackReaderException(string.Format("failed to read ushort. code : {0}", token));
                }
            }

            public int GetInt()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Int8:
                        return reader.ReadSByte();
                    case MsgPackToken.Uint16:
                        return reader.ReadUShort();
                    case MsgPackToken.Int16:
                        return reader.ReadShort();
                    case MsgPackToken.Int32:
                        return reader.ReadInt();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (MsgPackToken.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MessagePackReaderException(string.Format("failed to read int. code : {0}", token));
                }
            }

            public uint GetUInt()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Uint16:
                        return reader.ReadUShort();
                    case MsgPackToken.Uint32:
                        return reader.ReadUInt();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MessagePackReaderException(string.Format("failed to read uint. code : {0:X}", token));
                }
            }

            public long GetLong()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Int8:
                        return reader.ReadSByte();
                    case MsgPackToken.Uint16:
                        return reader.ReadUShort();
                    case MsgPackToken.Int16:
                        return reader.ReadShort();
                    case MsgPackToken.Int32:
                        return reader.ReadInt();
                    case MsgPackToken.Uint32:
                        return reader.ReadUInt();
                    case MsgPackToken.Int64:
                        return reader.ReadLong();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (MsgPackToken.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MessagePackReaderException(string.Format("failed to read long. code : {0:X}", token));
                }
            }

            public ulong GetULong()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case MsgPackToken.Uint8:
                        return reader.ReadByte();
                    case MsgPackToken.Uint16:
                        return reader.ReadUShort();
                    case MsgPackToken.Uint32:
                        return reader.ReadUInt();
                    case MsgPackToken.Uint64:
                        return reader.ReadULong();
                    default:
                        if (MsgPackToken.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MessagePackReaderException(string.Format("failed to read ulong. code : {0:X}", token));
                }
            }

            public float GetFloat()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                if (!MsgPackToken.IsFloat(token))
                {
                    throw new MessagePackReaderException(string.Format("failed to read float. code : {0}", token));
                }
                return reader.ReadFloat();
            }

            public double GetDouble()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch (token)
                {
                    case MsgPackToken.Float32:
                        return reader.ReadFloat();
                    case MsgPackToken.Float64:
                        return reader.ReadDouble();
                    default:
                        throw new MessagePackReaderException(string.Format("failed to read double. code : {0:X}", token));

                }
            }

            public Nil GetNil()
            {
                return new SequencialReader(_source, _position).ReadNil();
            }

            public bool GetBool()
            {
                return new SequencialReader(_source, _position).ReadBoolean();
            }

            public string GetString()
            {
                return new SequencialReader(_source, _position).ReadString();
            }

            public byte[] GetBinary()
            {
                return new SequencialReader(_source, _position).ReadBinary();
            }

            public DateTime GetTimestamp()
            {
                var reader = new SequencialReader(_source, _position);
                var header = reader.ReadExtHeader();
                return reader.ReadTimestamp(header);
            }

            public MessagePackExtension GetExtension()
            {
                var reader = new SequencialReader(_source, _position);
                var header = reader.ReadExtHeader();
                var data = reader.ReadExtensionData(header);
                return new MessagePackExtension() { TypeCode = header.TypeCode, Data = data };
            }

            byte[] _source;
            int _position;
        }

        struct SequencialReader
        {
            readonly byte[] _source;
            int _position;

            public SequencialReader(byte[] source, int position)
            {
                _source = source;
                _position = position;
            }

            public byte[] Source { get { return _source; } }

            public int Position { get { return _position; } }

            public byte PeekToken { get { return _source[_position]; } }

            public byte PeekSourceType { get { return MsgPackToken.GetSourceType(PeekToken); } }

            public string PeekFormatName { get { return MsgPackToken.GetFormatName(PeekToken); } }

            public void SkipElement()
            {
                var token = PeekToken;
                var sourceType = MsgPackToken.GetSourceType(token);

                switch (sourceType)
                {
                    case MsgPackToken.Type_Integer:
                        token = ReadToken();
                        if (MsgPackToken.IsNegativeFixInt(token) || MsgPackToken.IsPositiveFixInt(token))
                        {
                            // NOP
                            return;
                        }
                        else if (token == MsgPackToken.Int8 || token == MsgPackToken.Uint8)
                        {
                            _position += sizeof(byte);
                            // ReadSByte();
                            return;
                        }
                        else if (token == MsgPackToken.Int16 || token == MsgPackToken.Uint16)
                        {
                            _position += sizeof(short);
                            // ReadShort();
                            return;
                        }
                        else if (token == MsgPackToken.Int32 || token == MsgPackToken.Uint32)
                        {
                            _position += sizeof(int);
                            // ReadInt();
                            return;
                        }
                        else if (token == MsgPackToken.Int64 || token == MsgPackToken.Uint64)
                        {
                            _position += sizeof(long);
                            // ReadLong();
                            return;
                        }

                        throw new MessagePackReaderException("Invalid primitive bytes.");
                    case MsgPackToken.Type_Boolean:
                        token = ReadToken();
                        // ReadBoolean();
                        break;
                    case MsgPackToken.Type_Float:
                        token = ReadToken();
                        if (token == MsgPackToken.Float32)
                        {
                            _position += sizeof(float);
                            // ReadFloat();
                            return;
                        }
                        else
                        {
                            _position += sizeof(double);
                            // ReadDouble();
                            return;
                        }

                        throw new MessagePackReaderException("Invalid primitive bytes.");
                    case MsgPackToken.Type_String:
                        var stringLength = ReadStringByteLength();
                        _position += stringLength;
                        // ReadString();
                        return;
                    case MsgPackToken.Type_Binary:
                        var byteLength = ReadBinaryByteLength();
                        _position += byteLength;
                        // ReadBinary();
                        return;
                    case MsgPackToken.Type_Extension:
                        var extHeader = ReadExtHeader();
                        _position += (int)extHeader.Length;
                        // if (extHeader.TypeCode == Spec.ExtTypeCode_Timestamp)
                        // {
                        //     ReadTimestamp(extHeader);
                        //     return;
                        // }
                        throw new MessagePackReaderException("Invalid primitive bytes.");
                    case MsgPackToken.Type_Array:
                        var arrayElementCount = ReadArrayElementCount();
                        for (int i = 0; i < arrayElementCount; i++)
                        {
                            SkipElement();
                        }
                        return;
                    case MsgPackToken.Type_Map:
                        var mapElementCount = ReadMapElementCount();
                        for (int i = 0; i < mapElementCount; i++)
                        {
                            // ReadString();
                            SkipElement();// key
                            SkipElement();// value
                        }
                        return;
                    case MsgPackToken.Type_Nil:
                        ReadNil();
                        return;
                    default:
                        throw new MessagePackReaderException("Invalid primitive bytes.");
                }
            }

            public Nil ReadNil()
            {
                byte code;
                EoSCheck(TryRead(out code));
                if (!MsgPackToken.IsNil(code))
                {
                    ThrowInvalidCodeException(code);
                }
                return Nil.Default;
            }

            public int ReadArrayElementCount()
            {
                int v;
                EoSCheck(TryReadArrayElementCount(out v));
                return v;
            }

            public bool TryReadArrayElementCount(out int count)
            {
                byte token;
                if (!TryReadToken(out token))
                {
                    count = default(int);
                    return false;
                }

                switch (token)
                {
                    case MsgPackToken.Array16:
                        ushort ushortResult;
                        if (TryRead(out ushortResult))
                        {
                            count = ushortResult;
                            return true;
                        }
                        break;
                    case MsgPackToken.Array32:
                        uint uintResult;
                        if (TryRead(out uintResult))
                        {
                            count = checked((int)uintResult);
                            return true;
                        }
                        break;
                    default:
                        if (MsgPackToken.IsFixArray(token))
                        {
                            count = checked((byte)(token & 0xf));
                            return true;
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                count = default(int);
                return false;
            }

            public int ReadMapElementCount()
            {
                int v;
                EoSCheck(TryReadMapElementCount(out v));
                return v;
            }

            public bool TryReadMapElementCount(out int count)
            {
                count = 0;

                byte token;
                if (!TryReadToken(out token))
                {
                    return false;
                }

                switch (token)
                {
                    case MsgPackToken.Map16:
                        {
                            ushort value;
                            if (!TryRead(out value)) return false;
                            count = checked((int)value);
                        }
                        break;
                    case MsgPackToken.Map32:
                        {
                            uint value;
                            if (!TryRead(out value)) return false;
                            count = checked((int)value);
                        }
                        break;
                    default:
                        if (MsgPackToken.MinFixMap <= token && token <= MsgPackToken.MaxFixMap)
                        {
                            count = checked((int)(token & 0xf));
                            break;
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                return true;
            }

            public DateTime ReadTimestamp(ExtensionHeader header)
            {
                if (header.TypeCode != MsgPackToken.ExtTypeCode_Timestamp)
                {
                    throw new MessagePackReaderException(string.Format("Invalid Extension TypeCode {0}", header.TypeCode));
                }

                switch (header.Length)
                {
                    case 4:
                        {
                            uint uintResult;
                            EoSCheck(TryRead(out uintResult));
                            return DateTimeConverter.GetDateTime(uintResult);
                        }
                    case 8:
                        {
                            ulong ulongResult;
                            EoSCheck(TryRead(out ulongResult));
                            uint nanoseconds = (uint)(ulongResult >> 34);
                            uint seconds = (uint)(ulongResult & 0x00000003ffffffffL);
                            return DateTimeConverter.GetDateTime(seconds, nanoseconds);
                        }
                    case 12:
                        {
                            uint uintResult;
                            EoSCheck(TryRead(out uintResult));
                            long longResult;
                            EoSCheck(TryRead(out longResult));
                            return DateTimeConverter.GetDateTime(longResult, uintResult);
                        }
                    default:
                        throw new MessagePackReaderException(string.Format("Invalid Ext Timestamp length {0}", header.Length));
                }
            }

            public byte[] ReadExtensionData(ExtensionHeader header)
            {
                byte[] data = new byte[header.Length];
                Array.Copy(_source, _position, data, 0, header.Length);
                return data;
            }

            public ExtensionHeader ReadExtHeader()
            {
                var token = ReadToken();
                uint length = default(uint);
                switch (token)
                {
                    case MsgPackToken.FixExt1:
                        length = 1;
                        break;
                    case MsgPackToken.FixExt2:
                        length = 2;
                        break;
                    case MsgPackToken.FixExt4:
                        length = 4;
                        break;
                    case MsgPackToken.FixExt8:
                        length = 8;
                        break;
                    case MsgPackToken.FixExt16:
                        length = 16;
                        break;
                    case MsgPackToken.Ext8:
                        byte byteResult;
                        EoSCheck(TryRead(out byteResult));
                        length = byteResult;
                        break;
                    case MsgPackToken.Ext16:
                        short shortResult;
                        EoSCheck(TryRead(out shortResult));
                        length = checked((uint)shortResult);
                        break;
                    case MsgPackToken.Ext32:
                        int intResult;
                        EoSCheck(TryRead(out intResult));
                        length = checked((uint)intResult);
                        break;
                    default:
                        ThrowInvalidCodeException(token);
                        break;
                }

                sbyte typeCode;
                EoSCheck(TryRead(out typeCode));

                return new ExtensionHeader(typeCode, length);
            }

            public byte[] ReadBinary()
            {
                var byteLength = ReadBinaryByteLength();

                byte[] bytesResult = new byte[byteLength];
                Array.Copy(_source, _position, bytesResult, 0, byteLength);

                return bytesResult;
            }

            int ReadBinaryByteLength()
            {
                var token = ReadToken();
                switch (token)
                {
                    case MsgPackToken.Bin8:
                        byte byteResult;
                        EoSCheck(TryRead(out byteResult));
                        return checked((int)byteResult);
                    case MsgPackToken.Bin16:
                        ushort ushortResult;
                        EoSCheck(TryRead(out ushortResult));
                        return checked((int)ushortResult);
                    case MsgPackToken.Bin32:
                        uint uintResult;
                        EoSCheck(TryRead(out uintResult));
                        return checked((int)uintResult);
                    default:
                        ThrowInvalidCodeException(token);
                        break;
                }

                return default(int);
            }

            public bool CompareAndReadString(byte[] key)
            {
                var token = PeekToken;
                if (token == MsgPackToken.Nil)
                {
                    return false;
                }

                var byteLength = ReadStringByteLength();
                if (byteLength != key.Length)
                {
                    _position += byteLength;
                    return false;
                }
                for (int i = 0; i < byteLength; i++)
                {
                    if (_source[_position + i] != key[i])
                    {
                        _position += byteLength;
                        return false;
                    }
                }

                _position += byteLength;
                return true;
            }

            public string ReadString()
            {
                var token = PeekToken;
                if (token == MsgPackToken.Nil)
                {
                    return "";
                }

                var byteLength = ReadStringByteLength();

                var value = Native.Encoding.GetString(_source, _position, byteLength);
                _position += byteLength;
                return value;
            }

            int ReadStringByteLength()
            {
                var token = ReadToken();
                switch (token)
                {
                    case MsgPackToken.Str8:
                        byte byteResult;
                        EoSCheck(TryRead(out byteResult));
                        return checked((int)byteResult);
                    case MsgPackToken.Str16:
                        short shortResult;
                        EoSCheck(TryRead(out shortResult));
                        return checked((int)shortResult);
                    case MsgPackToken.Str32:
                        int intResult;
                        EoSCheck(TryRead(out intResult));
                        return intResult;
                    default:
                        if (MsgPackToken.MinFixStr <= token && token <= MsgPackToken.MaxFixStr)
                        {
                            return (token & 0x1f);
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                return default(int);
            }

            public bool ReadBoolean()
            {
                var token = ReadToken();

                switch (token)
                {
                    case MsgPackToken.False:
                        return false;
                    case MsgPackToken.True:
                        return true;
                    default:
                        throw ThrowInvalidCodeException(token);
                }
            }

            public byte ReadByte()
            {
                EoSCheck(TryRead(out byte value));
                return value;
            }

            public sbyte ReadSByte()
            {
                EoSCheck(TryRead(out sbyte value));
                return value;
            }

            public short ReadShort()
            {
                EoSCheck(TryRead(out short value));
                return value;
            }

            public ushort ReadUShort()
            {
                EoSCheck(TryRead(out ushort value));
                return value;
            }

            public int ReadInt()
            {
                EoSCheck(TryRead(out int value));
                return value;
            }

            public uint ReadUInt()
            {
                EoSCheck(TryRead(out uint value));
                return value;
            }

            public long ReadLong()
            {
                EoSCheck(TryRead(out long value));
                return value;
            }

            public ulong ReadULong()
            {
                EoSCheck(TryRead(out ulong value));
                return value;
            }

            public float ReadFloat()
            {
                EoSCheck(TryRead(out float value));
                return value;
            }

            public double ReadDouble()
            {
                EoSCheck(TryRead(out double value));
                return value;
            }

            public byte ReadToken()
            {
                EoSCheck(TryReadToken(out byte token));
                return token;
            }

            public bool TryReadToken(out byte token)
            {
                if (_position == _source.Length)
                {
                    token = _source[_position - 1];
                    return false;
                }
                token = _source[_position];
                _position++;
                return true;
            }

            public bool TryReadSourceType(out byte type)
            {
                byte token;
                var r = TryReadToken(out token);
                type = MsgPackToken.GetSourceType(token);
                return r;
            }

            #region private

            bool TryRead<T>(out T value)
            {
                var typeSize = TypeSizeProvider.Instance.Get<T>();
                if ((_source.Length - _position) < typeSize)
                {
                    value = default(T);
                    return false;
                }

                value = BytesConverterResolver.Instance.GetConverter<T>().To(_source, _position);
                _position += typeSize;
                return true;
            }

            MessagePackReaderException ThrowInvalidCodeException(byte code)
            {
                var ex = new MessagePackReaderException(string.Format("Invalid code 0x{0:X}", code));
#pragma warning disable CS0618 // Type or member is obsolete
                Utility.Throw(ex);
#pragma warning restore CS0618 // Type or member is obsolete
                return ex;
            }

            System.IO.EndOfStreamException ThrowEndOfStreamException()
            {
                throw new System.IO.EndOfStreamException();
            }

            void EoSCheck(bool cond)
            {
                if (!cond)
                {
                    ThrowEndOfStreamException();
                }
            }
            #endregion //private
        }

        struct ExtensionHeader
        {
            public sbyte TypeCode;
            public uint Length;

            public ExtensionHeader(sbyte typeCode, uint length)
            {
                TypeCode = typeCode;
                Length = length;
            }

            public ExtensionHeader(sbyte typeCode, int length)
            {
                TypeCode = typeCode;
                Length = (uint)length;
            }
        }

        static class DateTimeConverter
        {
            static readonly DateTime _baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public static DateTime GetDateTime(uint seconds)
            {
                return _baseTime.AddSeconds(seconds);
            }
            public static DateTime GetDateTime(uint seconds, uint nanoseconds)
            {
                return _baseTime.AddSeconds(seconds).AddTicks(nano2tick(nanoseconds));
            }
            public static DateTime GetDateTime(long seconds, uint nanoseconds)
            {
                return _baseTime.AddSeconds(seconds).AddTicks(nano2tick(nanoseconds));
            }
            static uint nano2tick(uint nanoseconds)
            {
                return nanoseconds / 100;
            }
        }

        class TypeSizeProvider
        {
            public static readonly TypeSizeProvider Instance = new TypeSizeProvider();
            public int Get<T>()
            {
                return Cache<T>.Size;
            }

            TypeSizeProvider() { }

            static class Cache<T>
            {
                public static readonly int Size;
                static Cache()
                {
                    Size = TypeSizeProviderCacheHelper.ToSize(typeof(T));
                }
            }

            static class TypeSizeProviderCacheHelper
            {
                public static int ToSize(Type type) { return TypeSizeMap[type]; }

                static readonly Dictionary<Type, int> TypeSizeMap = new Dictionary<Type, int>()
                {
                    { typeof(byte), sizeof(byte) },
                    { typeof(sbyte), sizeof(sbyte) },
                    { typeof(short), sizeof(short) },
                    { typeof(ushort), sizeof(ushort) },
                    { typeof(int), sizeof(int) },
                    { typeof(uint), sizeof(uint) },
                    { typeof(long), sizeof(long) },
                    { typeof(ulong), sizeof(ulong) },
                    { typeof(float), sizeof(float) },
                    { typeof(double), sizeof(double) },
                };
            }
        }

        interface IBytesConverter<T>
        {
            T To(byte[] data, int startIndex);
        }
        class BytesConverterResolver
        {
            public static readonly BytesConverterResolver Instance = new BytesConverterResolver();
            public IBytesConverter<T> GetConverter<T>()
            {
                return Cache<T>.converter;
            }

            BytesConverterResolver() { }

            static class Cache<T>
            {
                public static readonly IBytesConverter<T> converter;
                static Cache()
                {
                    converter = (IBytesConverter<T>)BytesConverterResolverCacheHelper.ToConverter(typeof(T));
                }
            }

            static class BytesConverterResolverCacheHelper
            {
                public static object ToConverter(Type type) { return ConverterMap[type]; }

                static readonly Dictionary<Type, object> ConverterMap = new Dictionary<Type, object>()
                {
                    { typeof(byte), new ByteConverter() },
                    { typeof(sbyte), new SByteConverter() },
                    { typeof(short), new ShortConverter() },
                    { typeof(ushort), new UShortConverter() },
                    { typeof(int), new IntConverter() },
                    { typeof(uint), new UIntConverter() },
                    { typeof(long), new LongConverter() },
                    { typeof(ulong), new ULongConverter() },
                    { typeof(float), new FloatConverter() },
                    { typeof(double), new DoubleConverter() },
                };
            }

            static ushort ReverseBytes(ushort value)
            {
                return (ushort)(((value & 0x00FFU) << 8) | ((value & 0xFF00U) >> 8));
            }

            static uint ReverseBytes(uint value)
            {
                return ((value & 0x000000FFU) << 24) |
                       ((value & 0x0000FF00U) << 8) |
                       ((value & 0x00FF0000U) >> 8) |
                       ((value & 0xFF000000U) >> 24);
            }

            static ulong ReverseBytes(ulong value)
            {
                return ((value & 0x00000000000000FFUL) << 56) |
                       ((value & 0x000000000000FF00UL) << 40) |
                       ((value & 0x0000000000FF0000UL) << 24) |
                       ((value & 0x00000000FF000000UL) << 8) |
                       ((value & 0x000000FF00000000UL) >> 8) |
                       ((value & 0x0000FF0000000000UL) >> 24) |
                       ((value & 0x00FF000000000000UL) >> 40) |
                       ((value & 0xFF00000000000000UL) >> 56);
            }

            static void SwapBytes(byte[] data, int idx1, int idx2)
            {
                byte tmp = data[idx1];
                data[idx1] = data[idx2];
                data[idx2] = tmp;
            }

            class ByteConverter : IBytesConverter<byte>
            {
                public byte To(byte[] data, int startIndex)
                {
                    return data[startIndex];
                }
            }

            class SByteConverter : IBytesConverter<sbyte>
            {
                public sbyte To(byte[] data, int startIndex)
                {
                    return unchecked((sbyte)data[startIndex]);
                }
            }

            class ShortConverter : IBytesConverter<short>
            {
                public short To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToInt16(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : (short)ReverseBytes((ushort)v);
                }
            }

            class UShortConverter : IBytesConverter<ushort>
            {
                public ushort To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToUInt16(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : ReverseBytes(v);
                }
            }

            class IntConverter : IBytesConverter<int>
            {
                public int To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToInt32(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : (int)ReverseBytes((uint)v);
                }
            }

            class UIntConverter : IBytesConverter<uint>
            {
                public uint To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToUInt32(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : ReverseBytes(v);
                }
            }

            class LongConverter : IBytesConverter<long>
            {
                public long To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToInt64(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : (long)ReverseBytes((ulong)v);
                }
            }

            class ULongConverter : IBytesConverter<ulong>
            {
                public ulong To(byte[] data, int startIndex)
                {
                    var v = BitConverter.ToUInt64(data, startIndex);
                    return (!BitConverter.IsLittleEndian) ? v : ReverseBytes(v);
                }
            }

            class FloatConverter : IBytesConverter<float>
            {
                public float To(byte[] data, int startIndex)
                {
                    float v = default(float);
                    if (!BitConverter.IsLittleEndian)
                    {
                        v = BitConverter.ToSingle(data, startIndex);
                    }
                    else
                    {
                        var tmp = BitConverter.ToUInt32(data, startIndex);
                        var bytes = BitConverter.GetBytes(tmp);
                        SwapBytes(bytes, 0, 3);
                        SwapBytes(bytes, 1, 2);
                        v = BitConverter.ToSingle(bytes, 0);
                    }
                    return v;
                }
            }

            class DoubleConverter : IBytesConverter<double>
            {
                public double To(byte[] data, int startIndex)
                {
                    double v = default(double);
                    if (!BitConverter.IsLittleEndian)
                    {
                        return BitConverter.ToDouble(data, startIndex);
                    }
                    else
                    {
                        var tmp = BitConverter.ToUInt64(data, startIndex);
                        var bytes = BitConverter.GetBytes(tmp);
                        SwapBytes(bytes, 0, 7);
                        SwapBytes(bytes, 1, 6);
                        SwapBytes(bytes, 2, 5);
                        SwapBytes(bytes, 3, 4);
                        v = BitConverter.ToDouble(bytes, 0);
                    }
                    return v;
                }
            }
        }

        internal struct Nil : IEquatable<Nil>
        {
            public static readonly Nil Default = default(Nil);

            public override bool Equals(object obj)
            {
                return obj is Nil;
            }

            public bool Equals(Nil other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "()";
            }
        }

    }

    /// <summary>
    /// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md"/>
    /// </summary>
    public static class MsgPackToken
    {
        public const byte MinPositiveFixInt = 0x00;
        public const byte MaxPositiveFixInt = 0x7f;
        public const byte MinFixMap = 0x80;
        public const byte MaxFixMap = 0x8f;
        public const byte MinFixArray = 0x90;
        public const byte MaxFixArray = 0x9f;
        public const byte MinFixStr = 0xa0;
        public const byte MaxFixStr = 0xbf;
        public const byte Nil = 0xc0;
        public const byte NeverUsed = 0xc1;
        public const byte False = 0xc2;
        public const byte True = 0xc3;
        public const byte Bin8 = 0xc4;
        public const byte Bin16 = 0xc5;
        public const byte Bin32 = 0xc6;
        public const byte Ext8 = 0xc7;
        public const byte Ext16 = 0xc8;
        public const byte Ext32 = 0xc9;
        public const byte Float32 = 0xca;
        public const byte Float64 = 0xcb;
        public const byte Uint8 = 0xcc;
        public const byte Uint16 = 0xcd;
        public const byte Uint32 = 0xce;
        public const byte Uint64 = 0xcf;
        public const byte Int8 = 0xd0;
        public const byte Int16 = 0xd1;
        public const byte Int32 = 0xd2;
        public const byte Int64 = 0xd3;
        public const byte FixExt1 = 0xd4;
        public const byte FixExt2 = 0xd5;
        public const byte FixExt4 = 0xd6;
        public const byte FixExt8 = 0xd7;
        public const byte FixExt16 = 0xd8;
        public const byte Str8 = 0xd9;
        public const byte Str16 = 0xda;
        public const byte Str32 = 0xdb;
        public const byte Array16 = 0xdc;
        public const byte Array32 = 0xdd;
        public const byte Map16 = 0xde;
        public const byte Map32 = 0xdf;
        public const byte MinNegativeFixInt = 0xe0;
        public const byte MaxNegativeFixInt = 0xff;

        public const byte Type_Unknown = 0;
        public const byte Type_Integer = 1;
        public const byte Type_Nil = 2;
        public const byte Type_Boolean = 3;
        public const byte Type_Float = 4;
        public const byte Type_String = 5;
        public const byte Type_Binary = 6;
        public const byte Type_Array = 7;
        public const byte Type_Map = 8;
        public const byte Type_Extension = 9;

        public const sbyte ExtTypeCode_Timestamp = -1;

        public static byte GetSourceType(byte token)
        {
            return _typeLookupTable[token];
        }
        public static string GetFormatName(byte token)
        {
            return _specNameLookupTable[token];
        }
        public static bool IsNegativeFixInt(byte token)
        {
            return MinNegativeFixInt <= token && token <= MaxNegativeFixInt;
        }
        public static bool IsPositiveFixInt(byte token)
        {
            return MinPositiveFixInt <= token && token <= MaxPositiveFixInt;
        }
        public static bool IsFixStr(byte token)
        {
            return MinFixStr <= token && token <= MaxFixStr;
        }
        public static bool IsFixMap(byte token)
        {
            return MinFixMap <= token && token <= MaxFixMap;
        }
        public static bool IsFixArray(byte token)
        {
            return MinFixArray <= token && token <= MaxFixArray;
        }
        public static bool IsByte(byte token)
        {
            if (token == Uint8) return true;
            if (IsPositiveFixInt(token)) return true;

            return false;
        }
        public static bool IsSByte(byte token)
        {
            if (token == Int8) return true;
            if (IsNegativeFixInt(token)) return true;
            return false;
        }
        public static bool IsShort(byte token)
        {
            return (token == Int16);
        }
        public static bool IsUShort(byte token)
        {
            return (token == Uint16);
        }
        public static bool IsInt(byte token)
        {
            return (token == Int32);
        }
        public static bool IsUInt(byte token)
        {
            return (token == Uint32);
        }
        public static bool IsLong(byte token)
        {
            return (token == Int64);
        }
        public static bool IsULong(byte token)
        {
            return (token == Uint64);
        }
        public static bool IsFloat(byte token)
        {
            return (token == Float32);
        }
        public static bool IsDouble(byte token)
        {
            return (token == Float64);
        }
        public static bool IsString(byte token)
        {
            switch (token)
            {
                case Str8:
                case Str16:
                case Str32:
                    return true;
                default:
                    return IsFixStr(token);
            }
        }
        public static bool IsBinary(byte token)
        {
            switch (token)
            {
                case Bin8:
                case Bin16:
                case Bin32:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsArray(byte token)
        {
            switch (token)
            {
                case Array16:
                case Array32:
                    return true;
                default:
                    return IsFixArray(token);
            }
        }
        public static bool IsMap(byte token)
        {
            switch (token)
            {
                case Map16:
                case Map32:
                    return true;
                default:
                    return IsFixMap(token);
            }
        }
        public static bool IsExtension(byte token)
        {
            switch (token)
            {
                case FixExt1:
                case FixExt2:
                case FixExt4:
                case FixExt8:
                case FixExt16:
                case Ext8:
                case Ext16:
                case Ext32:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsNil(byte token)
        {
            return (token == Nil);
        }

        private static readonly byte[] _typeLookupTable = new byte[0xff + 1];
        private static readonly string[] _specNameLookupTable = new string[0xff + 1];

        static MsgPackToken()
        {
            for (int i = MinPositiveFixInt; i <= MaxPositiveFixInt; i++)
            {
                _typeLookupTable[i] = Type_Integer;
                _specNameLookupTable[i] = "positive fixint";
            }
            for (int i = MinFixMap; i <= MaxFixMap; i++)
            {
                _typeLookupTable[i] = Type_Map;
                _specNameLookupTable[i] = "fixmap";
            }
            for (int i = MinFixArray; i <= MaxFixArray; i++)
            {
                _typeLookupTable[i] = Type_Array;
                _specNameLookupTable[i] = "fixarray";
            }
            for (int i = MinFixStr; i <= MaxFixStr; i++)
            {
                _typeLookupTable[i] = Type_String;
                _specNameLookupTable[i] = "fixstr";
            }
            _typeLookupTable[Nil] = Type_Nil; _specNameLookupTable[Nil] = "nil";
            _typeLookupTable[NeverUsed] = Type_Unknown; _specNameLookupTable[NeverUsed] = "(never used)";
            _typeLookupTable[False] = Type_Boolean; _specNameLookupTable[False] = "false";
            _typeLookupTable[True] = Type_Boolean; _specNameLookupTable[True] = "true";
            _typeLookupTable[Bin8] = Type_Binary; _specNameLookupTable[Bin8] = "bin 8";
            _typeLookupTable[Bin16] = Type_Binary; _specNameLookupTable[Bin16] = "bin 16";
            _typeLookupTable[Bin32] = Type_Binary; _specNameLookupTable[Bin32] = "bin 32";
            _typeLookupTable[Ext8] = Type_Extension; _specNameLookupTable[Ext8] = "ext 8";
            _typeLookupTable[Ext16] = Type_Extension; _specNameLookupTable[Ext16] = "ext 16";
            _typeLookupTable[Ext32] = Type_Extension; _specNameLookupTable[Ext32] = "ext 32";
            _typeLookupTable[Float32] = Type_Float; _specNameLookupTable[Float32] = "float 32";
            _typeLookupTable[Float64] = Type_Float; _specNameLookupTable[Float32] = "float 64";
            _typeLookupTable[Uint8] = Type_Integer; _specNameLookupTable[Uint8] = "uint 8";
            _typeLookupTable[Uint16] = Type_Integer; _specNameLookupTable[Uint16] = "uint 16";
            _typeLookupTable[Uint32] = Type_Integer; _specNameLookupTable[Uint32] = "uint 32";
            _typeLookupTable[Uint64] = Type_Integer; _specNameLookupTable[Uint64] = "uint 64";
            _typeLookupTable[Int8] = Type_Integer; _specNameLookupTable[Int8] = "int 8";
            _typeLookupTable[Int16] = Type_Integer; _specNameLookupTable[Int16] = "int 16";
            _typeLookupTable[Int32] = Type_Integer; _specNameLookupTable[Int32] = "int 32";
            _typeLookupTable[Int64] = Type_Integer; _specNameLookupTable[Int64] = "int 64";
            _typeLookupTable[FixExt1] = Type_Extension; _specNameLookupTable[FixExt1] = "fixext 1";
            _typeLookupTable[FixExt2] = Type_Extension; _specNameLookupTable[FixExt2] = "fixext 2";
            _typeLookupTable[FixExt4] = Type_Extension; _specNameLookupTable[FixExt4] = "fixext 4";
            _typeLookupTable[FixExt8] = Type_Extension; _specNameLookupTable[FixExt8] = "fixext 8";
            _typeLookupTable[FixExt16] = Type_Extension; _specNameLookupTable[FixExt16] = "fixext 16";
            _typeLookupTable[Str8] = Type_String; _specNameLookupTable[Str8] = "str 8";
            _typeLookupTable[Str16] = Type_String; _specNameLookupTable[Str16] = "str 16";
            _typeLookupTable[Str32] = Type_String; _specNameLookupTable[Str32] = "str 32";
            _typeLookupTable[Array16] = Type_Array; _specNameLookupTable[Array16] = "array 16";
            _typeLookupTable[Array32] = Type_Array; _specNameLookupTable[Array32] = "array 32";
            _typeLookupTable[Map16] = Type_Map; _specNameLookupTable[Map16] = "map 16";
            _typeLookupTable[Map32] = Type_Map; _specNameLookupTable[Map32] = "map 32";
            for (int i = MinNegativeFixInt; i <= MaxNegativeFixInt; i++)
            {
                _typeLookupTable[i] = Type_Integer;
                _specNameLookupTable[i] = "negative fixint";
            }
        }
    }
}
