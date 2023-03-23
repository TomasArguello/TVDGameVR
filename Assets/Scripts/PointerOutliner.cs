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
	 *  Outline the object currently under the cursor.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class PointerOutliner : MonoBehaviour
	{
		// Fields =================================================================================
		public LayerMask TargetableObjects;
		[SerializeField] private Camera _camera;

		private bool _isActive = false;

		[SerializeField] private OutlineManager _target;
		public OutlineManager Target => _target;

		public Color OutlineColor;

		private List<OutlineManager> _ignoreList = new List<OutlineManager>();
		// ========================================================================================

		// Mono ===================================================================================
		void Update()
		{
			if (!_isActive) return;

			Ray ray = _camera.ScreenPointToRay(Input.mousePosition + Vector3.forward * _camera.nearClipPlane);
			OutlineManager target = Physics.Raycast(ray, out RaycastHit hit, 100f, this.TargetableObjects)
				? hit.transform.GetComponent<OutlineManager>()
				: null;

			this.Highlight(target);
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
				if (!_isActive && _target != null)
					_target.RemoveOutline();
			}
		}
		// ------------------------------------------------------------------------------
		// Activation -------------------------------------------------------------------
		private void Highlight(OutlineManager target)
		{
			if (_target != null && target != _target)
			{
				if (!_ignoreList.Contains(_target))
					_target.RemoveOutline();
				_target = null;
			}
			_target = target;

			if (_target != null && !_ignoreList.Contains(_target))
				_target.AddOutline(this.OutlineColor);
		}
		// ------------------------------------------------------------------------------
		// Ignore Objects ---------------------------------------------------------------
		public void Ignore(OutlineManager om)
        {
			if (!_ignoreList.Contains(om))
				_ignoreList.Add(om);
        }
		public void Unignore(OutlineManager om)
        {
			if (_ignoreList.Contains(om))
				_ignoreList.Remove(om);
        }
		// ------------------------------------------------------------------------------
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}