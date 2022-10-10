using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Adrenak.UniVoice;
using Adrenak.UniVoice.AudioSourceOutput;
using Adrenak.UniVoice.PUN2Network;
using Adrenak.UniVoice.UniMicInput;

using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Android;
using UnityEngine;

// This is a *very* quickly written sample scene. Improvements soon.
public class GroupVoiceCallSample_Photon : MonoBehaviourPunCallbacks {
    ChatroomAgent agent;

    void Awake() {
#if UNITY_ANDROID 
            if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO")) 
            Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
#endif
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start() {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = "1";
        var nickName = "Player_RANDOM" + Random.Range(10, 100);
        PhotonNetwork.NickName = nickName;
        Debug.Log("NickName : " + nickName);
        InitializeAgent();
    }


    void InitializeAgent() {
        agent = new ChatroomAgent(
            UniVoicePUN2NetworkEmbedded.New(1), // Be sure to read why the '1' is passed here, 
                                                // inside the UniVoicePUN2NetworkEmbedded file. The value may be different for you
            new UniVoiceUniMicInput(0, 16000, 100),
            new UniVoiceAudioSourceOutput.Factory()
        );

        agent.Network.OnAudioReceived += (i, data) => {
            // maybe show Audio spectrum 
        };

        agent.Network.OnAudioSent += (id, segment) => {
            // Show some audio outgoing animation?
        };

        agent.MuteOthers = false;
        agent.MuteSelf = false;

        // THESE EVENT SUBSCRIPTIONS BELOW
        // ARE NOT REALLY NEEDED IF YOU'RE USING PHOTON
        // EVENTS FOR YOUR GAME (AND DON'T INTEND TO
        // CHANGE THE UNDERLYING NETWORK OF UNIVOICE LATER)

        agent.Network.OnPeerJoinedChatroom += id => {
            Debug.Log("Joined " + id);
        };

        agent.Network.OnPeerLeftChatroom += id => {
            Debug.Log("Left " + id);
        };
        agent.Network.OnCreatedChatroom += () => {
            Debug.Log($"Chatroom created!\n" +
            $" You are Peer ID " + agent.Network.OwnID);
        };

        agent.Network.OnChatroomCreationFailed += ex => {
            Debug.Log("Chatroom creation failed");
        };

        agent.Network.OnClosedChatroom += () => {
            Debug.Log("You closed the chatroom! All peers have been kicked");
        };

        agent.Network.OnJoinedChatroom += id => {
            Debug.Log("Joined chatroom ");
            Debug.Log("You are Peer ID " + id);
        };

        agent.Network.OnChatroomJoinFailed += ex => {
            Debug.Log(ex);
        };

        agent.Network.OnLeftChatroom += () => {
            Debug.Log("You left the chatroom");
        };
    }

    public string roomName = "Test";

    [ContextMenu("Host")]
    public void Host() {
        PhotonNetwork.CreateRoom(roomName);
    }

    [ContextMenu("Join")]
    public void Join() {
        PhotonNetwork.JoinRoom(roomName);
    }

    #region PHOTON EVENTS
    // Called when the application is connected to Photon Network
    public override void OnConnected() {
        Debug.Log("We're online.");
    }

    // Called when we are connected to a master server
    // Invoke JoinLobby after this
    public override void OnConnectedToMaster() {
        Debug.Log("We're Conected to Master.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        Debug.Log($"{roomList.Count} Rooms found : {string.Join(", ", roomList.Select(x => x.Name).ToList())}");
    }

    // Called when we are in a jobby. 
    public override void OnJoinedLobby() {
        Debug.Log("We're in the lobby.");
    }

    // Called before OnJoinedRoom
    public override void OnCreatedRoom() {
        Debug.Log("Room created " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnLeftRoom() {
        Debug.Log("Left room" + PhotonNetwork.LocalPlayer.IsMasterClient);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        Debug.Log("Left " + otherPlayer.NickName);
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        Debug.Log("New Master " + newMasterClient.NickName);
    }

    // Called on the MasterClient too
    public override void OnJoinedRoom() {
        Debug.Log("Joined room. Players in the room : " + 
            string.Join(", ", PhotonNetwork.CurrentRoom.Players.Values.Select(x => x.NickName).ToList()));
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.Log("New Player " + newPlayer.NickName);
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Failed to join random room. Return code " + returnCode + " Message " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to create room. Return code " + returnCode + " Message " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message) {
        Debug.Log("Failed to join room. Return code " + returnCode + " Message " + message);
    }

    public override void OnDisconnected(DisconnectCause cause) {
        Debug.Log("Disconnected. Cause " + cause);
    }
    #endregion
}
