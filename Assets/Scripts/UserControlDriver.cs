using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ControlMode = Tamu.Tvd.ControlModeManager.ControlMode;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd {
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Base class for a user controller to wrap functionality needed to instantiate from Photon.
	 *  
	 *  This allows us to implement different means of handling control modes in different
	 *  controller scripts, but still be able to drop any of those scripts into the PhotonManager
	 *  for runtime player instantiation.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(ControlModeManager))]
	public abstract class UserControlDriver : MonoBehaviourPun {
		// Fields =================================================================================
		[Tooltip("The current representation of the player in the scene.")]
		private static UserControlDriver _localPlayerInstance;
		public static UserControlDriver LocalPlayerInstance => _localPlayerInstance;

		[SerializeField] private Color _playerColor = Color.clear;
		public Color Color => _playerColor;


		protected ControlModeManager _mode;
		public ControlModeManager ModeManager => _mode;
		public ControlMode Mode => _mode.ActiveMode;

		public abstract Camera Camera { get; }
		// ========================================================================================

		// Exposed Properties for runtime dependency injection ====================================
		public abstract SelectionMenu ObjectMenu { get; set; }
		public abstract RotationSettings RotationSettings { get; set; }
		public abstract PlayButton PlayButton { get; set; }
		// ========================================================================================

		// Mono ===================================================================================
		protected void Awake() {
			DontDestroyOnLoad(this.gameObject); // Allows this to survive level syncing

			_mode = this.GetComponent<ControlModeManager>();
			Transform mesh = this.transform.Find("Mesh");

			bool isMine = this.photonView.IsMine;
			if (isMine)
				_localPlayerInstance = this;
			else {
				UserControlDriver[] players = GameObject.FindObjectsOfType<UserControlDriver>();
				List<Color> availableColors = GameObject.FindObjectOfType<PhotonManager>().PlayerColors;
				for (int p = 0; p < players.Length; p++) {
					for (int c = availableColors.Count - 1; c > -1; c--) {
						if (players[p].Color == availableColors[c]) {
							availableColors.RemoveAt(c);
							break;
						}
					}
				}
				_playerColor = availableColors.Count > 0
					? availableColors.RandomElement()
					: Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), .8f, .8f);

				if (mesh != null) {
					foreach (MeshRenderer renderer in mesh.GetComponentsInChildren<MeshRenderer>())
						renderer.material.color = _playerColor;
				}
			}

			foreach (UserList u in GameObject.FindObjectsOfType<UserList>())
				u.AddUser(this);

			//if (!Application.isEditor || isMine) {
			//	foreach (MeshRenderer renderer in mesh.GetComponentsInChildren<MeshRenderer>())
			//		renderer.enabled = false;
			//}

#if UNITY_EDITOR
			this.name = $"Player({this.photonView.Owner.NickName})";
#endif
		}
		// ------------------------------------------------------------------------------
		//protected void Start()
		//{
		//	// TODO: Should we emit starting mode here?
		//}
		// ========================================================================================

		// Methods ================================================================================
		// Initialize a Camera according to whether it is locally controlled ------------
		// Derived classes should call this as soon as they set their Camera reference
		protected void InitCameraSettings(Camera camera) {
			camera.enabled = this.photonView.IsMine;
			if (!camera.enabled) {
				camera.targetDisplay = 2;
				camera.targetTexture = null;
			} else
				camera.tag = "MainCamera";
		}
		// ------------------------------------------------------------------------------
		// Handle anything that needs to happen before OnDestroy ------------------------
		// The reason for this is that I tried executing an RPC in OnDestroy and OnPreNetDestroy
		// but the RPC never fired on the other clients...
		public abstract void Cleanup();
		// ------------------------------------------------------------------------------
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}