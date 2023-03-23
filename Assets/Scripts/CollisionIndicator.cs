using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Manage a ContactPool according to collision with other objects so that one ContactIndicator
	 *  is active for each object being collided with.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class CollisionIndicator : MonoBehaviour
	{
		// Fields =================================================================================
		public ContactPool ContactPool;
		public LayerMask Filter = int.MinValue;

		private Dictionary<Collider, ContactIndicator> _indicators = new Dictionary<Collider, ContactIndicator>();
		private Dictionary<Collider, IDisposable> _listeners = new Dictionary<Collider, IDisposable>();
        // ========================================================================================

        // Mono ===================================================================================
        private void Start()
        {
			if (this.ContactPool == null)
				this.ContactPool = this.gameObject.AddComponent<ContactPool>();
		}
        private void OnDestroy()
        {
			_listeners.Values.ToList().ForEach(v => v.Dispose());
        }
        // ========================================================================================

        // Collision ==============================================================================
        private void OnCollisionEnter(Collision collision)
		{
			if (((1 << collision.gameObject.layer) | Filter) == Filter
				&& !_indicators.ContainsKey(collision.collider))
            {
				Collider c = collision.collider;
				_indicators.Add(c, this.ContactPool.Next);
				
				if (!_listeners.ContainsKey(c))
                {
					_listeners.Add(c,
						collision.collider.OnDestroyAsObservable()
							.Subscribe(_ =>
							{
								if (_indicators.ContainsKey(c))
									_indicators.Remove(c);
							})
						);
                }
            }
		}
        private void OnCollisionStay(Collision collision)
        {
			if (((1 << collision.gameObject.layer) | Filter) == Filter
				&& _indicators.ContainsKey(collision.collider) && collision.contactCount > 0)
            {
				_indicators[collision.collider]
					.MoveTo(collision.contacts[0].point)
					.OrientTo(collision.contacts[0].normal);
            }
        }
        private void OnCollisionExit(Collision collision)
		{
			if (((1 << collision.gameObject.layer) | Filter) == Filter
				&& _indicators.ContainsKey(collision.collider))
            {
				this.ContactPool.Clear(_indicators[collision.collider]);
				_indicators.Remove(collision.collider);

				// I think it might be more performant not to clear the event listeners given how
				// often a PlacementCaster's preview object could enter and exit collision with the
				// same scene object. It's likely the preview object will have a shorter lifetime
				// than the scene objects will anyway, so it shouldn't be too burdensome to hold on
				// a few extra IDisposables until it dies.
				//if (_listeners.ContainsKey(collision.collider))
				//{
				//	_listeners[collision.collider].Dispose();
				//	_listeners.Remove(collision.collider);
				//}
            }
		}
		// ========================================================================================

		// Collision ==============================================================================
		public int CollisionCount => _indicators.Count;
		public Vector3[] CollisionPoints => _indicators.Values.Select(ci => ci.transform.position).ToArray();
		public Vector3[] FilteredCollisionPoints(LayerMask mask, bool excludeTapeContact = false)
        {
			return _indicators
				.Where(pair => ((1 << pair.Key.gameObject.layer) | mask) == mask)
				.Where(pair => !excludeTapeContact || !PunTapeHandler.IsTape(pair.Key.gameObject))
				.Select(pair => pair.Value.transform.position)
				.ToArray();
        }
		// ========================================================================================
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}