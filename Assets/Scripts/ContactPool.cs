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
	 *  Manage a set of ContactIndicator objects so that objects can be requested without creating
	 *  and destroying them anew every time they are brought in and out of use.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class ContactPool : MonoBehaviour
	{
		// Fields =================================================================================
		[SerializeField] private ContactIndicator _prefab;

		private List<ContactIndicator> _inactive = new List<ContactIndicator>();
		private List<ContactIndicator> _active = new List<ContactIndicator>();

		// ========================================================================================

		// Methods ================================================================================
		/// <summary>
		/// Get a new ContactIndicator from the pool.
		/// </summary>
		public ContactIndicator Next
        {
			get
			{
				ContactIndicator c = _inactive.RemoveGrabAt(0);
				if (c == null)
					c = GameObject.Instantiate(_prefab);
				
				_active.Add(c);
				c.Show(true);
				return c;
			}
        }

		/// <summary>
		/// Deactivate all ContactIndicators in the pool.
		/// </summary>
		public void Clear()
        {
			while (_active.Count > 0)
            {
				ContactIndicator c = _active.RemoveGrabAt(0);
				_inactive.Add(c);
				c.Show(false);
            }
		}

		/// <summary>
		/// Deactivate a specific ContactIndicator in the pool.
		/// </summary>
		public void Clear(ContactIndicator indicator)
		{
			if (_active.Remove(indicator))
            {
				_inactive.Add(indicator);
				indicator.Show(false);
            }
		}

		/// <summary>
		/// Set the pool's count of active ContactIndicators to N and get them.
		/// </summary>
		/// <param name="howMany">The number of ContactIndicators to get.</param>
		/// <returns>The set of active ContactIndicators.</returns>
		public ContactIndicator[] Current(int howMany)
        {
			ContactIndicator[] points = new ContactIndicator[howMany];
			int i = 0;
			while (i < points.Length && _active.Count > 0)
            {
				points[i++] = _active.RemoveGrabAt(0).Show(true);
            }
			while (i < points.Length)
            {
				points[i++] = this.Next;
			}

			this.Clear();
			for (int j = 0; j < points.Length; j++)
				_active.Add(points[j]);

			return points;
        }

		/// <summary>
		/// Set the pool's count of active ContactIndicators to 1 and get it.
		/// </summary>
		public ContactIndicator First => this.Current(1)[0];

		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}