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
	 *  Highlight objects for removal when the cursor is over them, and expose functionality to
	 *  remove the object currently under the cursor.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PointerOutliner))]
	public class RemovalCaster : MonoBehaviourPun
	{
		// Fields =================================================================================
		private bool _isActive = false;

		private PointerOutliner _outliner;
		public Color OutlineColor;

		public OutlineManager Target => _outliner.Target;
        // ========================================================================================

        // Mono ===================================================================================
        private void Awake()
        {
			_outliner = this.GetComponent<PointerOutliner>();
        }
        // ========================================================================================

        // Methods ================================================================================
        // Whether this component's managed objects are enabled -------------------------
        public bool IsActive
		{
			get => _isActive;
			set
			{
				_isActive = value;
				_outliner.IsActive = value;
				if (value)
					_outliner.OutlineColor = this.OutlineColor;
			}
		}
		// ------------------------------------------------------------------------------
		// Removal ----------------------------------------------------------------------
		public void RemoveTargetObject()
		{
			if (_outliner.Target != null && _outliner.Target.TryGetComponent(out PhotonView view))
            {
				this.photonView.RPC("DeleteTargetObject", RpcTarget.MasterClient, view.ViewID);
            }
		}
		[PunRPC]
		private void DeleteTargetObject(int viewID)
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