using OdinNative.Wrapper.Media.Rpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
        public uint Id { get; private set; }
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
        public ConcurrentDictionary<ulong, MediaDecoder> Medias { get; internal set; }

        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        public IRoom Parent { get; set; }

        /// <summary>
        /// Peer tags
        /// </summary>
        public List<string> Tags { get; internal set; }

        protected internal AudioParametersObject AudioParameters { get; set; }
        protected internal VideoParametersObject VideoParameters { get; set; }

        public PeerEntity GetBasePeer() => this;
        /// <summary>
        /// Get the base Room object
        /// </summary>
        /// <returns>Room if this.Parent is set</returns>
        public Room.Room GetRoomApi() => Parent as Room.Room;

        public MediaEncoder Encoder { get; internal set; }
        MediaEncoder IPeer.GetEncoder() => Encoder;

        public MediaDecoder Decoder { get; internal set; }
        MediaDecoder IPeer.GetDecoder() => Decoder;

        /// <summary>
        /// Client/Remote peer
        /// </summary>
        /// <param name="id">peer id</param>
        public PeerEntity(uint id)
        {
            Id = id;
            UserId = string.Empty;
            UserData = new UserData();
            Tags = new List<string>();
            Medias = new ConcurrentDictionary<ulong, MediaDecoder>();
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
        public bool GetDecoder(ulong mediaId, out MediaDecoder decoder)
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
        public bool GetOrCreateDecoder(ulong mediaId, uint samplerate, bool stereo, out MediaDecoder decoder)
        {
            if (Medias.TryGetValue(mediaId, out decoder))
                return true;

            decoder = MediaDecoder.Create(samplerate, stereo);
            if (decoder == null) return false;
            decoder.Parent = this;
            decoder.Id = mediaId;
            if (Medias.TryAdd(mediaId, decoder) == false)
            {
                // lost the race against a concurrent create for the same media id
                decoder.Dispose();
                return Medias.TryGetValue(mediaId, out decoder);
            }
            return true;
        }

        /// <summary>
        /// Create a new output media that will be added to <see cref="Medias"/>
        /// </summary>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(uint samplerate, bool stereo)
        {
            MediaDecoder decoder = MediaDecoder.Create(samplerate, stereo);
            if (decoder == null) return null;
            decoder.Parent = this;
            Medias.TryAdd(decoder.Id, decoder);

            return decoder;
        }
        /// <summary>
        /// Remove the output media from <see cref="Medias"/>
        /// </summary>
        /// <param name="mediaId">decoder id</param>
        /// <param name="decoder">output media that was removed</param>
        /// <returns>true if removed or false</returns>
        public bool RemoveDecoder(ulong mediaId, out MediaDecoder decoder) => Medias.TryRemove(mediaId, out decoder);

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
            return $"{nameof(PeerEntity)}: {nameof(Id)} {Id}" +
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
        /// Finalizer
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