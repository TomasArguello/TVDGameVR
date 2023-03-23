using System;
using System.Linq;
using UnityEngine;
using TMPro;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd {
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
	 *  Connect the player's entered name to their Photon profile for joining a room, as well as
	 *  save to / load from PlayerPrefs for continuity between sessions.
	 */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    public class NameLoader : MonoBehaviour {
        // Fields =================================================================================
        public TMP_InputField NameField;

        private const string _playerNamePrefKey = "PlayerName";

        // ========================================================================================

        // Mono ===================================================================================
        // ------------------------------------------------------------------------------
        void Awake() {
            if (this.NameField == null)
                Debug.LogError("NameLoader is missing a reference to a text input field.");
        }
        // ------------------------------------------------------------------------------
        void Start() {
            string defaultName = string.Empty;
            if (PlayerPrefs.HasKey(_playerNamePrefKey)) {
                defaultName = PlayerPrefs.GetString(_playerNamePrefKey);
                this.NameField.text = defaultName;
            }
            PhotonNetwork.NickName = defaultName;
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================

        // Methods ================================================================================
        public void SetPlayerName(string value) {
            if (string.IsNullOrEmpty(value))
                Debug.LogWarning("Player name is null or empty.");
            else {
                PhotonNetwork.NickName = value;
                PlayerPrefs.SetString(_playerNamePrefKey, value);
            }
            Debug.Log("name is:" + value);
        }
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}