using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Manage the location, orientation, and visibility of a set of particle emitters that
	 *  can be used to draw attention to a specified point.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class ContactIndicator : MonoBehaviour
	{
		// Fields =================================================================================
		//private MeshRenderer[] _renderers;
		private ParticleSystem[] _particles;
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
			//_renderers = this.GetComponentsInChildren<MeshRenderer>();
			_particles = this.GetComponentsInChildren<ParticleSystem>();
		}
		// ========================================================================================

		// Methods ================================================================================
		public bool IsVisible => _particles.Length > 0 && _particles[0].isEmitting;

		public ContactIndicator Show(bool show)
        {
			if (show == this.IsVisible)
				return this;

			//foreach (MeshRenderer r in _renderers)
			//	r.enabled = show;
			//Debug.Log("Showing: " + show);

			if (show)
				foreach (ParticleSystem p in _particles)
                {
					p.Simulate(5, false, true);
					p.Play(false);
                }
			else
				foreach (ParticleSystem p in _particles)
					p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            return this;
		}
		public ContactIndicator MoveTo(Vector3 point)
		{
			this.transform.position = point;
			return this;
		}
		public ContactIndicator OrientTo(Vector3 normal)
		{
			this.transform.rotation = Quaternion.LookRotation(normal);
			return this;
		}
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}