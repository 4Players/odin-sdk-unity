using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OdinNative.Wrapper.Socket
{
    /// <summary>
    /// Intern socket dictionary
    /// </summary>
    /// <remarks>Used by <see cref="OdinNative.Wrapper.OdinClient.Sockets"/></remarks>
    public class SocketCollection : IReadOnlyCollection<Socket>, IEqualityComparer<Socket>
    {
        private volatile ConcurrentDictionary<int, Socket> _Sockets;
        /// <summary>
        /// Intern socket dictionary
        /// </summary>
        public SocketCollection()
        {
            _Sockets = new ConcurrentDictionary<int, Socket>();
        }

        /// <summary>
        /// Get socket by Id
        /// </summary>
        /// <param name="id">socket id <see cref="Socket.GetSocketId"/></param>
        /// <returns>Socket</returns>
        public Socket this[ulong id] => _Sockets?.Values.FirstOrDefault(socket => socket?.Id == id);
        /// <summary>
        /// Get socket by hashcode
        /// </summary>
        /// <param name="hashCode">Socket object hashcode <see cref="object.GetHashCode">Socket.GetHashCode</see></param>
        /// <returns>Socket</returns>
        internal Socket this[int hashCode] => _Sockets?.Values.FirstOrDefault(socket => socket?.GetHashCode() == hashCode);

        /// <summary>
        /// Count of sockets in the collection
        /// </summary>
        public int Count => _Sockets.Count;

        /// <summary>
        /// Indicates whether elements can be removed from the collection
        /// </summary>
        public bool IsRemoveOnly { get; internal set; } = false;

        /// <summary>
        /// Add a socket to the collection
        /// </summary>
        /// <remarks>Always false if the collection IsRemoveOnly</remarks>
        /// <param name="item">socket to add</param>
        /// <returns>true on success or false</returns>
        public bool Add(Socket item)
        {
            if (IsRemoveOnly) return false;
            return _Sockets.TryAdd(item.GetHashCode(), item);
        }

        /// <summary>
        /// Try to get socket by label
        /// </summary>
        /// <param name="key">Socket <see cref="Socket.Label">label</see></param>
        /// <returns>Socket</returns>
        public Socket GetByLabel(int key)
        {
            return _Sockets?.FirstOrDefault(kvp => kvp.Value.Label == key).Value;
        }

        /// <summary>
        /// Free and empty the collection
        /// </summary>
        public void Clear()
        {
            _Sockets.Clear();
        }

        /// <summary>
        /// Determines whether the socket is in the collection
        /// </summary>
        /// <param name="item">socket</param>
        /// <returns>true on success or false</returns>
        public bool Contains(Socket item)
        {
            return _Sockets.Values.Contains(item);
        }

        /// <summary>
        /// Compares two sockets by hash code
        /// </summary>
        /// <param name="x">socket x</param>
        /// <param name="y">socket y</param>
        /// <returns>is equal</returns>
        public bool Equals(Socket x, Socket y)
        {
            return x?.GetHashCode() == y?.GetHashCode();
        }

        /// <summary>
        /// Get enumerator for iteration
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<Socket> GetEnumerator()
        {
            return _Sockets.Values.GetEnumerator();
        }

        /// <summary>
        /// Default GetHashCode
        /// </summary>
        /// <param name="obj">socket</param>
        /// <returns>hash code</returns>
        public int GetHashCode(Socket obj)
        {
            return obj.GetHashCode();
        }

        /// <summary>
        /// Removes the socket from this collection
        /// </summary>
        /// <remarks>does NOT reset the socket</remarks>
        /// <param name="id">socket id <see cref="Socket.GetSocketId"/></param>
        /// <returns>is removed</returns>
        public bool Remove(ulong id)
        {
            var socket = this[id];
            return socket != null && _Sockets.TryRemove(socket.GetHashCode(), out _);
        }

        /// <summary>
        /// Reset a socket
        /// </summary>
        /// <remarks>
        /// Will not remove the socket from collection.
        /// Which means messages can still be received on the socket, but it can no longer send.
        /// </remarks>
        /// <param name="socket">socket</param>
        /// <returns>true if socket to reset found</returns>
        public bool Reset(Socket socket)
        {
            return Reset(socket.Id);
        }

        /// <summary>
        /// Reset a socket
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if socket to reset is found</returns>
        public bool Reset(ulong id)
        {
            var socket = this[id];
            bool result = socket != null && _Sockets.TryGetValue(socket.GetHashCode(), out socket);
            if (result) socket.Reset();
            return result;
        }

        internal void ResetAll()
        {
            foreach (var kvp in _Sockets)
                Reset(kvp.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Sockets.GetEnumerator();
        }
    }
}
