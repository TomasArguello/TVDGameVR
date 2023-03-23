using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Highlight objects under the cursor and allow them to be "picked up" by activating the
	 *  PlacementCaster with a copy of them and deleting them.
	 *  
	 *  TODO: I don't like the _possiblePrefabs lookup as a solution to retrieve the prefab, it's
	 *  clunky. Should find a better way.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PointerOutliner))]
	public class GrabCaster : MonoBehaviourPun
	{
		// Fields =================================================================================
		[SerializeField] private PlacementCaster _placer;
		[SerializeField] private ControlModeManager _mode;
		private PointerOutliner _outliner;
		public Color OutlineColor;

		public OutlineManager Target => _outliner.Target;

		[SerializeField] private Transform[] _possiblePrefabs;
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
			_outliner = this.GetComponent<PointerOutliner>();
		}
		// ========================================================================================

		// Methods ================================================================================
		// Whether this component's managed objects are enabled -------------------------
		public bool IsActive
		{
			get => _outliner.IsActive;
			set
			{
				_outliner.IsActive = value;
				if (value)
					_outliner.OutlineColor = this.OutlineColor;
			}
		}
		// ------------------------------------------------------------------------------
		// Pickup -----------------------------------------------------------------------
		public void GrabTargetObject()
        {
            if (_outliner.Target != null && _outliner.Target.TryGetComponent(out PhotonView view)
				&& _outliner.Target.TryGetComponent(out PunPrefabName prefab))
            {
				Transform p = _possiblePrefabs.FirstOrDefault(t => t.name.Equals(prefab.PrefabName));
				if (p == null)
                {
					Debug.LogWarning($"Attempted to grab {_outliner.Target.NameAndID()} but no prefab match was found.");
					return;
                }
				_placer.PlacementPrefab = p;
				_placer.SetPlacementRotation(_outliner.Target.transform.rotation);
				_mode.ActiveMode = ControlModeManager.ControlMode.Placement;
				// TODO: Should we hide the mesh of the target in case of latency in deletion?
				this.photonView.RPC("GrabTargetObject", RpcTarget.MasterClient, view.ViewID);
            }
		}
		[PunRPC]
		private void GrabTargetObject(int viewID)
		{
			if (!PhotonNetwork.IsMasterClient)
				return;

			PhotonNetwork.Destroy(PhotonView.Find(viewID));

			PunTapeHandler[] tape = GameObject.FindObjectsOfType<PunTapeHandler>();
			foreach (PunTapeHandler t in tape)
			{
				if (t.GetTapedObjects().Length < PunTapeHandler.MinTapeConnections)
				{
					PhotonNetwork.Destroy(t.gameObject);
				}
			}
		}
		// ------------------------------------------------------------------------------
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}