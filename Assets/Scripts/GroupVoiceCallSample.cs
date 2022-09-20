using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using Adrenak.UniVoice.UniMicInput;
using Adrenak.UniVoice.AudioSourceOutput;
using Adrenak.UniVoice.TelepathyNetwork;
using System.Net.Sockets;
using System.Net;
using Adrenak.UniVoice.AirPeerNetwork;

namespace Adrenak.UniVoice.Samples {
    public class GroupVoiceCallSample : MonoBehaviour {
        [Header("Menu")]
        public GameObject menuGO;
        public InputField inputField;
        public Button hostButton;
        public Button joinButton;
        public Button exitButton;
        public Text menuMessage;

        [Header("Chatroom")]
        public GameObject chatroomGO;
        public Transform peerViewContainer;
        public PeerView peerViewTemplate;
        public Text chatroomMessage;
        public Toggle muteSelfToggle;
        public Toggle muteOthersToggle;

        ChatroomAgent agent;
        Dictionary<short, PeerView> peerViews = new Dictionary<short, PeerView>();

        void Start() {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!AndroidPermissionsManager.IsPermissionGranted("android.permission.RECORD_AUDIO")) {
                AndroidPermissionsManager.RequestPermission(
                    "android.permission.RECORD_AUDIO",
                    new AndroidPermissionCallback(x => { }, y => Application.Quit())
                );
            }
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            InitializeInput();
            InitializeAgent();
            Debug.Log(GetLocalIPv4Address());
            menuGO.SetActive(true);
            chatroomGO.SetActive(false);
            muteSelfToggle.SetIsOnWithoutNotify(agent.MuteSelf);
            muteSelfToggle.onValueChanged.AddListener(value =>
                agent.MuteSelf = value);

            muteOthersToggle.SetIsOnWithoutNotify(agent.MuteOthers);
            muteOthersToggle.onValueChanged.AddListener(value =>
                agent.MuteOthers = value);
        }

        void InitializeInput() {
            hostButton.onClick.AddListener(HostChatroom);
            joinButton.onClick.AddListener(JoinChatroom);
            exitButton.onClick.AddListener(ExitChatroom);
        }

        void InitializeAgent() {
            agent = new ChatroomAgent(
                UniVoiceTelepathyNetwork.New(8987), 
                // TIP: Try swapping the above network with UniVoiceAirPeerNetwork below
                //new InbuiltChatroomAgentFactory("ws://167.71.17.13:11000").Create();

                new UniVoiceUniMicInput(0, 16000, 100),
                new UniVoiceAudioSourceOutput.Factory()
            );

            agent.Network.OnCreatedChatroom += () => {
                ShowMessage($"Chatroom created!\n" +
                $" You are Peer ID 0");
                menuGO.SetActive(false);
                chatroomGO.SetActive(true);
            };

            agent.Network.OnChatroomCreationFailed += ex => {
                ShowMessage("Chatroom creation failed");
            };

            agent.Network.OnClosedChatroom += () => {
                ShowMessage("You closed the chatroom! All peers have been kicked");
                menuGO.SetActive(true);
                chatroomGO.SetActive(false);
            };

            agent.Network.OnJoinedChatroom += id => {
                ShowMessage("Joined chatroom ");
                ShowMessage("You are Peer ID " + id);

                menuGO.SetActive(false);
                chatroomGO.SetActive(true);
            };

            agent.Network.OnChatroomJoinFailed += ex => {
                ShowMessage(ex);
            };

            agent.Network.OnLeftChatroom += () => {
                ShowMessage("You left the chatroom");

                menuGO.SetActive(true);
                chatroomGO.SetActive(false);
            };

            agent.Network.OnPeerJoinedChatroom += id => {
                var view = Instantiate(peerViewTemplate, peerViewContainer);
                view.IncomingAudio = !agent.PeerSettings[id].muteThem;
                view.OutgoingAudio = !agent.PeerSettings[id].muteSelf;

                view.OnIncomingModified += value =>
                    agent.PeerSettings[id].muteThem = !value;

                view.OnOutgoingModified += value =>
                    agent.PeerSettings[id].muteSelf = !value;

                peerViews.Add(id, view);
                view.SetPeerID(id);
            };

            agent.Network.OnPeerLeftChatroom += id => {
                var peerViewInstance = peerViews[id];
                Destroy(peerViewInstance.gameObject);
                peerViews.Remove(id);
            };

            agent.MuteOthers = false;
            agent.MuteSelf = false;
        }

        public static string GetLocalIPv4Address() {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        void Update() {
            if (agent == null || agent.PeerOutputs == null) return;

            foreach (var output in agent.PeerOutputs) {
                if (peerViews.ContainsKey(output.Key)) {
                    /*
                     * This is an inefficient way of showing a part of the 
                     * audio source spectrum. AudioSource.GetSpectrumData returns
                     * frequency values up to 24000 Hz in some cases. Most human
                     * speech is no more than 5000 Hz. Showing the entire spectrum
                     * will therefore lead to a spectrum where much of it doesn't
                     * change. So we take only the spectrum frequencies between
                     * the average human vocal range.
                     * 
                     * Great source of information here: 
                     * http://answers.unity.com/answers/158800/view.html
                     */
                    var size = 512;
                    var minVocalFrequency = 50;
                    var maxVocalFrequency = 8000;
                    var sampleRate = AudioSettings.outputSampleRate;
                    var frequencyResolution = sampleRate / 2 / size;

                    var audioSource = (output.Value as UniVoiceAudioSourceOutput).AudioSource;
                    var spectrumData = new float[size];
                    audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

                    var indices = Enumerable.Range(0, size - 1).ToList();
                    var minVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - minVocalFrequency), x)).x;
                    var maxVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - maxVocalFrequency), x)).x;
                    var indexRange = maxVocalFrequencyIndex - minVocalFrequency;

                    spectrumData = spectrumData.Select(x => 1000 * x)
                        .ToList()
                        .GetRange(minVocalFrequency, indexRange)
                        .ToArray();
                    peerViews[output.Key].DisplaySpectrum(spectrumData);
                }
            }
        }

        void HostChatroom() {
            var roomName = inputField.text;
            agent.Network.HostChatroom(roomName);
        }

        void JoinChatroom() {
            var roomName = inputField.text;
            if (string.IsNullOrEmpty(roomName))
                agent.Network.JoinChatroom();
            else
                agent.Network.JoinChatroom(roomName);
        }

        void ExitChatroom() {
            Debug.Log(agent.CurrentMode);
            if (agent.CurrentMode == ChatroomAgentMode.Host)
                agent.Network.CloseChatroom();
            else if (agent.CurrentMode == ChatroomAgentMode.Guest)
                agent.Network.LeaveChatroom();
        }

        void ShowMessage(object obj) {
            Debug.Log("<color=blue>" + obj + "</color>");
            menuMessage.text = obj.ToString();
            if (agent.CurrentMode != ChatroomAgentMode.Unconnected)
                chatroomMessage.text = obj.ToString();
        }
    }
}
