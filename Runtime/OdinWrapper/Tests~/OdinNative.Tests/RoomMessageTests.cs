using System.Text;
using OdinNative.Utils.Json;
using OdinNative.Wrapper.Room;
using Xunit;

namespace OdinNative.Tests
{
    /// <summary>
    /// Pins the json of room messages in both directions: the <c>SendMessage</c> rpc the room
    /// sends and the <c>MessageReceived</c> event it raises. Both shapes come from the ODIN
    /// core sdk readme and need no native library, the room never connects.
    /// </summary>
    public class RoomMessageTests
    {
        /// <summary>
        /// Room that records outgoing rpc json instead of handing it to the native library and
        /// exposes the rpc dispatcher so incoming events can be injected.
        /// </summary>
        private sealed class CapturingRoom : Room
        {
            public string LastRpc;

            public CapturingRoom() : base("gateway", 48000, false) { }

            public override bool SendRpc<T>(T obj)
            {
                LastRpc = JSONWriter.ToJson(obj);
                return true;
            }

            public void Receive(string json) => ProcessJsonRpc(json);
        }

        [Fact]
        public void SendMessage_String_BroadcastsUtf8Bytes()
        {
            var room = new CapturingRoom();

            Assert.True(room.SendMessage("hi"));
            Assert.Equal("{\"SendMessage\":{\"message\":[104,105]}}", room.LastRpc);
        }

        [Fact]
        public void SendMessage_Bytes_TargetsPeers()
        {
            var room = new CapturingRoom();

            Assert.True(room.SendMessage(new byte[] { 1, 2 }, new uint[] { 7, 9 }));
            Assert.Equal("{\"SendMessage\":{\"message\":[1,2],\"peer_ids\":[7,9]}}", room.LastRpc);
        }

        [Fact]
        public void SendMessage_NullBytes_IsRejected()
        {
            var room = new CapturingRoom();

            Assert.False(room.SendMessage(null, null));
            Assert.Null(room.LastRpc);
        }

        [Fact]
        public void MessageReceived_Binary_RaisesOnMessageReceived()
        {
            var room = new CapturingRoom();
            uint sender = 0;
            byte[] payload = null;
            room.OnMessageReceived += (_, peerId, message) => { sender = peerId; payload = message; };

            room.Receive("{\"MessageReceived\":{\"sender_peer_id\":17,\"message\":[104,101,108,108,111]}}");

            Assert.Equal(17u, sender);
            Assert.Equal("hello", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public void MessageReceived_JsonObject_RaisesOnMessageReceivedWithJsonText()
        {
            var room = new CapturingRoom();
            byte[] payload = null;
            room.OnMessageReceived += (_, _, message) => payload = message;

            room.Receive("{\"MessageReceived\":{\"sender_peer_id\":17,\"message\":{\"type\":\"chat\",\"text\":\"Hello!\"}}}");

            string json = Encoding.UTF8.GetString(payload);
            Assert.Contains("\"type\":\"chat\"", json);
            Assert.Contains("\"text\":\"Hello!\"", json);
        }
    }
}
