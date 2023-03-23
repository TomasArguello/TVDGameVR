using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.Events;

#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Toggle visibility of the object selection menu based on the player's state as well as track
     *  which object prefab is currently selected.
     *  
     *  TODO: Marking this for whether the player controls visibility is not good design since the
     *  player could just call Show and Hide directly, but at this point I'm just trying to avoid
     *  breaking the existing system while building out features for the possible rework...
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    [RequireComponent(typeof(MenuActivator))]
    public class SelectionMenu : MonoBehaviour
    {
        // Fields =================================================================================
        private MenuActivator _menuActivator;

        [SerializeField] private bool _playerControlsVisbility = true;

        [SerializeField] private ListSelector _itemSelector;
        [SerializeField] private List<Transform> _items;
        private List<Button> _itemButtons;
        [SerializeField] private List<KeyCode> _selectionKeys;

        [SerializeField] private UnityEvent<Transform> _onSelect;
        public UnityEvent<Transform> OnSelect => _onSelect;
        public int SelectedIndex => _itemSelector.SelectedIndex;

        // ========================================================================================

        // Mono ===================================================================================
        private void Awake()
        {
            _menuActivator = this.GetComponent<MenuActivator>();
            _itemSelector.Selection.Subscribe(i =>
            {
                if (i > -1 && i < _items.Count)
                    _onSelect.Invoke(_items[i]);
                else
                    _onSelect.Invoke(null);
            })
            .AddTo(this);

            _itemButtons = _itemSelector.Items.Select(i => i.GetComponent<Button>()).ToList();
        }
        // ------------------------------------------------------------------------------
        private void Update()
        {
            for (int i = 0; i < _selectionKeys.Count() && i < _itemSelector.Count; i++)
            {
                if (Input.GetKeyDown(_selectionKeys[i]) && _itemButtons[i] != null)
                {
                    _itemButtons[i].onClick.Invoke();
                    break;
                }
            }
        }
        // ========================================================================================

        // Methods ================================================================================
        public void Show() =>  _menuActivator.Activate();
        public void Hide() => _menuActivator.Deactivate();

        public void PlayerShow()
        {
            if (_playerControlsVisbility)
                this.Show();
        }
        public void PlayerHide()
        {
            if (_playerControlsVisbility)
                this.Hide();
        }

        public void SelectObject(int index)
        {
            if (index > -1 && index < _selectionKeys.Count
                && _itemSelector.Items[index].TryGetComponent(out Button b))
            {
                b.onClick.Invoke();
            }
        }
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}