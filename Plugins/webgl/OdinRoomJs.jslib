var LibraryOdin = {
    buffer: undefined,
    $OShare: {
        verbose: true,
        currentRoom: undefined,
        currentPeer: undefined,
        currentMediaStream: undefined,
        currentOdinMedia: undefined,
        audioPlugin: undefined,
        PeerMedias: undefined,
    },
    $OdinCallbacks: {},
    $OdinFunctions: {
        initplugin: async function() {
            if (OShare.audioPlugin) return OShare.audioPlugin;
            OShare.audioPlugin = await WebPlugin.create(async (sampleRate) => {
                const audioContext = new AudioContext({ sampleRate });
                await audioContext.resume();
                return audioContext;
            });
            OShare.PeerMedias = new Map();
            return OShare.audioPlugin;
        },
        fetchToken: async function (roomName, userId, customerId, tokenRequestUrl) {
            try {
                const response = await fetch(
                    tokenRequestUrl,
                    {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                        },
                        body: JSON.stringify({
                            roomId: roomName,
                            userId: userId,
                            customer: customerId,
                        }),
                    }
                );

                if (!response.ok) {
                    throw new Error("Network response was not ok (jslib)");
                }

                const data = await response.json();
                return data.token;
            } catch (error) {
                console.error("#Odin-jslib Error fetching token:", error);
                throw error;
            }
        },
        onPluginEvent: function(methodName, properties) {
            if (OShare.verbose) console.log("#Odin-jslib onPluginEvent: " + methodName + " with " + JSON.stringify(properties));
            switch (methodName) {
                case "RoomStatusChanged":
                    OdinFunctions.dispatchUnityEvent(methodName, JSON.stringify(properties));
                    break;
                case "RoomUpdated":
                    for (const update of properties.updates) {
                        OdinFunctions.jsHandleRoomUpdate(update);
                    }
                    break;
                case "PeerUpdated":
                    OdinFunctions.jsHandlePeerUpdate(properties);
                    break;
                case "MessageReceived":
                    OdinFunctions.dispatchUnityEvent(methodName, JSON.stringify(
                        {
                            peer_id: properties.sender_peer_id,
                            message: Array.from(properties.message)
                        }));
                    break;
                default:
                    console.log("#Odin-jslib onPluginEvent: missing " + JSON.stringify({ methodName, propertiesData: properties }));
            }
        },
        jsHandleRoomUpdate: function (update) {
            if (OShare.verbose) console.log("#Odin-jslib onPluginEvent jsHandleRoomUpdate: " + JSON.stringify(update));
            switch (update.kind) {
                case "Joined":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                            room_id: update.room.id,
                            own_peer_id: 0,
                            room_user_data: Array.from(update.room.user_data),
                            peers: update.room.peers.map((peer) => ({
                                peer_id: peer.id,
                                user_id: peer.user_id,
                                user_data: Array.from(peer.user_data),
                                medias: Array.isArray(peer.medias) ? peer.medias.map((media) => ({
                                    media_id: media.id,
                                    media_uid: media.properties.uid,
                                    paused: media.paused
                                })) : []
                            })),
                        }));
                    break;
                case "PeerJoined":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                            peer_id: update.peer.id,
                            user_id: update.peer.user_id,
                            user_data: Array.from(update.peer.user_data),
                            medias: update.medias !== undefined ? Array.from(update.medias) : [],
                        }));
                    break;
                case "PeerLeft":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                            peer_id: update.peer_id,
                        }));
                    break;
                default:
                    console.log("#Odin-jslib onPluginEvent jsHandleRoomUpdate: missing kind " + update.kind);
                    return;
            }
        },
        jsHandlePeerUpdate: function (update) {
            if (OShare.verbose) console.log("#Odin-jslib onPluginEvent jsHandlePeerUpdate: " + JSON.stringify(update));
            switch (update.kind) {
                case "UserDataChanged":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                            peer_id: update.peer_id,
                            user_data: Array.from(update.user_data),
                        }));
                    break;
                case "MediaStarted":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                            peer_id: update.peer_id,
                            media_id: update.media.id,
                            media_uid: update.media.properties.uid,
                            paused: update.paused,
                        }));
                    break;
                case "MediaStopped":
                    OdinFunctions.dispatchUnityEvent(update.kind, JSON.stringify({
                        peer_id: update.peer_id,
                        media_id: update.media_id,
                    }));
                break;
                    default:
                    console.log("#Odin-jslib onPluginEvent jsHandlePeerUpdate: missing kind " + JSON.stringify(update));
                    return;
            }
        },
        linkCapture: async function(room, media) {
            if (OShare.verbose) console.log("#Odin-jslib LinkCapture: "+ media);
            await room.link(media)
        },
        unlinkCapture: function(room, media) {
            if (OShare.verbose) console.log("#Odin-jslib UnlinkCapture: " + media);

            // closing the media stream before unlinking will make the microphone input icon in the browser disappear
            media.close();
            room.unlink(media);
        },
        startRemoteMedia: async function (room, media_uid) {
            if (media_uid == undefined) return;
            
            var playback = OShare.PeerMedias.get(media_uid);
            if (!playback) {
                console.log("#Odin-jslib startRemoteMedia: room "+ room._Id +" creating playback "+ media_uid);
                playback = await OShare.audioPlugin.createAudioPlayback({ uid: media_uid });
            }
            
            console.log("#Odin-jslib startRemoteMedia: room "+ room._Id +" link playback "+ media_uid +" obj " + playback);
            room.link(playback);
            OShare.PeerMedias.set(media_uid, playback)
            return playback;
        },
        stopRemoteMedia: async function (room, media_uid) {
            if (media_uid == undefined) return;
            const playback = OShare.PeerMedias.get(media_uid);
            room.unlink(playback);
            return playback;
        },
        sendRequest: async function(name, properties) {
            if (OShare.debug) console.trace("#Odin-jslib trace rpc sendRequest \""+name+"\": "+ properties);
            await OShare.currentRoom.request(name, properties);
        },
        sendMessage: async function(message, targetPeerIds) {
            if (OShare.verbose) console.log("#Odin-jslib SendMessage rpc: "+ message);
            const params = { message };
            if (Array.isArray(targetPeerIds) && targetPeerIds.length > 0) {
                params.target_peer_ids = targetPeerIds;
            }
            await OdinFunctions.sendRequest("SendMessage", params);
        },
        sendUserData: async function(userData) {
            if (OShare.verbose) console.log("#Odin-jslib UpdatePeer rpc: "+ userData);
            await OdinFunctions.sendRequest("UpdatePeer", {
                user_data: userData
            });
        },
        disconnect: function () {
            if(OShare.audioPlugin)
                OShare.audioPlugin.close();
        },
        dispatchUnityEvent: function (eventName, payload) {
            if (OShare.verbose) console.log("#Odin-jslib dispatchUnityEvent \""+eventName+"\": "+ payload);
            if (OShare.debug) console.trace("#Odin-jslib trace dispatchUnityEvent \""+eventName+"\": "+ payload);
            if (OdinCallbacks[eventName])
                SendMessage(
                    OdinCallbacks[eventName].object,
                    OdinCallbacks[eventName].function,
                    payload
                );
        },
    },
    JsLibLoadWebPlugin: async function() {
        console.log("#Odin-jslib Loading Odin web plugin ...");
        OShare.audioPlugin = await OdinFunctions.initplugin();
        if (OShare.verbose) console.log("#Odin-jslib plugin type " + typeof(OShare.audioPlugin));
    },
    JsLibJoinRoomPlugin: async function(paramsUtf8) {
        OShare.audioPlugin.setOutputDevice({}).then();
        const jsonData = UTF8ToString(paramsUtf8)
        console.log("#Odin-jslib joining Odin web plugin with " + jsonData);
        const parameters = JSON.parse(jsonData);
        // we need to do it like this, because the acorn optimizer used by Unity's build system
        // can't handle spread operators when using the release mode.
        // The code is equivalent to this line:
        // const joinRoomParams = {...parameters, onEvent: OdinFunctions.onPluginEvent };
        const joinRoomParams = Object.assign({}, parameters, {onEvent: OdinFunctions.onPluginEvent});
        OShare.currentRoom = OShare.audioPlugin.joinRoom(joinRoomParams);
        if (OShare.verbose) console.log("#Odin-jslib room with token " + OShare.currentRoom.token);
    },
    JsLibCloseRoom: async function() {
        if (OShare.currentRoom) {
            console.log("#Odin-jslib close room");
            OShare.currentRoom.close();
            OShare.currentRoom = undefined;
        }
    },
    JsLibCreateCapture: async function (paramsUtf8) {
        const jsonData = UTF8ToString(paramsUtf8)
        const audioSettings = JSON.parse(jsonData);
        console.log("#Odin-jslib CreateCapture: "+ jsonData);
        if (OShare.currentOdinMedia) {
            OShare.currentRoom.unlink(OShare.currentOdinMedia);
            OShare.currentOdinMedia = undefined;
        }
        OShare.currentOdinMedia = await OShare.audioPlugin.createAudioCapture({
            vad: {
                voiceActivity: {
                    attackThreshold: audioSettings.rtc.vad_attackThreshold,
                    releaseThreshold: audioSettings.rtc.vad_releaseThreshold,
                },
                volumeGate: {
                    attackThreshold: audioSettings.rtc.vg_attackThreshold,
                    releaseThreshold: audioSettings.rtc.vg_releaseThreshold,
                },
            },
            apm: {
                echoCanceller: audioSettings.apm.echoCanceller,
                highPassFilter: audioSettings.apm.highPassFilter,
                preAmplifier: audioSettings.apm.preAmplifier,
                captureLevelAdjustment: audioSettings.apm.captureLevelAdjustment,
                noiseSuppression: audioSettings.apm.noiseSuppression,
                transientSuppressor: audioSettings.apm.transientSuppressor,
                gainController: audioSettings.apm.gainController,
            },
        });
        if (OShare.verbose) console.log("#Odin-jslib capture " + OShare.currentOdinMedia);
        if(OShare.currentOdinMedia)
            OShare.currentRoom.link(OShare.currentOdinMedia);
    },
    JsLibLinkCapture: function() {
        if (!OShare.currentOdinMedia) return console.log("#Odin-jslib LinkCapture is missing capture media!");
        if (OShare.currentRoom)
            OdinFunctions.linkCapture(OShare.currentRoom, OShare.currentOdinMedia);
    },
    JsLibUnlinkCapture: function() {
        if (!OShare.currentOdinMedia) return console.log("#Odin-jslib UnlinkCapture is missing capture media!");
        if (OShare.currentRoom)
            OdinFunctions.unlinkCapture(OShare.currentRoom, OShare.currentOdinMedia);
    },
    JsLibStartPlayback: function(uidUtf8) {
        const uid = UTF8ToString(uidUtf8);
        if (OShare.currentRoom)
            OdinFunctions.startRemoteMedia(OShare.currentRoom, uid);
    },
    JsLibStopPlayback: function(uidUtf8) {
        const uid = UTF8ToString(uidUtf8);
        if (OShare.currentRoom) {
            OdinFunctions.stopRemoteMedia(OShare.currentRoom, uid);
            OShare.PeerMedias.delete(uid);
        }
    },
    JsLibRequestToken: async function(roomNameUtf8, userUtf8, customerUtf8, tokenRequestUrlUtf8, callback) {
        const roomId = UTF8ToString(roomNameUtf8);
        const userId = UTF8ToString(userUtf8);
        const customerId = UTF8ToString(customerUtf8);
        const tokenRequestUrl = UTF8ToString(tokenRequestUrlUtf8);
        try {
            var token = await OdinFunctions.fetchToken(roomId, userId, customerId, tokenRequestUrl);
            var tokenBuffer = stringToNewUTF8(token);
            {{{ makeDynCall('vi', 'callback') }}}(tokenBuffer);
            _free(tokenBuffer);
        } catch (e) {
            console.log("#Odin-jslib Error requesting token" + e);
        }
    },
    JsLibSendMessage: async function(paramsUtf8) {
        if(!OShare.currentRoom) return;

        const jsonData = UTF8ToString(paramsUtf8)
        const args = JSON.parse(jsonData); // { data: [], peerIds: [] }
        var messageData = args.data;
        var targetPeerIds = args.peerIds;
        if (targetPeerIds.length > 0)
            await OdinFunctions.sendMessage(messageData, targetPeerIds);
        else
        await OdinFunctions.sendMessage(messageData);
    },
    JsLibUpdateUserData: async function(paramsUtf8) {
        if(!OShare.currentRoom) return;
        
        const args = UTF8ToString(paramsUtf8)
        const userdata = JSON.parse(args); // { data: [] }
        await OdinFunctions.sendUserData(Uint8Array.from(userdata.data));
    },
    // used to register callback functions. Functions will be dispatched to Unity when an ODIN event occurs.
    JsLibAddCallback: function (callbackType, callbackObject, callbackFunction) {
        var callbackTypeString = UTF8ToString(callbackType);
        var callbackFunctionString = UTF8ToString(callbackFunction);
        var callbackObjectString = UTF8ToString(callbackObject);
        OdinCallbacks[callbackTypeString] = {
            object: callbackObjectString,
            function: callbackFunctionString,
        };
    },
    JsLibDisconnectOdin: function () {
        OdinFunctions.disconnect();
    },
    JsLibSetVerbose: function(flag) {
        OShare.verbose = flag;
    }
};

autoAddDeps(LibraryOdin, "$OShare");
autoAddDeps(LibraryOdin, "$OdinCallbacks");
autoAddDeps(LibraryOdin, "$OdinFunctions");
mergeInto(LibraryManager.library, LibraryOdin);