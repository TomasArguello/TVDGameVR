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
	 *  Handle a queue of requests to apply an Outline to this object so that multiple asynchronous
	 *  requests can be made, but only one outline color is displayed at a time.
	 *  
	 *  TODO: We probably shouldn't need the localColor override...
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PhotonView))]
	public class OutlineManager : MonoBehaviourPun
	{
		// Fields =================================================================================
		//public static Color LocalPlayerColorOverride = Color.red;
//#if UNITY_EDITOR
//		[SerializeField] private Color _localColor = Color.red;
//#endif

		private const int LINE_WIDTH = 5;
		private List<(int, Color)> _outlines = new List<(int, Color)>();
		private Outline _outline;

        // ========================================================================================

        // Mono ===================================================================================
  //      private void Start()
  //      {
		//}
#if UNITY_EDITOR
        private void OnValidate()
        {
			//LocalPlayerColorOverride = _localColor;
		}
#endif
		// ========================================================================================

		// Methods ================================================================================
		public void AddOutline(Color outlineColor, bool prioritize = true)
        {
			UserControlDriver localPlayer = UserControlDriver.LocalPlayerInstance;
			this.AddOutline(localPlayer, outlineColor, prioritize);
			this.photonView.RPC("AddOutlineRpc", RpcTarget.OthersBuffered, localPlayer.photonView.ViewID, false);
        }
		[PunRPC]
		private void AddOutlineRpc(int playerID, bool prioritize)
        {
			PhotonView view = PhotonView.Find(playerID);
			if (view != null && view.TryGetComponent(out UserControlDriver player))
				this.AddOutline(player, player.Color, prioritize);
		}
		private void AddOutline(UserControlDriver player, Color outlineColor, bool prioritize)
		{
			int outlineAt = _outlines.FindIndex(0, (outline) => outline.Item1 == player.photonView.ViewID);
			if (outlineAt > -1)
				_outlines.RemoveAt(outlineAt);

			Color c = player == UserControlDriver.LocalPlayerInstance
				? outlineColor
				: player.Color;
			outlineAt = prioritize ? 0 : _outlines.Count;
			_outlines.Insert(outlineAt, (player.photonView.ViewID, c));

			if (_outline == null)
			{
				_outline = this.gameObject.AddComponent<Outline>();
				_outline.OutlineMode = Outline.Mode.OutlineVisible;
				_outline.OutlineWidth = LINE_WIDTH;
			}
			_outline.OutlineColor = _outlines[0].Item2;
		}
		// ------------------------------------------------------------------------------
		public void RemoveOutline() =>
			this.photonView.RPC("RemoveOutlineRpc", RpcTarget.AllBuffered, UserControlDriver.LocalPlayerInstance.photonView.ViewID);
		public void RemoveOutline(int playerID) =>
			this.photonView.RPC("RemoveOutlineRpc", RpcTarget.AllBuffered, playerID);

		[PunRPC]
		private void RemoveOutlineRpc(int playerID)
		{
			int outlineAt = _outlines.FindIndex(0, (outline) => outline.Item1 == playerID);
			if (outlineAt > -1)
				_outlines.RemoveAt(outlineAt);

			if (_outlines.Count > 0)
			{
				_outline.OutlineColor = _outlines[0].Item2;
			}
			else
            {
				GameObject.Destroy(_outline);
				_outline = null;
            }
		}
		// ------------------------------------------------------------------------------
		public void AddOutline_LocalOnly(Color color)
		{
			if (_outlines.Count > 0)
				_outlines.RemoveAt(0);

			_outlines.Insert(0, (0, color));

			if (_outline == null)
			{
				_outline = this.gameObject.AddComponent<Outline>();
				_outline.OutlineMode = Outline.Mode.OutlineVisible;
				_outline.OutlineWidth = LINE_WIDTH;
			}
			_outline.OutlineColor = _outlines[0].Item2;
		}
		public void RemoveOutline_LocalOnly()
		{
			if (_outlines.Count > 0)
				_outlines.RemoveAt(0);

			if (_outlines.Count > 0)
			{
				_outline.OutlineColor = _outlines[0].Item2;
			}
			else
			{
				GameObject.Destroy(_outline);
				_outline = null;
			}
		}
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}