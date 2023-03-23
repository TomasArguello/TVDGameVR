using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Wrap buttons used to trigger actions in a SelectorCaster in a class so that the component
	 *  can be injected into the local player when it spawns.
	 *  
	 *  NOTE: I know this is dumb, but this is the simplest way to do hook this up when the player
	 *  can only talk to the UI through external injection.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class SelectorActions : MonoBehaviour
	{
		// Fields =================================================================================
		[SerializeField] private MenuActivator _activator;

		[Header("Buttons")]
		[SerializeField] private Button _grab;
		public Button GrabButton => _grab;
		public KeyCode GrabKey = KeyCode.G;

		[SerializeField] private Button _delete;
		public Button RemoveButton => _delete;
		public KeyCode DeleteKey = KeyCode.Delete;
        // ========================================================================================

        // Mono ===================================================================================
        void Update()
        {
			if (_grab.interactable && Input.GetKeyDown(this.GrabKey))
				_grab.onClick.Invoke();
			else if (_delete.interactable && Input.GetKeyDown(this.DeleteKey))
				_delete.onClick.Invoke();
        }
        // ========================================================================================

        // Methods ================================================================================
        public void Hide() => _activator.Deactivate();
		public void Show(bool enabled = true)
		{
			_grab.interactable = enabled;
			_delete.interactable = enabled;
			_activator.Activate(enabled);
		}
		// ========================================================================================
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}