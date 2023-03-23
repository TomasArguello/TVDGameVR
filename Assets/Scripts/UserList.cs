using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Photon.Pun;
using UnityEngine.Events;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Maintain a list of UserIcon objects (as children of this object) for every
	 *  UserControlDriver provided to track, removing the UserIcon when that player is destroyed.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class UserList : MonoBehaviour
	{
		// Fields =================================================================================
		[SerializeField] private UserIcon _iconPrefab;

		[Header("Events")]
		public UnityEvent<int> AddedIcon = new UnityEvent<int>();
		public UnityEvent<int> RemovedIcon = new UnityEvent<int>();
		// ========================================================================================

		// Methods ================================================================================
		public void AddUser(UserControlDriver player)
        {
			if (this.Icon(player.photonView.ViewID) != null)
				return;

			UserIcon icon = Transform.Instantiate(_iconPrefab);
			icon.transform.SetParent(this.transform);
			icon.Init(player);
			icon.GetComponent<RectTransform>().localScale = Vector3.one;
			this.AddedIcon.Invoke(player.photonView.ViewID);

			player.OnDestroyAsObservable().Subscribe(_ =>
			{
				UserIcon i = this.Icon(player.photonView.ViewID);
				if (i != null)
                {
					GameObject.Destroy(i.gameObject);
					this.RemovedIcon.Invoke(player.photonView.ViewID);
                }
			})
			.AddTo(this);
        }

		public UserIcon Icon(int id)
		{
			for (int i = 0; i < this.transform.childCount; i++)
			{
				if (this.transform.GetChild(i).TryGetComponent(out UserIcon icon) && icon.ID == id)
					return icon;
			}
			return null;
		}
		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}