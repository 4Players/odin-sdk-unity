using System;
using System.Collections.Concurrent;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Client/Remote peer
    /// </summary>
    public class PeerEntity : IPeer, IDisposable
    {
        /// <summary>
        /// Peer id
        /// </summary>
        public ulong Id { get; private set; }
        /// <summary>
        /// Peers user id
        /// </summary>
        public string UserId { get; internal set; }
        /// <summary>
        /// Peer userdata
        /// </summary>
        public IUserData UserData { get; internal set; }
        /// <summary>
        /// Peer output medias
        /// </summary>
        public ConcurrentDictionary<ushort, MediaDecoder> Medias { get; internal set; }

        /// <summary>
        /// Default value <code>null</code> indicates root or not set
        /// </summary>
        public IRoom Parent { get; set; }
        public PeerEntity GetBasePeer() => this;
        /// <summary>
        /// Get the base Room object
        /// </summary>
        /// <returns>Room if this.Parent is set</returns>
        public Room.Room GetRoomApi() => Parent?.GetBaseRoom<Room.Room>();

        /// <summary>
        /// Client/Remote peer
        /// </summary>
        /// <param name="id">peer id</param>
        public PeerEntity(ulong id)
        {
            Id = id;
            UserId = string.Empty;
            UserData = new UserData();
            Medias = new ConcurrentDictionary<ushort, MediaDecoder>();
        }

        internal void SetUserData(byte[] newData)
        {
            UserData = new UserData(newData);
        }

        internal void SetUserData(UserData userData)
        {
            UserData = userData;
        }

        /// <summary>
        /// Get a output media by id.
        /// </summary>
        /// <param name="mediaId">decoder id</param>
        /// <param name="decoder">output media</param>
        /// <returns>true on found or false</returns>
        public bool GetDecoder(ushort mediaId, out MediaDecoder decoder)
        {
            return Medias.TryGetValue(mediaId, out decoder);
        }

        /// <summary>
        /// Get a output media by id. If the decoder is not found create a new one that will be added <see cref="Medias"/>
        /// </summary>
        /// <param name="mediaId">decoder id</param>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <param name="decoder">output media</param>
        /// <returns>true on success or false</returns>
        public bool GetOrCreateDecoder(ushort mediaId, uint samplerate, bool stereo, out MediaDecoder decoder)
        {
            if (Medias.TryGetValue(mediaId, out decoder))
                return true;

            decoder = CreateDecoder(mediaId, samplerate, stereo);
            return decoder != null;
        }

        /// <summary>
        /// Create a new output media that will be added to <see cref="Medias"/>
        /// </summary>
        /// <param name="mediaId">decoder id</param>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(ushort mediaId, uint samplerate, bool stereo)
        {
            MediaDecoder decoder = MediaDecoder.Create(mediaId, samplerate, stereo);
            Medias.TryAdd(mediaId, decoder);

            return decoder;
        }
        /// <summary>
        /// Remove the output media from <see cref="Medias"/>
        /// </summary>
        /// <param name="mediaId">decoder id</param>
        /// <param name="decoder">output media that was removed</param>
        /// <returns>true if removed or false</returns>
        public bool RemoveDecoder(ushort mediaId, out MediaDecoder decoder) => Medias.TryRemove(mediaId, out decoder);

        internal void FreeMedias()
        {
            foreach (MediaDecoder encoder in Medias.Values)
                encoder.Dispose();

            Medias.Clear();
        }

        /// <summary>
        /// Debug
        /// </summary>
        /// <returns>info</returns>
        public override string ToString()
        {
            return $"{nameof(Peer)}: {nameof(Id)} {Id}" +
                $", {nameof(UserId)} \"{UserId}\"" +
                $", {nameof(UserData)} {!UserData?.IsEmpty()}";
        }

        private bool disposedValue;
        /// <summary>
        /// Free peer with all associated medias
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FreeMedias();
                    UserData = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Default deconstructor
        /// </summary>
        ~PeerEntity()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Free peer with all associated medias
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}