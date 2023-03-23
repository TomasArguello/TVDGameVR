using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd {
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Handle connections and disconnections of players to this room, instantiating the local
	 *  player instance if necessary. When instantiating the player, also hook up its references to
	 *  relevant things in the room, like UI elements.
	 *  
	 *  TODO: Could probably OnValidate() that the player prefab is in a resources folder.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class PhotonManager : MonoBehaviourPunCallbacks {
		// Fields =================================================================================
		[Tooltip("Prefab defining a player. MUST BE IN A 'Resources' FOLDER!")]
		public GameObject _playerPrefab; // Photon requires the prefab to be in a Resources folder
										 // to instantiate it over the network
		private UserControlDriver _myPlayer;
		[SerializeField] private Transform _spawnLocation;

		public List<Color> PlayerColors = new List<Color>();

		//[Header("UI Injection")]
		//[SerializeField] private PlayButton _playButton;
		//[SerializeField] private SelectionMenu _toolMenu;
		//[SerializeField] private SelectionMenu _objectSelector;
		//[SerializeField] private RotationSettings _rotSettings;
		//[SerializeField] private CameraSettings _cameraSettings;
		//[SerializeField] private SelectorActions _selectorActions;
		// ========================================================================================

		// Mono ===================================================================================
		// ------------------------------------------------------------------------------
		void Awake() {
			if (_playerPrefab == null)
				Debug.LogError("PhotonManager is missing a reference to a player prefab.");

			PhotonNetwork.AutomaticallySyncScene = true;
		}
		// ------------------------------------------------------------------------------
		void Start() {
#if UNITY_EDITOR
			if (PhotonNetwork.CurrentRoom == null && !PhotonNetwork.OfflineMode) {
				Debug.LogWarning("Not in a room, returning to lobby...");
				PhotonNetwork.LoadLevel(Launcher.LAUNCH_SCREEN);
				return;
			}
#endif

			if (UserControlDriver.LocalPlayerInstance == null) {
				Debug.Log($"Instantiating local player from {SceneManagerHelper.ActiveSceneName}.");
				_myPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, _spawnLocation.position, _spawnLocation.rotation)
					.GetComponent<UserControlDriver>();

                //_myPlayer.PlayButton = _playButton;
                //_myPlayer.ObjectMenu = _objectSelector;
                //_myPlayer.RotationSettings = _rotSettings;
                if (_myPlayer is DemoController _demoPlayer) {
                    //_demoPlayer.ToolMenu = _toolMenu;
                    //_demoPlayer.CameraSettings = _cameraSettings;
                    //_demoPlayer.SelectorActions = _selectorActions;
                }
            }
            //else
            //	Debug.Log($"Ignoring scene load for {SceneManagerHelper.ActiveSceneName}.");

        }
		// ------------------------------------------------------------------------------
		// ========================================================================================

		// Methods ================================================================================
		public void LeaveRoom() {
			if (_myPlayer != null) {
				_myPlayer.Cleanup();
				PhotonNetwork.Destroy(_myPlayer.gameObject);
			}
			PhotonNetwork.LeaveRoom();
		}
		// ========================================================================================

		// Photon Callbacks =======================================================================
		public override void OnLeftRoom() {
			SceneManager.LoadScene(Launcher.LAUNCH_SCREEN);
		}

		public override void OnPlayerEnteredRoom(Player newPlayer) {
			Debug.Log($"OnPlayerEnteredRoom(): {newPlayer.NickName}");
		}

		public override void OnPlayerLeftRoom(Player otherPlayer) {
			Debug.Log($"OnPlayerLeftRoom(): {otherPlayer.NickName}");
		}
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}