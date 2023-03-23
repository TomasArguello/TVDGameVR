using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Handle local disabling/re-enabling of snapping points overlapped by a tape object when one
	 *  is instantiated by Photon.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(SphereCollider))]
	public class PunTapeHandler : MonoBehaviour, IPunInstantiateMagicCallback
	{
		// Fields =================================================================================
		public LayerMask SnappingMask;
		public LayerMask TapableObjectMask;

		public static int MinTapeConnections = 1;

		private SphereCollider _boundingSphere;
		// ========================================================================================

		// Methods ================================================================================
		public float TapeSize => this.transform.lossyScale.x * _boundingSphere.radius;   // TODO: find a better way

		public static bool IsTape(GameObject gameObject)
		{
			return gameObject.GetComponent<PunTapeHandler>() != null;
		}

		private void Awake() => _boundingSphere = this.GetComponent<SphereCollider>();

        // Disable overlapped snap points, but re-enable them if this dies --------------
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
			bool snapped = info.photonView != null && info.photonView.InstantiationData != null
				&& info.photonView.InstantiationData.Length > 0 && (bool)info.photonView.InstantiationData[0];
			if (!snapped)
				return;

			_boundingSphere = this.GetComponent<SphereCollider>();

			Collider[] overlappedSnapPoints =
				Physics.OverlapSphere(
					this.transform.position,
					this.TapeSize,
					this.SnappingMask
				)
				.Where(t => !t.gameObject.CompareTag("Structural"))
				.ToArray();

			for (int s = 0; s < overlappedSnapPoints.Length; s++)
			{
				Collider c = overlappedSnapPoints[s];
				c.enabled = false;
				this.OnDestroyAsObservable()
					.Subscribe(_ => c.enabled = true)
					.AddTo(c);

				// TODO: should probably account for the possibility of
				// multiple tape instances overlapping the same snap point
			}
		}
		// ------------------------------------------------------------------------------
		// Find all valid objects overlapped by this tape -------------------------------
		public Collider[] GetTapedObjects(LayerMask objectLayers)
        {
			Collider[] tapedObjects = Physics.OverlapSphere(
					this.transform.position,
					this.TapeSize,
					objectLayers
					)
					.Where(t => t.gameObject != this.gameObject)
					.ToArray();

			return tapedObjects;
		}
		public Collider[] GetTapedObjects() => this.GetTapedObjects(this.TapableObjectMask);

		public bool Overlaps(Vector3 checkPosition)
        {
			return (checkPosition - this.transform.position).magnitude <= this.TapeSize;
        }
		// ------------------------------------------------------------------------------

		public static Vector3[] UntapedLocations(Vector3[] positions)
        {
			List<Vector3> untapedPoints = positions.ToList();
			PunTapeHandler[] tapes = GameObject.FindObjectsOfType<PunTapeHandler>();// Or, all rigidbodies on the SnappingLayer
			for (int i = 0; i < tapes.Length && untapedPoints.Count > 0; i++)
            {
				for (int j = untapedPoints.Count - 1; j >= 0; j--)
                {
					if (tapes[i].Overlaps(untapedPoints[j]))
						untapedPoints.RemoveAt(j);
                }
            }

			return untapedPoints.ToArray();
        }

		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}