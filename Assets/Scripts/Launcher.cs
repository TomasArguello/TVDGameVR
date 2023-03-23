using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd {
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
	 *  Establish a connection to a game instance.
	 */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    public class Launcher : MonoBehaviourPunCallbacks {
        // Fields =================================================================================
        [Header("Settings")]
        [SerializeField]
        private const int MaxPlayersPerRoom = 5;
        private readonly string _gameVersion = "1";
        public static readonly string LAUNCH_SCREEN = "LaunchScene";
        public static readonly string LEVEL_TO_LOAD = "Photon2Room_Custom";
        public static readonly string ROOM_PREFIX = "tamu_tvd_";
        public static readonly string TUTORIAL_LEVEL = "TutorialScene";
        public static readonly string TUTORIAL_PREFIX = "tut_";
        public static readonly string DEMO_PREFIX = "demo_";
        public static readonly string DEMO_LEVEL = "DemoScene";

        [Header("UI References")]
        //[SerializeField] private GameObject _mainMenu;
        //[SerializeField] private GameObject _demoMenu;
        [Space]
        [SerializeField] private GameObject _roomPanel;
        [SerializeField] private TMP_InputField _nameInput;
        //[SerializeField] private ListSelector _selectedColor;
        [SerializeField] public ListSelector _selectedRoom;
        [SerializeField] private UnityEngine.UI.Button _connectButton;


        [Tooltip("The UI Text to inform the user about the connection progress.")]
        [SerializeField] private TMP_Text _feedbackText;
        private GameObject _feedbackPanel;

        private bool _isConnecting; // Connection is async, based on several callbacks from Photon,
                                    // so we need to track state to know how to respond to a particular callback.

        [SerializeField, Space]
        private List<RoomInfo> _rooms = new List<RoomInfo>();

        // ========================================================================================

        // Mono ===================================================================================
        void Awake() {
            // TODO: Add null checks

            if (_roomPanel == null)
                Debug.LogError("Launcher is missing a reference to the UI panel root object.");

            if (_feedbackText == null)
                Debug.LogError("Launcher is missing a reference to a TMP_Text object for connection feedback.");
            _feedbackPanel = _feedbackText.GetComponentInParent<Canvas>().gameObject;

            PhotonNetwork.OfflineMode = false;
            // Ensure we can use PhotonNetwork.LoadLevel() on the master client
            // and that all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        // ------------------------------------------------------------------------------
        private void Start() {
            _feedbackPanel.SetActive(false);

            PhotonNetwork.AutomaticallySyncScene = true;
            this.ConnectToPhoton();
        }
        // ------------------------------------------------------------------------------
        private void Update() {
            _connectButton.interactable = _selectedRoom.SelectedObject != null
                && PhotonNetwork.CurrentLobby != null;
        }
        private void FixedUpdate() {
            if (PhotonNetwork.InLobby) {
                foreach (RoomInfo room in _rooms)
                    this.CheckRoomAvailability(room);
            }
        }
        // ========================================================================================

        // Helpers ================================================================================
        // Start connection process to Photon Cloud Network -----------------------------
        public void ConnectToPhoton() {
            if (!PhotonNetwork.IsConnected) {
                this.LogFeedback("Connecting...");
                _isConnecting = PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = _gameVersion;
            } else
                this.LogFeedback("Already connected");
        }
        // ------------------------------------------------------------------------------
        // Attempt to join selected room ------------------------------------------------
        public void ConnectToRoom() {
            if (string.IsNullOrEmpty(_nameInput.text)) {
                EventSystem.current.SetSelectedGameObject(_nameInput.gameObject, null);
                _nameInput.ActivateInputField();
                _nameInput.Select();
                return;
            }

            //call SetPlayerName
            this.GetComponent<NameLoader>().SetPlayerName(_nameInput.text);

            _roomPanel.SetActive(false);
            _feedbackPanel.SetActive(true);
            _feedbackText.text = string.Empty;

            _isConnecting = true;

            if (PhotonNetwork.IsConnected) {
                if (PhotonNetwork.InLobby)
                    PhotonNetwork.LeaveLobby();

                this.LogFeedback($"Joining Room {_selectedRoom.SelectedIndex + 1}...");
                PhotonNetwork.JoinOrCreateRoom($"{ROOM_PREFIX}{_selectedRoom.SelectedIndex + 1}",
                    new RoomOptions() {
                        MaxPlayers = MaxPlayersPerRoom,
                        PlayerTtl = 10000,
                        PublishUserId = true
                    },
                    TypedLobby.Default
                    );
            } else
                this.ConnectToPhoton();
        }
        // ------------------------------------------------------------------------------
        // Launch Tutorial --------------------------------------------------------------
        public void ConnectToPrivateRoom() {
            PhotonNetwork.Disconnect();
        }
        private void LaunchPrivateRoom(string roomPrefix) {
            PhotonNetwork.OfflineMode = true;

            if (PhotonNetwork.InLobby)
                PhotonNetwork.LeaveLobby();

            Guid roomID = Guid.NewGuid();
            this.LogFeedback($"Joining Room {roomID}...");
            PhotonNetwork.JoinOrCreateRoom($"{roomPrefix}{roomID}",
                new RoomOptions() {
                    MaxPlayers = 1,
                    PlayerTtl = 20000,
                    PublishUserId = true
                },
                TypedLobby.Default
                );
        }
        // ------------------------------------------------------------------------------
        // Log connection feedback to the UI --------------------------------------------
        private void LogFeedback(string message) {
            _feedbackText.text += $"{Environment.NewLine}{message}";
        }
        // ------------------------------------------------------------------------------
        // Show  --------------------------------------------
        private async void ShowUIPanel(int msDelay, bool showUI = true) {
            await Cysharp.Threading.Tasks.UniTask.Delay(msDelay);

            if (_feedbackPanel != null)
                _feedbackPanel.SetActive(!showUI);
            if (_roomPanel != null)
                _roomPanel.SetActive(showUI);
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================

        // PUN Callbacks ==========================================================================
        #region PUN Callbacks
        // ------------------------------------------------------------------------------
        public override void OnConnectedToMaster() {
            if (PhotonNetwork.OfflineMode) {
                Debug.Log($"Launcher: OnConnectedToMaster called in offline mode.");
                return;
            }

            if (_isConnecting) {
                Debug.Log($"Launcher: OnConnectedToMaster called for {PhotonNetwork.NickName}." + $"{Environment.NewLine}"
                    + $"{PhotonNetwork.CloudRegion}:{PhotonNetwork.ServerAddress} : {PhotonNetwork.CurrentCluster}");
                PhotonNetwork.JoinLobby();
            } else {
                // This occurs, for example, when we exit from another scene and reload this scene.
                // We connect to the master, but we are not attempting to join a room.
                Debug.Log($"Launcher: OnConnectedToMaster() called for {PhotonNetwork.NickName} (not connecting).");
                if (!PhotonNetwork.InLobby)
                    PhotonNetwork.JoinLobby();
            }
        }
        // ------------------------------------------------------------------------------
        public override void OnDisconnected(DisconnectCause cause) {
            Debug.LogWarning($"Launcher: OnDisconnected() was called by PUN with reason '{cause}'");
            this.LogFeedback($"<color=\"red\">Disconnected</color> from {PhotonNetwork.ServerAddress}{Environment.NewLine}{cause}");

            if (cause == DisconnectCause.DisconnectByClientLogic) {
                //if (_mainMenu.activeInHierarchy)
                //    this.LaunchPrivateRoom(TUTORIAL_PREFIX);
                //else
                this.LaunchPrivateRoom(DEMO_PREFIX);
                return;
            }

            if (_feedbackPanel != null)
                _feedbackPanel.SetActive(false);
            if (_roomPanel != null)
                _roomPanel.SetActive(true);

            _isConnecting = false;
            this.ConnectToPhoton();
        }
        // ------------------------------------------------------------------------------
        public override void OnJoinedRoom() {
            Debug.Log($"Launcher: OnJoinedRoom() was called by PUN. Player {PhotonNetwork.NickName} joined room {PhotonNetwork.CurrentRoom.Name}, now at {PhotonNetwork.CurrentRoom.PlayerCount} players.");
            this.LogFeedback($"<color=\"green\">Joined Room</color> with {PhotonNetwork.CurrentRoom.PlayerCount} players");

            // Only load if we are the first player, else we rely on
            // PhotonNetwork.AutomaticallySyncScene to sync our instance scene
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1) {
                if (PhotonNetwork.OfflineMode) {
                    if (PhotonNetwork.CurrentRoom.Name.StartsWith(TUTORIAL_PREFIX)) {
                        Debug.Log("Loading Tutorial");
                        PhotonNetwork.LoadLevel(TUTORIAL_LEVEL);
                    } else {
                        Debug.Log("Loading Demo");
                        PhotonNetwork.LoadLevel(DEMO_LEVEL);
                    }
                } else {
                    Debug.Log("Loading GameRoom");
                    PhotonNetwork.LoadLevel(LEVEL_TO_LOAD);
                }
            }
        }
        // ------------------------------------------------------------------------------
        public override void OnJoinRoomFailed(short returnCode, string message) {
            Debug.Log("Launcher: OnJoinRoomFailed() was called by PUN." + Environment.NewLine
                + $"Code {returnCode}{Environment.NewLine}{message}");
            this.LogFeedback("<color=\"red\">Failed to join room.</color>"
                + Environment.NewLine
                + (returnCode == 32749 ? $"Please try again in a minute.{Environment.NewLine}"
                    : (returnCode == 32765 ? $"Room is full, please select a different room.{Environment.NewLine}" : "")
                  )
                + "Returning to lobby...");

            if (!PhotonNetwork.InLobby)
                PhotonNetwork.JoinLobby();

            this.ShowUIPanel(4000);
        }
        // ------------------------------------------------------------------------------
        //public override void OnCreateRoomFailed(short returnCode, string message)
        //{
        //    // TODO:
        //}
        // ------------------------------------------------------------------------------
        public override void OnRoomListUpdate(List<RoomInfo> roomList) {
            Debug.Log($"There are {roomList.Count} rooms.");
            _rooms = roomList;
            _rooms.ForEach(r => this.CheckRoomAvailability(r));
        }
        public bool CheckRoomAvailability(RoomInfo room) {
            if (room.Name.StartsWith(ROOM_PREFIX)
                && Int32.TryParse(room.Name.Substring(ROOM_PREFIX.Length), out int roomNumber)) {
                bool available = room.PlayerCount < MaxPlayersPerRoom && room.IsVisible && room.IsOpen;
                _selectedRoom.Items[roomNumber - 1].GetComponent<UnityEngine.UI.Button>().interactable = available;

                if (_selectedRoom.SelectedIndex == (roomNumber - 1) && !available)
                    _selectedRoom.Select(-1);

                return available;
            }
            return true;
        }
        // ------------------------------------------------------------------------------
        public override void OnJoinedLobby() {
            Debug.Log($"Joined Lobby {PhotonNetwork.CurrentLobby}");
            Debug.Log($"There are {PhotonNetwork.CountOfPlayers} players, {PhotonNetwork.CountOfPlayersInRooms} in rooms and {PhotonNetwork.CountOfPlayersOnMaster} looking to join.");
        }
        // ------------------------------------------------------------------------------
        #endregion
        // ==================================================================================

    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================

#if UNITY_EDITOR
    [CustomEditor(typeof(Launcher))]
    public class LauncherEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.HelpBox("The buttons below help with testing.", MessageType.Info);
            Launcher launcher = (Launcher)target;
            if(GUILayout.Button("Connect to room"))
            {
                launcher.ConnectToRoom();
            }
        }
    }
#endif
}