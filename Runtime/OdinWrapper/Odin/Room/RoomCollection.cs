using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OdinNative.Wrapper.Room
{
    /// <summary>
    /// Intern room dictionary
    /// </summary>
    /// <remarks>Used by <see cref="OdinNative.Wrapper.OdinClient.Rooms"/></remarks>
    public class RoomCollection : IReadOnlyCollection<Room>, IEqualityComparer<Room>
    {
        private volatile ConcurrentDictionary<int, Room> _Rooms;
        /// <summary>
        /// Intern room dictionary
        /// </summary>
        public RoomCollection()
        {
            _Rooms = new ConcurrentDictionary<int, Room>();
        }

        /// <summary>
        /// Try to get room by name
        /// </summary>
        /// <param name="key">Room <see cref="Room.Name">name</see></param>
        /// <returns>Room or null</returns>
        public Room this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key)) return null;
                return _Rooms?.FirstOrDefault(kvp => kvp.Value.Name.Equals(key)).Value;
            }
        }
        /// <summary>
        /// Get room by Id
        /// </summary>
        /// <param name="id">room id <see cref="Room.GetRoomId"/></param>
        /// <returns>Room</returns>
        public Room this[ulong id] => _Rooms?.Values.FirstOrDefault(room => room?.Id == id);
        /// <summary>
        /// Get room by hashcode
        /// </summary>
        /// <param name="hashCode">Room object hashcode <see cref="object.GetHashCode">Room.GetHashCode</see></param>
        /// <returns>Room</returns>
        internal Room this[int hashCode] => _Rooms?.Values.FirstOrDefault(room => room?.GetHashCode() == hashCode);

        /// <summary>
        /// Count of rooms in the collection
        /// </summary>
        public int Count => _Rooms.Count;

        /// <summary>
        /// Indicates whether elements can be removed from the collection
        /// </summary>
        public bool IsRemoveOnly { get; internal set; } = false;

        /// <summary>
        /// Add a room to the collection
        /// </summary>
        /// <remarks>Always false if the collection IsRemoveOnly</remarks>
        /// <param name="item">room to add</param>
        /// <returns>true on success or false</returns>
        public bool Add(Room item)
        {
            if(IsRemoveOnly) return false;
            return _Rooms.TryAdd(item.GetHashCode(), item);
        }

        /// <summary>
        /// Free and empty the collection
        /// </summary>
        public void Clear()
        {
            FreeAll();
            _Rooms.Clear();
        }

        /// <summary>
        /// Determines whether the room is in the collection
        /// </summary>
        /// <param name="item">room</param>
        /// <returns>true on success or false</returns>
        public bool Contains(Room item)
        {
            return _Rooms.Values.Contains(item);
        }

        /// <summary>
        /// Compares two rooms by name
        /// </summary>
        /// <param name="x">room</param>
        /// <param name="y">room</param>
        /// <returns>is equal</returns>
        public bool Equals(Room x, Room y)
        {
            return x?.GetHashCode() == y?.GetHashCode();
        }

        /// <summary>
        /// Get enumerator for iteration
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<Room> GetEnumerator()
        {
            return _Rooms.Values.GetEnumerator();
        }

        /// <summary>
        /// Default GetHashCode
        /// </summary>
        /// <param name="obj">room</param>
        /// <returns>hash code</returns>
        public int GetHashCode(Room obj)
        {
            return obj.GetHashCode();
        }

        /// <summary>
        /// Removes the room from this collection
        /// </summary>
        /// <remarks>does NOT leave or free the room</remarks>
        /// <param name="key">Room name</param>
        /// <returns>is removed</returns>
        public bool Remove(ulong id)
        {
            var room = this[id];
            return room != null && _Rooms.TryRemove(room.GetHashCode(), out _);
        }

        /// <summary>
        /// Close a room
        /// </summary>
        /// <remarks>Will not remove the room from collection</remarks>
        /// <param name="room">room</param>
        /// <returns>true if room to close found</returns>
        public bool Close(Room room)
        {
            return Close(room.Id);
        }

        /// <summary>
        /// Close a room
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if room to close found</returns>
        public bool Close(ulong id)
        {
            var room = this[id];
            bool result = room != null && _Rooms.TryGetValue(room.GetHashCode(), out room);
            if (result) room.Close();
            return result;
        }

        /// <summary>
        /// Free and remove the room
        /// </summary>
        /// <param name="key">room name</param>
        /// <returns>true if removed</returns>
        internal bool Free(Room room)
        {
            return Free(room.GetHashCode());
        }
        /// <summary>
        /// Free and remove the room
        /// </summary>
        /// <param name="id">room id <see cref="Room.GetRoomId"/></param>
        /// <returns>true if removed</returns>
        internal bool Free(ulong id)
        {
            var room = this[id];
            bool result = room != null && _Rooms.TryRemove(room.GetHashCode(), out _);
            if (result) room.Dispose();
            return result;
        }

        /// <summary>
        /// Free and remove the room
        /// </summary>
        /// <param name="key">room hashCode</param>
        /// <returns>true if removed</returns>
        internal bool Free(int key)
        {
            bool result = _Rooms.TryRemove(key, out var room);
            if (result) room.Dispose();
            return result;
        }

        internal void FreeAll()
        {
            foreach (var kvp in _Rooms)
                Free(kvp.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Rooms.GetEnumerator();
        }
    }
}
