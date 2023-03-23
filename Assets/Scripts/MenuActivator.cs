using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Expose properties for the management of a CanvasGroup's visibility to allow editor
     *  functionality (like buttons) to request changing the visibility of mutually exclusive menus
     *  while only needing to know about this menu.
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
	public class MenuActivator : MonoBehaviour
	{
        // Fields =================================================================================
        [SerializeField] private CanvasGroup _canvasGroup;
        public bool StartActive = true;

        private BoolReactiveProperty _triggerAction = new BoolReactiveProperty(false);
        public BoolReactiveProperty Trigger => _triggerAction;
        // ========================================================================================

        // Mono ===================================================================================
        void Awake()
        {
            if (this.StartActive)
                this.Activate();
            else
                this.Deactivate();
        }
        // ========================================================================================

        // Methods ================================================================================
        // Expose menu management trigger to editor for buttons -------------------------
        public void OpenMenu(bool open)
        {
            _triggerAction.Value = open;
        }
        // ------------------------------------------------------------------------------
        // Set visibility of managed menu -----------------------------------------------
        public void Activate(bool makeInteractible = true)
        {
            //Debug.Log($"{(makeInteractible ? "Activating" : "Deactivating")} {_canvasGroup.name}");
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = makeInteractible;
            _canvasGroup.interactable = makeInteractible;
        }
        public void Deactivate()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        // ------------------------------------------------------------------------------
        // State ------------------------------------------------------------------------
        public bool IsActive => _canvasGroup.interactable;
        public void Toggle()
        {
            if (_canvasGroup.interactable)
                this.Deactivate();
            else
                this.Activate();
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}