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
	 *  Highlight objects to be selected when the cursor is over them, and expose functionality to
	 *  grab or remove the currently selected object.
	 *  
	 *  TODO: I don't like the _possiblePrefabs lookup as a solution to retrieve the prefab, it's
	 *  clunky. Should find a better way.
	 *  TODO: Using the index of _possiblePrefabs as the selection index for ObjectMenu when
	 *  grabbing is TERRIBLE design and WILL break and is just AWFULLY written... but stringing
	 *  this spaghetti connection b/w the UI and the player is too annoying right now to solve
	 *  correctly, should probably figure out how to do that better in general...
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PointerOutliner))]
	public class SelectionCaster : MonoBehaviourPun
	{
		// Fields =================================================================================
		[SerializeField] private PlacementCaster _placer;
		//[SerializeField] private ControlModeManager _mode;
		[SerializeField] private DemoController _controller;
		[SerializeField] private Transform[] _possiblePrefabs;

		private PointerOutliner _outliner;
		public Color HoverColor;
		public Color SelectedColor;

		public OutlineManager Target => _outliner.Target;

		private OutlineManager _selection;
		public GameObject SelectedObject => _selection == null ? null : _selection.gameObject;
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
			get => _outliner.IsActive;
			set
			{
				_outliner.IsActive = value;
				if (value)
                {
					_outliner.OutlineColor = this.HoverColor;
					if (_selection != null)
						_selection.AddOutline(this.SelectedColor);
                }
				else if (_selection != null)
                {
					_selection.RemoveOutline();
					_outliner.Unignore(_selection);
					_selection = null;
                }
			}
		}
		// ------------------------------------------------------------------------------
		// Selection --------------------------------------------------------------------
		public void SelectTargetObject()
		{
			if (_selection != null)
			{
				if (_selection == _outliner.Target)
					return;

				_selection.RemoveOutline();
				_outliner.Unignore(_selection);
			}

			_selection = null;

			if (_outliner.Target != null && _outliner.Target.TryGetComponent(out PhotonView view))
			{
				_outliner.Target.AddOutline(this.SelectedColor);
				_selection = _outliner.Target;
				_outliner.Ignore(_selection);
			}
		}
		// ------------------------------------------------------------------------------
		// Pickup -----------------------------------------------------------------------
		public void GrabSelectedObject()
        {
			if (_selection != null && _selection.TryGetComponent(out PhotonView view)
				&& _selection.TryGetComponent(out PunPrefabName prefab))
			{
				Transform p = _possiblePrefabs.FirstOrDefault(t => t.name.Equals(prefab.PrefabName));
				if (p == null)
				{
					Debug.LogWarning($"Attempted to grab {_selection.NameAndID()} but no prefab match was found.");
					return;
				}
				_placer.PlacementPrefab = p;
				_placer.SetPlacementRotation(_selection.transform.rotation);
				_controller.ToolMenu.SelectObject((int)ControlModeManager.ControlMode.Placement);
				_controller.ObjectMenu.SelectObject(_possiblePrefabs.ToList().IndexOf(p));  // TODO: This is awful but I'm too annoyed with this to do it right...
				_controller.ModeManager.ActiveMode = ControlModeManager.ControlMode.Placement;

                // TODO: Should we hide the mesh of the target in case of latency in deletion?
                this.photonView.RPC("DeleteSelectedObject", RpcTarget.MasterClient, view.ViewID);
			}
		}
		// ------------------------------------------------------------------------------
		// Removal ----------------------------------------------------------------------
		public void DeleteSelectedObject()
		{
			if (_selection != null && _selection.TryGetComponent(out PhotonView view))
			{
				this.photonView.RPC("DeleteSelectedObject", RpcTarget.MasterClient, view.ViewID);
			}
		}
		[PunRPC]
		private void DeleteSelectedObject(int viewID)
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