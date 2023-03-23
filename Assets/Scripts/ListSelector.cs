using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd {
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
	 *  Move an indicator Image to the most recently selected item in a list of GameObjects.
	 *  
	 *  FUTURE: Add options for location offset, tweening, etc.
	 */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    public class ListSelector : MonoBehaviour {
        // Fields & Properties ====================================================================
        [SerializeField] private Image _indicator;

        //[SerializeField] private bool _selectFirstOnStartup = false;
        [SerializeField] private int _selectedIndexOnStartup = -1;
        [SerializeField] private int _frameDelayOnStartup = 1;


        [SerializeField] private IntReactiveProperty _selected = new IntReactiveProperty(-1);
        public IntReactiveProperty Selection => _selected;
        public int SelectedIndex => _selected.Value;

        private int _lastSelected = -1;
        public int LastSelectionIndex => _lastSelected;


        [SerializeField] private List<GameObject> _objects = new List<GameObject>();
        public List<GameObject> Items => _objects.ToList();
        public int Count => _objects.Count();
        public GameObject SelectedObject =>
            _selected.Value > -1 && _selected.Value < _objects.Count()
            ? _objects[_selected.Value]
            : null;


        [SerializeField] private UnityEvent<Transform> _onSelect = new UnityEvent<Transform>();
        public UnityEvent<Transform> OnSelect => _onSelect;

        // ========================================================================================

        // Mono ===================================================================================
        private async void Start() {
            _indicator.enabled = false;

            for (int i = 0; i < _frameDelayOnStartup; i++)
                await Cysharp.Threading.Tasks.UniTask.NextFrame();

            if (_selectedIndexOnStartup > -1 && _objects.All(o => o != null))
                this.Select(_objects[_selectedIndexOnStartup]);
        }
        // ------------------------------------------------------------------------------
        private void OnValidate() {
            _selectedIndexOnStartup = Mathf.Clamp(_selectedIndexOnStartup, -1, _objects.Count - 1);
            _frameDelayOnStartup = Math.Abs(_frameDelayOnStartup);
        }
        // ========================================================================================

        // Methods ================================================================================

        public void Select(GameObject g) {
            _lastSelected = _selected.Value;

            if (_selected.Value == _objects.IndexOf(g)) {
                _indicator.enabled = false;
                _onSelect.Invoke(null);
                return;
            }

            _selected.Value = _objects.IndexOf(g);

            if (_selected.Value > -1) {
                _indicator.enabled = true;
                _indicator.transform.position = g.transform.position;
                _onSelect.Invoke(g.transform);
                return;
            }

            _indicator.enabled = false;
            _onSelect.Invoke(null);
        }

        public GameObject Select(int index) {
            _lastSelected = _selected.Value;
            if (index < 0 || index >= _objects.Count) {
                _selected.Value = -1;
                _indicator.enabled = false;
                _onSelect.Invoke(null);
                return null;
            }

            _selected.Value = index;
            _indicator.enabled = true;
            _indicator.transform.position = _objects[index].transform.position;
            _onSelect.Invoke(_objects[index].transform);
            return _objects[index];
        }

        public bool IsCurrentSelectionSameAsLast => _selected.Value == _lastSelected;
        public Transform LastSelectedObject => _lastSelected > -1 ? _objects[_lastSelected].transform : null;
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================

#if UNITY_EDITOR
    [CustomEditor(typeof(ListSelector))]
    public class ListSelectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ListSelector listSelect = (ListSelector)target;
            EditorGUILayout.HelpBox("The button below selects room 1", MessageType.Info);
            if (GUILayout.Button("Select Room 1"))
            {
                listSelect.Select(0);
            }
        }
    }
#endif
}